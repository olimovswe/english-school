import { Dumbbell } from "lucide-react";
import type { Metadata } from "next";

import { ComingSoon } from "@/components/coming-soon";

export const metadata: Metadata = { title: "Mashqlar" };

export default function PracticePage() {
  return (
    <ComingSoon
      icon={Dumbbell}
      title="Mashqlar"
      description="Kun mashqlari"
      features={[
        "Boʻshliqni toʻldirish, test va moslashtirish mashqlari (GET /practice/days/{n}/exercises)",
        "Har bir javobga darhol baho va oʻzbekcha izoh (POST /practice/attempts)",
        "Takrorlanuvchi xatolar taksonomiyasi",
      ]}
    />
  );
}
