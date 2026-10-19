import { Circle, CircleCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Progress } from "@/components/ui/progress";
import { cn } from "@/lib/utils";
import type { StudySessionStatus, TodayPlan } from "@/modules/discipline/api";

const SESSION_LABELS: Record<StudySessionStatus, string> = {
  Created: "yaratildi",
  MorningGate: "darvozani kutmoqda",
  Active: "faol",
  FocusBlock: "fokus blokida",
  Completed: "yakunlandi",
  Abandoned: "toʻxtatildi",
};

/** Kunlik rejaning toʻrt bosqichi: hammasi bajarilganda kun yakunlanadi va ertaga keyingi kun ochiladi. */
export function DailyProgressCard({ plan, lessonTitle }: { plan: TodayPlan; lessonTitle: string | undefined }) {
  const steps = [
    { label: "Ertalabki takrorlash", done: plan.morningReviewPassed },
    { label: "Bugungi dars", done: plan.curriculumCompleted },
    { label: "Mashqlar", done: plan.practiceCompleted },
    { label: "Fokus sessiya", done: plan.focusCompleted },
  ];
  const doneCount = steps.filter((step) => step.done).length;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Bugungi reja</CardTitle>
        <CardDescription>
          {plan.dayNumber}-kun{lessonTitle ? `: ${lessonTitle}` : ""}
        </CardDescription>
        <CardAction>
          {plan.dayCompleted ? (
            <Badge className="bg-emerald-500/15 text-emerald-700 dark:text-emerald-400">Kun yakunlandi</Badge>
          ) : (
            <span className="text-sm text-muted-foreground tabular-nums">
              {doneCount}/{steps.length}
            </span>
          )}
        </CardAction>
      </CardHeader>

      <CardContent className="grid gap-4">
        <Progress value={(doneCount / steps.length) * 100} aria-label="Bugungi reja bajarilishi" />
        <ul className="grid gap-2.5">
          {steps.map(({ label, done }) => (
            <li key={label} className={cn("flex items-center gap-2.5 text-sm", !done && "text-muted-foreground")}>
              {done ? (
                <CircleCheck className="size-4 text-emerald-600 dark:text-emerald-400" aria-hidden />
              ) : (
                <Circle className="size-4" aria-hidden />
              )}
              {label}
              <span className="sr-only">{done ? "— bajarildi" : "— bajarilmagan"}</span>
            </li>
          ))}
        </ul>
        {plan.currentSessionState && (
          <p className="text-xs text-muted-foreground">Oʻquv sessiyasi: {SESSION_LABELS[plan.currentSessionState]}</p>
        )}
      </CardContent>
    </Card>
  );
}
