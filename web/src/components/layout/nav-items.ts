import { ChartLine, Dumbbell, Layers, LayoutDashboard, Mic, type LucideIcon } from "lucide-react";

export interface NavItem {
  href: "/dashboard" | "/practice" | "/flashcards" | "/speech" | "/analytics";
  label: string;
  description: string;
  icon: LucideIcon;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { href: "/dashboard", label: "Kunlik reja", description: "Darvoza va bugungi vazifalar", icon: LayoutDashboard },
  { href: "/practice", label: "Mashqlar", description: "Kun mashqlari va xatolar tahlili", icon: Dumbbell },
  { href: "/flashcards", label: "Kartochkalar", description: "Soʻz va iboralarni takrorlash", icon: Layers },
  { href: "/speech", label: "Talaffuz", description: "Ovoz yozib, baho olish", icon: Mic },
  { href: "/analytics", label: "Tahlil", description: "Progress va haftalik hisobot", icon: ChartLine },
];

export function isActive(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}
