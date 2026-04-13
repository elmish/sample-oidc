# Elmish OIDC Sample

Demonstrates [Fable.Elmish.OIDC](https://github.com/elmish/OIDC) with Microsoft Entra ID (Azure AD).

**[Live demo](https://elmish.github.io/sample-oidc/)**

## Prerequisites

- [.NET SDK 10.0+](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- An Azure AD tenant (any Microsoft work/school account, or [create a free tenant](https://learn.microsoft.com/entra/fundamentals/create-new-tenant))

## Azure AD Setup

### Option A: Scripted (Azure CLI)

Install [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli), then:

```bash
az login
./setup-azure.sh
```

The script creates an app registration and prints the `ClientId` and `TenantId` to paste into `src/app.fs`.

### Option B: Manual (Azure Portal)

1. Go to [Azure Portal → App registrations](https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade)
2. Click **New registration**
   - **Name:** `Elmish OIDC Sample`
   - **Supported account types:** Single tenant (this org directory only)
   - **Redirect URI:** Select **Single-page application (SPA)**, enter `http://localhost:8090/`
3. After creation, go to **Authentication** and add these redirect URIs:
   - `http://localhost:8090/silent-renew.html`
   - `https://elmish.github.io/sample-oidc/`
   - `https://elmish.github.io/sample-oidc/silent-renew.html`
4. Copy the **Application (client) ID** and **Directory (tenant) ID** from the Overview page
5. Edit `src/app.fs` and replace the placeholder values:

```fsharp
let ClientId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
let TenantId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

> **Note:** The library uses the Authorization Code flow with PKCE (public client). No client secret is needed.

## Development

```bash
dotnet tool restore
dotnet fsi build.fsx -t Watch
```

Open http://localhost:8090 in your browser.

## Production Build

```bash
dotnet fsi build.fsx
```

Output in `out/`.

## Deploy to GitHub Pages

```bash
dotnet fsi build.fsx -t Publish
```

This builds the sample and pushes to the `gh-pages` branch.

## Project Structure

```
src/
  app.fs            — Elmish application (model, update, view)
  app.fsproj        — F# project (references Fable.Elmish.OIDC)
  index.html        — HTML template
  silent-renew.html — Hidden iframe page for silent token renewal
build.fsx           — FAKE build script
setup-azure.sh      — Azure AD app registration script
webpack.config.js   — Webpack bundler configuration
```

## How It Works

1. **Init** — The app calls `Oidc.init` which fetches the OpenID Connect discovery document and JWKS from Azure AD
2. **Login** — Clicking "Sign in" dispatches `LogIn`, which redirects to Azure AD's authorize endpoint with PKCE
3. **Callback** — Azure AD redirects back with an auth code; the library exchanges it for tokens and validates the ID token's RS256 signature
4. **Session** — The authenticated session (tokens, claims, userinfo) is stored in `sessionStorage`
5. **Renewal** — A 30-second timer checks token expiry; when approaching, a hidden iframe performs silent renewal with `prompt=none`
6. **Logout** — Dispatching `LogOut` clears the session and redirects to Azure AD's end-session endpoint
