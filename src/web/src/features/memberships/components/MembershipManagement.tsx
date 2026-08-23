import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useMembershipManagement } from "../hooks/useMembershipManagement";
import { MembershipEditorDialog } from "./MembershipEditorDialog";
import { MembershipSummaryCards } from "./MembershipSummaryCards";
import { MembershipTable } from "./MembershipTable";

interface MembershipManagementProps {
  readonly id: string;
}

export function MembershipManagement({ id }: MembershipManagementProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const canManage = session?.activeTenant.permissions.includes("memberships.manage") ?? false;
  const management = useMembershipManagement({ canManage, t });
  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/team">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:memberships.eyebrow")}
          title={t("app:memberships.title")}
        />
        <MembershipSummaryCards id={`${id}.summary`} report={management.report} t={t} />
        {management.error && (
          <Alert
            id={`${id}.error`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={management.reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {management.error}
          </Alert>
        )}
        <MembershipTable
          id={`${id}.table`}
          loading={management.loading}
          memberships={management.memberships}
          onSelect={management.openDetail}
          t={t}
        />
      </Stack>
      <MembershipEditorDialog id={`${id}.dialog`} management={management} t={t} />
    </WorkspaceShell>
  );
}
