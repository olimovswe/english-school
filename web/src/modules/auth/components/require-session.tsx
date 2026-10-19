"use client";

import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

import { PageSpinner } from "@/components/page-spinner";

import { useSessionState } from "../hooks";
import { signInPath } from "../redirect";

/**
 * Sessiyasiz foydalanuvchini login sahifasiga yuboradi va kirgach shu sahifaga qaytaradi.
 * Tokenlar faqat brauzerda, shuning uchun tekshiruv ham shu yerda: server sahifani sessiyasiz chizadi.
 */
export function RequireSession({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const hydrated = useSessionState((state) => state.hydrated);
  const signedIn = useSessionState((state) => state.session !== null);

  useEffect(() => {
    if (hydrated && !signedIn) {
      router.replace(signInPath(pathname));
    }
  }, [hydrated, signedIn, pathname, router]);

  if (!hydrated || !signedIn) {
    return <PageSpinner label="Sessiya tekshirilmoqda…" />;
  }

  return children;
}
