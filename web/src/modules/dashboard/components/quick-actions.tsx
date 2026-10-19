import { ArrowRight } from "lucide-react";
import Link from "next/link";

import { NAV_ITEMS } from "@/components/layout/nav-items";

const ACTIONS = NAV_ITEMS.filter((item) => item.href !== "/dashboard");

export function QuickActions() {
  return (
    <section aria-labelledby="quick-actions" className="grid gap-3">
      <h2 id="quick-actions" className="font-heading text-sm font-medium text-muted-foreground">
        Tezkor harakatlar
      </h2>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        {ACTIONS.map(({ href, label, description, icon: Icon }) => (
          <Link
            key={href}
            href={href}
            className="group flex items-center gap-3 rounded-xl p-3 ring-1 ring-foreground/10 transition-colors outline-none hover:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50"
          >
            <span className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-muted group-hover:bg-background">
              <Icon className="size-4" aria-hidden />
            </span>
            <span className="grid min-w-0 flex-1">
              <span className="text-sm font-medium">{label}</span>
              <span className="text-xs text-muted-foreground">{description}</span>
            </span>
            <ArrowRight className="size-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" aria-hidden />
          </Link>
        ))}
      </div>
    </section>
  );
}
