"use client";

import { Loader2, LogOut } from "lucide-react";
import type { ComponentProps } from "react";

import { Button } from "@/components/ui/button";

import { useSignOut } from "../hooks";

export function SignOutButton(props: Omit<ComponentProps<typeof Button>, "onClick" | "children">) {
  const signOut = useSignOut();

  return (
    <Button variant="ghost" {...props} disabled={signOut.isPending} onClick={() => signOut.mutate()}>
      {signOut.isPending ? <Loader2 className="animate-spin" aria-hidden /> : <LogOut aria-hidden />}
      Chiqish
    </Button>
  );
}
