# AppTrust

A .NET 8 sample demonstrating machine-to-machine (M2M) trust between cooperating services using configurable trust modes: **JWT**, **mTLS**, or **Both**.

**AppTrust.Client** authenticates to **AppTrust.Service** (via signed JWT, client certificate, or both) and calls a protected endpoint; AppTrust.Service validates the request and returns a correlation ID.

## Architecture

```
Client  →  AppTrust.Client (:7203 HTTPS)  →  AppTrust.Service (:7214 HTTPS)
              │                            │
              │  POST /api/action/trigger
              │  (X-Trigger-Key required)
              │                            │
              └── trust strategy ────────► POST /api/secure-action
                                           validates JWT and/or mTLS
                                           returns correlationId
```

| Service | Role | Port |
|---------|------|------|
| **AppTrust.Client** | Outbound trust (JWT and/or mTLS), proxies secure actions | `https://localhost:7203` |
| **AppTrust.Service** | Inbound trust validation, executes secure actions | `https://localhost:7214` |

## Solution structure

```
AppTrust.sln
├── AppTrust.Client/
│   ├── AppTrust.Client.API / AppTrust.Client.Contracts / AppTrust.Client.Infrastructure
├── AppTrust.Service/
│   ├── AppTrust.Service.API / AppTrust.Service.Contracts / AppTrust.Service.Infrastructure
├── AppTrust.Sdk                                          # Internal NuGet: trust contracts & shared helpers
├── AppTrust.Client/AppTrust.Client.Tests
├── AppTrust.Service/AppTrust.Service.Tests
├── AppTrust.Sdk/AppTrust.Sdk.Tests
└── Tests/AppTrust.E2E.Tests                              # Cross-service end-to-end tests
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & test

```bash
dotnet build AppTrust.sln
dotnet test AppTrust.sln
```

## Run locally

Start AppTrust.Service and AppTrust.Client in either order. At request time, AppTrust.Service must be reachable for the trigger flow to succeed.

**Terminal 1 — AppTrust.Service** (use the `https` profile when `Trust:Mode` is `mTLS` or `Both`)

```bash
cd AppTrust.Service/AppTrust.Service.API
dotnet run --launch-profile https
```

**Terminal 2 — AppTrust.Client**

```bash
cd AppTrust.Client/AppTrust.Client.API
dotnet run
```

(`dotnet run` uses the `https` launch profile by default.)

Swagger UI:

- AppTrust.Client: https://localhost:7203/swagger
- AppTrust.Service: https://localhost:7214/swagger

## Manual testing

Trigger the end-to-end flow (requires `X-Trigger-Key` header; default Development key is `dev-trigger-key`).

Use `-k` unless you ran `dotnet dev-certs https --trust`:

```bash
curl -k -X POST https://localhost:7203/api/action/trigger \
  -H "X-Trigger-Key: dev-trigger-key"
```

Expected response (`200 OK`):

```json
{
  "message": "Secure action executed successfully!",
  "caller": "apptrust-client",
  "correlationId": "<guid>"
}
```

Other checks:

| Scenario | How | Expected |
|----------|-----|----------|
| Missing trigger API key | `curl -k -X POST https://localhost:7203/api/action/trigger` | `401 Unauthorized` |
| Unauthenticated call to AppTrust.Service | `curl -k -X POST https://localhost:7214/api/secure-action` | `401 Unauthorized` |
| AppTrust.Service unavailable | Stop AppTrust.Service with **Ctrl+C**, then trigger AppTrust.Client | Error from AppTrust.Client |
| Health | `curl -k https://localhost:7203/health` | `200 OK` |
| Readiness | `curl -k https://localhost:7203/health/ready` | `200 OK` with `trustMode` |

> **Tip:** Stop apps with **Ctrl+C**, not **Ctrl+Z**. Suspending a process keeps the port occupied and can cause Swagger requests to hang.

## Cryptographic keys

RSA keys and certificates are required for local development but are not included in the repo.
Generate your own:

```bash
# Generate private key
openssl genrsa -out AppTrust.Client/AppTrust.Client.API/apptrust_client_private.pem 2048

# Extract public key
openssl rsa -in AppTrust.Client/AppTrust.Client.API/apptrust_client_private.pem -pubout -out AppTrust.Service/AppTrust.Service.API/apptrust_client_public.pem

# Client certificate + key for mTLS (CN must be apptrust-client)
openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
  -subj "/CN=apptrust-client" \
  -keyout AppTrust.Client/AppTrust.Client.API/apptrust_client.key.tmp \
  -out AppTrust.Client/AppTrust.Client.API/apptrust_client.cert.tmp
cat AppTrust.Client/AppTrust.Client.API/apptrust_client.cert.tmp AppTrust.Client/AppTrust.Client.API/apptrust_client.key.tmp > AppTrust.Client/AppTrust.Client.API/apptrust_client.pem
rm AppTrust.Client/AppTrust.Client.API/apptrust_client.cert.tmp AppTrust.Client/AppTrust.Client.API/apptrust_client.key.tmp

# Server certificate + key for AppTrust.Service HTTPS (CN should match localhost)
openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
  -subj "/CN=localhost" \
  -keyout AppTrust.Service/AppTrust.Service.API/apptrust_service_server.key.tmp \
  -out AppTrust.Service/AppTrust.Service.API/apptrust_service_server.cert.tmp
cat AppTrust.Service/AppTrust.Service.API/apptrust_service_server.cert.tmp AppTrust.Service/AppTrust.Service.API/apptrust_service_server.key.tmp > AppTrust.Service/AppTrust.Service.API/apptrust_service_server.pem
rm AppTrust.Service/AppTrust.Service.API/apptrust_service_server.cert.tmp AppTrust.Service/AppTrust.Service.API/apptrust_service_server.key.tmp
```

These files are gitignored and should never be committed.

Committed `appsettings.Development.json` files default to **`Trust:Mode: Both`**, so all of the above artifacts are needed for the default local setup.

### Switching trust mode

DevOps (or local `appsettings`) must set the **same** `Trust:Mode` on both services. Valid values: `JWT`, `mTLS`, `Both`.

**JWT only** — only the RSA key pair is required. AppTrust.Service can use the `http` or `https` launch profile; AppTrust.Client still requires HTTPS to the Service base URL.

**mTLS or Both** — client cert, server cert, and (for JWT/Both) the RSA key pair are required. Start AppTrust.Service with the `https` launch profile.

Example `mTLS` Development config:

AppTrust.Service — `AppTrust.Service/AppTrust.Service.API/appsettings.Development.json`:

```json
{
  "Trust": { "Mode": "mTLS" },
  "AppTrustService": { "ServerCertificatePath": "apptrust_service_server.pem" }
}
```

AppTrust.Client — `AppTrust.Client/AppTrust.Client.API/appsettings.Development.json`:

```json
{
  "Trust": { "Mode": "mTLS" },
  "AppTrustService": { "BaseUrl": "https://localhost:7214/" },
  "AppTrustClient": {
    "ClientCertificatePath": "apptrust_client.pem",
    "AcceptAnyServerCertificate": true
  }
}
```

## Configuration

AppTrust.Client connects to AppTrust.Service via `AppTrustService:BaseUrl` in `AppTrust.Client/AppTrust.Client.API/appsettings.json` (default: `https://localhost:7214/`). **HTTPS is required** for all trust modes so JWTs are never sent in cleartext.

In Development, `AppTrustClient:AcceptAnyServerCertificate` defaults to `true` so the ASP.NET dev HTTPS certificate works locally. The flag is ignored outside Development and throws at startup if set.

**Trust mode** is configured independently on each service via `Trust:Mode`. DevOps must keep AppTrust.Client and AppTrust.Service in sync.

| Setting | Service | Purpose |
|---------|---------|---------|
| `Trust:Mode` | Both | `JWT`, `mTLS`, or `Both` |
| `AppTrustClient:TriggerApiKey` | Client | Required `X-Trigger-Key` value for the trigger endpoint |
| `AppTrustClient:PrivateKeyPath` | Client | JWT signing key (JWT and Both modes) |
| `AppTrustClient:ClientCertificatePath` | Client | mTLS client cert (mTLS and Both modes) |
| `AppTrustClient:CallerId` | Client | JWT `sub`/`iss` (default `apptrust-client`) |
| `AppTrustService:PublicKeyPath` | Service | JWT verification key |
| `AppTrustService:ServerCertificatePath` | Service | HTTPS server cert (mTLS and Both modes) |
| `AppTrustService:ExpectedJwtCallerId` | Service | Expected JWT issuer/sub |
| `AppTrustService:AllowedClientCertificateSubjects` | Service | Allowed mTLS certificate CNs |

Application identifiers default to `apptrust-client` and are defined in `AppTrust.Sdk/AppConstants.cs`.

### AppTrust.Sdk (internal NuGet)

AppTrust.Client and AppTrust.Service consume **`AppTrust.Sdk`** as a NuGet package (not a project reference). The SDK project lives in this repo for development; `dotnet build` packs it automatically into `local-nuget-feed/` (see `nuget.config`).

To pack manually:

```bash
dotnet pack AppTrust.Sdk/AppTrust.Sdk.csproj -c Release -o ./local-nuget-feed
```

In production, publish `AppTrust.Sdk` to your internal feed (Azure Artifacts, GitHub Packages, etc.) and pin `PackageReference Version` per service.

Set `AppTrustClient:TriggerApiKey` in production (e.g. via `AppTrustClient__TriggerApiKey` environment variable).

See [docs/architecture-review.md](docs/architecture-review.md) for design notes and production hardening guidance.
