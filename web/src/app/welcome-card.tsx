"use client";

import { GraduationCap } from "lucide-react";

import { Card, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { errorMessage } from "@/lib/api/errors";
import { useProfile } from "@/modules/auth/hooks";
import { SignOutButton } from "@/modules/auth/components/sign-out-button";

export function WelcomeCard() {
  const profile = useProfile();

  return (
    <Card className="w-full max-w-md">
      <CardHeader className="gap-3">
        <GraduationCap className="size-8 text-muted-foreground" aria-hidden />
        <CardTitle className="text-2xl">
          {profile.data ? `Salom, ${profile.data.displayName}!` : "English School"}
        </CardTitle>
        <CardDescription>
          {profile.isError ? errorMessage(profile.error) : (profile.data?.email ?? "Profil yuklanmoqda…")}
        </CardDescription>
      </CardHeader>
      <CardFooter>
        <SignOutButton variant="outline" />
      </CardFooter>
    </Card>
  );
}
