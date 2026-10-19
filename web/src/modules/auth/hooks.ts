import { useMutation, useQuery } from "@tanstack/react-query";
import { useStore } from "zustand";

import { authApi } from "./api";
import { toSession, type AuthTokens } from "./session";
import { sessionStore, type SessionState } from "./session-store";

export function useSessionState<T>(selector: (state: SessionState) => T): T {
  return useStore(sessionStore, selector);
}

export const authKeys = {
  profile: (userId: string | undefined) => ["identity", "me", userId] as const,
};

export function useProfile() {
  const userId = useSessionState((state) => state.session?.userId);
  return useQuery({
    queryKey: authKeys.profile(userId),
    queryFn: authApi.me,
    enabled: userId !== undefined,
    staleTime: 5 * 60_000,
  });
}

function useSignIn<TVariables>(request: (variables: TVariables) => Promise<AuthTokens>) {
  return useMutation({
    mutationFn: request,
    onSuccess: (tokens) => sessionStore.getState().signIn(toSession(tokens)),
  });
}

export const useLogin = () => useSignIn(authApi.login);

export const useRegister = () => useSignIn(authApi.register);

export const useTelegramSignIn = () => useSignIn(authApi.telegram);

/** Serverda sessiya bekor qilinadi (tarmoq boʻlmasa ham), brauzerda esa har holda tozalanadi. */
export function useSignOut() {
  return useMutation({
    mutationFn: async () => {
      const refreshToken = sessionStore.getState().session?.refreshToken;
      if (refreshToken) {
        await authApi.logout(refreshToken).catch(() => undefined);
      }
    },
    onSettled: () => sessionStore.getState().signOut(),
  });
}
