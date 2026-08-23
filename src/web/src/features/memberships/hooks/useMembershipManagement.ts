import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormBindings,
} from "@lily_platform/lily_ui/ui/forms";
import { useEffect, useMemo, useState } from "react";

import { getMembershipReport, listMemberships, updateMembership } from "../api/membershipApi";
import { tenantRoles, type Membership, type MembershipReport } from "../api/membershipContract";
import {
  createMembershipFormDefinition,
  toMembershipFormValues,
  type MembershipFormValues,
} from "../model/membershipForm";

const emptyReport: MembershipReport = { total: 0, active: 0, byRole: {} };

interface UseMembershipManagementOptions {
  readonly canManage: boolean;
  readonly t: (key: string) => string;
}

export function useMembershipManagement({ canManage, t }: UseMembershipManagementOptions) {
  const [memberships, setMemberships] = useState<readonly Membership[]>([]);
  const [report, setReport] = useState<MembershipReport>(emptyReport);
  const [selected, setSelected] = useState<Membership | null>(null);
  const [loading, setLoading] = useState(true);
  const [mutationPending, setMutationPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const definition = useMemo(() => createMembershipFormDefinition(t), [t]);
  const controller = useMemo(() => createLilyFormController<MembershipFormValues>(), []);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const initialValues = useMemo(
    () => (selected ? toMembershipFormValues(selected) : { role: "Employee" as const }),
    [selected],
  );
  const bindings = useMemo<LilyFormBindings<MembershipFormValues>>(
    () => ({
      role: {
        options: tenantRoles.map((role) => ({
          id: role,
          value: role,
          label: t(`app:memberships.roles.${role}`),
        })),
      },
    }),
    [t],
  );

  useEffect(() => {
    const abortController = new AbortController();
    void Promise.all([
      listMemberships(abortController.signal),
      getMembershipReport(abortController.signal),
    ])
      .then(([items, nextReport]) => {
        if (!abortController.signal.aborted) {
          setMemberships(items);
          setReport(nextReport);
          setError(null);
        }
      })
      .catch(() => {
        if (!abortController.signal.aborted) setError(t("app:memberships.loadError"));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setLoading(false);
      });
    return () => abortController.abort();
  }, [revision, t]);

  async function submit(values: MembershipFormValues) {
    if (!selected) return;
    setError(null);
    try {
      await updateMembership(selected.id, values.role, selected.isActive);
      setSelected(null);
      setLoading(true);
      setRevision((value) => value + 1);
    } catch {
      setError(t("app:memberships.saveError"));
    }
  }

  async function toggleActive() {
    if (!selected) return;
    setMutationPending(true);
    setError(null);
    try {
      await updateMembership(selected.id, selected.role, !selected.isActive);
      setSelected(null);
      setLoading(true);
      setRevision((value) => value + 1);
    } catch {
      setError(t("app:memberships.saveError"));
    } finally {
      setMutationPending(false);
    }
  }

  return {
    bindings,
    canManage,
    controller,
    definition,
    error,
    formSubmitting,
    initialValues,
    loading,
    memberships,
    mutationPending,
    report,
    selected,
    submit,
    toggleActive,
    openDetail: setSelected,
    close: () => !mutationPending && !formSubmitting && setSelected(null),
    clearError: () => setError(null),
    handleSubmitError: () => setError(t("app:memberships.saveError")),
    reload: () => {
      setLoading(true);
      setRevision((value) => value + 1);
    },
  };
}
