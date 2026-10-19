import { dirname } from "path";
import { fileURLToPath } from "url";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

const eslintConfig = [
  ...compat.extends("next/core-web-vitals", "next/typescript"),
  {
    rules: {
      // `shadcn add` komponentlarni "cn" paketiga ulaydi; loyihada yagona cn() — clsx + tailwind-merge.
      "no-restricted-imports": [
        "error",
        { paths: [{ name: "cn", message: "cn() ni \"@/lib/utils\" dan import qiling." }] },
      ],
    },
  },
  {
    ignores: ["node_modules/**", ".next/**", "out/**", "build/**", "next-env.d.ts"],
  },
];

export default eslintConfig;
