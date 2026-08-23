import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useAppointmentCalendar } from "../hooks/useAppointmentCalendar";
import { useAppointmentDetail } from "../hooks/useAppointmentDetail";
import { useAppointmentEditor } from "../hooks/useAppointmentEditor";
import { useAppointmentReschedule } from "../hooks/useAppointmentReschedule";
import { useAppointmentQuickActions } from "../hooks/useAppointmentQuickActions";
import { AppointmentDetailDialog } from "./AppointmentDetailDialog";
import { AppointmentEditorDialog } from "./AppointmentEditorDialog";
import { AppointmentRescheduleDialog } from "./AppointmentRescheduleDialog";
import { AppointmentToolbar } from "./AppointmentToolbar";
import { AppointmentWeek } from "./AppointmentWeek";

interface AppointmentManagementProps {
  readonly id: string;
}

export function AppointmentManagement({ id }: AppointmentManagementProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const permissions = new Set(session?.activeTenant.permissions ?? []);
  const canManage = permissions.has("appointments.manage");
  const scope = permissions.has("appointments.read") ? "tenant" : "own";
  const calendar = useAppointmentCalendar({
    canManage,
    scope,
    timeZone: session?.activeTenant.timeZone ?? "UTC",
  });
  const detail = useAppointmentDetail({ onChanged: calendar.reload, scope, t });
  const quickActions = useAppointmentQuickActions({
    onChanged: calendar.reload,
    scope,
    t,
  });
  const editor = useAppointmentEditor({
    customers: calendar.customers,
    employees: calendar.employees,
    onSaved: calendar.reload,
    services: calendar.services,
    t,
    today: calendar.today,
  });
  const reschedule = useAppointmentReschedule({
    appointment: detail.detail?.appointment ?? null,
    onSaved: detail.updateDetail,
    t,
  });
  if (!session) return null;

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/appointments">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={
            scope === "tenant" ? t("app:appointments.eyebrow") : t("app:appointments.ownEyebrow")
          }
          title={t("app:appointments.title")}
          createLabel={canManage ? t("app:appointments.create") : undefined}
          onCreate={canManage ? editor.openCreate : undefined}
        />
        <AppointmentToolbar id={`${id}.toolbar`} calendar={calendar} canManage={canManage} t={t} />
        {calendar.loadError && (
          <Alert
            id={`${id}.loadError`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={calendar.reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {t("app:appointments.loadError")}
          </Alert>
        )}
        {calendar.catalogError && canManage && (
          <Alert id={`${id}.catalogError`} severity="warning">
            {t("app:appointments.catalogError")}
          </Alert>
        )}
        {quickActions.error && (
          <Alert id={`${id}.quickActionError`} severity="error" onClose={quickActions.clearError}>
            {quickActions.error}
          </Alert>
        )}
        <AppointmentWeek
          id={`${id}.week`}
          appointments={calendar.appointments}
          dates={calendar.dates}
          loading={calendar.loading}
          onSelect={detail.openDetail}
          onQuickTransition={(appointment, transition) =>
            void quickActions.transition(appointment, transition)
          }
          pendingAppointmentId={quickActions.pendingId}
          t={t}
          today={calendar.today}
        />
      </Stack>
      {canManage && <AppointmentEditorDialog id={`${id}.createDialog`} editor={editor} t={t} />}
      <AppointmentDetailDialog
        id={`${id}.detailDialog`}
        detail={detail}
        canManage={canManage}
        onReschedule={reschedule.openEditor}
        t={t}
      />
      {canManage && (
        <AppointmentRescheduleDialog id={`${id}.rescheduleDialog`} reschedule={reschedule} t={t} />
      )}
    </WorkspaceShell>
  );
}
