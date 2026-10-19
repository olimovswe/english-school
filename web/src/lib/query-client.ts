import { QueryClient } from "@tanstack/react-query";

import { statusOf } from "@/lib/api/errors";

const MAX_RETRIES = 2;

/** 4xx javoblar (notoʻgʻri soʻrov, ruxsat yoʻq, topilmadi) qayta urinish bilan oʻzgarmaydi; 429 esa kutishni soʻraydi. */
function isClientError(error: unknown): boolean {
  const status = statusOf(error);
  return status !== null && status >= 400 && status < 500;
}

export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        retry: (failureCount, error) => !isClientError(error) && failureCount < MAX_RETRIES,
      },
      mutations: {
        retry: false,
      },
    },
  });
}
