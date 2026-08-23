import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useAuditList } from "../hooks/useAuditList";
import { AuditFilters } from "./AuditFilters";
import { AuditTable } from "./AuditTable";

interface AuditManagementProps {
  readonly id: string;
}

export function AuditManagement({ id }: AuditManagementProps) {
  const { t } = useAppTranslation();
  const list = useAuditList();
  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/audit">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:audit.eyebrow")}
          title={t("app:audit.title")}
        />
        <AuditFilters id={`${id}.filters`} list={list} t={t} />
        {list.loadError && (
          <Alert
            id={`${id}.error`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={list.reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {t("app:audit.loadError")}
          </Alert>
        )}
        <AuditTable id={`${id}.table`} list={list} t={t} />
      </Stack>
    </WorkspaceShell>
  );
}
