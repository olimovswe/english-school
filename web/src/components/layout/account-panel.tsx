"use client";

import { Skeleton } from "@/components/ui/skeleton";
import { SignOutButton } from "@/modules/auth/components/sign-out-button";
import { useProfile } from "@/modules/auth/hooks";

import { ThemeToggle } from "./theme-toggle";

export function AccountPanel() {
  const profile = useProfile();
  const contact = profile.data?.email ?? (profile.data?.telegramId ? "Telegram orqali" : null);

  return (
    <div className="grid gap-2">
      <div className="min-w-0 px-2">
        {profile.data ? (
          <>
            <p className="truncate text-sm font-medium">{profile.data.displayName}</p>
            {contact && <p className="truncate text-xs text-muted-foreground">{contact}</p>}
          </>
        ) : (
          <div className="grid gap-1.5 py-0.5">
            <Skeleton className="h-4 w-28" />
            <Skeleton className="h-3 w-36" />
          </div>
        )}
      </div>
      <div className="flex items-center justify-between">
        <SignOutButton size="sm" className="text-muted-foreground" />
        <ThemeToggle />
      </div>
    </div>
  );
}
