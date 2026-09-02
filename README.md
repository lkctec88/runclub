# RunClub

Multi-tenant running club app: React (Vite) frontend and ASP.NET Core 9 API, with PostgreSQL.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org/) 20 or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL)

On Windows, Docker needs virtualization (Hyper-V or WSL2). If Docker Desktop reports that virtualization is not detected, enable it in BIOS and in Windows Features.

## Run locally

From the repo root (`C:\Projects\github\runclub`), use **three terminals**.

### 1. PostgreSQL

```powershell
docker compose up postgres -d
```

This starts Postgres 16 on `localhost:5432` with database/user/password `runclub` / `runclub` / `runclub`.

### 2. API

```powershell
dotnet run --project api/RunClub.Api --launch-profile http
```

- App: http://localhost:5019
- Swagger (Development only): http://localhost:5019/swagger

On first start in Development the API applies EF migrations and seeds demo data.

Do not host the API in IIS Express for local work. Use `dotnet run` (or the `http` profile in Visual Studio / Cursor).

### 3. Web

```powershell
cd web
npm install
npm run dev
```

Open **http://localhost:5173**. Vite proxies `/api` and `/hubs` to the API on port 5019.

## Run locally against Azure Postgres

The frontend never talks to the database. Point the **API** at Azure, then run a production-style web build.

You need Azure CLI logged in as `mollypepperpot@hotmail.com`, Key Vault secret access, and this PC’s public IP allowed on the Postgres firewall.

**Terminal 1 — API using Azure DB** (repo root or `web/`)

```powershell
npm run prod:api
```

That loads the connection string from Key Vault (`postgres-connection`), turns **seed off**, and does **not** apply EF migrations. Do not paste the connection string into the repo.

**Terminal 2 — production frontend** (repo root or `web/`)

```powershell
npm run prod
```

Open **http://localhost:4173**. Login users are those in Azure (the seeded demo accounts if you seeded that database).

Local Docker Postgres is unchanged: `npm run dev` in `web/` plus `dotnet run --launch-profile http` still uses `localhost:5432`.

## Demo logins

| Role        | Email                     | Password         |
|-------------|---------------------------|------------------|
| Super admin | `superadmin@runclub.local` | `SuperAdmin123!` |
| Club admin  | `admin@runclub.local`      | `Admin123!`      |
| Member      | `member@runclub.local`     | `Member123!`     |

## Useful commands

```powershell
# Stop Postgres
docker compose stop postgres

# Reset the local database (deletes all data, including seed)
docker compose down -v
docker compose up postgres -d
# then restart the API so it migrates and seeds again
```

## Troubleshooting

**`MSB3027` / file is locked by `RunClub.Api`**  
A previous API process is still running, so the build cannot copy DLLs. Stop it, then build or run again:

```powershell
Stop-Process -Name RunClub.Api -Force
```

**`address already in use` on port 5019**  
Something else is already bound to that port (often a previous API process). Stop the other `RunClub.Api` / `dotnet` process, or find it with:

```powershell
Get-NetTCPConnection -LocalPort 5019 | Select-Object OwningProcess
```

**API will not start / cannot connect to Postgres**  
Confirm Docker is running and `docker compose up postgres -d` succeeded. The API expects `Host=localhost;Port=5432;Database=runclub;Username=runclub;Password=runclub`.

**Login returns a bad gateway**  
The frontend is up but the API is not. Start the API on port 5019, then retry.

**Swagger in the browser shows HTTP 500.19**  
That is IIS, not Kestrel. Use http://localhost:5019/swagger after `dotnet run --launch-profile http`.
