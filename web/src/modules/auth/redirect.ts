export const AFTER_SIGN_IN_PATH = "/dashboard";

/** `?next=` faqat shu saytdagi yoʻl boʻlishi mumkin: `//evil.example` yoki toʻliq URL ochiq redirect boʻlardi. */
export function safeRedirectPath(next: string | null): string {
  if (!next || !next.startsWith("/") || next.startsWith("//") || next.startsWith("/\\")) {
    return AFTER_SIGN_IN_PATH;
  }

  return next;
}

export function signInPath(returnTo: string): string {
  return `/login?next=${encodeURIComponent(returnTo)}`;
}
