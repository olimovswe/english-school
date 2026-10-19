"use client";

import { useEffect, useRef } from "react";

import type { TelegramAuthData } from "../types";

const WIDGET_SCRIPT = "https://telegram.org/js/telegram-widget.js?22";

declare global {
  interface Window {
    onTelegramAuth?: (user: TelegramAuthData) => void;
  }
}

interface TelegramLoginButtonProps {
  botUsername: string;
  onAuth: (user: TelegramAuthData) => void;
}

/**
 * Telegram Login Widget: rasmiy skript tugmani iframe sifatida chizadi va foydalanuvchi tasdiqlagach
 * imzolangan maʼlumotni global `onTelegramAuth` ga beradi. Bot domeni BotFather'da (/setdomain)
 * shu sayt manzili boʻlishi kerak, aks holda widget "Bot domain invalid" deydi (localhost ham).
 */
export function TelegramLoginButton({ botUsername, onAuth }: TelegramLoginButtonProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const onAuthRef = useRef(onAuth);

  useEffect(() => {
    onAuthRef.current = onAuth;
  }, [onAuth]);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) {
      return;
    }

    window.onTelegramAuth = (user) => onAuthRef.current(user);

    const script = document.createElement("script");
    script.src = WIDGET_SCRIPT;
    script.async = true;
    script.dataset.telegramLogin = botUsername;
    script.dataset.size = "large";
    script.dataset.radius = "8";
    script.dataset.userpic = "false";
    script.dataset.onauth = "onTelegramAuth(user)";
    container.appendChild(script);

    return () => {
      container.replaceChildren();
      delete window.onTelegramAuth;
    };
  }, [botUsername]);

  return <div ref={containerRef} className="flex min-h-10 justify-center" />;
}
