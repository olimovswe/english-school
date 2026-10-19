import { Mic } from "lucide-react";
import type { Metadata } from "next";

import { ComingSoon } from "@/components/coming-soon";

export const metadata: Metadata = { title: "Talaffuz" };

export default function SpeechPage() {
  return (
    <ComingSoon
      icon={Mic}
      title="Talaffuz"
      description="Ovoz yozib, baho olish"
      features={[
        "Kun soʻzlari va iboralarining namuna talaffuzi (GET /speech/days/{n}/audio)",
        "Brauzerda yozib olish va baholash: aniqlik, toʻliqlik, ravonlik (POST /speech/assess)",
        "Soʻzma-soʻz natija va th, w/v kabi tovushlar boʻyicha maslahat",
      ]}
    />
  );
}
