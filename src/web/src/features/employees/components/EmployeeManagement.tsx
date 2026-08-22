import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useEmployeeEditor } from "../hooks/useEmployeeEditor";
import { useEmployeeList } from "../hooks/useEmployeeList";
import { EmployeeEditorDialog } from "./EmployeeEditorDialog";
import { EmployeeFilters } from "./EmployeeFilters";
import { EmployeeTable } from "./EmployeeTable";

interface EmployeeManagementProps {
  readonly id: string;
}

export function EmployeeManagement({ id }: EmployeeManagementProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const canManage = session?.activeTenant.permissions.includes("employees.manage") ?? false;
  const list = useEmployeeList(canManage);
  const editor = useEmployeeEditor({
    canManage,
    onSaved: list.reload,
    serviceOptions: list.serviceOptions,
    t,
    userOptions: list.userOptions,
  });

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/employees">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:employees.eyebrow")}
          title={t("app:employees.title")}
          createLabel={canManage ? t("app:employees.create") : undefined}
          onCreate={canManage ? editor.openCreate : undefined}
        />
        <EmployeeFilters id={`${id}.filters`} list={list} t={t} />
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
            {t("app:employees.loadError")}
          </Alert>
        )}
        <EmployeeTable id={`${id}.table`} list={list} onSelect={editor.openDetail} t={t} />
      </Stack>
      <EmployeeEditorDialog id={`${id}.dialog`} editor={editor} t={t} />
    </WorkspaceShell>
  );
}
