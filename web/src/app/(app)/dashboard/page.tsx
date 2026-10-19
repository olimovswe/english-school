import type { Metadata } from "next";

import { DashboardView } from "@/modules/dashboard/components/dashboard-view";

export const metadata: Metadata = { title: "Kunlik reja" };

export default function DashboardPage() {
  return <DashboardView />;
}
