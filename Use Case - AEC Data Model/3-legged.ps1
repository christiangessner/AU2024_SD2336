Import-Module powerAPS

# 3-legged OAuth
$parameters = @{
    "ClientId"     = ""
    "ClientSecret" = ""
    "CallbackUrl"  = "http://localhost:8080/api/auth/callback"
    "Scope"        = "data:read data:write account:read"
    "Username"     = ""
    "Password"     = ""
}
$result = Open-APSConnection @parameters -Express 
if (-not $result) {
    throw "Authentication failed"
}

$response = Invoke-RestMethod "https://developer.api.autodesk.com/project/v1/hubs" -Headers $ApsConnection.RequestHeaders
foreach ($hub in $response.data) {
    Write-Host "Hub name: $($hub.attributes.name)" -ForegroundColor Green
}

$ApsConnection

Close-APSConnection
