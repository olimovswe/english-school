import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { disciplineApi } from "./api";

export const disciplineKeys = {
  today: ["discipline", "today"] as const,
};

export function useTodayPlan() {
  return useQuery({ queryKey: disciplineKeys.today, queryFn: disciplineApi.today });
}

export function useSubmitGate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: disciplineApi.submitGate,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: disciplineKeys.today }),
  });
}
