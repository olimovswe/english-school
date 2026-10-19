import { CircleAlert, CircleCheck, CircleX, type LucideIcon } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { formatPercent } from "@/lib/format";
import { cn } from "@/lib/utils";

import type { AttemptResultDto, FeedbackDto, Verdict } from "../api";
import { errorCodeLabel, tierLabel, VERDICT_LABELS } from "../grading";

const TONES: Record<Verdict, { icon: LucideIcon; box: string; iconClass: string }> = {
  Correct: { icon: CircleCheck, box: "border-emerald-500/40 bg-emerald-500/10", iconClass: "text-emerald-600 dark:text-emerald-400" },
  PartiallyCorrect: { icon: CircleAlert, box: "border-amber-500/40 bg-amber-500/10", iconClass: "text-amber-600 dark:text-amber-400" },
  Incorrect: { icon: CircleX, box: "border-destructive/40 bg-destructive/10", iconClass: "text-destructive" },
};

/** Bir xil xato kodli izohlar bitta toifa ostida (Matching'da har bir notoʻgʻri juftlik — VOCABULARY). */
function groupByErrorCode(feedback: readonly FeedbackDto[]): [string | null, FeedbackDto[]][] {
  const groups = new Map<string | null, FeedbackDto[]>();
  for (const item of feedback) {
    groups.set(item.errorCode, [...(groups.get(item.errorCode) ?? []), item]);
  }

  return [...groups];
}

/** Baholash natijasi: hukm, ball, xato toifasi, toʻgʻri javob va qoida, qaysi daraja baholagani. */
export function GradingFeedbackBanner({ result, id }: { result: AttemptResultDto; id?: string }) {
  const tone = TONES[result.verdict];
  const Icon = tone.icon;

  return (
    <section id={id} role="status" aria-label="Natija" className={cn("grid gap-3 rounded-xl border p-4", tone.box)}>
      <div className="flex flex-wrap items-center justify-between gap-2">
        <p className="flex items-center gap-2 font-heading text-base font-semibold">
          <Icon className={cn("size-5", tone.iconClass)} aria-hidden />
          {VERDICT_LABELS[result.verdict]}
        </p>
        <p className="text-sm">
          Ball: <span className="font-semibold tabular-nums">{formatPercent(result.score * 100)}</span>
        </p>
      </div>

      {groupByErrorCode(result.feedback).map(([code, items]) => (
        <div key={code ?? "general"} className="grid gap-1.5 text-sm">
          {code && (
            <Badge variant="outline" className="bg-background/60">
              {errorCodeLabel(code)}
            </Badge>
          )}
          <ul className="grid gap-1.5">
            {items.map((item) => (
              <li key={item.correction}>
                <span className="text-muted-foreground">Toʻgʻri javob: </span>
                <span className="font-medium">{item.correction}</span>
                {item.explanation && <p className="text-muted-foreground">{item.explanation}</p>}
              </li>
            ))}
          </ul>
        </div>
      ))}

      <p className="text-xs text-muted-foreground">Baholash: {tierLabel(result.evaluationTier)}</p>
    </section>
  );
}
