import { AuditManagement } from "@/features/audit";

interface AuditPageProps {
  readonly id: string;
}

export function AuditPage({ id }: AuditPageProps) {
  return <AuditManagement id={`${id}.audit`} />;
}
