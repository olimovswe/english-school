import type { Metadata, Viewport } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import type { ReactNode } from "react";

import { Providers } from "./providers";
import "./globals.css";

// Google'ning "latin" toʻplami U+02BB–02BC (oʻ, gʻ, tutuq belgisi) ni ham oʻz ichiga oladi.
const geistSans = Geist({
  variable: "--font-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: {
    default: "English School",
    template: "%s · English School",
  },
  description: "56 kunlik ingliz tili kursi: kunlik reja, mashqlar, kartochkalar va talaffuz.",
};

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#ffffff" },
    { media: "(prefers-color-scheme: dark)", color: "#0a0a0a" },
  ],
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    // Shrift oʻzgaruvchilari <html> da: globals.css `html { font-family: var(--font-sans) }` ni shu yerda oʻqiydi.
    // suppressHydrationWarning: next-themes <html> klassini gidratsiyadan oldin oʻrnatadi.
    <html lang="uz" className={`${geistSans.variable} ${geistMono.variable}`} suppressHydrationWarning>
      <body className="antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
