import js from "@eslint/js";
import { defineConfig, globalIgnores } from "eslint/config";
import playwright from "eslint-plugin-playwright";
import tseslint from "typescript-eslint";

export default defineConfig(
  globalIgnores([
    "playwright/",
    "playwright-report/",
    "test-results/",
    "blob-report/",
  ]),
  {
    linterOptions: {
      reportUnusedDisableDirectives: "error",
      reportUnusedInlineConfigs: "error",
    },
  },
  {
    files: ["**/*.ts"],
    extends: [
      js.configs.recommended,
      tseslint.configs.strictTypeChecked,
      tseslint.configs.stylisticTypeChecked,
      playwright.configs["flat/recommended"],
    ],
    languageOptions: {
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    rules: {
      eqeqeq: ["error", "always"],
      "object-shorthand": "error",
      "prefer-template": "error",
      "@typescript-eslint/consistent-type-imports": "error",
      "@typescript-eslint/no-import-type-side-effects": "error",
      "@typescript-eslint/switch-exhaustiveness-check": "error",
      "@typescript-eslint/restrict-template-expressions": [
        "error",
        { allowNumber: true },
      ],
      "playwright/expect-expect": [
        "error",
        { assertFunctionPatterns: ["^expect"] },
      ],
    },
  },
  {
    files: ["**/*.setup.ts"],
    rules: {
      "playwright/expect-expect": "off",
    },
  },
  {
    files: ["*.mjs"],
    extends: [js.configs.recommended],
  },
);
