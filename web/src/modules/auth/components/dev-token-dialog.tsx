"use client";

import { KeyRound } from "lucide-react";
import { useId, useState, type FormEvent } from "react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";

import { sessionFromDevInput } from "../session";
import { sessionStore } from "../session-store";

/**
 * Faqat `next dev`: curl bilan olingan tokenlar bilan kirish (Telegram widget localhost'da ishlamaydi).
 * Token imzosini bu yerda emas, birinchi soʻrovda API tekshiradi.
 */
export function DevTokenDialog() {
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const id = useId();

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const input = String(new FormData(event.currentTarget).get("token") ?? "");
    try {
      sessionStore.getState().signIn(sessionFromDevInput(input));
      setOpen(false);
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : "Tokenni oʻqib boʻlmadi.");
    }
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        setError(null);
      }}
    >
      <DialogTrigger asChild>
        <Button variant="ghost" size="sm" className="text-muted-foreground">
          <KeyRound aria-hidden />
          Token bilan kirish (dev)
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-lg">
        <form onSubmit={onSubmit} className="grid gap-4">
          <DialogHeader>
            <DialogTitle>Token bilan kirish</DialogTitle>
            <DialogDescription>
              Faqat ishlab chiqish uchun. <code>/identity/login</code> javobini (JSON) yoki faqat access token (JWT)
              ni joylang. Faqat JWT bilan kirilsa, sessiya token muddati (exp) bilan tugaydi.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-2">
            <Label htmlFor={id}>Token yoki JSON</Label>
            <Textarea
              id={id}
              name="token"
              rows={6}
              required
              spellCheck={false}
              className="font-mono text-xs"
              aria-invalid={error ? true : undefined}
              placeholder='{"userId":"…","accessToken":"eyJ…","refreshToken":"…",…}'
            />
            {error && (
              <p role="alert" className="text-sm text-destructive">
                {error}
              </p>
            )}
          </div>
          <DialogFooter>
            <Button type="submit">Kirish</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
