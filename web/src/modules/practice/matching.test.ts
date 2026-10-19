import { describe, expect, it } from "vitest";

import { emptyMatching, isComplete, pickOption, pickTarget, toMatchAnswers, type MatchingState } from "./matching";

const OPTIONS = ["hello", "goodbye", "name"];

function pairUp(...steps: (["option" | "target", string])[]): MatchingState {
  return steps.reduce(
    (state, [side, value]) => (side === "option" ? pickOption(state, value) : pickTarget(state, value)),
    emptyMatching,
  );
}

describe("matching", () => {
  it("pairs a word with a translation in either order", () => {
    expect(pairUp(["option", "hello"], ["target", "salom"]).pairs).toEqual({ hello: "salom" });
    expect(pairUp(["target", "xayr"], ["option", "goodbye"]).pairs).toEqual({ goodbye: "xayr" });
  });

  it("toggles a selection when the same item is picked twice", () => {
    expect(pairUp(["option", "hello"], ["option", "hello"]).selectedOption).toBeNull();
    expect(pairUp(["target", "ism"], ["target", "ism"]).selectedTarget).toBeNull();
  });

  it("switches the selection to another word on the same side", () => {
    expect(pairUp(["option", "hello"], ["option", "name"]).selectedOption).toBe("name");
  });

  it("unpairs a paired word and selects it, ready to be paired again", () => {
    const state = pairUp(["option", "hello"], ["target", "salom"], ["option", "hello"]);

    expect(state).toEqual({ pairs: {}, selectedOption: "hello", selectedTarget: null });
    expect(pickTarget(state, "xayr").pairs).toEqual({ hello: "xayr" });
  });

  it("frees a translation that is clicked again, so each translation is used once", () => {
    const state = pairUp(["option", "hello"], ["target", "salom"], ["target", "salom"]);
    expect(state).toEqual({ pairs: {}, selectedOption: null, selectedTarget: "salom" });

    expect(pickOption(state, "name").pairs).toEqual({ name: "salom" });
  });

  it("moves a translation to the newly selected word instead of sharing it", () => {
    const state = pairUp(["option", "hello"], ["target", "salom"], ["option", "name"], ["target", "salom"]);

    expect(state.pairs).toEqual({ name: "salom" });
  });

  it("is complete only when every word has a translation, and answers in word order", () => {
    const partial = pairUp(["option", "name"], ["target", "ism"], ["option", "hello"], ["target", "salom"]);
    expect(isComplete(partial, OPTIONS)).toBe(false);

    const complete = pickTarget(pickOption(partial, "goodbye"), "xayr");
    expect(isComplete(complete, OPTIONS)).toBe(true);
    expect(toMatchAnswers(complete, OPTIONS)).toEqual([
      { optionId: "hello", matchText: "salom" },
      { optionId: "goodbye", matchText: "xayr" },
      { optionId: "name", matchText: "ism" },
    ]);
  });
});
