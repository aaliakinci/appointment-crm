import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";
import path from "node:path";

export const repositoryRoot = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "../..",
);
export const webRoot = path.join(repositoryRoot, "src", "web");

export function platformCommand(command) {
  return process.platform === "win32" && command === "npm"
    ? "npm.cmd"
    : command;
}

function printableArgument(argument) {
  return /[\s"]/u.test(argument) ? JSON.stringify(argument) : argument;
}

export async function run(command, args, options = {}) {
  const executable = platformCommand(command);
  const useShell = process.platform === "win32" && executable.endsWith(".cmd");
  const cwd = options.cwd ?? repositoryRoot;
  const env = { ...process.env, ...options.env };
  const allowFailure = options.allowFailure ?? false;

  process.stdout.write(
    `> ${[executable, ...args].map(printableArgument).join(" ")}\n`,
  );

  const exitCode = await new Promise((resolve, reject) => {
    const child = spawn(executable, args, {
      cwd,
      env,
      shell: useShell,
      stdio: "inherit",
      windowsHide: true,
    });
    child.once("error", reject);
    child.once("exit", (code, signal) => {
      if (signal) {
        reject(new Error(`${command} stopped by signal ${signal}.`));
        return;
      }

      resolve(code ?? 1);
    });
  });

  if (exitCode !== 0 && !allowFailure) {
    throw new Error(`${command} exited with code ${exitCode}.`);
  }

  return exitCode;
}

export function composeArgs(projectName, args) {
  return projectName
    ? ["compose", "--project-name", projectName, ...args]
    : ["compose", ...args];
}

export async function runCompose(projectName, args, options = {}) {
  return run("docker", composeArgs(projectName, args), options);
}

export function installCleanupOnSignals(cleanup) {
  let stopping = false;
  for (const signal of ["SIGINT", "SIGTERM"]) {
    process.once(signal, () => {
      if (stopping) {
        return;
      }

      stopping = true;
      void cleanup().finally(() => process.exit(1));
    });
  }
}
