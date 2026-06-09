# Claims Management System

A production-style insurance **claims management backend** built with **.NET 8**,
**EF Core 8 + SQL Server**, **JWT authentication**, **Docker**, and **GitHub Actions CI**.
It models the full claims lifecycle — customers, policies, coverages, a claim
adjudication workflow, documents, payouts, and an immutable audit trail — using
**Clean / Onion Architecture**.

> Replaces the original scaffold (which only contained empty layer projects and the
> default WeatherForecast template). See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)
> for the full design.

---

## Features

- **Customers** — CRUD, managed by staff.
- **Policies** — issued against customers, with sub-coverages, a coverage limit,
  and a status (Active/Expired/Cancelled/Suspended).
- **Claims** — filed against a policy, with a strictly enforced state machine:

  ```
  Submitted ─▶ UnderReview ─▶ Approved ─▶ Paid
       │            │  ▲          │
       │            ▼  │          └─(payout processed)
       │   InformationRequested
       │            │
       └────────────┴─▶ Rejected / Cancelled   (terminal)
  ```

- **Business rules** enforced in the domain/application core:
  - Claims can only be filed on an *active* policy within its coverage window.
  - Incident date cannot be in the future.
  - Approved amount cannot exceed the claimed amount.
  - Aggregate approved amounts cannot exceed the policy coverage limit.
  - Invalid status transitions are rejected.
- **Documents** — file uploads attached to claims (local disk in dev, pluggable
  `IFileStorage` for blob storage in production).
- **Payouts** — created for approved claims and settled, which moves the claim to *Paid*.
- **Auth** — JWT bearer tokens with role-based authorization: `Admin`, `Adjuster`, `Claimant`.
  Claimants are automatically scoped to their own customer/policies/claims.
- **Cross-cutting** — RFC 7807 ProblemDetails errors, global exception handling,
  Serilog request logging, Swagger UI with JWT support, `/health` endpoint.

## Solution layout

```
src/
  Claims.Domain          Entities, enums, the claim state machine, domain exceptions  (no dependencies)
  Claims.Shared          Result<T>, pagination, role constants                        (no dependencies)
  Claims.Application      Use-case services, DTOs, abstractions (ports)                (no NuGet dependencies)
  Claims.Infrastructure   EF Core, repositories, JWT, password hashing, file storage, DI
  Claims.API              ASP.NET Core controllers, auth, Swagger, middleware, composition root
tests/
  Claims.Tests           xUnit + FluentAssertions: domain workflow + application service tests
```

The `Domain`, `Shared`, and `Application` projects intentionally carry **zero
external NuGet dependencies**, so the business core is portable and fast to test.

## Running it

### Option A — Docker Compose (API + SQL Server)

```bash
docker compose up --build
```

- API: <http://localhost:8080> · Swagger: <http://localhost:8080/swagger>
- A default admin is seeded: **`admin@claims.local`** / **`Admin#12345`**
  (override via `Seed__AdminEmail` / `Seed__AdminPassword`).

### Option B — Local dotnet + SQL Server

```bash
# start just the database
docker compose up -d db

dotnet restore
dotnet run --project src/Claims.API
```

The app **creates the database schema and seeds the admin on startup**
(via `EnsureCreated`, or applies migrations if any are present).

## Quick start (API)

```bash
# 1. Log in as the seeded admin
curl -s http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@claims.local","password":"Admin#12345"}'

# 2. Use the returned accessToken as a Bearer token for subsequent calls, e.g.
curl http://localhost:8080/api/customers -H "Authorization: Bearer <token>"
```

Typical flow: create a customer → issue a policy → a claimant registers &
submits a claim → an adjuster reviews/approves → a payout is created &
processed (claim becomes *Paid*).

## Testing

```bash
dotnet test
```

## Database migrations

The app runs out-of-the-box with `EnsureCreated`. To switch to EF Core
migrations (recommended for evolving schemas):

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  --project src/Claims.Infrastructure --startup-project src/Claims.API
```

Once a migration exists, startup automatically applies it via `Database.Migrate()`.

## Configuration

| Setting | Description |
|---|---|
| `ConnectionStrings:ClaimsDb` | SQL Server connection string |
| `Jwt:SigningKey` | Symmetric signing key (≥ 32 chars) — **set via secret in production** |
| `Jwt:Issuer` / `Jwt:Audience` / `Jwt:AccessTokenMinutes` | Token parameters |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Bootstrap admin credentials |
| `Storage:LocalRootPath` | Where claim documents are written |

All settings can be overridden with environment variables using the
`Section__Key` convention.

## CI

`.github/workflows/ci.yml` restores, builds, tests (with coverage), and builds
the Docker image on every push/PR to `main`.
