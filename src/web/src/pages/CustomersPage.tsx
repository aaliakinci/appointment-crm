import { CustomerManagement } from "@/features/customers";

interface CustomersPageProps {
  readonly id: string;
}

export function CustomersPage({ id }: CustomersPageProps) {
  return <CustomerManagement id={`${id}.feature`} />;
}
