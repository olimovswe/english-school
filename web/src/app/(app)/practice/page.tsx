import type { Metadata } from "next";

import { PracticeView } from "@/modules/practice/components/practice-view";

export const metadata: Metadata = { title: "Mashqlar" };

export default function PracticePage() {
  return <PracticeView />;
}
