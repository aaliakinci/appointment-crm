import { MembershipManagement } from "@/features/memberships";

interface TeamPageProps {
  readonly id: string;
}

export function TeamPage({ id }: TeamPageProps) {
  return <MembershipManagement id={`${id}.memberships`} />;
}
