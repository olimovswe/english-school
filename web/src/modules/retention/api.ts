import { api } from "@/lib/api/client";

export interface DueCard {
  id: string;
  kind: string;
  /** Inglizcha tomoni. */
  front: string;
  /** Oʻzbekcha tomoni: darvozada aynan shu javob kutiladi. */
  back: string;
  phase: string;
  dueAt: string;
  reps: number;
  lapses: number;
}

/** `GET /retention/due?limit=`: `totalDue` — hozir navbatdagi hamma kartochkalar, `cards` — birinchi partiya. */
export interface DueCards {
  totalDue: number;
  cards: DueCard[];
}

export const retentionApi = {
  due: (limit: number) => api.get("retention/due", { searchParams: { limit } }).json<DueCards>(),
};
