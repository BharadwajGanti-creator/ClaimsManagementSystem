# Claims Management System

A .NET 10 ASP.NET Core backend using Clean/Onion layers. Customers hold policies; claims move through review, approval/rejection and payout recording. JWT roles are Admin, Adjuster and Claimant; claimants are scoped to their customer record.

The alignment preserves newer main's domain, API contracts and repository/unit-of-work pattern. It adds validation, ownership fixes, stricter authentication, provider-specific migrations, readiness and real HTTP/SQLite integration tests. See [architecture](docs/ARCHITECTURE.md) and [system design and scaling](docs/SYSTEM-DESIGN-AND-SCALING.md).

## Layout

| Project | Responsibility |
| --- | --- |
| Claims.Domain | Entities, state machine and invariants |
| Claims.Shared | Results, roles and pagination |
| Claims.Application | Use cases, DTOs, mapping and interfaces |
| Claims.Infrastructure | EF Core, repositories, migrations, JWT creation, hashing and file storage |
| Claims.API | HTTP controllers, authentication, middleware and composition root |
| Claims.Tests | Domain/service tests and real HTTP/SQLite integration tests |

Domain, Shared and Application have no external NuGet dependencies.

## Local run

Install the .NET 10 SDK. Compose starts SQL Server, explicitly migrates/bootstrap-seeds, then serves the API:

```bash
docker compose up --build
```

Swagger: <http://localhost:8080/swagger>. Development admin: `admin@claims.local` / `LocalDev#12345`. Override through `Seed__AdminEmail` / `Seed__AdminPassword`. Compose's database credentials are local examples; its SQL and document volumes persist data.

Alternatively, use SQLite locally in PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Database__Provider = 'Sqlite'
$env:ConnectionStrings__ClaimsDb = 'Data Source=claims.db'
dotnet restore
dotnet run --project src/Claims.API --no-launch-profile -- --migrate-database
dotnet run --project src/Claims.API --no-launch-profile --urls http://localhost:8080
```

Public registration creates a claimant and customer. Issue a policy to that returned customer ID using staff credentials, then submit using the claimant token. Creating an unrelated customer first does not link registration to it. Payout processing records a payment reference and marks the claim Paid; it does not transfer funds.

## Configuration

| Setting | Purpose |
| --- | --- |
| Database:Provider | SqlServer (default) or Sqlite |
| ConnectionStrings:ClaimsDb | Selected provider's connection string |
| Jwt:SigningKey | Required secret, at least 32 UTF-8 bytes; no production fallback |
| Jwt:Issuer / Jwt:Audience / Jwt:AccessTokenMinutes | Token trust and lifetime |
| Seed:Enabled | Explicit opt-in; false outside development by default |
| Seed:AdminEmail / Seed:AdminPassword | Bootstrap credentials; password at least 12 characters |
| Storage:LocalRootPath | Document directory |

Environment overrides use `Section__Key`. Keep secrets out of version control. Development credentials and keys are unsuitable for shared environments.

## Migrations

Initial migrations are committed separately for SqliteClaimsDbContext and SqlServerClaimsDbContext. Normal startup checks the schema; it never applies migrations or seeds users. In deployment configuration, run this once before starting serving replicas:

```bash
dotnet Claims.API.dll --migrate-database
```

It applies migrations, optionally seeds when `Seed:Enabled=true`, then exits without serving HTTP. Production should use a separate release job and migration identity; serving identities should have only data permissions. Compose and the single-replica SQLite demo explicitly bootstrap before serving as development/demo exceptions.

For subsequent changes, generate and review both providers:

```bash
dotnet tool restore
dotnet ef migrations add ChangeSqlite --context SqliteClaimsDbContext --project src/Claims.Infrastructure --startup-project src/Claims.Infrastructure --output-dir Persistence/Migrations/Sqlite
dotnet ef migrations add ChangeSqlServer --context SqlServerClaimsDbContext --project src/Claims.Infrastructure --startup-project src/Claims.Infrastructure --output-dir Persistence/Migrations/SqlServer
dotnet ef migrations script --idempotent --context SqlServerClaimsDbContext --project src/Claims.Infrastructure --startup-project src/Claims.Infrastructure --output migration.sql
```

Factories read `ConnectionStrings__ClaimsDb` when set; script generation needs no database connection.

**Existing EnsureCreated databases need a deliberate adoption plan.** Back up, compare the full schema, and baseline only after proving equivalence, or migrate data into a newly migrated database. SQLite timestamps now store UTC ticks instead of the earlier DateTimeOffset text and need conversion. Never blindly apply the initial create migration to a populated database or insert a migration history entry without reconciling its schema/data. No destructive automatic upgrade is included.

## Verification

```bash
dotnet test --configuration Release
dotnet tool restore
dotnet ef migrations has-pending-model-changes --context SqliteClaimsDbContext --project src/Claims.Infrastructure --startup-project src/Claims.Infrastructure
dotnet ef migrations has-pending-model-changes --context SqlServerClaimsDbContext --project src/Claims.Infrastructure --startup-project src/Claims.Infrastructure
```

`/health` checks process liveness; `/health/ready` checks database connectivity and pending migrations. Document storage is not checked yet.

CI builds, checks both migration models, tests with coverage and builds the container. Demo deployment has a verification gate before publishing. Integration tests cover real JWTs, registration/login, invalid input, customer isolation, cancellation, review/approval/payout, document metadata, pagination and database reuse after restart. SQLite results do not establish SQL Server behavior or load capacity.

## Cloud demo

[Deployment](docs/DEPLOYMENT.md) uses Container Apps, GHCR and container-local SQLite/document files. Container replacement loses those files. It is a disposable, single-replica demo, with no guaranteed cost. Production needs shared SQL Server/Azure SQL and object storage, concurrency/idempotency controls and measured load tests; see the [scaling plan](docs/SYSTEM-DESIGN-AND-SCALING.md).
