// @vitest-environment jsdom
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import type { AttemptResultDto, ExerciseDto } from "../api";
import { FillInTheBlankCard, MAX_ANSWER_LENGTH, validateGapAnswer } from "./fill-in-the-blank-card";
import { GradingFeedbackBanner } from "./grading-feedback-banner";
import { MatchingCard } from "./matching-card";
import { MultipleChoiceCard } from "./multiple-choice-card";

const gap: ExerciseDto = {
  id: "gap",
  position: 1,
  type: "GapFill",
  instruction: "Boʻsh joyni toʻldiring.",
  prompt: "I ___ a student.",
  choices: [],
  matchTargets: [],
};

const choice: ExerciseDto = {
  id: "mc",
  position: 2,
  type: "MultipleChoice",
  instruction: "Toʻgʻri tarjimani tanlang.",
  prompt: "«Siz charchaganmisiz?»",
  choices: [
    { id: "a", text: "Are you tired?" },
    { id: "b", text: "Do you tired?" },
    { id: "c", text: "You are tired?" },
  ],
  matchTargets: [],
};

const matching: ExerciseDto = {
  id: "match",
  position: 3,
  type: "Matching",
  instruction: "Soʻzlarni tarjimasi bilan juftlang.",
  prompt: "1-kun soʻzlari",
  choices: [
    { id: "o1", text: "hello" },
    { id: "o2", text: "goodbye" },
    { id: "o3", text: "name" },
  ],
  matchTargets: ["ism", "salom", "xayr"],
};

const words = () => within(screen.getByRole("group", { name: "Inglizcha soʻzlar" }));
const translations = () => within(screen.getByRole("group", { name: "Tarjimalar" }));

describe("FillInTheBlankCard", () => {
  it("asks for an answer instead of submitting an empty one", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<FillInTheBlankCard exercise={gap} onSubmit={onSubmit} />);

    const input = screen.getByLabelText("Javobingiz");
    expect(input).toHaveFocus();
    await user.keyboard("{Enter}");

    expect(screen.getByText("Javobni yozing.")).toBeInTheDocument();
    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("flags Cyrillic letters while typing and never submits them", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<FillInTheBlankCard exercise={gap} onSubmit={onSubmit} />);

    await user.keyboard("ам");
    expect(screen.getByText("Javobni lotin harflarida, ingliz klaviaturasida yozing.")).toBeInTheDocument();

    await user.keyboard("{Enter}");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("previews the answer in the gap and submits it trimmed with Enter", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<FillInTheBlankCard exercise={gap} onSubmit={onSubmit} />);

    await user.keyboard("  am {Enter}");

    expect(screen.getByRole("form")).toHaveTextContent("I am a student.");
    expect(onSubmit).toHaveBeenCalledExactlyOnceWith({ answer: "am" });
  });

  it("rejects answers longer than the API accepts", () => {
    expect(validateGapAnswer("a".repeat(MAX_ANSWER_LENGTH))).toBeNull();
    expect(validateGapAnswer("a".repeat(MAX_ANSWER_LENGTH + 1))).toBe(`Javob ${MAX_ANSWER_LENGTH} belgidan oshmasin.`);
  });

  it("becomes read-only and hides the submit button once graded", () => {
    const result: AttemptResultDto = { attemptId: "1", verdict: "Correct", score: 1, evaluationTier: 0, feedback: [] };
    render(<FillInTheBlankCard exercise={gap} result={result} onSubmit={vi.fn()} />);

    expect(screen.getByLabelText("Javobingiz")).toHaveAttribute("readonly");
    expect(screen.queryByRole("button", { name: "Tekshirish" })).not.toBeInTheDocument();
  });
});

describe("MultipleChoiceCard", () => {
  it("asks for a choice when Enter is pressed with none selected", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<MultipleChoiceCard exercise={choice} onSubmit={onSubmit} />);

    expect(screen.getByRole("radio", { name: "Are you tired?" })).toHaveFocus();
    await user.keyboard("{Enter}");

    expect(screen.getByRole("alert")).toHaveTextContent("Variantlardan birini tanlang.");
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("selects with the keyboard and submits the choice once with Enter", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<MultipleChoiceCard exercise={choice} onSubmit={onSubmit} />);

    await user.keyboard(" ");
    await user.keyboard("{Enter}");

    expect(screen.getByRole("radio", { name: "Are you tired?" })).toBeChecked();
    expect(onSubmit).toHaveBeenCalledExactlyOnceWith({ selectedOptionId: "a" });
  });

  it("marks the chosen wrong option and the correct one after grading", async () => {
    const user = userEvent.setup();
    const result: AttemptResultDto = {
      attemptId: "1",
      verdict: "Incorrect",
      score: 0,
      evaluationTier: 0,
      feedback: [{ errorCode: "WORD_ORDER", correction: "Are you tired?", explanation: null }],
    };
    const { rerender } = render(<MultipleChoiceCard exercise={choice} onSubmit={vi.fn()} />);
    await user.click(screen.getByText("You are tired?"));
    rerender(<MultipleChoiceCard exercise={choice} result={result} onSubmit={vi.fn()} />);

    const option = (text: string) => screen.getByText(text).closest("label") as HTMLElement;
    expect(within(option("You are tired?")).getByLabelText("notoʻgʻri javob")).toBeInTheDocument();
    expect(within(option("Are you tired?")).getByLabelText("toʻgʻri javob")).toBeInTheDocument();
    expect(within(option("Do you tired?")).queryByLabelText(/javob/)).not.toBeInTheDocument();
    expect(screen.getAllByRole("radio").every((radio) => (radio as HTMLInputElement).disabled)).toBe(true);
  });
});

describe("MatchingCard", () => {
  it("pairs from either side, moves focus to the next open word and submits every pair", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<MatchingCard exercise={matching} onSubmit={onSubmit} />);

    await user.click(words().getByRole("button", { name: /hello/ }));
    await user.click(translations().getByRole("button", { name: /salom/ }));
    expect(words().getByRole("button", { name: /goodbye/ })).toHaveFocus();

    await user.click(translations().getByRole("button", { name: /xayr/ }));
    await user.click(words().getByRole("button", { name: /goodbye/ }));
    await user.click(words().getByRole("button", { name: /name/ }));
    await user.click(translations().getByRole("button", { name: /^ism/ }));
    expect(screen.getByRole("button", { name: "Tekshirish" })).toHaveFocus();

    await user.keyboard("{Enter}");
    expect(onSubmit).toHaveBeenCalledExactlyOnceWith({
      matches: [
        { optionId: "o1", matchText: "salom" },
        { optionId: "o2", matchText: "xayr" },
        { optionId: "o3", matchText: "ism" },
      ],
    });
  });

  it("unpairs on a second click and does not submit an incomplete matching", async () => {
    const onSubmit = vi.fn();
    const user = userEvent.setup();
    render(<MatchingCard exercise={matching} onSubmit={onSubmit} />);

    await user.click(words().getByRole("button", { name: /hello/ }));
    await user.click(translations().getByRole("button", { name: /salom/ }));
    expect(words().getByRole("button", { name: /hello/ })).toHaveAccessibleName(/juftligi: salom/);

    await user.click(words().getByRole("button", { name: /hello/ }));
    expect(words().getByRole("button", { name: /hello/ })).toHaveAttribute("aria-pressed", "true");
    expect(words().getByRole("button", { name: /hello/ })).not.toHaveAccessibleName(/juftligi/);

    await user.click(screen.getByRole("button", { name: "Tekshirish" }));
    expect(screen.getByRole("alert")).toHaveTextContent("Har bir soʻzga tarjimasini tanlang.");
    expect(onSubmit).not.toHaveBeenCalled();
  });
});

describe("GradingFeedbackBanner", () => {
  it("shows verdict, score, error class once per group, corrections and the grading tier", () => {
    render(
      <GradingFeedbackBanner
        result={{
          attemptId: "1",
          verdict: "PartiallyCorrect",
          score: 0.6,
          evaluationTier: 0,
          feedback: [
            { errorCode: "VOCABULARY", correction: "friend — doʻst", explanation: null },
            { errorCode: "VOCABULARY", correction: "woman — ayol", explanation: null },
          ],
        }}
      />,
    );

    const banner = screen.getByRole("status", { name: "Natija" });
    expect(banner).toHaveTextContent("Qisman toʻgʻri");
    // toHaveTextContent boʻshliqlarni (NBSP ham) oddiy boʻshliqqa keltiradi; NBSP format.test.ts da tekshirilgan.
    expect(banner).toHaveTextContent("Ball: 60 %");
    expect(within(banner).getAllByText("Soʻz maʼnosi")).toHaveLength(1);
    expect(banner).toHaveTextContent("friend — doʻst");
    expect(banner).toHaveTextContent("woman — ayol");
    expect(banner).toHaveTextContent("Baholash: 0-daraja, qoidalar boʻyicha");
  });

  it("shows an unknown error code as is", () => {
    render(
      <GradingFeedbackBanner
        result={{
          attemptId: "1",
          verdict: "Incorrect",
          score: 0,
          evaluationTier: 3,
          feedback: [{ errorCode: "NEW_CODE", correction: "x", explanation: "Qoida." }],
        }}
      />,
    );

    expect(screen.getByText("NEW_CODE")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent("Baholash: 3-daraja, AI tahlili");
  });
});
