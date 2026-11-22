// ESLint flat config. Kept to ESLint's own rules, typescript-eslint, and the React team's hooks plugin
// so the dependency tree stays small. eslint-plugin-react is left out: it does not support ESLint 10.
import js from "@eslint/js";
import { defineConfig, globalIgnores } from "eslint/config";
import reactHooks from "eslint-plugin-react-hooks";
import globals from "globals";
import tseslint from "typescript-eslint";

export default defineConfig(
  globalIgnores(["dist/", "node_modules/"]),
  {
    linterOptions: {
      reportUnusedDisableDirectives: "error",
      reportUnusedInlineConfigs: "error",
    },
  },
  {
    files: ["src/**/*.{ts,tsx}"],
    extends: [
      js.configs.recommended,
      tseslint.configs.strictTypeChecked,
      tseslint.configs.stylisticTypeChecked,
      reactHooks.configs.flat.recommended,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    rules: {
      eqeqeq: ["error", "always"],
      "no-console": ["error", { allow: ["warn", "error"] }],
      "object-shorthand": "error",
      "prefer-template": "error",
      "@typescript-eslint/consistent-type-imports": "error",
      "@typescript-eslint/no-import-type-side-effects": "error",
      "@typescript-eslint/switch-exhaustiveness-check": "error",
      // Numbers stringify predictably (counts, ids, URLs); strictTypeChecked forbids them, recommendedTypeChecked allows them
      "@typescript-eslint/restrict-template-expressions": [
        "error",
        { allowNumber: true },
      ],
      // Every hook dependency missing from the list is a stale-closure bug
      "react-hooks/exhaustive-deps": "error",
    },
  },
  // Build tooling (CommonJS, runs in Node)
  {
    files: ["*.{js,mjs}"],
    extends: [js.configs.recommended],
    languageOptions: {
      globals: globals.node,
    },
  },
  {
    files: ["*.js"],
    languageOptions: {
      sourceType: "commonjs",
    },
  },
);
