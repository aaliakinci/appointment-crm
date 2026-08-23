import { AppointmentManagement } from "@/features/appointments";

interface AppointmentsPageProps {
  readonly id: string;
}

export function AppointmentsPage({ id }: AppointmentsPageProps) {
  return <AppointmentManagement id={`${id}.feature`} />;
}
