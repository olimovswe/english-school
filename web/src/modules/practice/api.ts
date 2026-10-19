import { api } from "@/lib/api/client";

/** Backend `ExerciseType` (JSON'da nomi bilan). */
export type ExerciseType = "GapFill" | "MultipleChoice" | "Matching";

/** Backend `Verdict`. */
export type Verdict = "Correct" | "PartiallyCorrect" | "Incorrect";

export interface ExerciseChoiceDto {
  id: string;
  text: string;
}

/**
 * `GET /practice/days/{n}/exercises`: toʻgʻri javoblar va xato kodlari yashirilgan.
 * - GapFill: `prompt` ichida `___` — boʻsh joy; `choices` va `matchTargets` boʻsh.
 * - MultipleChoice: `choices` — variantlar, alifbo tartibida (tartib javobni bildirmaydi).
 * - Matching: `choices` — chap ustun (inglizcha, muallif tartibida), `matchTargets` — oʻng ustun
 *   (tarjimalar, alifbo tartibida).
 */
export interface ExerciseDto {
  id: string;
  position: number;
  type: ExerciseType;
  instruction: string;
  prompt: string;
  choices: ExerciseChoiceDto[];
  matchTargets: string[];
}

/** Matching javobi: chap ustundagi har bir element (`optionId`) uchun tanlangan tarjima. */
export interface MatchAnswer {
  optionId: string;
  matchText: string;
}

/** Mashq turiga mos bitta javob maydoni. */
export type AttemptAnswer = { answer: string } | { selectedOptionId: string } | { matches: MatchAnswer[] };

/** `POST /practice/attempts`. `timeTakenMs`: 0 – 1 soat. */
export type SubmitAttemptRequest = { exerciseId: string; timeTakenMs: number } & AttemptAnswer;

/**
 * Javobdagi bitta xato: taksonomiya kodi (aniqlanmagan boʻlsa null), toʻgʻri variant va qoida.
 * Backend `FeedbackDto`.
 */
export interface FeedbackDto {
  errorCode: string | null;
  correction: string;
  explanation: string | null;
}

/**
 * Backend `AttemptResultResponse`. `score` 0–1 (imlo xatosi 0,5; Matching — toʻgʻri juftliklar ulushi).
 * `evaluationTier`: 0 — qoidalar, 1 — normalizatsiya va imlo, 2–3 — AiGateway.
 */
export interface AttemptResultDto {
  attemptId: string;
  verdict: Verdict;
  score: number;
  evaluationTier: number;
  feedback: FeedbackDto[];
}

export const practiceApi = {
  exercises: (dayNumber: number) => api.get(`practice/days/${dayNumber}/exercises`).json<ExerciseDto[]>(),
  submitAttempt: (request: SubmitAttemptRequest) =>
    api.post("practice/attempts", { json: request }).json<AttemptResultDto>(),
};
