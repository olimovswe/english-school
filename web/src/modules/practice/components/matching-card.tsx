"use client";

import { useId, useRef, useState, type FormEvent, type MouseEvent } from "react";

import { cn } from "@/lib/utils";

import type { AttemptResultDto, ExerciseDto } from "../api";
import { emptyMatching, isComplete, pickOption, pickTarget, toMatchAnswers, type MatchingState } from "../matching";
import { ExerciseFrame, type ExerciseCardProps } from "./exercise-frame";

/** Baholangach notoʻgʻri juftliklar: backend har biri uchun `"hello — salom"` koʻrinishida toʻgʻrisini qaytaradi. */
function wrongOptionIds(exercise: ExerciseDto, result: AttemptResultDto | undefined): ReadonlySet<string> {
  if (!result) {
    return new Set();
  }

  return new Set(
    exercise.choices
      .filter((choice) => result.feedback.some((item) => item.correction.startsWith(`${choice.text} — `)))
      .map((choice) => choice.id),
  );
}

const ITEM_CLASS =
  "flex min-h-10 w-full items-center gap-2 rounded-lg border px-2.5 py-2 text-left text-sm transition-colors outline-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-default";

function PairNumber({ value }: { value: number | null }) {
  return (
    <span
      aria-hidden
      className={cn(
        "flex size-5 shrink-0 items-center justify-center rounded-full text-xs tabular-nums",
        value === null ? "border border-dashed border-muted-foreground/50" : "bg-foreground text-background",
      )}
    >
      {value}
    </span>
  );
}

/**
 * Soʻzlarni tarjimasi bilan juftlash: chapdan soʻz, oʻngdan tarjima (yoki teskarisi). Juftlangan element
 * yana bosilsa juftlik bekor boʻladi. Hammasi tugma — Tab va Enter/Space bilan ham ishlaydi; juftlik
 * hosil boʻlgach fokus keyingi juftlanmagan soʻzga oʻtadi.
 */
export function MatchingCard({ exercise, result, submitting = false, error, onSubmit }: ExerciseCardProps) {
  const [state, setState] = useState<MatchingState>(emptyMatching);
  const [attempted, setAttempted] = useState(false);
  const optionRefs = useRef(new Map<string, HTMLButtonElement>());
  const titleId = useId();
  const hintId = useId();
  const problemId = useId();
  const graded = result !== undefined;
  const locked = graded || submitting;
  const optionIds = exercise.choices.map((choice) => choice.id);
  const wrong = wrongOptionIds(exercise, result);
  const missing = attempted && !isComplete(state, optionIds);

  function apply(next: MatchingState, event: MouseEvent<HTMLButtonElement>) {
    const paired = Object.keys(next.pairs).length > Object.keys(state.pairs).length;
    setState(next);
    if (!paired) {
      return;
    }

    const nextOpen = optionIds.find((id) => !(id in next.pairs));
    if (nextOpen) {
      optionRefs.current.get(nextOpen)?.focus();
    } else {
      event.currentTarget.form?.querySelector<HTMLButtonElement>("button[type=submit]")?.focus();
    }
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (locked) {
      return;
    }

    setAttempted(true);
    if (isComplete(state, optionIds)) {
      onSubmit({ matches: toMatchAnswers(state, optionIds) });
    }
  }

  function tone(optionId: string | undefined): string | false {
    if (!graded || optionId === undefined) {
      return false;
    }

    return wrong.has(optionId)
      ? "border-destructive bg-destructive/10"
      : "border-emerald-500 bg-emerald-500/10";
  }

  return (
    <ExerciseFrame
      exercise={exercise}
      title={exercise.prompt}
      titleId={titleId}
      graded={graded}
      submitting={submitting}
      error={error}
      onSubmit={submit}
    >
      {!graded && (
        <p id={hintId} className="text-sm text-muted-foreground">
          Chapdan soʻzni, keyin oʻngdan uning tarjimasini tanlang. Juftlikni bekor qilish uchun uni yana bosing.
        </p>
      )}
      <div className="grid grid-cols-2 gap-3">
        <div role="group" aria-label="Inglizcha soʻzlar" aria-describedby={graded ? undefined : hintId} className="grid content-start gap-2">
          {exercise.choices.map((choice, index) => {
            const pairedWith = state.pairs[choice.id];
            const selected = state.selectedOption === choice.id;
            return (
              <button
                key={choice.id}
                type="button"
                ref={(element) => {
                  if (element) {
                    optionRefs.current.set(choice.id, element);
                  } else {
                    optionRefs.current.delete(choice.id);
                  }
                }}
                aria-pressed={selected}
                disabled={locked}
                // Yangi mashq ochilganda klaviatura darhol birinchi soʻzda boʻlsin.
                autoFocus={index === 0}
                onClick={(event) => apply(pickOption(state, choice.id), event)}
                className={cn(
                  ITEM_CLASS,
                  !locked && "hover:bg-muted/50",
                  pairedWith !== undefined && "bg-muted",
                  selected && "border-primary ring-2 ring-primary/30",
                  tone(pairedWith === undefined ? undefined : choice.id),
                )}
              >
                <PairNumber value={index + 1} />
                <span className="flex-1">{choice.text}</span>
                {pairedWith !== undefined && <span className="sr-only">, juftligi: {pairedWith}</span>}
              </button>
            );
          })}
        </div>

        <div role="group" aria-label="Tarjimalar" className="grid content-start gap-2">
          {exercise.matchTargets.map((target) => {
            const owner = Object.keys(state.pairs).find((optionId) => state.pairs[optionId] === target);
            const ownerIndex = owner === undefined ? -1 : optionIds.indexOf(owner);
            const selected = state.selectedTarget === target;
            return (
              <button
                key={target}
                type="button"
                aria-pressed={selected}
                disabled={locked}
                onClick={(event) => apply(pickTarget(state, target), event)}
                className={cn(
                  ITEM_CLASS,
                  !locked && "hover:bg-muted/50",
                  owner !== undefined && "bg-muted",
                  selected && "border-primary ring-2 ring-primary/30",
                  tone(owner),
                )}
              >
                <PairNumber value={ownerIndex === -1 ? null : ownerIndex + 1} />
                <span className="flex-1">{target}</span>
                {owner !== undefined && (
                  <span className="sr-only">, juftligi: {exercise.choices[ownerIndex]?.text}</span>
                )}
              </button>
            );
          })}
        </div>
      </div>
      {missing && (
        <p id={problemId} role="alert" className="text-sm text-destructive">
          Har bir soʻzga tarjimasini tanlang.
        </p>
      )}
    </ExerciseFrame>
  );
}
