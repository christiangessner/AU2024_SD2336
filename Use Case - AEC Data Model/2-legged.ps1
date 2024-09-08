Import-Module powerAPS

# 2-legged OAuth (requires coolOrange powerVault to be installed)
Import-Module powerVault
$vaultLogin = [Autodesk.Connectivity.WebServicesTools.AutodeskAccount]::Login(
    [IntPtr]::Zero)

$parameters = @{
    "ClientId"     = ""
    "ClientSecret" = ""
    "Scope"        = "data:read data:write account:read"
    "AccountId"    = $vaultLogin.AccountId
}
$result = Open-APSConnection @parameters
if (-not $result) {
    throw "Authentication failed"
}

$response = Invoke-RestMethod "https://developer.api.autodesk.com/project/v1/hubs" -Headers $ApsConnection.RequestHeaders
foreach ($hub in $response.data) {
    Write-Host "Hub name: $($hub.attributes.name)" -ForegroundColor Green
}

$ApsConnection

Close-APSConnection
