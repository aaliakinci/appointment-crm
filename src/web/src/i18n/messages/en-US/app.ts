export const appMessagesEnUs = {
  brand: "Appointment CRM",
  navigation: {
    status: "System status",
    login: "Sign in",
  },
  shell: {
    skipToContent: "Skip to main content",
    portfolioNotice: "Phase 1 technical skeleton",
  },
  status: {
    eyebrow: "Platform foundation",
    title: "System readiness",
    description:
      "This screen proves that the Lily UI frontend can reach the ASP.NET Core API and its PostgreSQL readiness check.",
    api: "API",
    database: "PostgreSQL",
    loading: "Checking services…",
    healthy: "Healthy",
    unavailable: "Unavailable",
    retry: "Check again",
    traceId: "Trace ID",
    error: "The readiness response could not be loaded.",
  },
  login: {
    eyebrow: "Authentication",
    title: "Sign-in arrives in Phase 2",
    description:
      "The unauthenticated application shell and route boundary are ready. Credentials and secure sessions are intentionally not mocked in Phase 1.",
    back: "View system status",
  },
  error: {
    title: "The application could not be displayed",
    description:
      "Reload the page. If the problem continues, use the trace information from the API.",
    reload: "Reload",
  },
} as const;

type StringCatalog<T> = {
  [Key in keyof T]: T[Key] extends string ? string : StringCatalog<T[Key]>;
};

export type AppMessageCatalog = StringCatalog<typeof appMessagesEnUs>;
