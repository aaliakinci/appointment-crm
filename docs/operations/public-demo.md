# Public demo operations

The public demo is an internal CRM demonstration, not public customer self-booking. Only `receptionist@demo.local` receives the configured public password. Owner, manager, and employee users exist so audit and domain relationships remain realistic, but their passwords are random and are not shared. The receptionist role is rate-limited and cannot administer memberships or scheduling policy.

The public deployment uses `DemoSeed:PublicMode=true` only in one-shot migration/reset jobs. The long-running API has demo seeding disabled.

## Tenant-scoped reset

`dotnet AppointmentCrm.Api.dll --reset-demo` refuses to run unless all three explicit controls are true:

- `DemoSeed:Enabled=true`
- `DemoSeed:PublicMode=true`
- `DemoSeed:ResetEnabled=true`

The reset target is compiled as the fixed Atlas demo tenant ID. It takes a PostgreSQL transaction advisory lock, removes only that tenant's sessions and operational graph, rebuilds the idempotent seed, and commits atomically. User records are removed only when no membership remains. Integration tests insert another tenant sentinel and prove two consecutive resets preserve it.

The scheduled production operation first creates a backup, stops the API, runs the reset job, restarts the API, and requires public HTTPS smoke. Existing demo sessions are deliberately invalidated. A failed reset restarts the API and leaves the transaction rolled back.

Manual reset:

```bash
  /opt/appointment-crm/environments/production/current/deploy/release.sh reset-demo \
  /opt/appointment-crm/shared/production/runtime.env \
  /opt/appointment-crm/environments/production/state/current.env \
  /opt/appointment-crm/environments/production/state
```

Never enable reset mode for a real tenant or change the fixed demo identity into a configuration-supplied tenant ID.
