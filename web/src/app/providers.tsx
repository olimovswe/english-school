"use client";

import { QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "next-themes";
import { useState, type ReactNode } from "react";

import { Toaster } from "@/components/ui/sonner";
import { createQueryClient } from "@/lib/query-client";
import { SessionSync } from "@/modules/auth/components/session-sync";

export function Providers({ children }: { children: ReactNode }) {
  // Har bir brauzer sessiyasiga bitta kesh; serverda esa har bir render uchun yangisi.
  const [queryClient] = useState(createQueryClient);

  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>
        <SessionSync />
        {children}
        <Toaster richColors position="top-right" />
      </QueryClientProvider>
    </ThemeProvider>
  );
}
