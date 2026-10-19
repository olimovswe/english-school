# English School: web app

The student web app for the English School API: sign-in with Telegram or email, the daily plan (morning
review gate, today's steps, progress and streak), and practice sessions with instant grading feedback.

**Stack:** Next.js 15 (App Router) · TypeScript · Tailwind CSS 4 · shadcn/ui (Radix) · Lucide · TanStack
Query · ky · zustand · Vitest + Testing Library.

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
later, the stats show the day's 33 words. **Mashqlar** then plays the day's exercises (four on day 1).

| Command | What it does |
|---|---|
| `npm run dev` | Development server with Turbopack on port 3000 |
| `npm run build` | Production build with webpack, which also lints and type-checks |
| `npm start` | Serves the production build |
| `npm run lint` | ESLint; any warning fails |
| `npm run typecheck` | `tsc --noEmit` |
| `npm test` | Vitest: unit tests (node) and component tests (jsdom, Testing Library) |

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

## Practice

`/practice` plays the exercises of today's course day: the day comes from `GET /discipline/today`, the
exercises from `GET /practice/days/{n}/exercises`, ordered by position. Each answer goes to
`POST /practice/attempts` and is graded right away.

| Backend type | Card | Interaction |
|---|---|---|
| `GapFill` | `FillInTheBlankCard` | The `___` in the prompt fills in as the student types. Empty answers are caught on submit, and Cyrillic letters (a Russian keyboard layout) are flagged while typing and never sent. |
| `MultipleChoice` | `MultipleChoiceCard` | Native radio buttons: arrow keys or Space to choose, Enter to submit. |
| `Matching` | `MatchingCard` | Pick a word, then its translation, from either side; clicking a pair again undoes it. Numbered badges show the pairs, and focus moves to the next open word. |

The backend has no word-order (sentence reordering) exercise type, so there is no card for one.

**Flow.** Answer → feedback → **Keyingi mashq** → … → summary. The feedback banner shows:
- the verdict and the score (0–100 %);
- the error class, with Uzbek labels for the backend's error codes;
- the correct answer and the rule;
- which tier graded the answer.

After grading, the card turns read-only and marks right and wrong choices or pairs. The summary shows:
- the average score and the correct / partial / incorrect counts;
- the error classes met, most frequent first;
- each exercise's result;
- **Qayta ishlash** to restart the set.

A failed submission keeps the answer and shows why. Pressing Enter again while an attempt is being
graded sends nothing.

**Keyboard.** Every step works without a mouse. Each new exercise focuses its first control, Enter
submits, and after grading focus moves to **Keyingi mashq**, so Enter continues. When the summary
appears, focus moves to its heading.

The session logic is pure and tested on its own:
- [`session.ts`](src/modules/practice/session.ts): phases, per-exercise `timeTakenMs` clamped to the
  API's 0–1 h, and the summary;
- [`matching.ts`](src/modules/practice/matching.ts): the pairing rules.

Completing a practice set does not yet mark the plan's practice step as done; see the Discipline "known
gap" in the root README.

## Project layout

```text
src/
├── app/                       routes (App Router)
│   ├── (app)/                 signed-in pages; layout = RequireSession + AppShell
│   │   ├── dashboard/         the daily plan
│   │   ├── practice/          today's exercise session
│   │   └── flashcards/ speech/ analytics/   placeholders
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
    ├── dashboard/             dashboard widgets
    └── practice/              exercise cards, feedback, session and summary; session.ts, matching.ts
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
npm test                         # Vitest: unit and component tests
npm run lint && npm run typecheck && npm run build
```

Tests run in node by default. A component test opts into jsdom with `// @vitest-environment jsdom` on
its first line and uses Testing Library with the jest-dom matchers ([`src/test/setup.ts`](src/test/setup.ts)).

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

The practice module has unit and component tests:
- [`session.test.ts`](src/modules/practice/session.test.ts) and
  [`matching.test.ts`](src/modules/practice/matching.test.ts): the session phases and the pairing rules.
- [`exercise-cards.test.tsx`](src/modules/practice/components/exercise-cards.test.tsx): each card's
  validation, keyboard use and graded marks, and the feedback banner.
- [`practice-session.test.tsx`](src/modules/practice/components/practice-session.test.tsx): the submit
  workflow, with only `practiceApi` mocked:
  - a keyboard-only run through all three exercise types to the summary;
  - a restart;
  - a failed submission retried;
  - no double submit while an attempt is being graded.

There are no browser end-to-end tests in the repository yet.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| Every request fails with "Serverda xatolik yuz berdi", and the dev terminal shows `Failed to proxy … ECONNREFUSED` | The API isn't running, or `API_PROXY_TARGET` points to the wrong port. Start `src/Host.Api`, or set the target and restart `npm run dev`. |
| The Telegram button shows "Bot domain invalid" | Telegram allows the widget only on the domain set with `/setdomain` in @BotFather, never on localhost. Use email or, in `next dev`, token sign-in. |
| The dev server answers 500 with `ENOENT … _buildManifest.js.tmp` | `npm run build` ran while `npm run dev` was running; in Next.js 15 both use `.next`. Stop the dev server, `rm -rf .next`, start it again. |
| Words, streak or completed days stay at 0 after the gate opens | These read models are built from events by the Worker. Start `src/Host.Worker`; the numbers appear a few seconds later. |
| A new `API_PROXY_TARGET` has no effect on `npm start` | Rewrites are fixed at build time. Rebuild with the new value. |
