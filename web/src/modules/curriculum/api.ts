import { publicApi } from "@/lib/api/client";

/** `GET /curriculum/days`: kurs tuzilmasi, hammaga ochiq. */
export interface CourseDaySummary {
  dayNumber: number;
  weekNumber: number;
  kind: string;
  title: string;
}

export const curriculumApi = {
  days: () => publicApi.get("curriculum/days").json<CourseDaySummary[]>(),
};
