"use client";

import { Loader2 } from "lucide-react";
import { useId, useState, type ComponentProps, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { errorMessage, fieldErrorsOf } from "@/lib/api/errors";

import { useLogin, useRegister } from "../hooks";

type Mode = "login" | "register";

const VISIBLE_FIELDS: Record<Mode, readonly string[]> = {
  login: ["email", "password"],
  register: ["displayName", "email", "password"],
};

function browserTimeZone(): string | undefined {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
  } catch {
    return undefined;
  }
}

/**
 * Email va parol: kirish yoki roʻyxatdan oʻtish. Brauzer validatsiyasi oʻchiq (`noValidate`): xabarlarni
 * server oʻzbek tilida qaytaradi va ular maydon ostida koʻrsatiladi.
 */
export function CredentialsForm() {
  const [mode, setMode] = useState<Mode>("login");
  const login = useLogin();
  const register = useRegister();
  const mutation = mode === "login" ? login : register;

  const fieldErrors = fieldErrorsOf(mutation.error);
  // Maydoni koʻrinmaydigan xato (masalan, timeZone) ham yoʻqolib qolmasin.
  const otherFieldError = Object.entries(fieldErrors).find(([field]) => !VISIBLE_FIELDS[mode].includes(field))?.[1];
  const formError = !mutation.error
    ? null
    : Object.keys(fieldErrors).length === 0
      ? errorMessage(mutation.error)
      : (otherFieldError ?? null);

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const email = String(form.get("email") ?? "").trim();
    const password = String(form.get("password") ?? "");

    if (mode === "login") {
      login.mutate({ email, password });
    } else {
      const displayName = String(form.get("displayName") ?? "").trim();
      register.mutate({ email, password, displayName, timeZone: browserTimeZone() });
    }
  }

  function switchMode() {
    login.reset();
    register.reset();
    setMode(mode === "login" ? "register" : "login");
  }

  return (
    <form onSubmit={onSubmit} className="grid gap-4" noValidate>
      {mode === "register" && (
        <TextField label="Ismingiz" name="displayName" autoComplete="name" error={fieldErrors.displayName} />
      )}
      <TextField label="Email" name="email" type="email" autoComplete="email" error={fieldErrors.email} />
      <TextField
        label="Parol"
        name="password"
        type="password"
        autoComplete={mode === "login" ? "current-password" : "new-password"}
        error={fieldErrors.password}
      />

      {formError && (
        <p role="alert" className="text-sm text-destructive">
          {formError}
        </p>
      )}

      <Button type="submit" size="lg" disabled={mutation.isPending}>
        {mutation.isPending && <Loader2 className="animate-spin" aria-hidden />}
        {mode === "login" ? "Kirish" : "Roʻyxatdan oʻtish"}
      </Button>

      <p className="text-center text-sm text-muted-foreground">
        {mode === "login" ? "Akkauntingiz yoʻqmi?" : "Akkauntingiz bormi?"}{" "}
        <Button type="button" variant="link" className="h-auto p-0" onClick={switchMode}>
          {mode === "login" ? "Roʻyxatdan oʻting" : "Kiring"}
        </Button>
      </p>
    </form>
  );
}

function TextField({ label, error, ...inputProps }: { label: string; error?: string } & ComponentProps<typeof Input>) {
  const id = useId();
  const errorId = `${id}-error`;

  return (
    <div className="grid gap-2">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        {...inputProps}
      />
      {error && (
        <p id={errorId} className="text-sm text-destructive">
          {error}
        </p>
      )}
    </div>
  );
}
