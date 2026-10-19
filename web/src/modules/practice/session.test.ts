import { describe, expect, it } from "vitest";

import type { AttemptResultDto, Verdict } from "./api";
import {
  currentResult,
  elapsedMs,
  MAX_TIME_TAKEN_MS,
  sessionReducer,
  startSession,
  summarize,
  type AttemptRecord,
  type SessionState,
} from "./session";

function result(verdict: Verdict, score: number, ...errorCodes: (string | null)[]): AttemptResultDto {
  return {
    attemptId: `attempt-${verdict}-${score}`,
    verdict,
    score,
    evaluationTier: 0,
    feedback: errorCodes.map((errorCode) => ({ errorCode, correction: "…", explanation: null })),
  };
}

function graded(state: SessionState, exerciseId: string, verdict: Verdict = "Correct"): SessionState {
  return sessionReducer(state, { type: "graded", exerciseId, result: result(verdict, verdict === "Correct" ? 1 : 0) });
}

describe("practice session", () => {
  it("starts on the first exercise, or straight at the summary when there is nothing to do", () => {
    expect(startSession(3, 1_000)).toEqual({ total: 3, index: 0, phase: "answering", results: [], startedAt: 1_000 });
    expect(startSession(0, 1_000).phase).toBe("summary");
  });

  it("walks answering → feedback → next exercise, restarting the clock for each one", () => {
    let state = startSession(2, 1_000);

    state = graded(state, "e1");
    expect(state).toMatchObject({ index: 0, phase: "feedback" });
    expect(currentResult(state)?.verdict).toBe("Correct");

    state = sessionReducer(state, { type: "next", now: 5_000 });
    expect(state).toMatchObject({ index: 1, phase: "answering", startedAt: 5_000 });
    expect(currentResult(state)).toBeUndefined();
  });

  it("ends with the summary after the last exercise", () => {
    let state = graded(startSession(1, 0), "e1", "Incorrect");

    state = sessionReducer(state, { type: "next", now: 1 });

    expect(state.phase).toBe("summary");
    expect(state.results.map((record) => record.exerciseId)).toEqual(["e1"]);
  });

  it("ignores a grading that arrives when no answer is awaited, and 'next' before grading", () => {
    const answering = startSession(2, 0);
    const feedback = graded(answering, "e1");

    expect(graded(feedback, "e1")).toBe(feedback);
    expect(sessionReducer(answering, { type: "next", now: 1 })).toBe(answering);
    expect(feedback.results).toHaveLength(1);
  });

  it("restarts the same set from the beginning", () => {
    const finished = sessionReducer(graded(startSession(1, 0), "e1"), { type: "next", now: 1 });

    expect(sessionReducer(finished, { type: "restart", now: 9 })).toEqual(startSession(1, 9));
  });

  it("measures time per exercise within the range the API accepts", () => {
    const state = startSession(1, 10_000);

    expect(elapsedMs(state, 14_500.4)).toBe(4_500);
    expect(elapsedMs(state, 9_000)).toBe(0);
    expect(elapsedMs(state, 10_000 + 2 * MAX_TIME_TAKEN_MS)).toBe(MAX_TIME_TAKEN_MS);
  });
});

describe("summarize", () => {
  it("counts verdicts, averages scores and ranks error classes", () => {
    const records: AttemptRecord[] = [
      { exerciseId: "e1", result: result("Correct", 1) },
      { exerciseId: "e2", result: result("PartiallyCorrect", 0.5, "SPELLING") },
      { exerciseId: "e3", result: result("PartiallyCorrect", 0.6, "VOCABULARY", "VOCABULARY") },
      { exerciseId: "e4", result: result("Incorrect", 0, "BE_FORM", null) },
    ];

    expect(summarize(records)).toEqual({
      total: 4,
      correct: 1,
      partial: 2,
      incorrect: 1,
      averageScore: 0.525,
      errorCodes: [
        { code: "VOCABULARY", count: 2 },
        { code: "BE_FORM", count: 1 },
        { code: "SPELLING", count: 1 },
      ],
    });
  });

  it("is all zeros for an empty set", () => {
    expect(summarize([])).toMatchObject({ total: 0, averageScore: 0, errorCodes: [] });
  });
});
