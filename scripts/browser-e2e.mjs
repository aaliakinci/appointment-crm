import {
  installCleanupOnSignals,
  run,
  runCompose,
  webRoot,
} from "./lib/commands.mjs";

const projectName = "appointment-crm-browser-e2e";
const postgresPort = process.env.APPOINTMENTCRM_E2E_POSTGRES_PORT ?? "55436";
const redisPort = process.env.APPOINTMENTCRM_E2E_REDIS_PORT ?? "56382";
const apiPort = process.env.APPOINTMENTCRM_E2E_API_PORT ?? "58082";
const webPort = process.env.APPOINTMENTCRM_E2E_WEB_PORT ?? "55175";
const password =
  process.env.APPOINTMENTCRM_E2E_PASSWORD ?? "Browser-local-2026!";
const composeEnvironment = {
  APPOINTMENTCRM_POSTGRES_PORT: postgresPort,
  APPOINTMENTCRM_REDIS_PORT: redisPort,
  APPOINTMENTCRM_API_PORT: apiPort,
  APPOINTMENTCRM_WEB_PORT: webPort,
  APPOINTMENTCRM_DEMO_PASSWORD: password,
};

let cleaned = false;
async function cleanup() {
  if (cleaned) {
    return;
  }

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
    ["up", "--build", "--detach", "--wait", "--wait-timeout", "180"],
    { env: composeEnvironment },
  );
  await run("npm", ["run", "e2e"], {
    cwd: webRoot,
    env: {
      APPOINTMENTCRM_E2E_BASE_URL: `http://127.0.0.1:${webPort}`,
      APPOINTMENTCRM_E2E_PASSWORD: password,
    },
  });
} finally {
  await cleanup();
}
