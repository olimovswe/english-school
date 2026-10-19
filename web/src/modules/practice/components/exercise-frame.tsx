"use client";

import { Loader2 } from "lucide-react";
import type { FormEvent, ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

import type { AttemptAnswer, AttemptResultDto, ExerciseDto } from "../api";

/** Har bir mashq kartasi oladigan umumiy xususiyatlar. */
export interface ExerciseCardProps {
  exercise: ExerciseDto;
  /** Baholangandan keyin: karta faqat oʻqish uchun va natija belgilari bilan. */
  result?: AttemptResultDto;
  submitting?: boolean;
  /** Server rad etgan javob sababi (masalan, validatsiya). */
  error?: string | null;
  onSubmit: (answer: AttemptAnswer) => void;
}

interface ExerciseFrameProps {
  exercise: ExerciseDto;
  /** Sarlavha: odatda `prompt`; GapFill uni boʻsh joy bilan oʻzi chizadi. */
  title: ReactNode;
  titleId: string;
  graded: boolean;
  submitting: boolean;
  error?: string | null;
  /**
   * "Tekshirish" yoki Enter. Tugma javob toʻliq boʻlmasa ham faol: karta nima yetishmasligini aytadi
   * (oʻchirilgan tugmaga fokus tushmaydi va sababi koʻrinmaydi).
   */
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  children: ReactNode;
}

/** Karta: koʻrsatma, savol, javob maydoni va "Tekshirish" — hammasi bitta forma (Enter yuboradi). */
export function ExerciseFrame({ exercise, title, titleId, graded, submitting, error, onSubmit, children }: ExerciseFrameProps) {
  return (
    <Card>
      <form onSubmit={onSubmit} aria-labelledby={titleId} noValidate className="contents">
        <CardHeader>
          <CardDescription>{exercise.instruction}</CardDescription>
          <CardTitle id={titleId} className="text-lg leading-relaxed">
            {title}
          </CardTitle>
        </CardHeader>
        <CardContent className="grid gap-3">
          {children}
          {error && (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          )}
        </CardContent>
        {!graded && (
          <CardFooter>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="animate-spin" aria-hidden />}
              Tekshirish
            </Button>
          </CardFooter>
        )}
      </form>
    </Card>
  );
}
