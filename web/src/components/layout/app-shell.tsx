"use client";

import { GraduationCap, Menu } from "lucide-react";
import Link from "next/link";
import { useState, type ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetDescription, SheetTitle, SheetTrigger } from "@/components/ui/sheet";

import { SidebarContent } from "./sidebar-content";

/** Kompyuterda (md+) doimiy yon panel; telefonda yuqori panel va chapdan ochiladigan menyu. */
export function AppShell({ children }: { children: ReactNode }) {
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <div className="min-h-svh md:grid md:grid-cols-[15rem_minmax(0,1fr)]">
      <aside className="sticky top-0 hidden h-svh border-r bg-sidebar text-sidebar-foreground md:block">
        <SidebarContent />
      </aside>

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-40 flex h-14 items-center gap-2 border-b bg-background/80 px-4 backdrop-blur md:hidden">
          <Sheet open={menuOpen} onOpenChange={setMenuOpen}>
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" aria-label="Menyuni ochish">
                <Menu aria-hidden />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-72 bg-sidebar p-0 text-sidebar-foreground">
              <SheetTitle className="sr-only">Menyu</SheetTitle>
              <SheetDescription className="sr-only">Ilova boʻlimlari va akkaunt</SheetDescription>
              <SidebarContent onNavigate={() => setMenuOpen(false)} />
            </SheetContent>
          </Sheet>
          <Link href="/dashboard" className="flex items-center gap-2 font-heading font-semibold">
            <GraduationCap className="size-5" aria-hidden />
            English School
          </Link>
        </header>

        <main className="mx-auto w-full max-w-5xl flex-1 px-4 py-6 md:px-8 md:py-10">{children}</main>
      </div>
    </div>
  );
}
