import {
  installCleanupOnSignals,
  repositoryRoot,
  run,
  runCompose,
  webRoot,
} from "./lib/commands.mjs";

const nodeMajor = Number.parseInt(
  process.versions.node.split(".")[0] ?? "0",
  10,
);
if (nodeMajor !== 22) {
  throw new Error(`Node.js 22 is required; found ${process.version}.`);
}

await run("dotnet", [
  "restore",
  "AppointmentCrm.slnx",
  "--locked-mode",
  "--disable-parallel",
  "--verbosity",
  "minimal",
  "-m:1",
]);
await run("dotnet", [
  "format",
  "AppointmentCrm.slnx",
  "--verify-no-changes",
  "--no-restore",
  "--verbosity",
  "minimal",
]);
await run("dotnet", [
  "build",
  "AppointmentCrm.slnx",
  "--configuration",
  "Release",
  "--no-restore",
  "--verbosity",
  "minimal",
  "-m:1",
]);
await run("dotnet", [
  "test",
  "tests/AppointmentCrm.UnitTests/AppointmentCrm.UnitTests.csproj",
  "--configuration",
  "Release",
  "--no-build",
  "--no-restore",
  "--verbosity",
  "minimal",
  "-m:1",
]);

const projectName = "appointment-crm-verify";
const postgresPort = process.env.APPOINTMENTCRM_VERIFY_POSTGRES_PORT ?? "55433";
const redisPort = process.env.APPOINTMENTCRM_VERIFY_REDIS_PORT ?? "56380";
const postgresPassword = "verify-local-only";
const composeEnvironment = {
  APPOINTMENTCRM_POSTGRES_DB: "appointment_crm",
  APPOINTMENTCRM_POSTGRES_USER: "appointment_crm",
  APPOINTMENTCRM_POSTGRES_PASSWORD: postgresPassword,
  APPOINTMENTCRM_POSTGRES_PORT: postgresPort,
  APPOINTMENTCRM_REDIS_PORT: redisPort,
};

let cleaned = false;
async function cleanup() {
  if (cleaned) return;
  cleaned = true;
  await runCompose(projectName, ["down", "--volumes"], {
    env: composeEnvironment,
    allowFailure: true,
  });
}

installCleanupOnSignals(cleanup);

try {
  await runCompose(
    projectName,
    ["up", "--detach", "--wait", "--wait-timeout", "90", "postgres", "redis"],
    { env: composeEnvironment },
  );
  await run(
    "dotnet",
    [
      "test",
      "tests/AppointmentCrm.IntegrationTests/AppointmentCrm.IntegrationTests.csproj",
      "--configuration",
      "Release",
      "--no-build",
      "--no-restore",
      "--verbosity",
      "minimal",
      "-m:1",
    ],
    {
      env: {
        APPOINTMENTCRM_TEST_POSTGRES:
          `Host=127.0.0.1;Port=${postgresPort};Database=appointment_crm;` +
          `Username=appointment_crm;Password=${postgresPassword}`,
        APPOINTMENTCRM_TEST_REDIS:
          `127.0.0.1:${redisPort},abortConnect=false,` +
          "connectTimeout=1000,syncTimeout=1000",
      },
    },
  );
} finally {
  await cleanup();
}

await run("npm", ["ci", "--no-audit", "--no-fund"], { cwd: webRoot });
await run("npm", ["run", "format:check"], { cwd: webRoot });
await run("npm", ["run", "lint"], { cwd: webRoot });
await run("npm", ["run", "typecheck"], { cwd: webRoot });
await run("npm", ["test", "--", "--run"], { cwd: webRoot });
await run("npm", ["run", "build"], { cwd: webRoot });
await runCompose(null, ["config", "--quiet"]);
await run(process.execPath, ["scripts/verify-deployment.mjs"], {
  cwd: repositoryRoot,
});
await run(process.execPath, ["scripts/container-smoke.mjs"], {
  cwd: repositoryRoot,
});
