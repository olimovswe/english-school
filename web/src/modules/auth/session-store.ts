import { createJSONStorage, persist, type StateStorage } from "zustand/middleware";
import { createStore } from "zustand/vanilla";

import type { Session } from "./session";

export const SESSION_STORAGE_KEY = "english-school.session";

export interface SessionState {
  session: Session | null;
  /** Sessiya brauzer xotirasidan oʻqildimi. Undan oldin `session: null` "kirilmagan" degani emas. */
  hydrated: boolean;
  /** Sessiya refresh token rad etilgani uchun tugadi: login sahifasi buni aytadi. */
  expired: boolean;
  signIn(session: Session): void;
  signOut(): void;
  expire(): void;
}

function browserStorage(): StateStorage {
  if (typeof window === "undefined") {
    // createJSONStorage xatoni ushlaydi: serverda (SSR) persist oʻchiq, holat faqat xotirada.
    throw new Error("localStorage faqat brauzerda mavjud.");
  }

  return window.localStorage;
}

/**
 * Sessiya localStorage'da saqlanadi: sahifa yangilanganda va boshqa tablarda ham saqlanib qoladi.
 * `skipHydration`: birinchi render server HTML'i bilan bir xil boʻlishi uchun xotira effektda oʻqiladi
 * (SessionSync).
 */
export function createSessionStore(getStorage: () => StateStorage = browserStorage) {
  const store = createStore<SessionState>()(
    persist(
      (set) => ({
        session: null,
        hydrated: false,
        expired: false,
        signIn: (session) => set({ session, expired: false }),
        signOut: () => set({ session: null, expired: false }),
        expire: () => set({ session: null, expired: true }),
      }),
      {
        name: SESSION_STORAGE_KEY,
        storage: createJSONStorage(getStorage),
        partialize: (state) => ({ session: state.session }),
        skipHydration: true,
      },
    ),
  );

  store.persist?.onFinishHydration(() => store.setState({ hydrated: true }));
  return store;
}

export type SessionStore = ReturnType<typeof createSessionStore>;

export const sessionStore = createSessionStore();
