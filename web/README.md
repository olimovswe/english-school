# English School: web app

The student web app for the English School API: sign-in with Telegram or email, and the daily plan with
the morning review gate, today's steps, progress and streak.

**Stack:** Next.js 15 (App Router) · TypeScript · Tailwind CSS 4 · shadcn/ui (Radix) · Lucide · TanStack
Query · ky · zustand · Vitest.

The UI is in Uzbek (Latin script). All data comes from the .NET API in this repository; see the
[root README](../README.md) for the backend.

## Quick start

Requires **Node.js 20.9 or later**; 24 LTS is recommended (see [`.nvmrc`](.nvmrc)). The API, the Worker,
PostgreSQL and Valkey must be running; [Running everything together](../README.md#running-everything-together)
shows how.

```bash
cd web
npm ci
cp .env.example .env.local   # optional; the defaults work with `dotnet run`
npm run dev                  # http://localhost:3000
```

Open http://localhost:3000. You are sent to the sign-in page; choose **Roʻyxatdan oʻting**, register with
an email and a password of 8–128 characters, and you land on the daily plan. On day 1 no cards are due,
so **Darvozani ochish** opens the gate at once. When the Worker has delivered the events a few seconds
later, the stats show the day's 33 words.

| Command | What it does |
|---|---|
| `npm run dev` | Development server with Turbopack on port 3000 |
| `npm run build` | Production build with webpack, which also lints and type-checks |
| `npm start` | Serves the production build |
| `npm run lint` | ESLint; any warning fails |
| `npm run typecheck` | `tsc --noEmit` |
| `npm test` | Vitest unit tests |

## Configuration

Copy [`.env.example`](.env.example) to `.env.local` (git ignores it). Every variable is optional.

| Variable | Default | Takes effect | Purpose |
|---|---|---|---|
| `API_PROXY_TARGET` | `http://localhost:5080` | `next dev` start; `next build` for production | Where `/api/v1/*` is forwarded. `dotnet run`: 5080; `docker compose --profile app`: 8080. |
| `NEXT_PUBLIC_API_BASE_URL` | `/api/v1` | build | The base URL the browser calls. An absolute URL bypasses the proxy, so that origin must be in the API's `Cors:AllowedOrigins`. |
| `NEXT_PUBLIC_TELEGRAM_BOT_USERNAME` | empty | build | The Login Widget's bot, without `@`. Empty hides the Telegram button. |

`NEXT_PUBLIC_*` values are compiled into the JavaScript bundle. Never put secrets in them; the Telegram
bot **token** belongs to the API only (`TELEGRAM_BOT_TOKEN`).

## How it talks to the API

The browser only ever calls its own origin: Next.js rewrites `/api/v1/*` to `API_PROXY_TARGET`
([`next.config.ts`](next.config.ts)), so the API needs no CORS entry for the web app.
[`src/lib/api/client.ts`](src/lib/api/client.ts) exports two [ky](https://github.com/sindresorhus/ky)
instances:

- **`api`**: adds `Authorization: Bearer <access token>` and handles token refresh (see below).
- **`publicApi`**: sends no token. Used for sign-in, registration, Telegram, refresh, logout and the
  public curriculum.

The API returns errors as ProblemDetails with Uzbek `detail` messages.
[`src/lib/api/errors.ts`](src/lib/api/errors.ts) turns any error into a message for the user, and
validation errors into messages under the matching form fields. Network failures, timeouts, 429 and
5xx get their own wording.

TanStack Query owns all server state: queries are fresh for 30 seconds, and are retried twice on 5xx
and network errors but never on 4xx. Mutations are not retried.

## Authentication

**Sign-in methods** ([`src/modules/auth`](src/modules/auth)):

- **Telegram Login Widget.** The widget's signed user object is sent unchanged to
  `POST /identity/telegram`; the API checks the signature with the bot token. In @BotFather, set the
  site's domain with `/setdomain`. The widget refuses other domains, localhost included ("Bot domain
  invalid").
- **Email and password**, with registration. The browser's time zone is sent as `timeZone`.
- **Token sign-in, `next dev` only**: paste the JSON from `POST /identity/login` or a bare access token.
  Useful when Telegram can't run locally. A bare token cannot be refreshed, so its session ends when the
  token expires.

**Session.** The tokens live in `localStorage` (`english-school.session`), so a session survives
reloads and is shared by all tabs. Signing out in one tab signs out the others. The trade-off: any
script running on the page can read that storage, so an XSS bug would expose the tokens. What limits
the damage:
- access tokens last 15 minutes;
- refresh tokens rotate, and the API revokes the whole session when an old one is reused;
- the only third-party script is Telegram's widget, on the sign-in page.

Moving the tokens into httpOnly cookies behind a Next.js backend-for-frontend would remove the exposure.

**Refresh.** An access token that expires within 30 seconds is refreshed before the request is sent. A
401 triggers one refresh and one retry of the original request, body included. The API revokes the
whole session when an already-rotated refresh token is presented, so refreshes never overlap:
- within a tab, all requests share one in-flight refresh;
- across tabs, a Web Lock serializes refreshes;
- inside the lock, the session is re-read from `localStorage` first, so a token another tab has just
  rotated is reused rather than redeemed again.

A rejected refresh token ends the session and the sign-in page says so. A network failure or a 5xx
keeps the session.

**Protected pages.** The tokens exist only in the browser, so the server renders every page without a
session. `RequireSession` (the `(app)` layout) redirects to `/login?next=…` once the stored session has
loaded, and `next` only accepts paths on this site. The API enforces authorization on every request.

## Project layout

```text
src/
├── app/                       routes (App Router)
│   ├── (app)/                 signed-in pages; layout = RequireSession + AppShell
│   │   ├── dashboard/         the daily plan
│   │   └── practice/ flashcards/ speech/ analytics/   placeholders
│   ├── login/                 sign-in page
│   ├── layout.tsx             fonts, <html lang="uz">, providers
│   ├── providers.tsx          TanStack Query, theme, toaster, session sync
│   └── page.tsx               redirects to /dashboard
├── components/
│   ├── ui/                    shadcn/ui primitives (generated)
│   ├── layout/                app shell: sidebar, mobile sheet, account panel, theme toggle
│   └── coming-soon.tsx  page-spinner.tsx  query-error.tsx
├── lib/
│   ├── api/                   client.ts (tokens, refresh), errors.ts (ProblemDetails)
│   └── env.ts  format.ts  query-client.ts  utils.ts (cn)
└── modules/                   one folder per backend module
    ├── auth/                  session store, sign-in flows, guard, sign-out
    ├── discipline/  retention/  analytics/  curriculum/
    └── dashboard/             dashboard widgets
```

A module folder holds:
- `api.ts`: response types and HTTP calls, typed against the backend DTOs;
- `hooks.ts`: query keys and TanStack Query hooks;
- `components/`, when the module has its own UI.

The backend's module names are kept, so a frontend module maps to one API area.

## Conventions

- **Language:** UI text in Uzbek, using `ʻ` (U+02BB) for oʻ and gʻ and `ʼ` (U+02BC) for the tutuq belgisi,
  never `'` or `‘`. Code comments are in Uzbek, like the backend's.
- **Formatting:** dates go through `formatCalendarDate` and percentages through `formatPercent`
  ([`src/lib/format.ts`](src/lib/format.ts)), not `Intl`: Chrome has no Uzbek locale data and prints
  "M10 19, Mon".
- **shadcn/ui components:** add them with `npx shadcn add <name>`. The CLI imports `cn` from its own `cn`
  package and adds that package to `package.json`. This project's `cn()` is `clsx` + `tailwind-merge`
  in `@/lib/utils`, so after adding a component:
  - change the import to `@/lib/utils`; ESLint rejects `"cn"`;
  - run `npm uninstall cn`;
  - translate any visible English text (`Close` → `Yopish`).
- **Zero warnings:** `npm run lint` and `npm run build` must pass with no warnings.

## Testing

```bash
npm test                         # Vitest: API client, query retries, formatting
npm run lint && npm run typecheck && npm run build
```

[`client.test.ts`](src/lib/api/client.test.ts) runs the API client against a fake Identity service that
rotates refresh tokens and rejects reused ones, like the real one. It covers:
- token injection;
- refresh and retry with the request body;
- one refresh shared by concurrent requests;
- reusing a token another tab already rotated;
- refresh ahead of expiry;
- a rejected refresh token;
- network failures;
- public endpoints, which never carry a token.

There are no browser end-to-end tests in the repository yet.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| Every request fails with "Serverda xatolik yuz berdi", and the dev terminal shows `Failed to proxy … ECONNREFUSED` | The API isn't running, or `API_PROXY_TARGET` points to the wrong port. Start `src/Host.Api`, or set the target and restart `npm run dev`. |
| The Telegram button shows "Bot domain invalid" | Telegram allows the widget only on the domain set with `/setdomain` in @BotFather, never on localhost. Use email or, in `next dev`, token sign-in. |
| The dev server answers 500 with `ENOENT … _buildManifest.js.tmp` | `npm run build` ran while `npm run dev` was running; in Next.js 15 both use `.next`. Stop the dev server, `rm -rf .next`, start it again. |
| Words, streak or completed days stay at 0 after the gate opens | These read models are built from events by the Worker. Start `src/Host.Worker`; the numbers appear a few seconds later. |
| A new `API_PROXY_TARGET` has no effect on `npm start` | Rewrites are fixed at build time. Rebuild with the new value. |
