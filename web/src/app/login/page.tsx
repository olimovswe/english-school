import type { Metadata } from "next";
import { Suspense } from "react";

import { PageSpinner } from "@/components/page-spinner";
import { LoginView } from "@/modules/auth/components/login-view";

export const metadata: Metadata = { title: "Kirish" };

export default function LoginPage() {
  // useSearchParams (?next=) statik sahifada Suspense chegarasini talab qiladi.
  return (
    <Suspense fallback={<PageSpinner />}>
      <LoginView />
    </Suspense>
  );
}
