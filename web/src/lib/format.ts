// Intl'ga tayanmaymiz: Chrome'ning ICU toʻplamida oʻzbek (uz) maʼlumoti yoʻq va "M10 19, Mon" chiqaradi.
const MONTHS = ["yanvar", "fevral", "mart", "aprel", "may", "iyun", "iyul", "avgust", "sentabr", "oktabr", "noyabr", "dekabr"];
const WEEKDAYS = ["Yakshanba", "Dushanba", "Seshanba", "Chorshanba", "Payshanba", "Juma", "Shanba"];

/** `2026-10-19` → `Dushanba, 19-oktabr`. Sana kalendar kuni sifatida oʻqiladi, vaqt zonasi uni surmaydi. */
export function formatCalendarDate(isoDate: string): string {
  const date = new Date(`${isoDate}T00:00:00Z`);
  return `${WEEKDAYS[date.getUTCDay()]}, ${date.getUTCDate()}-${MONTHS[date.getUTCMonth()]}`;
}

/** Raqam va % orasida boʻlinmas boʻshliq: qator oxirida "80" va "%" ajralib qolmaydi. */
export function formatPercent(value: number): string {
  return `${Math.round(value)} %`;
}
