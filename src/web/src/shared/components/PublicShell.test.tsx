import { ThemeProvider } from "@lily_platform/lily_ui/ui/themes";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";

import { createAppointmentCrmTheme } from "@/app/theme";

import { PublicShell } from "./PublicShell";

describe("PublicShell", () => {
  it("renders stable shell and main-content identifiers", () => {
    const markup = renderToStaticMarkup(
      <ThemeProvider theme={createAppointmentCrmTheme("ltr")}>
        <PublicShell
          id="test-shell"
          activePath="/"
          brandLabel="Appointment CRM"
          statusLabel="Status"
          loginLabel="Login"
          skipToContentLabel="Skip"
          portfolioNotice="Secure appointment operations"
          onNavigate={() => undefined}
        >
          <p>Ready</p>
        </PublicShell>
      </ThemeProvider>,
    );

    expect(markup).toContain('id="test-shell"');
    expect(markup).toContain('id="test-shell.main"');
  });
});
