import { createTheme } from "@lily_platform/lily_ui/ui/themes";

export function createAppointmentCrmTheme(direction: "ltr" | "rtl") {
  return createTheme({
    direction,
    palette: {
      mode: "light",
      background: {
        default: "#f6f7fb",
        paper: "#ffffff",
      },
      primary: {
        main: "#4f46a5",
      },
      secondary: {
        main: "#0f766e",
      },
    },
    shape: {
      borderRadius: 10,
    },
    typography: {
      fontFamily:
        'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
      button: {
        fontWeight: 600,
        textTransform: "none",
      },
      h1: {
        fontWeight: 700,
      },
    },
  });
}
