# AppTrust

A .NET 8 sample demonstrating cross-application trust using asymmetric JWT authentication.

**App A** signs a JWT with a private key and calls a protected endpoint on **App B**, which validates the token with the matching public key and creates an in-memory session.

## Architecture

```
Client  →  App A (:5057)  →  App B (:5278)
              │                    │
              │  POST /api/action/trigger
              │                    │
              └── signs JWT ──────► POST /api/secure-action
                                   validates JWT
                                   creates session
```

| App | Role | Port |
|-----|------|------|
| **App A** | Issues JWTs, proxies secure actions to App B | `http://localhost:5057` |
| **App B** | Validates JWTs, executes secure actions, manages sessions | `http://localhost:5278` |

## Solution structure

```
AppTrust.sln
├── AppA.API / AppA.Contracts / AppA.Infrastructure   # Token issuer & caller
├── AppB.API / AppB.Contracts / AppB.Infrastructure   # Token validator & secure endpoint
├── Shared.Infrastructure                             # IKeyLoader, FileKeyLoader, AppConstants
└── Tests/
    ├── UnitTests
    └── IntegrationTests
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build & test

```bash
dotnet build AppTrust.sln
dotnet test AppTrust.sln
```

## Run locally

Start **App B first**, then App A.

**Terminal 1 — App B**

```bash
cd AppB.API
dotnet run --launch-profile http
```

**Terminal 2 — App A**

```bash
cd AppA.API
dotnet run --launch-profile http
```

Swagger UI:

- App A: http://localhost:5057/swagger
- App B: http://localhost:5278/swagger

## Manual testing

Trigger the end-to-end flow:

```bash
curl -X POST http://localhost:5057/api/action/trigger
```

Expected response (`200 OK`):

```json
{
  "message": "Secure action executed successfully!",
  "caller": "application-a",
  "sessionId": "<guid>"
}
```

Other checks:

| Scenario | How | Expected |
|----------|-----|----------|
| Unauthenticated call to App B | `curl -X POST http://localhost:5278/api/secure-action` | `401 Unauthorized` |
| App B unavailable | Stop App B with **Ctrl+C**, then trigger App A | Error from App A |

> **Tip:** Stop apps with **Ctrl+C**, not **Ctrl+Z**. Suspending a process keeps the port occupied and can cause Swagger requests to hang.

## Cryptographic keys

Development RSA key pairs are included for local use:

| File | Used by |
|------|---------|
| `AppA.API/appA_private.pem` | App A — JWT signing |
| `AppB.API/appA_public.pem` | App B — JWT verification |

These are **development keys only**. Do not use them in production.

## Configuration

App A connects to App B via `AppB:BaseUrl` in `AppA.API/appsettings.json` (default: `http://localhost:5278/`).

Application identifiers (`application-a`, `application-b`) are defined in `Shared.Infrastructure/AppConstants.cs`.
