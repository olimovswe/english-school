import "@testing-library/jest-dom/vitest";
import { afterEach } from "vitest";

// Testing Library globals oʻchiq boʻlganda tozalashni oʻzi ulamaydi; node muhitidagi testlarda DOM yoʻq.
afterEach(async () => {
  if (typeof document !== "undefined") {
    const { cleanup } = await import("@testing-library/react");
    cleanup();
  }
});
