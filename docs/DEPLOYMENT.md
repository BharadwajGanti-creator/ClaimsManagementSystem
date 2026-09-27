# Deploying the disposable Azure demo

The `Deploy to Azure (demo)` workflow (`.github/workflows/deploy.yml`) verifies
the build, tests and migration models before deploying a public demo API.
Costs depend on usage and subscription terms; set budget alerts:

- **Image** is built and pushed to **GitHub Container Registry (GHCR)** — free.
- **Compute** is **Azure Container Apps** (Consumption) with **scale-to-zero** —
  idle compute can scale down; usage may qualify for the applicable monthly grant.
- **Database** is **embedded SQLite inside the container** — no Azure SQL, no DB
  cost at all.

> ⚠️ Trade-off: because the container scales to zero and SQLite lives inside it,
> **data resets whenever the app cold-starts after being idle.** This is intended
> for a free demo, not durable storage. (To make data durable later, switch the
> `Database__Provider` env var back to `SqlServer` and point `ConnectionStrings__ClaimsDb`
> at a real database and replace local document storage with private object storage.)

The demo explicitly runs `--migrate-database` before serving because each
replacement container starts with an empty database. Production should run
migrations once in a release job with a separate identity, then start replicas.
Normal startup checks migrations and never seeds users. Existing EnsureCreated
databases require the adoption process in the README.

After a successful run, the live URL is printed in the job summary
(root redirects to `/swagger`).

---

## One-time setup

### 1. Create a deployment service principal

With the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli),
signed in to your subscription:

```bash
SUB_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "claims-deploy" \
  --role contributor \
  --scopes "/subscriptions/$SUB_ID" \
  --sdk-auth
```

Copy the **entire JSON** output — that's the `AZURE_CREDENTIALS` secret.

### 2. Add GitHub repository **secrets**

`Settings → Secrets and variables → Actions → Secrets`:

| Secret | Value |
|---|---|
| `AZURE_CREDENTIALS` | The full JSON from step 1 |
| `JWT_SIGNING_KEY` | A random secret of at least 32 UTF-8 bytes |
| `SEED_ADMIN_PASSWORD` | Explicit password of at least 12 characters for `admin@claims.local` |

(No SQL password and no registry secret are needed.)

### 3. (Optional) repository **variables**

| Variable | Default | Notes |
|---|---|---|
| `AZURE_RG` | `claims-rg` | Resource group name |
| `AZURE_LOCATION` | `eastus` | Azure region |

### 4. Run the deploy once, then make the image public

1. Merge to `main` (or **Actions → Deploy to Azure (demo) → Run workflow**).
   The first run pushes the image to GHCR and provisions Container Apps.
2. The GHCR package is **private by default**, so the very first deploy may show
   the app failing to pull the image. Make it public **once**:
   `GitHub → your profile → Packages → claimsmanagementsystem → Package settings →
   Change visibility → Public`.
3. Re-run the workflow (or restart the container app). It will now pull and run.

After that, every push to `main` redeploys automatically with no manual steps.

---

## After deployment

```bash
# Log in as the seeded admin (use your SEED_ADMIN_PASSWORD)
curl -s https://<app>.<region>.azurecontainerapps.io/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@claims.local","password":"<SEED_ADMIN_PASSWORD>"}'
```

Open `https://<app>.<region>.azurecontainerapps.io/swagger` to explore the API.

> First request after idle takes a few seconds (cold start from zero replicas).

## Cost & teardown

Scale-to-zero and SQLite reduce demo costs but do not guarantee a zero bill.
Monitor subscription charges and budgets. To remove the demo resources:

```bash
az group delete --name claims-rg --yes --no-wait
```
