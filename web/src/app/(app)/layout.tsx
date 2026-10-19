import type { ReactNode } from "react";

import { AppShell } from "@/components/layout/app-shell";
import { RequireSession } from "@/modules/auth/components/require-session";

/** Kirgan oʻquvchi sahifalari: sessiya tekshiruvi va umumiy navigatsiya. */
export default function AppLayout({ children }: { children: ReactNode }) {
  return (
    <RequireSession>
      <AppShell>{children}</AppShell>
    </RequireSession>
  );
}
