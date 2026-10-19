"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";

import { SESSION_STORAGE_KEY, sessionStore } from "../session-store";

/**
 * Sessiyani localStorage'dan oʻqiydi va tablarni sinxron tutadi: boshqa tabdagi login, logout yoki token
 * almashuvi bu yerga ham yetadi. Foydalanuvchi almashganda (yoki chiqqanda) oldingisining keshi tozalanadi.
 */
export function SessionSync() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const unsubscribe = sessionStore.subscribe((state, previous) => {
      if (state.session?.userId !== previous.session?.userId) {
        queryClient.clear();
      }
    });

    const onStorage = (event: StorageEvent) => {
      if (event.key === SESSION_STORAGE_KEY || event.key === null) {
        void sessionStore.persist.rehydrate();
      }
    };

    void sessionStore.persist.rehydrate();
    window.addEventListener("storage", onStorage);
    return () => {
      window.removeEventListener("storage", onStorage);
      unsubscribe();
    };
  }, [queryClient]);

  return null;
}
