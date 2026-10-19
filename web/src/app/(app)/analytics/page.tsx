import { ChartLine } from "lucide-react";
import type { Metadata } from "next";

import { ComingSoon } from "@/components/coming-soon";

export const metadata: Metadata = { title: "Tahlil" };

export default function AnalyticsPage() {
  return (
    <ComingSoon
      icon={ChartLine}
      title="Tahlil"
      description="Progress va haftalik hisobot"
      features={[
        "Haftalik aniqlik, fokus daqiqalari va oʻrganilgan soʻzlar (GET /analytics/reports/weekly)",
        "Takrorlash aniqligi va mashqlar soni (GET /analytics/progress)",
      ]}
    />
  );
}
