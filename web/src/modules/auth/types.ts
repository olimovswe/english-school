/** `GET /identity/me` */
export interface UserProfile {
  id: string;
  email: string | null;
  displayName: string;
  timeZone: string;
  telegramId: number | null;
  createdAt: string;
}

export interface Credentials {
  email: string;
  password: string;
}

export interface Registration extends Credentials {
  displayName: string;
  /** IANA vaqt zonasi; boʻlmasa server standartini oladi. */
  timeZone?: string;
}

/**
 * Telegram Login Widget foydalanuvchi obyekti. API'ga oʻzgarishsiz yuboriladi: `hash` aynan shu maydonlar
 * ustidan hisoblangan, bitta maydon qoʻshilsa yoki tushib qolsa imzo tekshiruvdan oʻtmaydi.
 */
export interface TelegramAuthData {
  id: number;
  first_name: string;
  last_name?: string;
  username?: string;
  photo_url?: string;
  auth_date: number;
  hash: string;
}
