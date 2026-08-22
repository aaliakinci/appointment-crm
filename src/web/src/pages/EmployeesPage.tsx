import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Table } from "@lily_platform/lily_ui/ui/atoms/Table";
import type { TableColumn, TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useEffect, useMemo, useState, type FormEvent } from "react";

import {
  createEmployee,
  listEmployees,
  listEmployeeUserOptions,
  listServices,
  setEmployeeActive,
  setEmployeeServices,
  updateEmployee,
  type Employee,
  type EmployeeInput,
  type EmployeeUserOption,
  type PagedResponse,
  type ServiceOffering,
} from "@/api";
import { WorkspaceShell } from "@/components";
import { useAppTranslation } from "@/i18n";
import { useAuth } from "@/state";

import { validateEmployeeInput } from "./masterDataValidation";

interface EmployeesPageProps {
  readonly id: string;
}

interface EmployeeRow extends TableRowData {
  readonly resource: Employee;
  readonly name: string;
  readonly services: string;
  readonly contact: string;
  readonly status: string;
}

const emptyPage: PagedResponse<Employee> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

const emptyInput: EmployeeInput = { userId: null, name: "", email: null, phone: null };

export function EmployeesPage({ id }: EmployeesPageProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const canManage = session?.activeTenant.permissions.includes("employees.manage") ?? false;
  const [result, setResult] = useState<PagedResponse<Employee>>(emptyPage);
  const [serviceOptions, setServiceOptions] = useState<readonly ServiceOffering[]>([]);
  const [userOptions, setUserOptions] = useState<readonly EmployeeUserOption[]>([]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [serviceFilter, setServiceFilter] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selected, setSelected] = useState<Employee | null>(null);
  const [input, setInput] = useState<EmployeeInput>(emptyInput);
  const [selectedServiceIds, setSelectedServiceIds] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    const employeesPromise = listEmployees(
      {
        page: page + 1,
        pageSize,
        search,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        serviceId: serviceFilter || undefined,
        sortBy: "name",
        sortDirection: "asc",
      },
      controller.signal,
    );
    const servicesPromise = listServices(
      { page: 1, pageSize: 100, sortBy: "name", sortDirection: "asc" },
      controller.signal,
    );
    const usersPromise = canManage
      ? listEmployeeUserOptions(controller.signal)
      : Promise.resolve([] as readonly EmployeeUserOption[]);
    void Promise.all([employeesPromise, servicesPromise, usersPromise])
      .then(([employees, services, users]) => {
        if (!controller.signal.aborted) {
          setResult(employees);
          setServiceOptions(services.items);
          setUserOptions(users);
          setLoadError(false);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setLoadError(true);
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [activeFilter, canManage, page, pageSize, reloadVersion, search, serviceFilter]);

  const rows = useMemo<EmployeeRow[]>(
    () =>
      result.items.map((employee) => ({
        id: employee.id,
        resource: employee,
        name: employee.name,
        services: employee.services.map((service) => service.name).join(", ") || "—",
        contact: employee.email ?? employee.phone ?? "—",
        status: employee.isActive ? t("app:common.active") : t("app:common.inactive"),
      })),
    [result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "name", label: t("app:employees.name"), priority: "primary" },
      { id: "services", label: t("app:employees.services"), priority: "secondary" },
      { id: "contact", label: t("app:employees.contact"), priority: "tertiary" },
      {
        id: "status",
        label: t("app:common.status"),
        format: (value) => (
          <Chip
            id={`${id}.status.${String(value)}`}
            size="small"
            color={value === t("app:common.active") ? "success" : "default"}
            label={String(value)}
          />
        ),
      },
    ],
    [id, t],
  );

  function openCreate() {
    setSelected(null);
    setInput(emptyInput);
    setSelectedServiceIds([]);
    setFormError(null);
    setDialogOpen(true);
  }

  function openDetail(employee: Employee) {
    setSelected(employee);
    setInput({
      userId: employee.userId,
      name: employee.name,
      email: employee.email,
      phone: employee.phone,
    });
    setSelectedServiceIds(
      employee.services.filter((service) => service.isActive).map((service) => service.id),
    );
    setFormError(null);
    setDialogOpen(true);
  }

  async function save() {
    if (validateEmployeeInput(input)) {
      setFormError(t("app:employees.validation"));
      return;
    }

    setSaving(true);
    setFormError(null);
    try {
      if (selected) {
        await updateEmployee(selected.id, input);
        await setEmployeeServices(selected.id, selectedServiceIds);
      } else {
        await createEmployee({ ...input, serviceIds: selectedServiceIds });
      }
      setDialogOpen(false);
      reload();
    } catch {
      setFormError(t("app:employees.saveError"));
    } finally {
      setSaving(false);
    }
  }

  async function changeActivation() {
    if (!selected) {
      return;
    }

    setSaving(true);
    setFormError(null);
    try {
      await setEmployeeActive(selected.id, !selected.isActive);
      setDialogOpen(false);
      reload();
    } catch {
      setFormError(t("app:employees.activationError"));
    } finally {
      setSaving(false);
    }
  }

  function applySearch(event: FormEvent<HTMLElement>) {
    event.preventDefault();
    reload();
    setPage(0);
    setSearch(searchDraft.trim());
  }

  const availableUsers = userOptions.filter(
    (option) => !option.isLinked || option.userId === selected?.userId,
  );

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/employees">
      <Stack id={`${id}.content`} spacing={3}>
        <Stack
          id={`${id}.heading`}
          direction={{ xs: "column", sm: "row" }}
          spacing={2}
          sx={{ alignItems: { sm: "center" } }}
        >
          <Box id={`${id}.titleBlock`} sx={{ flex: 1 }}>
            <Typography id={`${id}.eyebrow`} component="p" variant="overline" color="primary">
              {t("app:employees.eyebrow")}
            </Typography>
            <Typography id={`${id}.title`} component="h1" variant="h3">
              {t("app:employees.title")}
            </Typography>
          </Box>
          {canManage && (
            <Button id={`${id}.create`} variant="contained" onClick={openCreate}>
              {t("app:employees.create")}
            </Button>
          )}
        </Stack>

        <Stack
          id={`${id}.filters`}
          component="form"
          direction={{ xs: "column", lg: "row" }}
          spacing={2}
          onSubmit={applySearch}
        >
          <TextField
            id={`${id}.search`}
            label={t("app:common.search")}
            value={searchDraft}
            onValueChange={setSearchDraft}
            fullWidth
          />
          <Select
            id={`${id}.activeFilter`}
            label={t("app:common.status")}
            value={activeFilter}
            options={[
              { id: "all", value: "all", label: t("app:common.all") },
              { id: "active", value: "active", label: t("app:common.active") },
              { id: "inactive", value: "inactive", label: t("app:common.inactive") },
            ]}
            onValueChange={(value) => {
              setLoading(true);
              setPage(0);
              setActiveFilter(String(value));
            }}
            sx={{ minWidth: 180 }}
          />
          <Select
            id={`${id}.serviceFilter`}
            label={t("app:employees.serviceFilter")}
            value={serviceFilter}
            options={[
              { id: "all", value: "", label: t("app:common.all") },
              ...serviceOptions.map((service) => ({
                id: service.id,
                value: service.id,
                label: service.name,
              })),
            ]}
            onValueChange={(value) => {
              setLoading(true);
              setPage(0);
              setServiceFilter(String(value));
            }}
            sx={{ minWidth: 220 }}
          />
          <Button id={`${id}.applyFilters`} type="submit" variant="outlined">
            {t("app:common.apply")}
          </Button>
        </Stack>

        {loadError && (
          <Alert
            id={`${id}.loadError`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {t("app:employees.loadError")}
          </Alert>
        )}

        <Table
          id={`${id}.table`}
          columns={columns}
          rows={rows}
          loading={loading}
          emptyContent={t("app:employees.empty")}
          pagination
          page={page}
          rowsPerPage={pageSize}
          totalCount={result.totalCount}
          rowsPerPageOptions={[10, 20, 50, 100]}
          onPageChange={(value) => {
            setLoading(true);
            setPage(value);
          }}
          onRowsPerPageChange={(value) => {
            setLoading(true);
            setPage(0);
            setPageSize(value);
          }}
          getRowAriaLabel={(row) => String(row.name)}
          onRowActivate={(row) => openDetail((row as EmployeeRow).resource)}
        />
      </Stack>

      <Dialog
        id={`${id}.dialog`}
        open={dialogOpen}
        fullWidth
        maxWidth="sm"
        dialogTitle={selected ? t("app:employees.detailTitle") : t("app:employees.createTitle")}
        onOpenChange={(open) => !saving && setDialogOpen(open)}
        content={
          <Stack id={`${id}.dialogFields`} spacing={2} sx={{ pt: 1 }}>
            {formError && (
              <Alert id={`${id}.formError`} severity="error">
                {formError}
              </Alert>
            )}
            <TextField
              id={`${id}.name`}
              label={t("app:employees.name")}
              value={input.name}
              onValueChange={(value) => setInput((current) => ({ ...current, name: value }))}
              required
              fullWidth
              disabled={!canManage}
            />
            <TextField
              id={`${id}.email`}
              type="email"
              label={t("app:employees.email")}
              value={input.email ?? ""}
              onValueChange={(value) =>
                setInput((current) => ({ ...current, email: value || null }))
              }
              fullWidth
              disabled={!canManage}
            />
            <TextField
              id={`${id}.phone`}
              label={t("app:employees.phone")}
              value={input.phone ?? ""}
              onValueChange={(value) =>
                setInput((current) => ({ ...current, phone: value || null }))
              }
              fullWidth
              disabled={!canManage}
            />
            {canManage && (
              <Select
                id={`${id}.user`}
                label={t("app:employees.user")}
                value={input.userId ?? ""}
                options={[
                  { id: "none", value: "", label: t("app:employees.noUser") },
                  ...availableUsers.map((option) => ({
                    id: option.userId,
                    value: option.userId,
                    label: `${option.displayName} — ${option.role}`,
                  })),
                ]}
                onValueChange={(value) =>
                  setInput((current) => ({ ...current, userId: String(value) || null }))
                }
                fullWidth
              />
            )}
            <Select
              id={`${id}.services`}
              label={t("app:employees.services")}
              multiple
              value={selectedServiceIds}
              options={serviceOptions.map((service) => ({
                id: service.id,
                value: service.id,
                label: service.name,
                disabled: !service.isActive,
              }))}
              onValueChange={(value) =>
                setSelectedServiceIds(Array.isArray(value) ? value.map(String) : [])
              }
              fullWidth
              disabled={!canManage}
            />
          </Stack>
        }
        actions={
          <Stack id={`${id}.dialogActions`} direction="row" spacing={1}>
            {selected && canManage && (
              <Button
                id={`${id}.activation`}
                color={selected.isActive ? "warning" : "success"}
                disabled={saving}
                onClick={() => void changeActivation()}
              >
                {selected.isActive ? t("app:common.deactivate") : t("app:common.activate")}
              </Button>
            )}
            <Button id={`${id}.close`} disabled={saving} onClick={() => setDialogOpen(false)}>
              {t("app:common.close")}
            </Button>
            {canManage && (
              <Button
                id={`${id}.save`}
                variant="contained"
                loading={saving}
                onClick={() => void save()}
              >
                {t("app:common.save")}
              </Button>
            )}
          </Stack>
        }
      />
    </WorkspaceShell>
  );
}
