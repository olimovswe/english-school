import type { NextConfig } from "next";

/**
 * Brauzer API'ga shu sayt orqali (`/api/v1/*`) murojaat qiladi va Next.js soʻrovni backend'ga uzatadi:
 * bitta manba, CORS kerak emas. `dotnet run` — 5080, `docker compose --profile app` — 8080.
 * Qiymat `next dev` ishga tushganda, production'da esa `next build` vaqtida oʻqiladi.
 */
const apiProxyTarget = (process.env.API_PROXY_TARGET || "http://localhost:5080").replace(/\/+$/, "");

const securityHeaders = [
  { key: "X-Content-Type-Options", value: "nosniff" },
  { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
  { key: "X-Frame-Options", value: "DENY" },
  // Mikrofon faqat oʻz sahifamizga (talaffuz yozuvi); kamera va joylashuv kerak emas.
  { key: "Permissions-Policy", value: "camera=(), geolocation=(), microphone=(self)" },
];

const nextConfig: NextConfig = {
  poweredByHeader: false,
  // Yon panel chapda: dev indikatori "Chiqish" tugmasini yopmasin.
  devIndicators: { position: "bottom-right" },
  async headers() {
    return [{ source: "/:path*", headers: securityHeaders }];
  },
  async rewrites() {
    return [{ source: "/api/v1/:path*", destination: `${apiProxyTarget}/api/v1/:path*` }];
  },
};

export default nextConfig;
