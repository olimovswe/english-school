"use client";

import { ArrowRight, Layers, Loader2, Lock, LockOpen } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";

import { QueryError } from "@/components/query-error";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardAction, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { Skeleton } from "@/components/ui/skeleton";
import { errorMessage } from "@/lib/api/errors";
import { formatPercent } from "@/lib/format";
import { cn } from "@/lib/utils";
import { GATE_PASS_PERCENTAGE, type TodayPlan } from "@/modules/discipline/api";
import { useSubmitGate } from "@/modules/discipline/hooks";
import { useDueCount } from "@/modules/retention/hooks";

/**
 * Ertalabki darvoza: kechagi kartochkalar ≥ 80 % toʻgʻri takrorlanmaguncha bugungi dars yopiq.
 * Navbatda kartochka boʻlmasa (masalan, 1-kun), darvoza boʻsh javoblar bilan darhol ochiladi.
 */
export function MorningGateCard({ plan }: { plan: TodayPlan }) {
  const passed = plan.morningReviewPassed;
  const score = plan.gatePassPercentage;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          {passed ? <LockOpen className="size-4" aria-hidden /> : <Lock className="size-4" aria-hidden />}
          Ertalabki darvoza
        </CardTitle>
        <CardDescription>
          Bugungi dars kechagi soʻzlarni kamida {formatPercent(GATE_PASS_PERCENTAGE)} toʻgʻri takrorlagach ochiladi.
        </CardDescription>
        <CardAction>
          <Badge
            className={cn(
              passed
                ? "bg-emerald-500/15 text-emerald-700 dark:text-emerald-400"
                : "bg-amber-500/15 text-amber-700 dark:text-amber-400",
            )}
          >
            {passed ? "Ochiq" : "Yopiq"}
          </Badge>
        </CardAction>
      </CardHeader>

      <CardContent className="grid gap-3">
        {score === null ? (
          <p className="text-sm text-muted-foreground">Bugun hali takrorlanmagan.</p>
        ) : (
          <div className="grid gap-2">
            <div className="flex items-baseline justify-between text-sm">
              <span className="text-muted-foreground">Natija</span>
              <span>
                <span className="text-lg font-semibold tabular-nums">{formatPercent(score)}</span>
                <span className="text-muted-foreground"> / kerak {formatPercent(GATE_PASS_PERCENTAGE)}</span>
              </span>
            </div>
            <Progress value={score} aria-label="Darvoza natijasi" />
          </div>
        )}
        {!passed && <DueCardsLine />}
      </CardContent>

      <CardFooter>{passed ? <LessonLink /> : <GateAction />}</CardFooter>
    </Card>
  );
}

function DueCardsLine() {
  const due = useDueCount();

  if (due.isPending) {
    return <Skeleton className="h-4 w-48" />;
  }

  if (due.isError) {
    return <QueryError error={due.error} onRetry={() => void due.refetch()} />;
  }

  return (
    <p className="text-sm">
      {due.data > 0
        ? `Takrorlanishi kerak: ${due.data} ta kartochka.`
        : "Takrorlanadigan kartochka yoʻq: darvozani hoziroq ochish mumkin."}
    </p>
  );
}

function GateAction() {
  const due = useDueCount();
  const submit = useSubmitGate();

  if (due.data === undefined) {
    return null;
  }

  if (due.data > 0) {
    return (
      <Button asChild>
        <Link href="/flashcards">
          <Layers aria-hidden />
          Takrorlashni boshlash
        </Link>
      </Button>
    );
  }

  return (
    <Button
      disabled={submit.isPending}
      onClick={() =>
        submit.mutate([], {
          onSuccess: (result) =>
            result.passed
              ? toast.success("Darvoza ochildi: bugungi dars sizni kutmoqda.")
              : toast.warning("Avval navbatdagi kartochkalarni takrorlang."),
          onError: (error) => toast.error(errorMessage(error)),
        })
      }
    >
      {submit.isPending ? <Loader2 className="animate-spin" aria-hidden /> : <LockOpen aria-hidden />}
      Darvozani ochish
    </Button>
  );
}

function LessonLink() {
  return (
    <Button asChild variant="outline">
      <Link href="/practice">
        Bugungi mashqlar
        <ArrowRight aria-hidden />
      </Link>
    </Button>
  );
}
