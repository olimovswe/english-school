import { isHTTPError } from "ky";
import { beforeEach, describe, expect, it } from "vitest";
import type { StateStorage } from "zustand/middleware";

import { toSession, type AuthTokens } from "@/modules/auth/session";
import { createSessionStore, SESSION_STORAGE_KEY, type SessionStore } from "@/modules/auth/session-store";

import { createApiClient, type ApiClient } from "./client";

const MINUTE = 60_000;

class MemoryStorage implements StateStorage {
  private readonly items = new Map<string, string>();

  getItem = (name: string) => this.items.get(name) ?? null;

  setItem = (name: string, value: string) => void this.items.set(name, value);

  removeItem = (name: string) => void this.items.delete(name);
}

function tokens(generation: number, accessTtlMs = 15 * MINUTE): AuthTokens {
  return {
    userId: "user-1",
    accessToken: `access-${generation}`,
    accessTokenExpiresAt: new Date(Date.now() + accessTtlMs).toISOString(),
    refreshToken: `refresh-${generation}`,
    refreshTokenExpiresAt: new Date(Date.now() + 30 * 24 * 60 * MINUTE).toISOString(),
  };
}

function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json" } });
}

/**
 * Identity kabi: har refresh yangi juftlik beradi va eskisini bekor qiladi; eski refresh token qayta kelsa — 401.
 * `/echo` faqat amaldagi access token bilan javob beradi va soʻrov tanasini qaytaradi.
 */
class FakeIdentity {
  private generation = 1;
  private validAccess: string | null = "access-1";
  private validRefresh = "refresh-1";
  refreshCalls = 0;
  refreshFailsWithNetworkError = false;
  readonly authorizations: (string | null)[] = [];

  fetch = async (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    const request = new Request(input, init);
    if (new URL(request.url).pathname === "/api/v1/identity/refresh") {
      return this.refresh(request);
    }

    const authorization = request.headers.get("authorization");
    this.authorizations.push(authorization);
    if (authorization === null || authorization !== `Bearer ${this.validAccess}`) {
      return new Response(null, { status: 401 });
    }

    return json(200, { method: request.method, body: request.method === "POST" ? await request.json() : null });
  };

  /** Access token serverda yaroqsiz (muddati tugadi), refresh token esa hali yaroqli. */
  expireAccessToken(): void {
    this.validAccess = null;
  }

  /** Tokenlar boshqa joyda almashtirildi (masalan, boshqa tab refresh qildi). */
  rotateElsewhere(): AuthTokens {
    return this.issue();
  }

  private async refresh(request: Request): Promise<Response> {
    this.refreshCalls++;
    if (this.refreshFailsWithNetworkError) {
      throw new TypeError("fetch failed");
    }

    const { refreshToken } = (await request.json()) as { refreshToken: string };
    if (refreshToken !== this.validRefresh) {
      return json(401, { status: 401, detail: "Sessiya yaroqsiz yoki muddati tugagan. Qayta kiring." });
    }

    return json(200, this.issue());
  }

  private issue(): AuthTokens {
    this.generation++;
    const issued = tokens(this.generation);
    this.validAccess = issued.accessToken;
    this.validRefresh = issued.refreshToken;
    return issued;
  }
}

async function failure(promise: Promise<unknown>): Promise<number | null> {
  const error: unknown = await promise.then(
    () => null,
    (caught: unknown) => caught,
  );
  return isHTTPError(error) ? error.response.status : null;
}

describe("api client", () => {
  let storage: MemoryStorage;
  let store: SessionStore;
  let identity: FakeIdentity;
  let client: ApiClient;

  beforeEach(async () => {
    storage = new MemoryStorage();
    store = createSessionStore(() => storage);
    await store.persist.rehydrate();
    identity = new FakeIdentity();
    client = createApiClient({ prefix: "http://api.test/api/v1", store, fetch: identity.fetch });
    store.getState().signIn(toSession(tokens(1)));
  });

  it("sends the access token of the current session", async () => {
    await expect(client.api.get("echo").json()).resolves.toEqual({ method: "GET", body: null });
    expect(identity.authorizations).toEqual(["Bearer access-1"]);
  });

  it("refreshes after a 401, stores the rotated tokens and retries the request with its body", async () => {
    identity.expireAccessToken();

    const result = await client.api.post("echo", { json: { answer: "is" } }).json();

    expect(result).toEqual({ method: "POST", body: { answer: "is" } });
    expect(identity.refreshCalls).toBe(1);
    expect(identity.authorizations).toEqual(["Bearer access-1", "Bearer access-2"]);
    expect(store.getState().session).toMatchObject({ accessToken: "access-2", refreshToken: "refresh-2" });
    expect(storage.getItem(SESSION_STORAGE_KEY)).toContain("refresh-2");
  });

  it("shares one refresh between concurrent requests, so a refresh token is never redeemed twice", async () => {
    identity.expireAccessToken();

    const results = await Promise.all([1, 2, 3].map((n) => client.api.post("echo", { json: { n } }).json()));

    expect(results).toEqual([1, 2, 3].map((n) => ({ method: "POST", body: { n } })));
    expect(identity.refreshCalls).toBe(1);
    expect(store.getState().session?.refreshToken).toBe("refresh-2");
  });

  it("uses tokens another tab already rotated instead of redeeming the old refresh token", async () => {
    // Boshqa tab refresh-1 ni almashtirib, natijani localStorage'ga yozdi; bu tab xotirasida hali access-1.
    const otherTab = createSessionStore(() => storage);
    otherTab.getState().signIn(toSession(identity.rotateElsewhere()));

    await expect(client.api.get("echo").json()).resolves.toEqual({ method: "GET", body: null });

    expect(identity.refreshCalls).toBe(0);
    expect(identity.authorizations).toEqual(["Bearer access-1", "Bearer access-2"]);
    expect(store.getState().session?.accessToken).toBe("access-2");
  });

  it("refreshes ahead of expiry instead of waiting for a 401", async () => {
    store.getState().signIn(toSession(tokens(1, 10_000)));

    await client.api.get("echo").json();

    expect(identity.refreshCalls).toBe(1);
    expect(identity.authorizations).toEqual(["Bearer access-2"]);
  });

  it("ends the session when the refresh token is rejected", async () => {
    identity.rotateElsewhere(); // refresh-1 endi eskirgan: server uni rad etadi.

    expect(await failure(client.api.get("echo").json())).toBe(401);
    expect(identity.refreshCalls).toBe(1);
    expect(store.getState()).toMatchObject({ session: null, expired: true });
  });

  it("keeps the session when the refresh fails for a network reason", async () => {
    identity.expireAccessToken();
    identity.refreshFailsWithNetworkError = true;

    expect(await failure(client.api.get("echo").json())).toBe(401);
    expect(store.getState()).toMatchObject({ session: { refreshToken: "refresh-1" }, expired: false });
  });

  it("never attaches or refreshes tokens on public endpoints", async () => {
    expect(await failure(client.publicApi.get("echo").json())).toBe(401);
    expect(identity.authorizations).toEqual([null]);
    expect(identity.refreshCalls).toBe(0);
  });
});
