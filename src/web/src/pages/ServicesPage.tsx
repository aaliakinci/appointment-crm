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
  createService,
  listServices,
  setServiceActive,
  updateService,
  type PagedResponse,
  type ServiceInput,
  type ServiceOffering,
} from "@/api";
import { WorkspaceShell } from "@/components";
import { useAppTranslation } from "@/i18n";
import { useAuth } from "@/state";

import { validateServiceInput } from "./masterDataValidation";

interface ServicesPageProps {
  readonly id: string;
}

interface ServiceRow extends TableRowData {
  readonly resource: ServiceOffering;
  readonly name: string;
  readonly duration: string;
  readonly price: string;
  readonly status: string;
}

const emptyPage: PagedResponse<ServiceOffering> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

export function ServicesPage({ id }: ServicesPageProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const currency = session?.activeTenant.currency ?? "TRY";
  const canManage = session?.activeTenant.permissions.includes("services.manage") ?? false;
  const [result, setResult] = useState<PagedResponse<ServiceOffering>>(emptyPage);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("all");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selected, setSelected] = useState<ServiceOffering | null>(null);
  const [name, setName] = useState("");
  const [duration, setDuration] = useState("30");
  const [price, setPrice] = useState("0");
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    void listServices(
      {
        page: page + 1,
        pageSize,
        search,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        sortBy: "name",
        sortDirection: "asc",
      },
      controller.signal,
    )
      .then((response) => {
        if (!controller.signal.aborted) {
          setResult(response);
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
  }, [activeFilter, page, pageSize, reloadVersion, search]);

  const rows = useMemo<ServiceRow[]>(
    () =>
      result.items.map((service) => ({
        id: service.id,
        resource: service,
        name: service.name,
        duration: `${service.durationMinutes} ${t("app:services.minutes")}`,
        price: formatMoney(service.price, service.currency),
        status: service.isActive ? t("app:common.active") : t("app:common.inactive"),
      })),
    [result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "name", label: t("app:services.name"), priority: "primary" },
      { id: "duration", label: t("app:services.duration"), priority: "secondary" },
      { id: "price", label: t("app:services.price"), align: "right" },
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
    setName("");
    setDuration("30");
    setPrice("0");
    setFormError(null);
    setDialogOpen(true);
  }

  function openDetail(service: ServiceOffering) {
    setSelected(service);
    setName(service.name);
    setDuration(String(service.durationMinutes));
    setPrice(String(service.price));
    setFormError(null);
    setDialogOpen(true);
  }

  async function save() {
    const input: ServiceInput = {
      name,
      durationMinutes: Number(duration),
      price: Number(price),
      currency,
    };
    if (validateServiceInput(input)) {
      setFormError(t("app:services.validation"));
      return;
    }

    setSaving(true);
    setFormError(null);
    try {
      if (selected) {
        await updateService(selected.id, input);
      } else {
        await createService(input);
      }
      setDialogOpen(false);
      reload();
    } catch {
      setFormError(t("app:services.saveError"));
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
      await setServiceActive(selected.id, !selected.isActive);
      setDialogOpen(false);
      reload();
    } catch {
      setFormError(t("app:services.activationError"));
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

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/services">
      <Stack id={`${id}.content`} spacing={3}>
        <Stack
          id={`${id}.heading`}
          direction={{ xs: "column", sm: "row" }}
          spacing={2}
          sx={{ alignItems: { sm: "center" } }}
        >
          <Box id={`${id}.titleBlock`} sx={{ flex: 1 }}>
            <Typography id={`${id}.eyebrow`} component="p" variant="overline" color="primary">
              {t("app:services.eyebrow")}
            </Typography>
            <Typography id={`${id}.title`} component="h1" variant="h3">
              {t("app:services.title")}
            </Typography>
          </Box>
          {canManage && (
            <Button id={`${id}.create`} variant="contained" onClick={openCreate}>
              {t("app:services.create")}
            </Button>
          )}
        </Stack>

        <Stack
          id={`${id}.filters`}
          component="form"
          direction={{ xs: "column", md: "row" }}
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
            {t("app:services.loadError")}
          </Alert>
        )}

        <Table
          id={`${id}.table`}
          columns={columns}
          rows={rows}
          loading={loading}
          emptyContent={t("app:services.empty")}
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
          onRowActivate={(row) => openDetail((row as ServiceRow).resource)}
        />
      </Stack>

      <Dialog
        id={`${id}.dialog`}
        open={dialogOpen}
        fullWidth
        maxWidth="sm"
        dialogTitle={selected ? t("app:services.detailTitle") : t("app:services.createTitle")}
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
              label={t("app:services.name")}
              value={name}
              onValueChange={setName}
              required
              fullWidth
              disabled={!canManage}
            />
            <TextField
              id={`${id}.duration`}
              type="number"
              label={t("app:services.duration")}
              value={duration}
              onValueChange={setDuration}
              inputProps={{ min: 5, max: 480, step: 5 }}
              required
              fullWidth
              disabled={!canManage}
            />
            <TextField
              id={`${id}.price`}
              type="number"
              label={t("app:services.price")}
              value={price}
              onValueChange={setPrice}
              inputProps={{ min: 0, max: 1_000_000, step: 0.01 }}
              required
              fullWidth
              disabled={!canManage}
            />
            <TextField
              id={`${id}.currency`}
              label={t("app:services.currency")}
              value={currency}
              onValueChange={() => undefined}
              fullWidth
              disabled
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

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(value);
}
