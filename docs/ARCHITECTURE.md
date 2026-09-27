# Architecture

## 1. Why the original repository was incomplete

The starting point was a **scaffold only**: six projects wired together in the
correct Onion Architecture dependency order, but every layer contained nothing
more than a `Class1.cs` placeholder, and the API exposed only the default
`WeatherForecast` template. The README advertised ".NET 8, Azure SQL, EF Core,
Docker, JWT Auth, CI/CD" but none of that existed in code, and the projects
actually targeted the now end-of-life `net7.0`.

In short: the *table of contents* was there; the *chapters* were not.

## 2. Architectural style

This system uses **Clean / Onion Architecture**. Dependencies point inward; the
domain knows nothing about infrastructure.

```
                 ┌─────────────────────────────┐
                 │           Claims.API         │  controllers, auth, swagger,
                 │      (composition root)      │  middleware, CurrentUser
                 └──────────────┬──────────────┘
                                │ depends on
        ┌───────────────────────┼───────────────────────┐
        ▼                                               ▼
┌────────────────┐                          ┌────────────────────────┐
│ Claims.        │  implements ports        │  Claims.Application      │
│ Infrastructure │ ───────────────────────▶ │  (use cases, DTOs,       │
│ EF Core, JWT,  │                          │   port interfaces)       │
│ repositories   │                          └───────────┬────────────┘
└───────┬────────┘                                      │ depends on
        │ depends on                                    ▼
        │                                   ┌────────────────────────┐
        └─────────────────────────────────▶│  Claims.Domain          │
                                            │  entities, enums,        │
                                            │  state machine, rules    │
                                            └────────────────────────┘
                 Claims.Shared: Result<T>, pagination, role constants
                 (referenced by Application, Infrastructure, API)
```

### Layer responsibilities

| Layer | Responsibility | External deps |
|-------|----------------|---------------|
| **Domain** | Entities, value semantics, enums, **invariants & the claim state machine**, domain exceptions. | none |
| **Shared** | `Result`/`Result<T>`, `Error`, pagination primitives, role name constants. | none |
| **Application** | Use-case **services**, request/response **DTOs**, and **ports** (interfaces) for persistence, identity, time, and storage. Maps entities ↔ DTOs manually. | none (NuGet) |
| **Infrastructure** | Adapters that implement the ports: `ClaimsDbContext` + configurations + repositories, `JwtTokenService`, `PasswordHasherAdapter`, `LocalFileStorage`, `SystemDateTimeProvider`, DI wiring, DB seeding. | EF Core, JWT libs |
| **API** | HTTP surface: controllers, JWT auth + role policies, Swagger, exception middleware, `CurrentUser` over `HttpContext`, and the composition root. | ASP.NET Core, Swashbuckle, Serilog |

Keeping Domain/Shared/Application free of NuGet dependencies means the entire
business core compiles and unit-tests without a database, web host, or network.

## 3. Key design decisions

- **Rich domain model, not anemic.** `Claim` owns its lifecycle. Status changes
  go through `ChangeStatus` / `Approve` / `Reject` / `MarkPaid`, which validate
  transitions against an explicit allowed-transitions table and append an
  immutable `ClaimStatusHistory` entry. Illegal transitions throw
  `InvalidClaimStatusTransitionException`.

- **Result pattern over exceptions for expected failures.** Services return
  `Result<T>` carrying a categorized `Error` (Validation/NotFound/Conflict/
  Unauthorized/Forbidden). The API maps these to HTTP status codes in one place
  (`ResultExtensions`). Truly exceptional cases bubble up to the exception
  middleware and become RFC 7807 ProblemDetails.

- **Ports & adapters.** Application depends on interfaces (`IClaimRepository`,
  `IJwtTokenService`, `IFileStorage`, `IDateTimeProvider`, `ICurrentUser`, …);
  Infrastructure/API supply implementations. This is what makes the core testable
  with simple in-memory fakes (see `tests/Claims.Tests/Fakes`).

- **Repository + Unit of Work.** Repositories express intent-revealing queries;
  `IUnitOfWork` (implemented by the `DbContext`) commits a transaction. Services
  never call `SaveChanges` on EF directly.

- **Authorization in two tiers.** Coarse role checks via `[Authorize(Roles=…)]`
  on controllers; fine-grained ownership checks inside services (a claimant may
  only see/act on their own policies and claims, enforced via the `customer_id`
  JWT claim).

- **Auditing.** `AuditableEntity` carries created/updated metadata, stamped by
  the services (actor) with a `SaveChanges` backstop for timestamps.

## 4. Domain model

```
Customer 1───* Policy 1───* Coverage
                  │
                  1
                  │
                  *
                Claim 1───* ClaimDocument
                  │  └────* ClaimStatusHistory
                  1
                  │
                  0..1
                Payout

User *──0..1 Customer        (claimants are linked to a Customer; staff are not)
```

## 5. Claim workflow (state machine)

| From | Allowed to |
|------|-----------|
| Submitted | UnderReview, Cancelled |
| UnderReview | InformationRequested, Approved, Rejected, Cancelled |
| InformationRequested | UnderReview, Cancelled |
| Approved | Paid |
| Rejected / Paid / Cancelled | *(terminal)* |

## 6. Request lifecycle (example: approve a claim)

1. `POST /api/claims/{id}/approve` hits `ClaimsController` (requires `Admin`/`Adjuster`).
2. `ClaimService.ApproveAsync` loads the claim with details, re-checks the policy
   coverage limit, then calls `claim.Approve(amount, actor, notes)`.
3. The domain validates the transition and amount, sets `ApprovedAmount`, and
   records history.
4. `IUnitOfWork.SaveChangesAsync` commits; a `Result<ClaimDetailDto>` is returned.
5. `ResultExtensions` turns it into `200 OK` or the appropriate error status.

## 7. Security notes

- Passwords hashed with ASP.NET Core's PBKDF2 `PasswordHasher`.
- JWTs signed with HMAC-SHA256; issuer/audience/lifetime validated.
- The committed signing keys and seed password are **development placeholders** —
  supply real values via environment variables / secrets in any shared environment.

## 8. Production hardening (future work)

- Switch `EnsureCreated` → committed EF migrations (command in the README).
- Replace `LocalFileStorage` with an Azure Blob Storage adapter.
- Add refresh tokens / token revocation.
- Add integration tests with `WebApplicationFactory` + Testcontainers SQL Server.
- Add rate limiting, output caching, and OpenTelemetry traces/metrics.
