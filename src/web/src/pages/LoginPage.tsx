import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useState, type FormEvent } from "react";

import type { TenantOption } from "@/api";
import { PublicShell } from "@/components";
import { useAppTranslation } from "@/i18n";
import { useAuth } from "@/state";

import { validateLoginInput } from "./loginValidation";

interface LoginPageProps {
  readonly id: string;
}

export function LoginPage({ id }: LoginPageProps) {
  const navigate = useLilyNavigate();
  const { t } = useAppTranslation();
  const auth = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [tenantId, setTenantId] = useState("");
  const [tenantOptions, setTenantOptions] = useState<readonly TenantOption[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit(event: FormEvent<HTMLElement>) {
    event.preventDefault();
    setError(null);
    const validationError = validateLoginInput(email, password, tenantOptions.length > 0, tenantId);
    if (validationError) {
      setError(
        validationError === "tenant" ? t("app:login.tenantRequired") : t("app:login.validation"),
      );
      return;
    }

    setSubmitting(true);
    try {
      const response = await auth.login({
        email,
        password,
        tenantId: tenantId || null,
      });
      if (response.requiresTenantSelection) {
        setTenantOptions(response.tenants);
        setTenantId(response.tenants[0]?.id ?? "");
        return;
      }

      setPassword("");
      await navigate("/account");
    } catch {
      setError(t("app:login.error"));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <PublicShell
      id={`${id}.shell`}
      activePath="/login"
      brandLabel={t("app:brand")}
      statusLabel={t("app:navigation.status")}
      loginLabel={t("app:navigation.login")}
      skipToContentLabel={t("app:shell.skipToContent")}
      portfolioNotice={t("app:shell.portfolioNotice")}
      onNavigate={(path) => {
        void navigate(path);
      }}
    >
      <Stack id={`${id}.content`} spacing={3} sx={{ maxWidth: 560 }}>
        <Box id={`${id}.heading`}>
          <Typography
            id={`${id}.eyebrow`}
            component="p"
            variant="overline"
            sx={{ color: "primary.main" }}
          >
            {t("app:login.eyebrow")}
          </Typography>
          <Typography id={`${id}.title`} component="h1" variant="h3">
            {t("app:login.title")}
          </Typography>
          <Typography
            id={`${id}.description`}
            component="p"
            sx={{ mt: 2, color: "text.secondary" }}
          >
            {t("app:login.description")}
          </Typography>
        </Box>

        {error && (
          <Alert id={`${id}.error`} severity="error">
            {error}
          </Alert>
        )}

        <Box id={`${id}.form`} component="form" onSubmit={(event) => void submit(event)}>
          <Stack id={`${id}.fields`} spacing={2.5}>
            <TextField
              id={`${id}.email`}
              name="email"
              type="email"
              label={t("app:login.email")}
              value={email}
              onValueChange={setEmail}
              autoComplete="username"
              required
              fullWidth
              disabled={submitting}
            />
            <TextField
              id={`${id}.password`}
              name="password"
              type="password"
              label={t("app:login.password")}
              value={password}
              onValueChange={setPassword}
              autoComplete="current-password"
              required
              fullWidth
              disabled={submitting}
            />
            {tenantOptions.length > 0 && (
              <Select
                id={`${id}.tenant`}
                name="tenantId"
                label={t("app:login.tenant")}
                value={tenantId}
                options={tenantOptions.map((tenant) => ({
                  id: tenant.id,
                  value: tenant.id,
                  label: `${tenant.name} — ${tenant.role}`,
                }))}
                onValueChange={(value) => setTenantId(String(value))}
                required
                fullWidth
                disabled={submitting}
              />
            )}
            <Button id={`${id}.submit`} type="submit" variant="contained" disabled={submitting}>
              {submitting ? t("app:login.submitting") : t("app:login.submit")}
            </Button>
          </Stack>
        </Box>

        <Alert id={`${id}.security`} severity="info">
          {t("app:login.securityNotice")}
        </Alert>
      </Stack>
    </PublicShell>
  );
}
