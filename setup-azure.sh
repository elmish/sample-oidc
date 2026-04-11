#!/usr/bin/env bash
set -euo pipefail

# ---------------------------------------------------------------------------
# Register a Microsoft Entra ID (Azure AD) SPA application for the OIDC sample.
#
# Prerequisites:
#   - Azure CLI installed: https://learn.microsoft.com/cli/azure/install-azure-cli
#   - Logged in: az login
# ---------------------------------------------------------------------------

APP_NAME="Elmish OIDC Sample"
LOCALHOST_BASE="http://localhost:8090"
GHPAGES_BASE="https://elmish.github.io/samples-oidc"

echo "==> Checking Azure CLI login..."
if ! az account show &>/dev/null; then
    echo "Not logged in. Running 'az login'..."
    az login
fi

TENANT_ID=$(az account show --query tenantId -o tsv)
echo "==> Tenant ID: $TENANT_ID"

echo "==> Creating app registration: $APP_NAME"
APP_ID=$(az ad app create \
    --display-name "$APP_NAME" \
    --sign-in-audience AzureADMyOrg \
    --query appId -o tsv)

echo "==> App (Client) ID: $APP_ID"

echo "==> Configuring SPA platform with redirect URIs..."
az rest --method PATCH \
    --uri "https://graph.microsoft.com/v1.0/applications/$(az ad app show --id "$APP_ID" --query id -o tsv)" \
    --headers "Content-Type=application/json" \
    --body "{
        \"spa\": {
            \"redirectUris\": [
                \"${LOCALHOST_BASE}/\",
                \"${LOCALHOST_BASE}/silent-renew.html\",
                \"${GHPAGES_BASE}/\",
                \"${GHPAGES_BASE}/silent-renew.html\"
            ]
        }
    }"

echo ""
echo "==> Done! Update src/app.fs with these values:"
echo ""
echo "    let ClientId = \"$APP_ID\""
echo "    let TenantId = \"$TENANT_ID\""
echo ""
echo "Portal: https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps/ApplicationMenuBlade/~/Overview/appId/$APP_ID"
