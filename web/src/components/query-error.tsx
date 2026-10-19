"use client";

import { RotateCw } from "lucide-react";

import { Button } from "@/components/ui/button";
import { errorMessage } from "@/lib/api/errors";

export function QueryError({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  return (
    <div role="alert" className="grid gap-3 text-sm">
      <p className="text-destructive">{errorMessage(error)}</p>
      <Button variant="outline" size="sm" className="w-fit" onClick={onRetry}>
        <RotateCw aria-hidden />
        Qayta urinish
      </Button>
    </div>
  );
}
