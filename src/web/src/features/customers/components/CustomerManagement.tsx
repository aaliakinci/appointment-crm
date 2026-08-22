import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useCustomerEditor } from "../hooks/useCustomerEditor";
import { useCustomerList } from "../hooks/useCustomerList";
import { CustomerEditorDialog } from "./CustomerEditorDialog";
import { CustomerFilters } from "./CustomerFilters";
import { CustomerTable } from "./CustomerTable";

interface CustomerManagementProps {
  readonly id: string;
}

export function CustomerManagement({ id }: CustomerManagementProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const canManage = session?.activeTenant.permissions.includes("customers.manage") ?? false;
  const list = useCustomerList();
  const editor = useCustomerEditor({ canManage, onSaved: list.reload, t });

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/customers">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:customers.eyebrow")}
          title={t("app:customers.title")}
          createLabel={canManage ? t("app:customers.create") : undefined}
          onCreate={canManage ? editor.openCreate : undefined}
        />
        <CustomerFilters
          id={`${id}.filters`}
          list={list}
          searchLabel={t("app:common.search")}
          includeArchivedLabel={t("app:customers.includeArchived")}
          applyLabel={t("app:common.apply")}
        />
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
            {t("app:customers.loadError")}
          </Alert>
        )}
        <CustomerTable id={`${id}.table`} list={list} onSelect={editor.openDetail} t={t} />
      </Stack>
      <CustomerEditorDialog id={`${id}.dialog`} editor={editor} t={t} />
    </WorkspaceShell>
  );
}
