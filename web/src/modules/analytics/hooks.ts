import { useQuery } from "@tanstack/react-query";

import { analyticsApi } from "./api";

export const analyticsKeys = {
  progress: ["analytics", "progress"] as const,
};

export function useProgress() {
  return useQuery({ queryKey: analyticsKeys.progress, queryFn: analyticsApi.progress });
}
