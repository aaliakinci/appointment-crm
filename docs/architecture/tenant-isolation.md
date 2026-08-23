# Tenant isolation

## The risk

The application stores several businesses in one PostgreSQL database. A user from one tenant must never read or change another tenant's customers, employees, appointments, or settings.

Filtering only in controllers is too easy to forget. Appointment CRM therefore applies tenant isolation at several layers.

## How it works

| Layer | Control |
| --- | --- |
| Authentication | The session identifies the selected membership and tenant. |
| Authorization | Controller policies check the required permission on the server. |
| Reads | EF Core query filters add the active tenant to tenant-owned queries. |
| Writes | `SaveChanges` rejects tenant-owned entities outside the active tenant. |
| Database | Composite keys and foreign keys keep related rows in the same tenant. |
| Tests | Requests use two known tenants and guessed foreign identifiers. |

The active tenant comes from the authenticated membership. It is not trusted from a free-form request header or body field.

## Response behavior

When a tenant-scoped identifier exists in another tenant, the API returns `404 Not Found`. It does not reveal that the foreign record exists.

A rejected cross-tenant appointment write must leave no partial data. Integration tests verify that appointment, history, audit, and outbox counts stay unchanged.

## Why a shared schema

A shared schema keeps deployment and migration work small for this product size. It also makes reporting and operations simpler than a database-per-tenant model.

The trade-off is that every tenant-owned path needs strict automated checks. PostgreSQL row-level security is not enabled, so application filters and database relationships must remain part of every feature review.

## Evidence

- [`AppointmentCrmDbContext`](../../src/server/AppointmentCrm.Infrastructure/Persistence/AppointmentCrmDbContext.cs) defines query filters, write guards, and tenant-scoped relationships.
- [`IdentitySecurityTests`](../../tests/AppointmentCrm.IntegrationTests/IdentitySecurityTests.cs) covers membership, session, and write isolation.
- [`ReleaseQualityGateTests`](../../tests/AppointmentCrm.IntegrationTests/ReleaseQualityGateTests.cs) checks non-disclosing reads and zero-side-effect rejected writes across features.

Any new tenant-owned entity must add the same read, write, database, and test controls before it is complete.
