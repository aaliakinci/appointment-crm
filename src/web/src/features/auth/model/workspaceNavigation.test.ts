import { describe, expect, it } from "vitest";

import { workspaceNavigationFor } from "./workspaceNavigation";

describe("workspaceNavigationFor", () => {
  it("shows tenant operations but not management/reporting to a receptionist", () => {
    const paths = workspaceNavigationFor([
      "customers.read",
      "services.read",
      "employees.read",
      "appointments.read",
      "availability.read",
    ]).map((item) => item.path);

    expect(paths).toEqual(["/customers", "/services", "/employees", "/appointments", "/account"]);
    expect(paths).not.toContain("/dashboard");
    expect(paths).not.toContain("/scheduling");
    expect(paths).not.toContain("/team");
    expect(paths).not.toContain("/audit");
  });

  it("shows only own appointments, services and account to an employee", () => {
    expect(
      workspaceNavigationFor([
        "services.read",
        "availability.read",
        "appointments.read-own",
        "appointments.transition-own",
      ]).map((item) => item.path),
    ).toEqual(["/services", "/appointments", "/account"]);
  });

  it("keeps reporting and audit visibility coupled to reporting.read", () => {
    const paths = workspaceNavigationFor(["reporting.read"]).map((item) => item.path);

    expect(paths).toEqual(["/dashboard", "/audit", "/account"]);
  });
});
