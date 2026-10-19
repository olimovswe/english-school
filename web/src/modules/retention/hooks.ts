import { useQuery } from "@tanstack/react-query";

import { retentionApi } from "./api";

export const retentionKeys = {
  dueCount: ["retention", "due", "count"] as const,
};

/** Ertalabki darvoza uchun nechta kartochka takrorlanishi kerak. */
export function useDueCount() {
  return useQuery({
    queryKey: retentionKeys.dueCount,
    queryFn: () => retentionApi.due(1),
    select: (due) => due.totalDue,
  });
}
