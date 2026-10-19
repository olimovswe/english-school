import type { MatchAnswer } from "./api";

/**
 * Juftlash holati: chap ustundagi element (`optionId`) → tanlangan tarjima. Har bir tarjima faqat bitta
 * juftlikda boʻladi; ikki tomondan istalgan tartibda tanlash mumkin.
 */
export interface MatchingState {
  pairs: Readonly<Record<string, string>>;
  selectedOption: string | null;
  selectedTarget: string | null;
}

export const emptyMatching: MatchingState = { pairs: {}, selectedOption: null, selectedTarget: null };

function withoutTarget(pairs: Readonly<Record<string, string>>, target: string): Record<string, string> {
  return Object.fromEntries(Object.entries(pairs).filter(([, paired]) => paired !== target));
}

/** Chap element bosildi: juftlangan boʻlsa — juftlik bekor qilinib, element tanlanadi. */
export function pickOption(state: MatchingState, optionId: string): MatchingState {
  const pairs = { ...state.pairs };
  const wasPaired = optionId in pairs;
  delete pairs[optionId];

  if (!wasPaired && state.selectedTarget !== null) {
    const target = state.selectedTarget;
    return { pairs: { ...withoutTarget(pairs, target), [optionId]: target }, selectedOption: null, selectedTarget: null };
  }

  const selectedOption = !wasPaired && state.selectedOption === optionId ? null : optionId;
  return { pairs, selectedOption, selectedTarget: wasPaired ? null : state.selectedTarget };
}

/** Oʻng element bosildi: band boʻlsa — avvalgi juftligi bekor qilinadi. */
export function pickTarget(state: MatchingState, target: string): MatchingState {
  const pairs = withoutTarget(state.pairs, target);
  const wasPaired = Object.keys(pairs).length !== Object.keys(state.pairs).length;

  if (state.selectedOption !== null) {
    return { pairs: { ...pairs, [state.selectedOption]: target }, selectedOption: null, selectedTarget: null };
  }

  const selectedTarget = !wasPaired && state.selectedTarget === target ? null : target;
  return { pairs, selectedOption: null, selectedTarget };
}

export function isComplete(state: MatchingState, optionIds: readonly string[]): boolean {
  return optionIds.every((id) => id in state.pairs);
}

export function toMatchAnswers(state: MatchingState, optionIds: readonly string[]): MatchAnswer[] {
  return optionIds.flatMap((optionId) => {
    const matchText = state.pairs[optionId];
    return matchText === undefined ? [] : [{ optionId, matchText }];
  });
}
