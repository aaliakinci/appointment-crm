import js from "@eslint/js";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import globals from "globals";
import tseslint from "typescript-eslint";

export default tseslint.config(
  {
    ignores: ["dist", "coverage"],
  },
  js.configs.recommended,
  ...tseslint.configs.recommendedTypeChecked,
  {
    files: ["**/*.{ts,tsx}"],
    languageOptions: {
      ecmaVersion: "latest",
      globals: globals.browser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      "react-hooks": reactHooks,
      "react-refresh": reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      "react-refresh/only-export-components": ["warn", { allowConstantExport: true }],
    },
  },
  {
    files: ["src/shared/**/*.{ts,tsx}"],
    ignores: ["src/shared/**/*.test.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/app/**", "@/features/**", "@/pages/**"],
              message: "Shared code must remain independent from app, feature, and page layers.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/app/**", "@/pages/**"],
              message: "Features must not depend on the app composition or page layers.",
            },
            {
              group: [
                "@/features/*/api",
                "@/features/*/api/**",
                "@/features/*/components",
                "@/features/*/components/**",
                "@/features/*/hooks",
                "@/features/*/hooks/**",
                "@/features/*/model",
                "@/features/*/model/**",
              ],
              message:
                "Features must consume another feature through an explicit public entrypoint.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/features/scheduling/components/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/app/**", "@/pages/**"],
              message: "Features must not depend on the app composition or page layers.",
            },
            {
              group: [
                "@/features/*/api",
                "@/features/*/api/**",
                "@/features/*/components",
                "@/features/*/components/**",
                "@/features/*/hooks",
                "@/features/*/hooks/**",
                "@/features/*/model",
                "@/features/*/model/**",
              ],
              message:
                "Features must consume another feature through an explicit public entrypoint.",
            },
            {
              group: ["**/api/schedulingApi"],
              message:
                "Scheduling components must invoke use cases through feature-local hooks, not the API client.",
            },
          ],
        },
      ],
    },
  },
  {
    files: ["src/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-syntax": [
        "error",
        {
          selector: "ImportSpecifier[imported.name='useLilyBlocker']",
          message:
            "useLilyBlocker requires a React Router data router, while AppRouter currently uses the classic hash router.",
        },
        {
          selector: "JSXAttribute[name.name='type'][value.value='date']",
          message:
            "Native date inputs are forbidden. Use LocalizedLilyDatePicker or a date field rendered by LocalizedLilyDateForm.",
        },
      ],
    },
  },
  {
    files: ["src/pages/**/*.{ts,tsx}"],
    rules: {
      "no-restricted-imports": [
        "error",
        {
          patterns: [
            {
              group: ["@/features/*/**"],
              message: "Pages must consume a feature through its public entrypoint.",
            },
          ],
        },
      ],
    },
  },
  {
    ...tseslint.configs.disableTypeChecked,
    files: ["**/*.js"],
    languageOptions: {
      ...tseslint.configs.disableTypeChecked.languageOptions,
      globals: globals.node,
    },
  },
);
