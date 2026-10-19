import ky, { isHTTPError, type KyInstance } from "ky";

import { env } from "@/lib/env";
import { toSession, type AuthTokens, type Session } from "@/modules/auth/session";
import { sessionStore, type SessionStore } from "@/modules/auth/session-store";

/** Access token tugashiga shuncha qolganda soʻrovdan oldin yangilanadi (soat farqi va tarmoq kechikishi uchun). */
const REFRESH_AHEAD_MS = 30_000;
/** Bitta brauzerdagi barcha tablar uchun umumiy Web Lock nomi. */
const REFRESH_LOCK = "english-school:token-refresh";

export interface ApiClientOptions {
  /** `/api/v1` yoki toʻliq manzil; soʻrov yoʻllari unga qoʻshiladi (`identity/me`). */
  prefix: string;
  store: SessionStore;
  /** Testlar uchun; odatda brauzerning fetch'i. */
  fetch?: typeof globalThis.fetch;
}

export interface ApiClient {
  /** Bearer token bilan. 401 boʻlsa sessiyani yangilab, soʻrovni bir marta takrorlaydi. */
  api: KyInstance;
  /** Tokensiz: login, roʻyxatdan oʻtish, Telegram, refresh, logout. */
  publicApi: KyInstance;
}

function expiresSoon(session: Session): boolean {
  return session.accessTokenExpiresAt - Date.now() <= REFRESH_AHEAD_MS;
}

function bearerTokenOf(request: Request): string | null {
  return /^Bearer (.+)$/.exec(request.headers.get("authorization") ?? "")?.[1] ?? null;
}

/** Web Locks: tablar refresh'ni navbat bilan qiladi. Qulf yoʻq muhitda (eski brauzer, test) — toʻgʻridan-toʻgʻri. */
function withRefreshLock<T>(task: () => Promise<T>): Promise<T> {
  const locks = typeof navigator === "undefined" ? undefined : navigator.locks;
  return locks ? (locks.request(REFRESH_LOCK, task) as Promise<T>) : task();
}

/**
 * Identity refresh token'larni har safar almashtiradi va eskisi qayta kelsa butun sessiyani bekor qiladi
 * (reuse detection). Shuning uchun refresh hech qachon parallel ketmaydi:
 * - bitta tab ichida barcha soʻrovlar bitta refresh'ni kutadi;
 * - tablar orasida Web Lock, qulf ichida esa eng soʻnggi sessiya localStorage'dan qayta oʻqiladi:
 *   boshqa tab tokenni allaqachon almashtirgan boʻlsa, yangi token ishlatiladi va refresh qilinmaydi.
 */
export function createApiClient({ prefix, store, fetch }: ApiClientOptions): ApiClient {
  const publicApi = ky.create({ prefix, fetch, timeout: 15_000, retry: 0 });

  let pendingRefresh: Promise<string | null> | null = null;

  async function rotate(rejectedToken: string): Promise<string | null> {
    await store.persist?.rehydrate();
    const session = store.getState().session;
    if (!session) {
      return null;
    }

    if (session.accessToken !== rejectedToken && !expiresSoon(session)) {
      return session.accessToken;
    }

    if (!session.refreshToken || (session.refreshTokenExpiresAt !== null && session.refreshTokenExpiresAt <= Date.now())) {
      store.getState().expire();
      return null;
    }

    try {
      const tokens = await publicApi
        .post("identity/refresh", { json: { refreshToken: session.refreshToken } })
        .json<AuthTokens>();
      store.getState().signIn(toSession(tokens));
      return tokens.accessToken;
    } catch (error) {
      if (isHTTPError(error) && error.response.status === 401) {
        store.getState().expire();
        return null;
      }

      // Tarmoq, 429 yoki 5xx: sessiya saqlanadi, keyingi soʻrov yana urinadi.
      throw error;
    }
  }

  /** Yangi access token; sessiya tugagan boʻlsa null. */
  function refreshSession(rejectedToken: string): Promise<string | null> {
    pendingRefresh ??= withRefreshLock(() => rotate(rejectedToken)).finally(() => {
      pendingRefresh = null;
    });
    return pendingRefresh;
  }

  async function currentAccessToken(): Promise<string | null> {
    const session = store.getState().session;
    if (!session || !expiresSoon(session)) {
      return session?.accessToken ?? null;
    }

    try {
      return await refreshSession(session.accessToken);
    } catch {
      // Yangilab boʻlmadi (tarmoq): eski token hali bir necha soniya yaroqli boʻlishi mumkin.
      return session.accessToken;
    }
  }

  const api = publicApi.extend({
    // Avtomatik qayta urinishlar TanStack Query'da; bu yerda faqat token yangilangandan keyingi bitta takror.
    retry: { limit: 1, shouldRetry: () => false },
    hooks: {
      beforeRequest: [
        async ({ request }) => {
          if (request.headers.has("authorization")) {
            return;
          }

          const token = await currentAccessToken();
          if (token) {
            request.headers.set("authorization", `Bearer ${token}`);
          }
        },
      ],
      afterResponse: [
        async ({ request, response, retryCount }) => {
          const rejectedToken = bearerTokenOf(request);
          if (response.status !== 401 || retryCount > 0 || !rejectedToken) {
            return;
          }

          const token = await refreshSession(rejectedToken).catch(() => null);
          if (!token) {
            return;
          }

          const headers = new Headers(request.headers);
          headers.set("authorization", `Bearer ${token}`);
          return ky.retry({ request: new Request(request, { headers }), delay: 0, code: "TOKEN_REFRESHED" });
        },
      ],
    },
  });

  return { api, publicApi };
}

export const { api, publicApi } = createApiClient({ prefix: env.apiBaseUrl, store: sessionStore });
