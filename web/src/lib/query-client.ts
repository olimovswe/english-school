import { QueryClient } from "@tanstack/react-query";

const MAX_RETRIES = 2;

/** 4xx javoblar (notoʻgʻri soʻrov, ruxsat yoʻq, topilmadi) qayta urinish bilan oʻzgarmaydi. */
function isClientError(error: unknown): boolean {
  if (typeof error !== "object" || error === null || !("status" in error)) {
    return false;
  }

  const { status } = error as { status: unknown };
  return typeof status === "number" && status >= 400 && status < 500;
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
