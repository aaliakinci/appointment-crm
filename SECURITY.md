# Security policy

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Send the repository owner a private GitHub security advisory containing the affected revision, impact, reproduction steps, and any known workaround. Do not include production credentials, access tokens, refresh cookies, passwords, or customer data in the report.

## Data and logging rules

- Access tokens, refresh credentials, passwords, cookie values, authorization headers, and request bodies must never be logged or written to audit summaries.
- Request logs use the controller route template, method, status, latency, correlation ID, and trace ID. Raw query strings and bodies are deliberately excluded.
- Audit and notification delivery records use allow-listed metadata. Customer notes and outbox payloads are not copied into delivery records.
- Background failures persist only a bounded exception type/error code. Exception messages from external providers are not persisted.
- Any new structured log property must be reviewed for secret, credential, customer-content, and unbounded-cardinality risk.

## Dependency and container vulnerability triage

CI audits NuGet and npm dependencies and scans the API and web runtime images with Trivy. High and critical findings fail the quality workflow.

For every finding:

1. Confirm the affected package or image layer and whether the vulnerable code is reachable.
2. Prefer an upstream patched dependency or base image and rerun the complete quality workflow.
3. Treat critical findings as immediate release blockers. Triage high findings within two business days.
4. If no fix exists, document the CVE, reachability evidence, compensating control, owner, and expiry date in a private security advisory before accepting the risk.
5. Never suppress a finding with an unscoped ignore rule. Any temporary ignore must name one vulnerability, include its expiry, and link to the risk acceptance.

False positives and accepted risks remain visible until the upstream fix is deployed. A passing scan is required again when an exception expires.

## Supported versions

Until the first stable release, only the latest revision of the `main` branch receives security fixes.
