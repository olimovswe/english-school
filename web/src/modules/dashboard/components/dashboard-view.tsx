"use client";

import { Trophy } from "lucide-react";

import { QueryError } from "@/components/query-error";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { statusOf } from "@/lib/api/errors";
import { formatCalendarDate } from "@/lib/format";
import { useProfile } from "@/modules/auth/hooks";
import { useCourseDays } from "@/modules/curriculum/hooks";
import { useTodayPlan } from "@/modules/discipline/hooks";

import { DailyProgressCard } from "./daily-progress-card";
import { MorningGateCard } from "./morning-gate-card";
import { ProgressStats } from "./progress-stats";
import { QuickActions } from "./quick-actions";

export function DashboardView() {
  const profile = useProfile();
  const today = useTodayPlan();
  const days = useCourseDays();
  const totalDays = days.data?.length;
  const lessonTitle = days.data?.find((day) => day.dayNumber === today.data?.dayNumber)?.title;
  const courseFinished = today.isError && statusOf(today.error) === 404;

  return (
    <div className="grid gap-8">
      <header className="grid gap-1">
        <p className="text-sm text-muted-foreground">
          {today.data ? (
            <>
              {formatCalendarDate(today.data.assignedDay)} · {today.data.dayNumber}-kun
              {totalDays ? ` / ${totalDays}` : ""}
            </>
          ) : (
            "Kunlik reja"
          )}
        </p>
        {profile.data ? (
          <h1 className="font-heading text-2xl font-semibold">Salom, {profile.data.displayName}!</h1>
        ) : (
          <Skeleton className="h-8 w-48" />
        )}
      </header>

      {courseFinished ? (
        <CourseFinished totalDays={totalDays} />
      ) : today.isError ? (
        <Card>
          <CardContent>
            <QueryError error={today.error} onRetry={() => void today.refetch()} />
          </CardContent>
        </Card>
      ) : today.data ? (
        <div className="grid gap-4 lg:grid-cols-2">
          <MorningGateCard plan={today.data} />
          <DailyProgressCard plan={today.data} lessonTitle={lessonTitle} />
        </div>
      ) : (
        <div className="grid gap-4 lg:grid-cols-2">
          <Skeleton className="h-64 rounded-xl" />
          <Skeleton className="h-64 rounded-xl" />
        </div>
      )}

      <ProgressStats totalDays={totalDays} />
      <QuickActions />
    </div>
  );
}

function CourseFinished({ totalDays }: { totalDays: number | undefined }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Trophy className="size-4 text-amber-500" aria-hidden />
          Kurs yakunlandi
        </CardTitle>
        <CardDescription>
          {totalDays ? `${totalDays} kunlik` : "Butun"} kursni tugatdingiz. Kartochkalarni takrorlashda davom eting:
          soʻzlar shunda esda qoladi.
        </CardDescription>
      </CardHeader>
    </Card>
  );
}
