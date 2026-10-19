import { useQuery } from "@tanstack/react-query";

import { curriculumApi } from "./api";

export const curriculumKeys = {
  days: ["curriculum", "days"] as const,
};

/** Kurs mazmuni versiyalangan va kam oʻzgaradi: sessiya davomida bir marta yuklanadi. */
export function useCourseDays() {
  return useQuery({ queryKey: curriculumKeys.days, queryFn: curriculumApi.days, staleTime: 60 * 60_000 });
}
