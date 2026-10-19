import { Loader2 } from "lucide-react";

export function PageSpinner({ label = "Yuklanmoqda…" }: { label?: string }) {
  return (
    <div role="status" className="flex min-h-svh items-center justify-center gap-2 text-sm text-muted-foreground">
      <Loader2 className="size-4 animate-spin" aria-hidden />
      {label}
    </div>
  );
}
