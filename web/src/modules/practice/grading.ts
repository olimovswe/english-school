import type { Verdict } from "./api";

/** Backend `ErrorCodes` taksonomiyasi: kodlar barqaror, nomlari qisqa toifa sifatida. */
const ERROR_CODE_LABELS: Record<string, string> = {
  "3SG_S": "3-shaxs -s qoʻshimchasi",
  ART_MISSING: "Artikl tushib qolgan",
  ART_WRONG: "a / an tanlovi",
  BE_MISSING: "to be tushib qolgan",
  BE_FORM: "to be shakli",
  PRONOUN_POSSESSIVE: "Egalik olmoshi",
  DEMONSTRATIVE: "this / that / these / those",
  PLURAL_FORM: "Koʻplik shakli",
  WORD_ORDER: "Soʻz tartibi",
  VOCABULARY: "Soʻz maʼnosi",
  SPELLING: "Imlo",
  PHONICS: "Oʻqish qoidasi",
};

/** Nomaʼlum (yangi) kod oʻzi koʻrsatiladi: xato yoʻqolib qolmasin. */
export function errorCodeLabel(code: string): string {
  return ERROR_CODE_LABELS[code] ?? code;
}

const TIER_LABELS: Record<number, string> = {
  0: "qoidalar boʻyicha",
  1: "normalizatsiya va imlo",
  2: "grammatika tekshiruvi",
  3: "AI tahlili",
};

export function tierLabel(tier: number): string {
  return `${tier}-daraja${TIER_LABELS[tier] ? `, ${TIER_LABELS[tier]}` : ""}`;
}

export const VERDICT_LABELS: Record<Verdict, string> = {
  Correct: "Toʻgʻri!",
  PartiallyCorrect: "Qisman toʻgʻri",
  Incorrect: "Notoʻgʻri",
};
