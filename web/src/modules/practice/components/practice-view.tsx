"use client";

import { Layers } from "lucide-react";
import Link from "next/link";

import { QueryError } from "@/components/query-error";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { statusOf } from "@/lib/api/errors";
import { useCourseDays } from "@/modules/curriculum/hooks";
import { useTodayPlan } from "@/modules/discipline/hooks";

import { useExerciseListQuery } from "../hooks";
import { SUPPORTED_EXERCISE_TYPES } from "./exercise-player";
import { PracticeSession } from "./practice-session";

/** `/practice`: bugungi reja kunining mashqlari (kun Discipline'dan: `GET /discipline/today`). */
export function PracticeView() {
  const today = useTodayPlan();
  const days = useCourseDays();
  const dayNumber = today.data?.dayNumber;
  const exercises = useExerciseListQuery(dayNumber);
  const lessonTitle = days.data?.find((day) => day.dayNumber === dayNumber)?.title;
  const playable = exercises.data?.filter((exercise) => SUPPORTED_EXERCISE_TYPES.includes(exercise.type));

  return (
    <div className="grid gap-6">
      <header className="grid gap-1">
        <h1 className="font-heading text-2xl font-semibold">Mashqlar</h1>
        <p className="text-sm text-muted-foreground">
          {dayNumber ? `${dayNumber}-kun${lessonTitle ? `: ${lessonTitle}` : ""}` : "Bugungi kun mashqlari"}
        </p>
      </header>

      {today.isError && statusOf(today.error) === 404 ? (
        <Notice title="Kurs yakunlandi" description="Barcha kunlar tugagan. Soʻzlarni kartochkalarda takrorlashda davom eting." />
      ) : today.isError || exercises.isError ? (
        <Card>
          <CardContent>
            <QueryError
              error={today.error ?? exercises.error}
              onRetry={() => void (today.isError ? today.refetch() : exercises.refetch())}
            />
          </CardContent>
        </Card>
      ) : !dayNumber || !playable ? (
        <div className="grid gap-4" aria-busy="true" aria-label="Mashqlar yuklanmoqda">
          <Skeleton className="h-2 w-full rounded-full" />
          <Skeleton className="h-72 rounded-xl" />
        </div>
      ) : playable.length === 0 ? (
        <Notice
          title="Bugun mashq yoʻq"
          description={`${dayNumber}-kun uchun mashqlar hali tayyor emas yoki bu takrorlash kuni. Soʻzlarni kartochkalarda takrorlang.`}
        />
      ) : (
        <PracticeSession key={dayNumber} dayNumber={dayNumber} exercises={playable} />
      )}
    </div>
  );
}

function Notice({ title, description }: { title: string; description: string }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>{description}</CardDescription>
      </CardHeader>
      <CardFooter>
        <Button asChild variant="outline">
          <Link href="/flashcards">
            <Layers aria-hidden />
            Kartochkalar
          </Link>
        </Button>
      </CardFooter>
    </Card>
  );
}
