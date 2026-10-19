import { describe, expect, it } from "vitest";

import { formatCalendarDate, formatPercent } from "./format";

describe("formatCalendarDate", () => {
  it.each([
    ["2026-10-19", "Dushanba, 19-oktabr"],
    ["2026-01-04", "Yakshanba, 4-yanvar"],
    ["2026-12-31", "Payshanba, 31-dekabr"],
  ])("%s → %s", (isoDate, expected) => {
    expect(formatCalendarDate(isoDate)).toBe(expected);
  });
});

describe("formatPercent", () => {
  it("rounds and keeps the sign on the same line", () => {
    expect(formatPercent(83.33)).toBe("83 %");
  });
});
