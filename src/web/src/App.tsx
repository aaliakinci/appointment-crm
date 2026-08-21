import { AppRouter } from "@lily_platform/lily_ui/router";

import { APP_ROUTES, APP_ROUTER_STATE, appGuardRegistry } from "@/router";

export function App() {
  return (
    <AppRouter
      routes={APP_ROUTES}
      guardRegistry={appGuardRegistry}
      state={APP_ROUTER_STATE}
      routerType="hash"
      fallbackPath="/"
    />
  );
}
