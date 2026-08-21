import { fileURLToPath, URL } from "node:url";

import react from "@vitejs/plugin-react";
import { defineConfig, loadEnv } from "vite";

const DEFAULT_API_PROXY_TARGET = "http://127.0.0.1:8080";

function resolveApiProxyTarget(value: string | undefined): string {
  const candidate = value?.trim() || DEFAULT_API_PROXY_TARGET;
  const parsedUrl = new URL(candidate);

  if (parsedUrl.protocol !== "http:" && parsedUrl.protocol !== "https:") {
    throw new Error("VITE_DEV_API_PROXY_TARGET must use HTTP or HTTPS.");
  }

  if (parsedUrl.username || parsedUrl.password) {
    throw new Error("VITE_DEV_API_PROXY_TARGET must not contain credentials.");
  }

  return parsedUrl.origin;
}

export default defineConfig(({ mode }) => {
  const environment = loadEnv(mode, process.cwd(), "");
  const apiProxyTarget = resolveApiProxyTarget(environment.VITE_DEV_API_PROXY_TARGET);

  return {
    plugins: [react()],
    resolve: {
      alias: {
        "@": fileURLToPath(new URL("./src", import.meta.url)),
      },
      dedupe: ["react", "react-dom"],
    },
    server: {
      host: "127.0.0.1",
      port: 5173,
      strictPort: true,
      proxy: {
        "/api": apiProxyTarget,
        "/health": apiProxyTarget,
        "/openapi": apiProxyTarget,
      },
    },
  };
});
