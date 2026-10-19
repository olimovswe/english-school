import { redirect } from "next/navigation";

import { AFTER_SIGN_IN_PATH } from "@/modules/auth/redirect";

/** Bosh sahifa — kunlik reja; sessiya boʻlmasa (app) layout login'ga yuboradi. */
export default function HomePage() {
  redirect(AFTER_SIGN_IN_PATH);
}
