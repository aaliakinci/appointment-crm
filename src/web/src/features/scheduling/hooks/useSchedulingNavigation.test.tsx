import type { LilyPageComponent } from "@lily_platform/lily_ui";
import { AppRouter, createLilyRouterKit } from "@lily_platform/lily_ui/router";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { useSchedulingNavigation } from "./useSchedulingNavigation";

const NavigationHarness: LilyPageComponent = ({ id }) => {
  useSchedulingNavigation();
  return <div id={id}>scheduling</div>;
};

describe("useSchedulingNavigation", () => {
  it("renders within Lily UI's classic router without requiring a data router", () => {
    const routerKit = createLilyRouterKit<Record<string, never>>();
    const routes = routerKit.createRoutes([
      { id: "scheduling-navigation", path: "/", page: NavigationHarness },
    ]);

    expect(() =>
      renderToStaticMarkup(
        <AppRouter
          routes={routes}
          guardRegistry={routerKit.createGuardRegistry()}
          state={{}}
          routerType="memory"
        />,
      ),
    ).not.toThrow();
  });
});
