import { RequireSession } from "@/modules/auth/components/require-session";

import { WelcomeCard } from "./welcome-card";

export default function HomePage() {
  return (
    <RequireSession>
      <main className="flex min-h-svh items-center justify-center p-6">
        <WelcomeCard />
      </main>
    </RequireSession>
  );
}
