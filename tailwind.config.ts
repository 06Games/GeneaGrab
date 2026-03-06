import type { Config } from "tailwindcss";

export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        mono: ['"JetBrains Mono"', '"Fira Code"', "monospace"],
      },
      colors: {
        gg: {
          base:    "#0c0e10",
          surface: "#131517",
          raised:  "#1a1d21",
          hover:   "#21252a",
          border:  "#272b30",
          accent:  "#c8924a",
          muted:   "#5a6170",
          dim:     "#3a3f47",
          fg:      "#e4e1db",
        },
      },
    },
  },
  plugins: [
    require("tailwind-scrollbar")({ nocompatible: true }),
  ],
} satisfies Config;
