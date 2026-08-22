import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useServiceEditor } from "../hooks/useServiceEditor";
import { useServiceList } from "../hooks/useServiceList";
import { ServiceEditorDialog } from "./ServiceEditorDialog";
import { ServiceFilters } from "./ServiceFilters";
import { ServiceTable } from "./ServiceTable";

interface ServiceManagementProps {
  readonly id: string;
}

export function ServiceManagement({ id }: ServiceManagementProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const currency = session?.activeTenant.currency ?? "TRY";
  const canManage = session?.activeTenant.permissions.includes("services.manage") ?? false;
  const list = useServiceList();
  const editor = useServiceEditor({ canManage, currency, onSaved: list.reload, t });

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/services">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:services.eyebrow")}
          title={t("app:services.title")}
          createLabel={canManage ? t("app:services.create") : undefined}
          onCreate={canManage ? editor.openCreate : undefined}
        />
        <ServiceFilters id={`${id}.filters`} list={list} t={t} />
        {list.loadError && (
          <Alert
            id={`${id}.loadError`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={list.reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {t("app:services.loadError")}
          </Alert>
        )}
        <ServiceTable id={`${id}.table`} list={list} onSelect={editor.openDetail} t={t} />
      </Stack>
      <ServiceEditorDialog id={`${id}.dialog`} editor={editor} t={t} />
    </WorkspaceShell>
  );
}
