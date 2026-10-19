// @vitest-environment jsdom
import { QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { createQueryClient } from "@/lib/query-client";

import { practiceApi, type AttemptResultDto, type ExerciseDto } from "../api";
import { PracticeSession } from "./practice-session";

// Faqat tarmoq chaqiruvi soxta: sessiya, mutation va kartalar haqiqiy.
vi.mock("../api", () => ({ practiceApi: { exercises: vi.fn(), submitAttempt: vi.fn() } }));
vi.mock("next/link", () => ({
  default: ({ href, children, ...rest }: { href: string; children: ReactNode }) => (
    <a href={href} {...rest}>
      {children}
    </a>
  ),
}));

const exercises: ExerciseDto[] = [
  {
    id: "mc",
    position: 1,
    type: "MultipleChoice",
    instruction: "Toʻgʻri javobni tanlang.",
    prompt: "Qaysi soʻzda «magic e» bor?",
    choices: [
      { id: "cap", text: "cap" },
      { id: "cape", text: "cape" },
    ],
    matchTargets: [],
  },
  {
    id: "gap",
    position: 2,
    type: "GapFill",
    instruction: "Boʻsh joyni toʻldiring.",
    prompt: "— ___! My name is Ali.",
    choices: [],
    matchTargets: [],
  },
  {
    id: "match",
    position: 3,
    type: "Matching",
    instruction: "Soʻzlarni tarjimasi bilan juftlang.",
    prompt: "1-kun soʻzlari",
    choices: [
      { id: "o1", text: "hello" },
      { id: "o2", text: "goodbye" },
    ],
    matchTargets: ["salom", "xayr"],
  },
];

const results: Record<string, AttemptResultDto> = {
  mc: {
    attemptId: "a1",
    verdict: "Incorrect",
    score: 0,
    evaluationTier: 0,
    feedback: [{ errorCode: "PHONICS", correction: "cape", explanation: "cap — qisqa /æ/, oxirida e yoʻq." }],
  },
  gap: {
    attemptId: "a2",
    verdict: "PartiallyCorrect",
    score: 0.5,
    evaluationTier: 1,
    feedback: [{ errorCode: "SPELLING", correction: "Hello", explanation: "Imlo xatosi: toʻgʻri yozilishi — Hello." }],
  },
  match: { attemptId: "a3", verdict: "Correct", score: 1, evaluationTier: 0, feedback: [] },
};

const submitAttempt = vi.mocked(practiceApi.submitAttempt);

function renderSession(items: ExerciseDto[] = exercises) {
  return render(
    <QueryClientProvider client={createQueryClient()}>
      <PracticeSession dayNumber={1} exercises={items} />
    </QueryClientProvider>,
  );
}

describe("PracticeSession", () => {
  beforeEach(() => {
    submitAttempt.mockReset();
    submitAttempt.mockImplementation(async (request) => results[request.exerciseId]!);
  });

  it("plays a whole set from the keyboard: answer, instant feedback, next, summary", async () => {
    const user = userEvent.setup();
    renderSession();

    // 1. MultipleChoice: birinchi variant fokusda; Space tanlaydi, Enter yuboradi.
    expect(screen.getByText("Mashq 1 / 3")).toBeInTheDocument();
    expect(screen.getByRole("progressbar", { name: "Bajarildi: 0 / 3" })).toBeInTheDocument();
    await user.keyboard(" {Enter}");

    const banner = await screen.findByRole("status", { name: "Natija" });
    expect(banner).toHaveTextContent("Notoʻgʻri");
    expect(banner).toHaveTextContent("Oʻqish qoidasi");
    expect(banner).toHaveTextContent("Toʻgʻri javob: cape");
    expect(screen.getByRole("progressbar", { name: "Bajarildi: 1 / 3" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Keyingi mashq/ })).toHaveFocus();

    // 2. GapFill: Enter keyingisiga oʻtadi, maydon fokusda.
    await user.keyboard("{Enter}");
    expect(screen.getByText("Mashq 2 / 3")).toBeInTheDocument();
    expect(screen.queryByRole("status", { name: "Natija" })).not.toBeInTheDocument();
    expect(screen.getByLabelText("Javobingiz")).toHaveFocus();
    await user.keyboard("Helo{Enter}");
    expect(await screen.findByRole("status", { name: "Natija" })).toHaveTextContent("Qisman toʻgʻri");

    // 3. Matching: oxirgi mashqdan keyin "Natijalarni koʻrish".
    await user.keyboard("{Enter}");
    const words = within(screen.getByRole("group", { name: "Inglizcha soʻzlar" }));
    const translations = within(screen.getByRole("group", { name: "Tarjimalar" }));
    await user.click(words.getByRole("button", { name: /hello/ }));
    await user.click(translations.getByRole("button", { name: /salom/ }));
    await user.click(words.getByRole("button", { name: /goodbye/ }));
    await user.click(translations.getByRole("button", { name: /xayr/ }));
    await user.keyboard("{Enter}");
    await user.click(await screen.findByRole("button", { name: /Natijalarni koʻrish/ }));

    // Yakun: (0 + 0,5 + 1) / 3 = 50 %.
    const heading = await screen.findByRole("heading", { name: "Mashqlar yakunlandi" });
    expect(heading).toHaveFocus();
    const stat = (label: string) => screen.getByText(label, { selector: "dt" }).nextElementSibling;
    expect(stat("Oʻrtacha ball")).toHaveTextContent("50 %");
    expect(stat("Toʻgʻri")).toHaveTextContent("1");
    expect(stat("Qisman")).toHaveTextContent("1");
    expect(stat("Notoʻgʻri")).toHaveTextContent("1");
    expect(screen.getByText("Oʻqish qoidasi")).toBeInTheDocument();
    expect(screen.getByText("Imlo")).toBeInTheDocument();

    // Har bir urinish toʻgʻri shaklda yuborilgan.
    expect(submitAttempt.mock.calls.map(([request]) => request)).toEqual([
      { exerciseId: "mc", selectedOptionId: "cap", timeTakenMs: expect.any(Number) },
      { exerciseId: "gap", answer: "Helo", timeTakenMs: expect.any(Number) },
      {
        exerciseId: "match",
        matches: [
          { optionId: "o1", matchText: "salom" },
          { optionId: "o2", matchText: "xayr" },
        ],
        timeTakenMs: expect.any(Number),
      },
    ]);
    expect(submitAttempt.mock.calls.every(([request]) => request.timeTakenMs >= 0)).toBe(true);
  });

  it("restarts the set from the summary", async () => {
    const user = userEvent.setup();
    renderSession([exercises[1]!]);

    await user.keyboard("Hello{Enter}");
    await user.click(await screen.findByRole("button", { name: /Natijalarni koʻrish/ }));
    await user.click(await screen.findByRole("button", { name: /Qayta ishlash/ }));

    expect(screen.getByText("Mashq 1 / 1")).toBeInTheDocument();
    expect(screen.getByLabelText("Javobingiz")).toHaveValue("");
    expect(screen.getByLabelText("Javobingiz")).toHaveFocus();
  });

  it("keeps the answer and shows the error when submitting fails, then lets the student try again", async () => {
    submitAttempt.mockRejectedValueOnce(new Error("Server bilan aloqa yoʻq."));
    const user = userEvent.setup();
    renderSession([exercises[1]!]);

    await user.keyboard("Helo{Enter}");

    expect(await screen.findByRole("alert")).toHaveTextContent("Server bilan aloqa yoʻq.");
    expect(screen.getByLabelText("Javobingiz")).toHaveValue("Helo");
    expect(screen.queryByRole("status", { name: "Natija" })).not.toBeInTheDocument();
    expect(screen.getByRole("progressbar", { name: "Bajarildi: 0 / 1" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Tekshirish" }));
    expect(await screen.findByRole("status", { name: "Natija" })).toHaveTextContent("Qisman toʻgʻri");
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(submitAttempt).toHaveBeenCalledTimes(2);
  });

  it("sends one attempt even if Enter is pressed again while it is being graded", async () => {
    let finish: (value: AttemptResultDto) => void = () => {};
    submitAttempt.mockImplementationOnce(() => new Promise((resolve) => (finish = resolve)));
    const user = userEvent.setup();
    renderSession([exercises[1]!]);

    await user.keyboard("Helo{Enter}");
    await user.keyboard("{Enter}{Enter}");
    expect(screen.getByRole("button", { name: "Tekshirish" })).toBeDisabled();

    finish(results.gap!);
    expect(await screen.findByRole("status", { name: "Natija" })).toBeInTheDocument();
    expect(submitAttempt).toHaveBeenCalledTimes(1);
  });
});
