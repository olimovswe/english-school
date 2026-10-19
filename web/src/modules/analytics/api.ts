import { api } from "@/lib/api/client";

/**
 * `GET /analytics/progress`: boshqa modullar eventlaridan Worker quradigan oʻqish modeli, shuning uchun
 * yangi natijalar bir necha soniya kechikib koʻrinadi. Yangi oʻquvchida hammasi 0.
 */
export interface ProgressSummary {
  userId: string;
  currentDay: number;
  focusMinutesThisWeek: number;
  /** 0–1. */
  reviewAccuracy: number;
  activeVocabulary: number;
  completedDays: number;
  totalPracticeAttempts: number;
  activeStreak: number;
}

export const analyticsApi = {
  progress: () => api.get("analytics/progress").json<ProgressSummary>(),
};
