import { ReportingDashboardFeature } from "@/features/reporting";

interface DashboardPageProps {
  readonly id: string;
}

export function DashboardPage({ id }: DashboardPageProps) {
  return <ReportingDashboardFeature id={`${id}.reporting`} />;
}
