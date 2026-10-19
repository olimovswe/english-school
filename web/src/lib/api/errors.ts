import { isHTTPError, isNetworkError, isTimeoutError } from "ky";

/** RFC 9457 ProblemDetails: API xatolari shu koʻrinishda, `detail` esa oʻzbekcha va foydalanuvchiga mos. */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  /** 400 validatsiya xatolari: camelCase maydon → xabarlar. */
  errors?: Record<string, string[]>;
}

export function statusOf(error: unknown): number | null {
  return isHTTPError(error) ? error.response.status : null;
}

export function problemOf(error: unknown): ProblemDetails | null {
  if (!isHTTPError(error)) {
    return null;
  }

  return typeof error.data === "object" && error.data !== null ? (error.data as ProblemDetails) : null;
}

/** Har bir maydon uchun birinchi validatsiya xabari. */
export function fieldErrorsOf(error: unknown): Record<string, string> {
  const errors = problemOf(error)?.errors ?? {};
  return Object.fromEntries(
    Object.entries(errors).flatMap(([field, messages]) => (messages[0] ? [[field, messages[0]]] : [])),
  );
}

/** Istalgan xatoni foydalanuvchiga koʻrsatiladigan oʻzbekcha matnga aylantiradi. */
export function errorMessage(error: unknown): string {
  if (isHTTPError(error)) {
    const problem = problemOf(error);
    const status = error.response.status;
    if (problem?.detail) {
      return problem.detail;
    }

    if (problem?.errors && Object.keys(problem.errors).length > 0) {
      return "Kiritilgan maʼlumotlarni tekshiring.";
    }

    if (status === 401) {
      return "Sessiya tugadi. Qayta kiring.";
    }

    if (status === 429) {
      return "Juda koʻp soʻrov. Birozdan keyin qayta urinib koʻring.";
    }

    if (status >= 500) {
      return "Serverda xatolik yuz berdi. Birozdan keyin qayta urinib koʻring.";
    }

    return `Soʻrov bajarilmadi (${status}).`;
  }

  if (isTimeoutError(error)) {
    return "Server oʻz vaqtida javob bermadi. Qayta urinib koʻring.";
  }

  if (isNetworkError(error)) {
    return "Server bilan aloqa yoʻq. Internet va API ishlayotganini tekshiring.";
  }

  return error instanceof Error && error.message ? error.message : "Kutilmagan xatolik yuz berdi.";
}
