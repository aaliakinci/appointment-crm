import { LoginFeature } from "@/features/auth";

interface LoginPageProps {
  readonly id: string;
}

export function LoginPage({ id }: LoginPageProps) {
  return <LoginFeature id={`${id}.feature`} />;
}
