import { Layers } from "lucide-react";
import type { Metadata } from "next";

import { ComingSoon } from "@/components/coming-soon";

export const metadata: Metadata = { title: "Kartochkalar" };

export default function FlashcardsPage() {
  return (
    <ComingSoon
      icon={Layers}
      title="Kartochkalar"
      description="Soʻz va iboralarni takrorlash"
      features={[
        "Ertalabki darvoza: navbatdagi kartochkalarga javob (POST /discipline/gate/submit)",
        "FSRS-5 boʻyicha takrorlash: Again · Hard · Good · Easy (POST /retention/review)",
        "Har bir soʻz uchun namuna talaffuz",
      ]}
    />
  );
}
