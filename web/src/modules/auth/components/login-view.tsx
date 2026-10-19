"use client";

import { GraduationCap, Loader2 } from "lucide-react";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect } from "react";
import { toast } from "sonner";

import { PageSpinner } from "@/components/page-spinner";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { errorMessage } from "@/lib/api/errors";
import { env } from "@/lib/env";

import { useSessionState, useTelegramSignIn } from "../hooks";
import { safeRedirectPath } from "../redirect";
import { CredentialsForm } from "./credentials-form";
import { DevTokenDialog } from "./dev-token-dialog";
import { TelegramLoginButton } from "./telegram-login-button";

export function LoginView() {
  const router = useRouter();
  const target = safeRedirectPath(useSearchParams().get("next"));
  const hydrated = useSessionState((state) => state.hydrated);
  const signedIn = useSessionState((state) => state.session !== null);
  const expired = useSessionState((state) => state.expired);

  // Har qanday usul bilan kirilganda (yoki boshqa tabda) sessiya paydo boʻladi va shu yerdan yoʻnaltiriladi.
  useEffect(() => {
    if (hydrated && signedIn) {
      router.replace(target);
    }
  }, [hydrated, signedIn, router, target]);

  if (!hydrated || signedIn) {
    return <PageSpinner />;
  }

  return (
    <main className="flex min-h-svh items-center justify-center p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="gap-2 text-center">
          <GraduationCap className="mx-auto size-8 text-muted-foreground" aria-hidden />
          <CardTitle className="text-xl">English School&apos;ga kirish</CardTitle>
          <CardDescription>Kunlik darsni davom ettirish uchun Telegram yoki email orqali kiring.</CardDescription>
        </CardHeader>
        <CardContent className="grid gap-6">
          {expired && (
            <p role="status" className="rounded-lg border bg-muted/50 px-3 py-2 text-center text-sm">
              Sessiya muddati tugadi. Qayta kiring.
            </p>
          )}
          <TelegramSection />
          <CredentialsForm />
        </CardContent>
        {env.isDevelopment && (
          <CardFooter className="justify-center">
            <DevTokenDialog />
          </CardFooter>
        )}
      </Card>
    </main>
  );
}

function TelegramSection() {
  const telegram = useTelegramSignIn();

  if (!env.telegramBotUsername) {
    return env.isDevelopment ? (
      <p className="text-center text-xs text-muted-foreground">
        Telegram tugmasi uchun <code>NEXT_PUBLIC_TELEGRAM_BOT_USERNAME</code> ni sozlang.
      </p>
    ) : null;
  }

  return (
    <div className="grid gap-4">
      {telegram.isPending ? (
        <p role="status" className="flex min-h-10 items-center justify-center gap-2 text-sm text-muted-foreground">
          <Loader2 className="size-4 animate-spin" aria-hidden />
          Telegram orqali kirilmoqda…
        </p>
      ) : (
        <TelegramLoginButton
          botUsername={env.telegramBotUsername}
          onAuth={(user) => telegram.mutate(user, { onError: (error) => toast.error(errorMessage(error)) })}
        />
      )}
      <div className="flex items-center gap-3 text-xs text-muted-foreground" aria-hidden>
        <span className="h-px flex-1 bg-border" />
        yoki
        <span className="h-px flex-1 bg-border" />
      </div>
    </div>
  );
}
