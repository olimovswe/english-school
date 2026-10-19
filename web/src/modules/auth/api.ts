import { api, publicApi } from "@/lib/api/client";

import type { AuthTokens } from "./session";
import type { Credentials, Registration, TelegramAuthData, UserProfile } from "./types";

/** `/api/v1/identity` */
export const authApi = {
  login: (credentials: Credentials) => publicApi.post("identity/login", { json: credentials }).json<AuthTokens>(),

  register: (registration: Registration) =>
    publicApi.post("identity/register", { json: registration }).json<AuthTokens>(),

  telegram: (data: TelegramAuthData) => publicApi.post("identity/telegram", { json: data }).json<AuthTokens>(),

  /** Sessiyaning butun token oilasini bekor qiladi; nomaʼlum token uchun ham 204. */
  logout: async (refreshToken: string) => {
    await publicApi.post("identity/logout", { json: { refreshToken } });
  },

  me: () => api.get("identity/me").json<UserProfile>(),
};
