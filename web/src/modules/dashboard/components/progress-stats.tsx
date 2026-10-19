"use client";

import { BookOpen, Clock, Flame, Trophy, type LucideIcon } from "lucide-react";

import { QueryError } from "@/components/query-error";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { useProgress } from "@/modules/analytics/hooks";

interface Stat {
  label: string;
  value: string;
  hint: string;
  icon: LucideIcon;
  accent?: string;
}

/** Analytics oʻqish modeli: seriya, tugallangan kunlar, haftalik fokus va faol lugʻat. */
export function ProgressStats({ totalDays }: { totalDays: number | undefined }) {
  const progress = useProgress();

  if (progress.isError) {
    return (
      <Card>
        <CardContent>
          <QueryError error={progress.error} onRetry={() => void progress.refetch()} />
        </CardContent>
      </Card>
    );
  }

  const data = progress.data;
  const stats: Stat[] = data
    ? [
        {
          label: "Faol seriya",
          value: `${data.activeStreak} kun`,
          hint: data.activeStreak > 0 ? "Ketma-ket oʻqilgan kunlar" : "Bugun boshlang",
          icon: Flame,
          accent: data.activeStreak > 0 ? "text-orange-500" : undefined,
        },
        {
          label: "Tugallangan kunlar",
          value: totalDays ? `${data.completedDays} / ${totalDays}` : String(data.completedDays),
          hint: "Kurs boʻyicha",
          icon: Trophy,
        },
        {
          label: "Bu hafta fokus",
          value: `${data.focusMinutesThisWeek} daq`,
          hint: "Fokus sessiyalarda",
          icon: Clock,
        },
        {
          label: "Faol lugʻat",
          value: `${data.activeVocabulary} soʻz`,
          hint: "Kartochkalarda",
          icon: BookOpen,
        },
      ]
    : [];

  return (
    <section aria-label="Progress" className="grid grid-cols-2 gap-4 lg:grid-cols-4">
      {data
        ? stats.map(({ label, value, hint, icon: Icon, accent }) => (
            <Card key={label} size="sm">
              <CardContent className="grid gap-1">
                <div className="flex items-center justify-between text-xs text-muted-foreground">
                  {label}
                  <Icon className={cn("size-4", accent)} aria-hidden />
                </div>
                <p className="font-heading text-2xl font-semibold tabular-nums">{value}</p>
                <p className="text-xs text-muted-foreground">{hint}</p>
              </CardContent>
            </Card>
          ))
        : Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-[6.5rem] rounded-xl" />)}
    </section>
  );
}
