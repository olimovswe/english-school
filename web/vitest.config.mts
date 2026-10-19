import { fileURLToPath } from "node:url";
import { defineConfig } from "vitest/config";

export default defineConfig({
  resolve: {
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  // tsconfig JSX'ni Next.js uchun saqlab qoldiradi ("preserve"); testlarda uni React oʻzi oʻzgartiradi.
  oxc: { jsx: { runtime: "automatic" } },
  test: {
    // Odatda node; komponent testlari faylning boshida `// @vitest-environment jsdom` deb oʻzi tanlaydi.
    environment: "node",
    include: ["src/**/*.test.{ts,tsx}"],
    setupFiles: ["src/test/setup.ts"],
  },
});
