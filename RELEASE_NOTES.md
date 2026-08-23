# Release notes

## v1.0.0 — Draft

This release has not been tagged or published. The notes are ready for the repository owner to use when the external release checks are complete.

### Product

- Multi-tenant workspaces with owner, manager, receptionist, and employee roles
- Customer, service, employee, membership, and account management
- Versioned working schedules, date overrides, time off, and availability
- Day/week appointment calendar with create, reschedule, status, and history flows
- Tenant dashboard reporting and actor-attributed audit history
- Turkish and English UI built with Lily UI

### Reliability and security

- Layered tenant isolation and server-side authorization
- PostgreSQL-enforced appointment overlap protection
- Short-lived access tokens and rotating single-use refresh sessions
- Stable API error codes and centralized exception handling
- Transactional appointment history, audit, and durable outbox writes
- Replaceable Redis reporting cache with PostgreSQL fallback
- IANA time-zone and DST-aware scheduling rules

### Delivery and operations

- Non-root production images and TLS ingress
- Build-once, digest-based staging and production promotion
- One-shot serialized migration job
- Encrypted backup and guarded separate-database restore
- Tenant-scoped public-demo reset
- Health, smoke, OpenTelemetry, and uptime controls

### Database

This version includes the complete schema through migration `20260823180000_ScopedPublicDemoReset`.

Database migrations are forward only. Create and verify a backup before promotion. Application rollback is allowed only when the previous image is compatible with the migrated schema.

### Known limits

- No public customer booking or tenant self-registration
- No payments, subscriptions, invoices, or mobile application
- Notification delivery uses a demo provider; it does not send real email or SMS
- No public demo URL is available yet

See [known limitations](KNOWN_LIMITATIONS.md) and the [release checklist](RELEASE_CHECKLIST.md) before publishing.
