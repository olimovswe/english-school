"use client";

import type { ExerciseType } from "../api";
import type { ExerciseCardProps } from "./exercise-frame";
import { FillInTheBlankCard } from "./fill-in-the-blank-card";
import { MatchingCard } from "./matching-card";
import { MultipleChoiceCard } from "./multiple-choice-card";

export const SUPPORTED_EXERCISE_TYPES: readonly string[] = ["GapFill", "MultipleChoice", "Matching"] satisfies ExerciseType[];

/**
 * Mashq turiga mos karta. Yangi mashq ochilganda karta qayta yaratilishi (`key={exercise.id}`) kerak:
 * javob qoralamasi kartaning oʻz holatida.
 */
export function ExercisePlayer(props: ExerciseCardProps) {
  switch (props.exercise.type) {
    case "GapFill":
      return <FillInTheBlankCard {...props} />;
    case "MultipleChoice":
      return <MultipleChoiceCard {...props} />;
    case "Matching":
      return <MatchingCard {...props} />;
    default:
      // Backend yangi tur qoʻshsa: sessiya bunday mashqlarni oldindan chiqarib tashlaydi.
      return null;
  }
}
