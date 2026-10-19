"use client";

import { GraduationCap } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

import { cn } from "@/lib/utils";

import { AccountPanel } from "./account-panel";
import { isActive, NAV_ITEMS } from "./nav-items";

/** Brend, navigatsiya va akkaunt: kompyuterda yon panel, telefonda Sheet ichida bir xil. */
export function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const pathname = usePathname();

  return (
    <div className="flex h-full flex-col gap-6 p-4">
      <Link href="/dashboard" onClick={onNavigate} className="flex items-center gap-2 px-2 font-heading font-semibold">
        <GraduationCap className="size-5" aria-hidden />
        English School
      </Link>

      <nav aria-label="Asosiy boʻlimlar" className="grid gap-1">
        {NAV_ITEMS.map(({ href, label, icon: Icon }) => {
          const active = isActive(pathname, href);
          return (
            <Link
              key={href}
              href={href}
              onClick={onNavigate}
              aria-current={active ? "page" : undefined}
              className={cn(
                "flex items-center gap-3 rounded-lg px-2 py-2 text-sm transition-colors",
                active
                  ? "bg-sidebar-accent font-medium text-sidebar-accent-foreground"
                  : "text-muted-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground",
              )}
            >
              <Icon className="size-4" aria-hidden />
              {label}
            </Link>
          );
        })}
      </nav>

      <div className="mt-auto border-t pt-4">
        <AccountPanel />
      </div>
    </div>
  );
}
