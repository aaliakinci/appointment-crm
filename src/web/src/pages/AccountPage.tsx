import { AccountFeature } from "@/features/auth";

interface AccountPageProps {
  readonly id: string;
}

export function AccountPage({ id }: AccountPageProps) {
  return <AccountFeature id={`${id}.feature`} />;
}
