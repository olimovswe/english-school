import { skipToken, useMutation, useQuery } from "@tanstack/react-query";

import { practiceApi } from "./api";

export const practiceKeys = {
  exercises: (dayNumber: number | undefined) => ["practice", "exercises", dayNumber] as const,
};

/** Kun mashqlari; `dayNumber` hali nomaʼlum boʻlsa (kunlik reja yuklanmoqda) soʻrov yuborilmaydi. */
export function useExerciseListQuery(dayNumber: number | undefined) {
  return useQuery({
    queryKey: practiceKeys.exercises(dayNumber),
    queryFn: dayNumber === undefined ? skipToken : () => practiceApi.exercises(dayNumber),
    // Mashqlar kurs versiyasiga bogʻlangan va oʻzgarmaydi.
    staleTime: 60 * 60_000,
    select: (exercises) => [...exercises].sort((a, b) => a.position - b.position),
  });
}

/** Har bir urinish alohida baholanadi; qayta urinishni foydalanuvchi oʻzi qiladi (avtomatik emas). */
export function useSubmitAttemptMutation() {
  return useMutation({ mutationFn: practiceApi.submitAttempt });
}
