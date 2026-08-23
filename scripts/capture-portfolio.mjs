import {
  installCleanupOnSignals,
  run,
  runCompose,
  webRoot,
} from "./lib/commands.mjs";

const projectName = "appointment-crm-portfolio";
const postgresPort =
  process.env.APPOINTMENTCRM_PORTFOLIO_POSTGRES_PORT ?? "55437";
const redisPort = process.env.APPOINTMENTCRM_PORTFOLIO_REDIS_PORT ?? "56383";
const apiPort = process.env.APPOINTMENTCRM_PORTFOLIO_API_PORT ?? "58083";
const webPort = process.env.APPOINTMENTCRM_PORTFOLIO_WEB_PORT ?? "55176";
const password =
  process.env.APPOINTMENTCRM_PORTFOLIO_PASSWORD ?? "Portfolio-local-2026!";
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

async function capture(language, seedAppointment) {
  await run("npm", ["run", "portfolio:capture"], {
    cwd: webRoot,
    env: {
      APPOINTMENTCRM_PORTFOLIO_BASE_URL: `http://localhost:${webPort}`,
      APPOINTMENTCRM_PORTFOLIO_PASSWORD: password,
      APPOINTMENTCRM_PORTFOLIO_LANGUAGE: language,
      APPOINTMENTCRM_PORTFOLIO_SEED: String(seedAppointment),
    },
  });
}

installCleanupOnSignals(cleanup);

try {
  await runCompose(
    projectName,
    ["up", "--build", "--detach", "--wait", "--wait-timeout", "180"],
    { env: composeEnvironment },
  );
  await capture("tr", true);
  await capture("en", false);
} finally {
  await cleanup();
}
