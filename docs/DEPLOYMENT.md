# Deploying to Azure

The `Deploy to Azure` GitHub Actions workflow (`.github/workflows/deploy.yml`)
provisions and updates everything needed for a public, live API:

- **Azure Container Registry** — builds the image from the `Dockerfile` (server-side, via `az acr build`).
- **Azure SQL** — server + `ClaimsDb` (Basic tier); schema is created automatically on app startup.
- **Azure Web App for Containers** — Linux B1 plan, pulls the image and runs it on port 8080.

It runs on every push to `main` (and can be triggered manually via
**Actions → Deploy to Azure → Run workflow**).

After a successful run, the public URL is printed in the job summary:
`https://<app>.azurewebsites.net` (root redirects to `/swagger`).

---

## One-time setup

### 1. Create a deployment service principal

With the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli),
logged in to the target subscription:

```bash
SUB_ID=$(az account show --query id -o tsv)

az ad sp create-for-rbac \
  --name "claims-deploy" \
  --role contributor \
  --scopes "/subscriptions/$SUB_ID" \
  --sdk-auth
```

Copy the **entire JSON** output — it's the value for the `AZURE_CREDENTIALS` secret.

### 2. Add GitHub repository **secrets**

`Settings → Secrets and variables → Actions → Secrets → New repository secret`:

| Secret | Value |
|---|---|
| `AZURE_CREDENTIALS` | The full JSON from step 1 |
| `SQL_ADMIN_PASSWORD` | A strong SQL password (e.g. 16+ chars, mixed case, digit, symbol) |
| `JWT_SIGNING_KEY` | A random string **≥ 32 characters** |
| `SEED_ADMIN_PASSWORD` | Password for the seeded `admin@claims.local` login |

### 3. Add GitHub repository **variables**

`Settings → Secrets and variables → Actions → Variables → New repository variable`:

| Variable | Required | Default | Notes |
|---|---|---|---|
| `AZURE_ACR_NAME` | **yes** | — | Globally-unique, 5–50 alphanumeric (e.g. `claimsacr<yourinitials>`) |
| `AZURE_RG` | no | `claims-rg` | Resource group name |
| `AZURE_LOCATION` | no | `eastus` | Azure region |
| `SQL_ADMIN_LOGIN` | no | `claimsadmin` | SQL admin username |

### 4. Deploy

- Merge this PR into `main` (the workflow runs automatically), **or**
- Run it manually: **Actions → Deploy to Azure → Run workflow**.

The first run takes a few minutes (provisioning SQL + plan). Subsequent runs
only rebuild the image and roll the Web App.

---

## After deployment

```bash
# Log in as the seeded admin (use your SEED_ADMIN_PASSWORD)
curl -s https://<app>.azurewebsites.net/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@claims.local","password":"<SEED_ADMIN_PASSWORD>"}'
```

Open `https://<app>.azurewebsites.net/swagger` to explore and exercise the API.

## Approximate cost

Basic tiers: App Service B1 (~$13/mo), Azure SQL Basic (~$5/mo), ACR Basic
(~$5/mo). Delete the resource group to stop all charges:
`az group delete --name claims-rg`.

## Teardown

```bash
az group delete --name claims-rg --yes --no-wait
```
