import { SchedulingManagement } from "@/features/scheduling";

interface SchedulingPageProps {
  readonly id: string;
}

export function SchedulingPage({ id }: SchedulingPageProps) {
  return <SchedulingManagement id={`${id}.feature`} />;
}
