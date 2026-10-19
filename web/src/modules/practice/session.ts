import type { AttemptResultDto } from "./api";

/** Backend `AttemptValidation`: sarflangan vaqt 0 – 1 soat. */
export const MAX_TIME_TAKEN_MS = 60 * 60 * 1000;

/** answering → (javob baholandi) → feedback → (keyingi) → answering … oxirgisidan keyin summary. */
export type SessionPhase = "answering" | "feedback" | "summary";

export interface AttemptRecord {
  exerciseId: string;
  result: AttemptResultDto;
}

export interface SessionState {
  total: number;
  /** Joriy mashq (0 dan). */
  index: number;
  phase: SessionPhase;
  /** Baholangan urinishlar, mashqlar tartibida: har bir mashqqa bittadan. */
  results: AttemptRecord[];
  /** Joriy mashq koʻrsatilgan vaqt (ms): `timeTakenMs` shundan hisoblanadi. */
  startedAt: number;
}

export type SessionAction =
  | { type: "graded"; exerciseId: string; result: AttemptResultDto }
  | { type: "next"; now: number }
  | { type: "restart"; now: number };

export function startSession(total: number, now: number): SessionState {
  return { total, index: 0, phase: total === 0 ? "summary" : "answering", results: [], startedAt: now };
}

export function sessionReducer(state: SessionState, action: SessionAction): SessionState {
  switch (action.type) {
    case "graded":
      // Kechikib kelgan yoki takroriy javob: holat faqat javob kutilayotganda oʻzgaradi.
      if (state.phase !== "answering") {
        return state;
      }

      return { ...state, phase: "feedback", results: [...state.results, { exerciseId: action.exerciseId, result: action.result }] };

    case "next":
      if (state.phase !== "feedback") {
        return state;
      }

      return state.index + 1 >= state.total
        ? { ...state, phase: "summary" }
        : { ...state, index: state.index + 1, phase: "answering", startedAt: action.now };

    case "restart":
      return startSession(state.total, action.now);
  }
}

/** Joriy mashqqa sarflangan vaqt, backend qabul qiladigan oraliqda. */
export function elapsedMs(state: SessionState, now: number): number {
  return Math.min(Math.max(0, Math.round(now - state.startedAt)), MAX_TIME_TAKEN_MS);
}

/** Feedback bosqichida joriy mashq natijasi; aks holda undefined. */
export function currentResult(state: SessionState): AttemptResultDto | undefined {
  return state.phase === "feedback" ? state.results[state.results.length - 1]?.result : undefined;
}

export interface SessionSummary {
  total: number;
  correct: number;
  partial: number;
  incorrect: number;
  /** 0–1. */
  averageScore: number;
  /** Uchragan xato toifalari, eng koʻpi birinchi. */
  errorCodes: { code: string; count: number }[];
}

export function summarize(results: readonly AttemptRecord[]): SessionSummary {
  const counts = new Map<string, number>();
  for (const { result } of results) {
    for (const { errorCode } of result.feedback) {
      if (errorCode) {
        counts.set(errorCode, (counts.get(errorCode) ?? 0) + 1);
      }
    }
  }

  const verdicts = results.map(({ result }) => result.verdict);
  return {
    total: results.length,
    correct: verdicts.filter((verdict) => verdict === "Correct").length,
    partial: verdicts.filter((verdict) => verdict === "PartiallyCorrect").length,
    incorrect: verdicts.filter((verdict) => verdict === "Incorrect").length,
    averageScore: results.length === 0 ? 0 : results.reduce((sum, { result }) => sum + result.score, 0) / results.length,
    errorCodes: [...counts]
      .map(([code, count]) => ({ code, count }))
      .sort((a, b) => b.count - a.count || a.code.localeCompare(b.code)),
  };
}
