import { ServiceManagement } from "@/features/services";

interface ServicesPageProps {
  readonly id: string;
}

export function ServicesPage({ id }: ServicesPageProps) {
  return <ServiceManagement id={`${id}.feature`} />;
}
