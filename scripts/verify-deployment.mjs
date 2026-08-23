import { mkdtemp, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";

import { repositoryRoot, run, runCompose } from "./lib/commands.mjs";

const temporaryDirectory = await mkdtemp(
  path.join(tmpdir(), "appointment-crm-deployment-"),
);
const secretNames = [
  "postgres-connection",
  "redis-connection",
  "demo-password",
  "data-protection-password",
  "data-protection.pfx",
  "postgres-maintenance-url",
  "postgres-restore-url",
  "backup-passphrase",
];
const apiImage =
  "ghcr.io/aaliakinci/appointment_crm/api@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
const webImage =
  "ghcr.io/aaliakinci/appointment_crm/web@sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
const opsImage =
  "ghcr.io/aaliakinci/appointment_crm/ops@sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";

try {
  await Promise.all(
    secretNames.map((secretName) =>
      writeFile(path.join(temporaryDirectory, secretName), "", "utf8"),
    ),
  );

  await runCompose(
    null,
    [
      "--env-file",
      "deploy/environments/staging.env.example",
      "--file",
      "deploy/compose.release.yaml",
      "config",
      "--quiet",
    ],
    {
      env: {
        APPOINTMENTCRM_SECRET_DIRECTORY: temporaryDirectory,
        APPOINTMENTCRM_API_IMAGE: apiImage,
        APPOINTMENTCRM_WEB_IMAGE: webImage,
      },
    },
  );

  await runCompose(
    null,
    [
      "--env-file",
      "deploy/environments/staging.env.example",
      "--file",
      "deploy/compose.operations.yaml",
      "config",
      "--quiet",
    ],
    {
      env: {
        APPOINTMENTCRM_SECRET_DIRECTORY: temporaryDirectory,
        APPOINTMENTCRM_OPS_IMAGE: opsImage,
        APPOINTMENTCRM_BACKUP_FILE: "fixture.dump.enc",
      },
    },
  );

  const shellScripts = [
    "deploy/generate-data-protection-certificate.sh",
    "deploy/ops/backup.sh",
    "deploy/ops/restore-rehearsal.sh",
    "deploy/release.sh",
    "deploy/smoke.sh",
    "scripts/browser-e2e.sh",
    "scripts/capture-portfolio.sh",
    "scripts/container-smoke.sh",
    "scripts/dev-down.sh",
    "scripts/dev.sh",
    "scripts/migrate.sh",
    "scripts/publish-release.sh",
    "scripts/uptime-check.sh",
    "scripts/verify-deployment.sh",
    "scripts/verify.sh",
  ];
  await run("docker", [
    "run",
    "--rm",
    "--volume",
    `${repositoryRoot}:/workspace:ro`,
    "--workdir",
    "/workspace",
    "alpine:3.24.1@sha256:28bd5fe8b56d1bd048e5babf5b10710ebe0bae67db86916198a6eec434943f8b",
    "sh",
    "-n",
    ...shellScripts,
  ]);
} finally {
  await rm(temporaryDirectory, { recursive: true, force: true });
}
