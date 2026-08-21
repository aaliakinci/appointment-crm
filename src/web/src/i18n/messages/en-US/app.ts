export const appMessagesEnUs = {
  brand: "Appointment CRM",
  navigation: {
    status: "System status",
    login: "Sign in",
    account: "Account",
  },
  shell: {
    skipToContent: "Skip to main content",
    portfolioNotice: "Secure appointment operations",
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
    title: "Sign in to your workspace",
    description:
      "Use your account credentials. If you belong to more than one business, you will select the active workspace after verification.",
    email: "Email address",
    password: "Password",
    tenant: "Business",
    submit: "Continue",
    submitting: "Signing in…",
    validation: "Enter a valid email address and password.",
    tenantRequired: "Select a business to continue.",
    error: "The credentials are invalid or the account is not available.",
    securityNotice:
      "The access token remains in application memory. The rotating refresh credential is kept in an HttpOnly cookie and is not exposed to browser storage.",
  },
  auth: {
    initializing: "Restoring the secure session…",
  },
  account: {
    eyebrow: "Secure session",
    role: "Role",
    tenant: "Active business",
    switchTenant: "Switch business",
    logout: "Sign out",
    revokeAll: "Sign out everywhere",
    tenantLoadError: "Available businesses could not be loaded.",
    switchError: "The business could not be changed.",
    logoutError: "The session could not be closed cleanly. Please try again.",
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
