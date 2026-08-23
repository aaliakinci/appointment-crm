# Architecture overview

Appointment CRM is a modular monolith. It keeps one deployable backend while giving each business area a clear boundary.

## System context

```mermaid
flowchart LR
    staff[Business staff]
    operator[System operator]
    crm[Appointment CRM]
    notification[Notification provider]
    telemetry[Telemetry platform]

    staff -->|Manage daily operations| crm
    operator -->|Deploy, monitor, back up| crm
    crm -->|Send prepared notifications| notification
    crm -->|Send traces and metrics| telemetry
```

Business staff use one tenant workspace. The active tenant and permissions are part of the authenticated session. Operators manage deployment and recovery without using business endpoints.

## Containers

```mermaid
flowchart LR
    browser[Browser]
    proxy[Caddy<br/>TLS ingress]
    web[React + Nginx<br/>static UI and API proxy]
    api[ASP.NET Core API<br/>controllers and background worker]
    db[(PostgreSQL<br/>source of truth)]
    redis[(Redis<br/>replaceable cache)]
    provider[Notification provider]
    collector[OTLP collector]

    browser -->|HTTPS| proxy
    proxy --> web
    web -->|Same-origin /api| api
    api --> db
    api --> redis
    api --> provider
    api --> collector
```

Only Caddy is public in the release deployment. PostgreSQL, Redis, Nginx, and the API remain on private container networks.

Redis is optional for correctness. If it is unavailable, reporting reads from PostgreSQL.

## Backend modules

```mermaid
flowchart TB
    api[API<br/>HTTP contracts, auth, validation]
    contracts[Contracts<br/>request and response models]
    application[Application<br/>use-case interfaces, errors, permissions]
    domain[Domain<br/>business state and rules]
    infrastructure[Infrastructure<br/>EF Core, identity, outbox, cache]
    database[(PostgreSQL)]
    cache[(Redis)]

    api --> contracts
    api --> application
    api --> infrastructure
    infrastructure --> application
    infrastructure --> domain
    infrastructure --> database
    infrastructure --> cache
    application --> domain
```

- **Domain** owns business state and valid state changes.
- **Application** defines use cases, permissions, stable errors, and service contracts.
- **Infrastructure** implements persistence, identity, reporting cache, and outbox delivery.
- **Contracts** contains the public HTTP request and response shapes.
- **API** contains controllers and cross-cutting HTTP behavior.

The frontend follows the same business areas under `src/web/src/features`. Route pages only connect a URL to a feature entry point.

## Main domain areas

```mermaid
flowchart LR
    identity[Identity and memberships]
    catalog[Customers, services, employees]
    scheduling[Schedules and availability]
    appointments[Appointments and status history]
    reporting[Reporting]
    operations[Audit and outbox]

    identity --> catalog
    catalog --> scheduling
    catalog --> appointments
    scheduling --> appointments
    appointments --> reporting
    appointments --> operations
```

The arrows show data dependency, not separate network services. A single database transaction can therefore update an appointment, its history, audit entry, and outbox message together.

## Important boundaries

- [Tenant isolation](tenant-isolation.md)
- [Appointment concurrency](appointment-concurrency.md)
- [Known limitations](../../KNOWN_LIMITATIONS.md)
- [Deployment](../operations/deployment.md)
