/**
 * Brauzerga ochiq sozlamalar. `NEXT_PUBLIC_*` qiymatlari build vaqtida kodga yoziladi, shuning uchun
 * `process.env.NEXT_PUBLIC_…` toʻliq nomi bilan oʻqiladi.
 */
export const env = {
  /** Odatda `/api/v1`: Next.js uni `API_PROXY_TARGET` ga uzatadi (next.config.ts), CORS kerak emas. */
  apiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL || "/api/v1",
  /** Telegram Login Widget boti (@ belgisisiz); boʻsh boʻlsa Telegram tugmasi koʻrsatilmaydi. */
  telegramBotUsername: process.env.NEXT_PUBLIC_TELEGRAM_BOT_USERNAME || null,
  /** `next dev`: token bilan kirish kabi dasturchi vositalari. */
  isDevelopment: process.env.NODE_ENV === "development",
} as const;
