"use client";

import { CircleCheck, CircleX } from "lucide-react";
import { useId, useState, type FormEvent, type KeyboardEvent } from "react";

import { cn } from "@/lib/utils";

import type { AttemptResultDto, ExerciseChoiceDto } from "../api";
import { ExerciseFrame, type ExerciseCardProps } from "./exercise-frame";

type Mark = "correct" | "wrong" | null;

/** Baholangach: tanlangan variant toʻgʻri yoki notoʻgʻri, notoʻgʻri boʻlsa toʻgʻrisi (`correction` matni) ham belgilanadi. */
function markOf(choice: ExerciseChoiceDto, selectedId: string | null, result: AttemptResultDto | undefined): Mark {
  if (!result) {
    return null;
  }

  if (choice.id === selectedId) {
    return result.verdict === "Correct" ? "correct" : "wrong";
  }

  return result.verdict !== "Correct" && result.feedback.some((item) => item.correction === choice.text) ? "correct" : null;
}

/** Enter tanlangan variantni yuboradi (brauzerlar radio'da buni turlicha qiladi). */
function submitOnEnter(event: KeyboardEvent<HTMLInputElement>) {
  if (event.key === "Enter") {
    event.preventDefault();
    event.currentTarget.form?.requestSubmit();
  }
}

/** Bitta toʻgʻri variant: oddiy radio tugmalar — strelkalar bilan tanlash, Enter bilan yuborish. */
export function MultipleChoiceCard({ exercise, result, submitting = false, error, onSubmit }: ExerciseCardProps) {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [attempted, setAttempted] = useState(false);
  const titleId = useId();
  const problemId = useId();
  const graded = result !== undefined;
  const missing = attempted && selectedId === null;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (graded || submitting) {
      return;
    }

    setAttempted(true);
    if (selectedId !== null) {
      onSubmit({ selectedOptionId: selectedId });
    }
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
      <fieldset className="grid gap-2" aria-describedby={missing ? problemId : undefined}>
        <legend className="sr-only">Variantlar</legend>
        {exercise.choices.map((choice, index) => {
          const mark = markOf(choice, selectedId, result);
          return (
            <label
              key={choice.id}
              className={cn(
                "flex items-center gap-3 rounded-lg border px-3 py-2.5 text-sm transition-colors",
                "has-checked:border-primary has-checked:bg-muted has-focus-visible:ring-3 has-focus-visible:ring-ring/50",
                graded ? "cursor-default" : "cursor-pointer hover:bg-muted/50",
                mark === "correct" && "border-emerald-500 bg-emerald-500/10 has-checked:border-emerald-500 has-checked:bg-emerald-500/10",
                mark === "wrong" && "border-destructive bg-destructive/10 has-checked:border-destructive has-checked:bg-destructive/10",
              )}
            >
              <input
                type="radio"
                name={`choice-${exercise.id}`}
                value={choice.id}
                checked={selectedId === choice.id}
                onChange={() => setSelectedId(choice.id)}
                onKeyDown={submitOnEnter}
                disabled={graded || submitting}
                autoFocus={index === 0}
                className="size-4 shrink-0 accent-primary"
              />
              <span className="flex-1">{choice.text}</span>
              {mark === "correct" && (
                <CircleCheck className="size-4 text-emerald-600 dark:text-emerald-400" aria-label="toʻgʻri javob" />
              )}
              {mark === "wrong" && <CircleX className="size-4 text-destructive" aria-label="notoʻgʻri javob" />}
            </label>
          );
        })}
      </fieldset>
      {missing && (
        <p id={problemId} role="alert" className="text-sm text-destructive">
          Variantlardan birini tanlang.
        </p>
      )}
    </ExerciseFrame>
  );
}
