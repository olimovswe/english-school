import { api } from "@/lib/api/client";

/** Backend `DailyPlan.MorningReviewPassPercentage`: dars shu natijadan boshlab ochiladi. */
export const GATE_PASS_PERCENTAGE = 80;

export type StudySessionStatus = "Created" | "MorningGate" | "Active" | "FocusBlock" | "Completed" | "Abandoned";

/** `GET /discipline/today`; kurs tugagan boʻlsa 404. */
export interface TodayPlan {
  dayNumber: number;
  /** UTC sana, `YYYY-MM-DD`. */
  assignedDay: string;
  morningReviewPassed: boolean;
  /** Oxirgi darvoza natijasi (0–100); bugun hali urinilmagan boʻlsa null. */
  gatePassPercentage: number | null;
  lessonsUnlocked: boolean;
  curriculumCompleted: boolean;
  practiceCompleted: boolean;
  focusCompleted: boolean;
  dayCompleted: boolean;
  currentSessionState: StudySessionStatus | null;
}

export interface GateAnswer {
  cardId: string;
  answer: string;
}

/** `POST /discipline/gate/submit`; `assessmentValid: false` — javoblar navbatdagi kartochkalarga mos emas. */
export interface GateResult {
  dayNumber: number;
  assessmentValid: boolean;
  score: number;
  correctAnswers: number;
  totalCards: number;
  passed: boolean;
}

export const disciplineApi = {
  today: () => api.get("discipline/today").json<TodayPlan>(),
  submitGate: (answers: GateAnswer[]) => api.post("discipline/gate/submit", { json: { answers } }).json<GateResult>(),
};
