"use client";

import { useId, useState, type FormEvent } from "react";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cn } from "@/lib/utils";

import type { Verdict } from "../api";
import { ExerciseFrame, type ExerciseCardProps } from "./exercise-frame";

/** Backend `AttemptValidation.MaxAnswerLength`. */
export const MAX_ANSWER_LENGTH = 200;

const CYRILLIC = /[Ѐ-ӿ]/;

/** Yuborishdan oldingi tekshiruv; javob yaroqli boʻlsa null. */
export function validateGapAnswer(text: string): string | null {
  const answer = text.trim();
  if (!answer) {
    return "Javobni yozing.";
  }

  // Rus klaviaturasida yozilgan javob baribir notoʻgʻri: urinish va xato hisobiga yozilmasin.
  if (CYRILLIC.test(answer)) {
    return "Javobni lotin harflarida, ingliz klaviaturasida yozing.";
  }

  return answer.length > MAX_ANSWER_LENGTH ? `Javob ${MAX_ANSWER_LENGTH} belgidan oshmasin.` : null;
}

const BLANK_TONE: Record<Verdict, string> = {
  Correct: "border-emerald-500 text-emerald-700 dark:text-emerald-400",
  PartiallyCorrect: "border-amber-500 text-amber-700 dark:text-amber-400",
  Incorrect: "border-destructive text-destructive",
};

/** Boʻsh joyni toʻldirish: savoldagi `___` yozilayotgan javobni koʻrsatadi. */
export function FillInTheBlankCard({ exercise, result, submitting = false, error, onSubmit }: ExerciseCardProps) {
  const [text, setText] = useState("");
  const [attempted, setAttempted] = useState(false);
  const titleId = useId();
  const inputId = useId();
  const problemId = useId();
  const graded = result !== undefined;
  const problem = validateGapAnswer(text);
  // Boʻsh javob faqat yuborishga urinilganda aytiladi; kirill harfi esa darhol (klaviatura tilini almashtirish kerak).
  const shownProblem = attempted || CYRILLIC.test(text) ? problem : null;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (graded || submitting) {
      return;
    }

    setAttempted(true);
    if (problem === null) {
      onSubmit({ answer: text.trim() });
    }
  }

  const [before, ...after] = exercise.prompt.split("___");
  const title =
    after.length === 0 ? (
      exercise.prompt
    ) : (
      <>
        {before}
        <span
          className={cn(
            "mx-0.5 inline-block min-w-16 border-b-2 border-dashed border-muted-foreground/60 px-1 text-center",
            text && "border-solid border-foreground",
            result && BLANK_TONE[result.verdict],
          )}
        >
          {text.trim() || <span className="sr-only">boʻsh joy</span>}
        </span>
        {after.join("___")}
      </>
    );

  return (
    <ExerciseFrame
      exercise={exercise}
      title={title}
      titleId={titleId}
      graded={graded}
      submitting={submitting}
      error={error}
      onSubmit={submit}
    >
      <div className="grid gap-2">
        <Label htmlFor={inputId}>Javobingiz</Label>
        <Input
          id={inputId}
          value={text}
          onChange={(event) => setText(event.target.value)}
          readOnly={graded || submitting}
          // Yangi mashq ochilganda darhol yozish mumkin boʻlsin.
          autoFocus
          autoComplete="off"
          autoCapitalize="off"
          autoCorrect="off"
          spellCheck={false}
          maxLength={MAX_ANSWER_LENGTH}
          aria-invalid={shownProblem ? true : undefined}
          aria-describedby={shownProblem ? problemId : undefined}
          className="max-w-sm"
        />
        {shownProblem && (
          <p id={problemId} className="text-sm text-destructive">
            {shownProblem}
          </p>
        )}
      </div>
    </ExerciseFrame>
  );
}
