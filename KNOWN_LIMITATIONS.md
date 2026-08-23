# Known limitations

This repository currently targets an internal, multi-tenant appointment CRM release candidate. The following boundaries are intentional and should not be presented as implemented functionality.

- There is no public customer self-booking, tenant self-registration, marketplace, mobile application, payment, subscription, accounting, or invoicing workflow.
- Each appointment has exactly one customer, service, and employee. Packages, recurring appointments, waitlists, rooms, equipment, and other reservable resources are not modeled.
- Notification delivery uses the durable outbox and an idempotent demo provider. It does not send real email/SMS/push messages and there is no notification inbox or provider-administration UI.
- Reporting is operational and tenant-scoped; it is not a data warehouse, financial ledger, configurable BI system, or regulatory export.
- Multi-tenancy uses a shared PostgreSQL schema with application query filters, write guards, tenant-scoped constraints, and isolation tests. PostgreSQL row-level security is not enabled.
- The root Compose configuration remains a disposable local stack. Production uses a separate immutable-image deployment definition with TLS ingress, external secrets, and a persistent certificate-protected Data Protection key ring.
- The repository contains staging/production promotion, encrypted backup/guarded restore, public-demo reset, and external health-probe automation. No specific live hostname, cloud account, off-host backup object, measured production RPO/RTO, or completed DNS/TLS evidence is committed; operators must supply and verify that environment state.
- Accessibility automation blocks serious and critical WCAG 2.1 A/AA findings on covered flows, but it is not a certification and does not replace assistive-technology testing with users.
- Browser E2E covers the release-critical UI, permission, validation, error, responsive, and keyboard boundaries. Domain-complete appointment completion and exact dashboard accounting are also proven deterministically at the real API/database boundary.
