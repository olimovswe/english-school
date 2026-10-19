"use client";

import { ArrowRight } from "lucide-react";
import { useId, useReducer } from "react";

import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { errorMessage, fieldErrorsOf } from "@/lib/api/errors";

import type { AttemptAnswer, ExerciseDto } from "../api";
import { useSubmitAttemptMutation } from "../hooks";
import { currentResult, elapsedMs, sessionReducer, startSession } from "../session";
import { ExercisePlayer } from "./exercise-player";
import { GradingFeedbackBanner } from "./grading-feedback-banner";
import { SessionSummary } from "./session-summary";

interface PracticeSessionProps {
  dayNumber: number;
  exercises: readonly ExerciseDto[];
}

/**
 * Bir partiya mashq: har biriga javob → darhol baho → "Keyingi mashq" → … → yakun. Fokus oqimi klaviatura
 * uchun: yangi mashq ochilganda uning birinchi maydoni, baholangach "Keyingi mashq" tugmasi.
 */
export function PracticeSession({ dayNumber, exercises }: PracticeSessionProps) {
  const [state, dispatch] = useReducer(sessionReducer, exercises.length, (total) => startSession(total, Date.now()));
  const submit = useSubmitAttemptMutation();
  const feedbackId = useId();
  const exercise = exercises[state.index];
  const result = currentResult(state);
  const isLast = state.index + 1 >= state.total;
  // Validatsiya (400) xabari maydon nomi bilan keladi; kartada bitta umumiy xabar sifatida koʻrsatiladi.
  const error = submit.error ? (Object.values(fieldErrorsOf(submit.error))[0] ?? errorMessage(submit.error)) : null;

  function onSubmit(answer: AttemptAnswer) {
    if (!exercise || submit.isPending) {
      return;
    }

    submit.mutate(
      { exerciseId: exercise.id, timeTakenMs: elapsedMs(state, Date.now()), ...answer },
      { onSuccess: (graded) => dispatch({ type: "graded", exerciseId: exercise.id, result: graded }) },
    );
  }

  function next() {
    submit.reset();
    dispatch({ type: "next", now: Date.now() });
  }

  if (state.phase === "summary" || !exercise) {
    return (
      <SessionSummary
        dayNumber={dayNumber}
        exercises={exercises}
        results={state.results}
        onRestart={() => {
          submit.reset();
          dispatch({ type: "restart", now: Date.now() });
        }}
      />
    );
  }

  return (
    <div className="grid gap-6">
      <div className="grid gap-2">
        <div className="flex items-baseline justify-between gap-4 text-sm">
          <p className="font-medium tabular-nums">
            Mashq {state.index + 1} / {state.total}
          </p>
          <p className="text-muted-foreground tabular-nums">Bajarildi: {state.results.length}</p>
        </div>
        <Progress
          value={(state.results.length / state.total) * 100}
          aria-label={`Bajarildi: ${state.results.length} / ${state.total}`}
        />
      </div>

      <ExercisePlayer
        key={exercise.id}
        exercise={exercise}
        result={result}
        submitting={submit.isPending}
        error={error}
        onSubmit={onSubmit}
      />

      {result && (
        <div className="grid gap-4">
          <GradingFeedbackBanner id={feedbackId} result={result} />
          {/* Baholangach fokus shu yerga: Enter keyingisiga oʻtadi, ekran oʻquvchi natijani oʻqiydi. */}
          <Button autoFocus size="lg" className="w-fit" aria-describedby={feedbackId} onClick={next}>
            {isLast ? "Natijalarni koʻrish" : "Keyingi mashq"}
            <ArrowRight aria-hidden />
          </Button>
        </div>
      )}
    </div>
  );
}
