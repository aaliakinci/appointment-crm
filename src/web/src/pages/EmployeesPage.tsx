import { EmployeeManagement } from "@/features/employees";

interface EmployeesPageProps {
  readonly id: string;
}

export function EmployeesPage({ id }: EmployeesPageProps) {
  return <EmployeeManagement id={`${id}.feature`} />;
}
