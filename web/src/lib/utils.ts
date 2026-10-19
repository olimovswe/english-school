import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/** Shartli klasslarni birlashtiradi; Tailwind ziddiyatlarida oxirgisi yutadi (`px-2 px-4` → `px-4`). */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
