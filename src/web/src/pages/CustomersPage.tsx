import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Checkbox } from "@lily_platform/lily_ui/ui/atoms/Checkbox";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Table } from "@lily_platform/lily_ui/ui/atoms/Table";
import type { TableColumn, TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useEffect, useMemo, useState, type FormEvent } from "react";

import {
  archiveCustomer,
  createCustomer,
  listCustomers,
  updateCustomer,
  type Customer,
  type CustomerInput,
  type PagedResponse,
} from "@/api";
import { WorkspaceShell } from "@/components";
import { useAppTranslation } from "@/i18n";
import { useAuth } from "@/state";

import { validateCustomerInput } from "./masterDataValidation";

interface CustomersPageProps {
  readonly id: string;
}

interface CustomerRow extends TableRowData {
  readonly resource: Customer;
  readonly name: string;
  readonly contact: string;
  readonly status: string;
  readonly updatedAtUtc: string;
}

const emptyPage: PagedResponse<Customer> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

const emptyInput: CustomerInput = { name: "", email: null, phone: null, notes: null };

export function CustomersPage({ id }: CustomersPageProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const canManage = session?.activeTenant.permissions.includes("customers.manage") ?? false;
  const [result, setResult] = useState<PagedResponse<Customer>>(emptyPage);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [includeArchived, setIncludeArchived] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selected, setSelected] = useState<Customer | null>(null);
  const [input, setInput] = useState<CustomerInput>(emptyInput);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    void listCustomers(
      {
        page: page + 1,
        pageSize,
        search,
        includeArchived,
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
  }, [includeArchived, page, pageSize, reloadVersion, search]);

  const rows = useMemo<CustomerRow[]>(
    () =>
      result.items.map((customer) => ({
        id: customer.id,
        resource: customer,
        name: customer.name,
        contact: customer.email ?? customer.phone ?? "—",
        status: customer.archivedAtUtc ? t("app:common.archived") : t("app:common.active"),
        updatedAtUtc: customer.updatedAtUtc,
      })),
    [result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "name", label: t("app:customers.name"), priority: "primary" },
      { id: "contact", label: t("app:customers.contact"), priority: "secondary" },
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
      {
        id: "updatedAtUtc",
        label: t("app:common.updated"),
        priority: "tertiary",
        format: (value) => formatDate(String(value)),
      },
    ],
    [id, t],
  );

  function openCreate() {
    setSelected(null);
    setInput(emptyInput);
    setFormError(null);
    setDialogOpen(true);
  }

  function openDetail(customer: Customer) {
    setSelected(customer);
    setInput({
      name: customer.name,
      email: customer.email,
      phone: customer.phone,
      notes: customer.notes,
    });
    setFormError(null);
    setDialogOpen(true);
  }

  async function save() {
    if (validateCustomerInput(input)) {
      setFormError(t("app:customers.validation"));
      return;
    }

    setSaving(true);
    setFormError(null);
    try {
      if (selected) {
        await updateCustomer(selected.id, input);
      } else {
        await createCustomer(input);
      }
      setDialogOpen(false);
      reload();
    } catch {
      setFormError(t("app:customers.saveError"));
    } finally {
      setSaving(false);
    }
  }

  async function archive() {
    if (!selected) {
      return;
    }

    setSaving(true);
    setFormError(null);
    try {
      await archiveCustomer(selected.id);
      setDialogOpen(false);
      reload();
    } catch {
      setFormError(t("app:customers.archiveError"));
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

  const archived = selected?.archivedAtUtc != null;
  const editable = canManage && !archived;

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/customers">
      <Stack id={`${id}.content`} spacing={3}>
        <Stack
          id={`${id}.heading`}
          direction={{ xs: "column", sm: "row" }}
          spacing={2}
          sx={{ alignItems: { sm: "center" } }}
        >
          <Box id={`${id}.titleBlock`} sx={{ flex: 1 }}>
            <Typography id={`${id}.eyebrow`} component="p" variant="overline" color="primary">
              {t("app:customers.eyebrow")}
            </Typography>
            <Typography id={`${id}.title`} component="h1" variant="h3">
              {t("app:customers.title")}
            </Typography>
          </Box>
          {canManage && (
            <Button id={`${id}.create`} variant="contained" onClick={openCreate}>
              {t("app:customers.create")}
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
          <Checkbox
            id={`${id}.includeArchived`}
            label={t("app:customers.includeArchived")}
            checked={includeArchived}
            onCheckedChange={(checked) => {
              setLoading(true);
              setPage(0);
              setIncludeArchived(checked);
            }}
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
            {t("app:customers.loadError")}
          </Alert>
        )}

        <Table
          id={`${id}.table`}
          columns={columns}
          rows={rows}
          loading={loading}
          emptyContent={t("app:customers.empty")}
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
          onRowActivate={(row) => openDetail((row as CustomerRow).resource)}
        />
      </Stack>

      <Dialog
        id={`${id}.dialog`}
        open={dialogOpen}
        fullWidth
        maxWidth="sm"
        dialogTitle={selected ? t("app:customers.detailTitle") : t("app:customers.createTitle")}
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
              label={t("app:customers.name")}
              value={input.name}
              onValueChange={(value) => setInput((current) => ({ ...current, name: value }))}
              required
              fullWidth
              disabled={!editable && selected !== null}
            />
            <TextField
              id={`${id}.email`}
              type="email"
              label={t("app:customers.email")}
              value={input.email ?? ""}
              onValueChange={(value) =>
                setInput((current) => ({ ...current, email: value || null }))
              }
              fullWidth
              disabled={!editable && selected !== null}
            />
            <TextField
              id={`${id}.phone`}
              label={t("app:customers.phone")}
              value={input.phone ?? ""}
              onValueChange={(value) =>
                setInput((current) => ({ ...current, phone: value || null }))
              }
              fullWidth
              disabled={!editable && selected !== null}
            />
            <TextField
              id={`${id}.notes`}
              label={t("app:customers.notes")}
              value={input.notes ?? ""}
              onValueChange={(value) =>
                setInput((current) => ({ ...current, notes: value || null }))
              }
              multiline
              minRows={3}
              fullWidth
              disabled={!editable && selected !== null}
            />
          </Stack>
        }
        actions={
          <Stack id={`${id}.dialogActions`} direction="row" spacing={1}>
            {selected && editable && (
              <Button
                id={`${id}.archive`}
                color="warning"
                disabled={saving}
                onClick={() => void archive()}
              >
                {t("app:customers.archive")}
              </Button>
            )}
            <Button id={`${id}.close`} disabled={saving} onClick={() => setDialogOpen(false)}>
              {t("app:common.close")}
            </Button>
            {(editable || !selected) && (
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

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}
