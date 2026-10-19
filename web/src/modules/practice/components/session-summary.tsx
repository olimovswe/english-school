"use client";

import { CircleAlert, CircleCheck, CircleX, LayoutDashboard, RotateCcw } from "lucide-react";
import Link from "next/link";
import { useEffect, useId, useRef } from "react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";
import { formatPercent } from "@/lib/format";
import { cn } from "@/lib/utils";

import type { ExerciseDto, Verdict } from "../api";
import { errorCodeLabel, VERDICT_LABELS } from "../grading";
import { summarize, type AttemptRecord } from "../session";

/** Shu natijadan boshlab mashqlar yaxshi bajarilgan hisoblanadi (darvoza chegarasi bilan bir xil). */
const GOOD_AVERAGE = 0.8;

const VERDICT_ICONS: Record<Verdict, { icon: typeof CircleCheck; className: string }> = {
  Correct: { icon: CircleCheck, className: "text-emerald-600 dark:text-emerald-400" },
  PartiallyCorrect: { icon: CircleAlert, className: "text-amber-600 dark:text-amber-400" },
  Incorrect: { icon: CircleX, className: "text-destructive" },
};

interface SessionSummaryProps {
  dayNumber: number;
  exercises: readonly ExerciseDto[];
  results: readonly AttemptRecord[];
  onRestart: () => void;
}

/** Partiya yakuni: oʻrtacha ball, hukmlar soni, uchragan xato toifalari va har bir mashq natijasi. */
export function SessionSummary({ dayNumber, exercises, results, onRestart }: SessionSummaryProps) {
  const summary = summarize(results);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const errorsHeadingId = useId();

  // Ekran oʻquvchi va klaviatura uchun: yakun paydo boʻlganda fokus sarlavhaga oʻtadi.
  useEffect(() => headingRef.current?.focus(), []);

  const stats = [
    { label: "Oʻrtacha ball", value: formatPercent(summary.averageScore * 100), className: "" },
    { label: "Toʻgʻri", value: summary.correct, className: "text-emerald-600 dark:text-emerald-400" },
    { label: "Qisman", value: summary.partial, className: "text-amber-600 dark:text-amber-400" },
    { label: "Notoʻgʻri", value: summary.incorrect, className: "text-destructive" },
  ];

  return (
    <Card>
      <CardHeader>
        <CardTitle>
          <h2 ref={headingRef} tabIndex={-1} className="font-heading text-xl outline-none">
            Mashqlar yakunlandi
          </h2>
        </CardTitle>
        <CardDescription>
          {dayNumber}-kun · {summary.total} ta mashq.{" "}
          {summary.averageScore >= GOOD_AVERAGE
            ? "Ajoyib natija!"
            : "Quyidagi toifalarga eʼtibor bering va mashqlarni qayta ishlang."}
        </CardDescription>
      </CardHeader>

      <CardContent className="grid gap-6">
        <dl className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {stats.map(({ label, value, className }) => (
            <div key={label} className="rounded-lg bg-muted/50 p-3">
              <dt className="text-xs text-muted-foreground">{label}</dt>
              <dd className={cn("font-heading text-2xl font-semibold tabular-nums", className)}>{value}</dd>
            </div>
          ))}
        </dl>

        {summary.errorCodes.length > 0 && (
          <section aria-labelledby={errorsHeadingId} className="grid gap-2">
            <h3 id={errorsHeadingId} className="text-sm font-medium">
              Xato toifalari
            </h3>
            <ul className="flex flex-wrap gap-2">
              {summary.errorCodes.map(({ code, count }) => (
                <li key={code}>
                  <Badge variant="outline">
                    {errorCodeLabel(code)}
                    {count > 1 && <span className="tabular-nums"> ×{count}</span>}
                  </Badge>
                </li>
              ))}
            </ul>
          </section>
        )}

        <ol className="grid gap-2" aria-label="Mashqlar natijasi">
          {results.map(({ exerciseId, result }, index) => {
            const exercise = exercises.find((item) => item.id === exerciseId);
            const { icon: Icon, className } = VERDICT_ICONS[result.verdict];
            return (
              <li key={exerciseId} className="flex items-center gap-3 rounded-lg border px-3 py-2 text-sm">
                <Icon className={cn("size-4 shrink-0", className)} aria-label={VERDICT_LABELS[result.verdict]} />
                <span className="text-muted-foreground tabular-nums">{index + 1}.</span>
                <span className="min-w-0 flex-1 truncate">{exercise?.prompt}</span>
                <span className="tabular-nums">{formatPercent(result.score * 100)}</span>
              </li>
            );
          })}
        </ol>
      </CardContent>

      <CardFooter className="flex-wrap gap-2">
        <Button onClick={onRestart}>
          <RotateCcw aria-hidden />
          Qayta ishlash
        </Button>
        <Button asChild variant="outline">
          <Link href="/dashboard">
            <LayoutDashboard aria-hidden />
            Kunlik rejaga qaytish
          </Link>
        </Button>
      </CardFooter>
    </Card>
  );
}
