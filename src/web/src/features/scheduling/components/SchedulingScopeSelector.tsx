import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";

import type { Employee } from "@/features/employees/catalog";

interface SchedulingScopeSelectorProps {
  readonly employees: readonly Employee[];
  readonly id: string;
  readonly label: string;
  readonly onChange: (scope: string) => void;
  readonly scope: string;
  readonly tenantLabel: string;
}

export function SchedulingScopeSelector({
  employees,
  id,
  label,
  onChange,
  scope,
  tenantLabel,
}: SchedulingScopeSelectorProps) {
  return (
    <Select
      id={id}
      label={label}
      value={scope}
      options={[
        { id: "tenant", value: "tenant", label: tenantLabel },
        ...employees.map((employee) => ({
          id: employee.id,
          value: employee.id,
          label: employee.name,
        })),
      ]}
      onValueChange={(value) => onChange(String(value))}
      sx={{ maxWidth: 420 }}
    />
  );
}
