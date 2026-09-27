# Architecture

One .NET 10 ASP.NET Core backend uses feature-organized services and Clean/Onion layers. These projects share a deployment and relational database; they are not microservices. No broker, cache, payment provider or frontend is implemented here.

API composes Application and Infrastructure. Application references Domain and Shared, and defines persistence, identity, time and storage interfaces. Infrastructure implements those interfaces. Domain owns entities and the claim state machine. Domain, Shared and Application have no external NuGet dependencies.

Controllers enforce roles and map Result errors to HTTP responses. Services validate inputs, ownership and policy eligibility, orchestrate repositories and invoke domain behavior. Repositories stage changes; one scoped EF DbContext implements IUnitOfWork and commits related writes together.

## Model

```text
Customer 1 -> many Policy
Policy   1 -> many Coverage and Claim
Claim    1 -> many ClaimDocument and ClaimStatusHistory
Claim    1 -> zero or one Payout
User     many -> zero or one Customer
```

SQL Server amounts use decimal(18,2); no explicit currency field exists. Document bytes live in LocalFileStorage and metadata/path in the database. History is appended by domain methods but remains mutable persisted data, not tamper-proof evidence.

## Workflow

| From | Allowed next states |
| --- | --- |
| Submitted | UnderReview, Cancelled |
| UnderReview | InformationRequested, Approved, Rejected, Cancelled |
| InformationRequested | UnderReview, Cancelled |
| Approved | Paid |
| Rejected, Paid, Cancelled | Terminal |

Submission checks ownership, input, active policy, incident coverage window and available coverage, then commits the claim and initial history. Claimants see their own claims/policies and may cancel eligible claims. Staff adjudicate. Approval rechecks coverage; this read-then-write check is not safe under concurrent approvals of different claims.

Payout creation requires approval and is unique per claim. Processing records a reference and moves the claim to Paid in one database commit. No money transfer occurs.

## Persistence and cross-cutting behavior

Provider-specific DbContexts share entity configuration and have separate migrations. SQLite DateTimeOffset values use UTC ticks for ordering. Domain-assigned GUIDs are configured as non-generated; tracked aggregate updates preserve new history/document inserts. Normal startup verifies schema; `--migrate-database` applies migrations, optionally seeds, and exits. README describes adoption of existing EnsureCreated databases.

HS256 JWT authentication validates signature, issuer, audience, lifetime, algorithm, user ID and one supported role. Claimant tokens require customer_id. Public registration forces Claimant; staff roles are Admin/Adjuster. Services enforce ownership in addition to controller roles.

Expected errors map to 400/401/403/404/409; unexpected errors produce generic 500 ProblemDetails. Serilog logs requests. `/health` is liveness; `/health/ready` is database/schema readiness. Swagger and permissive CORS remain; production needs identity/account controls, restricted origins and throttling.

File writes and database metadata commits are separate; database failure can leave orphan files. See [system design and scaling](SYSTEM-DESIGN-AND-SCALING.md) for the staged production plan and verification limits.
