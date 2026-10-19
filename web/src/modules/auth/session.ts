/** Identity javobi (register, login, telegram, refresh) — API qaytarganidek. */
export interface AuthTokens {
  userId: string;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

/** Brauzerda saqlanadigan sessiya; muddatlar epoch millisekundlarda. */
export interface Session {
  userId: string;
  accessToken: string;
  accessTokenExpiresAt: number;
  /** Faqat access token bilan (dasturchi rejimida) kirilganda yoʻq: muddati tugagach qayta kirish kerak. */
  refreshToken: string | null;
  refreshTokenExpiresAt: number | null;
}

export function toSession(tokens: AuthTokens): Session {
  return {
    userId: tokens.userId,
    accessToken: tokens.accessToken,
    accessTokenExpiresAt: Date.parse(tokens.accessTokenExpiresAt),
    refreshToken: tokens.refreshToken,
    refreshTokenExpiresAt: Date.parse(tokens.refreshTokenExpiresAt),
  };
}

interface JwtClaims {
  sub?: unknown;
  exp?: unknown;
}

/** Imzo tekshirilmaydi: buni API qiladi. Bu yerda faqat foydalanuvchi ID va muddat oʻqiladi. */
function readJwtClaims(token: string): JwtClaims {
  const parts = token.split(".");
  const payload = parts[1];
  if (parts.length !== 3 || !payload) {
    throw new Error("Bu JWT emas: token uch qismdan iborat boʻlishi kerak.");
  }

  try {
    const base64 = payload.replace(/-/g, "+").replace(/_/g, "/");
    const json = new TextDecoder().decode(Uint8Array.from(atob(base64), (char) => char.charCodeAt(0)));
    return JSON.parse(json) as JwtClaims;
  } catch {
    throw new Error("JWT ichidagi maʼlumotni oʻqib boʻlmadi.");
  }
}

function fromAccessToken(accessToken: string, refreshToken: string | null, refreshTokenExpiresAt: number | null): Session {
  const { sub, exp } = readJwtClaims(accessToken);
  if (typeof sub !== "string" || typeof exp !== "number") {
    throw new Error("JWT'da sub yoki exp yoʻq.");
  }

  if (exp * 1000 <= Date.now()) {
    throw new Error("Access token muddati tugagan.");
  }

  return { userId: sub, accessToken, accessTokenExpiresAt: exp * 1000, refreshToken, refreshTokenExpiresAt };
}

/**
 * Dasturchi rejimi: `/identity/login` javobi (JSON) yoki faqat access token (JWT).
 * Xato boʻlsa foydalanuvchiga koʻrsatiladigan matn bilan Error tashlaydi.
 */
export function sessionFromDevInput(input: string): Session {
  const text = input.trim();
  if (!text.startsWith("{")) {
    return fromAccessToken(text, null, null);
  }

  let parsed: Partial<Record<keyof AuthTokens, unknown>>;
  try {
    parsed = JSON.parse(text) as typeof parsed;
  } catch {
    throw new Error("JSON notoʻgʻri.");
  }

  if (typeof parsed.accessToken !== "string") {
    throw new Error("JSON'da accessToken yoʻq.");
  }

  const refreshToken = typeof parsed.refreshToken === "string" ? parsed.refreshToken : null;
  const refreshExpiry = typeof parsed.refreshTokenExpiresAt === "string" ? Date.parse(parsed.refreshTokenExpiresAt) : NaN;
  return fromAccessToken(parsed.accessToken, refreshToken, refreshToken && !Number.isNaN(refreshExpiry) ? refreshExpiry : null);
}
