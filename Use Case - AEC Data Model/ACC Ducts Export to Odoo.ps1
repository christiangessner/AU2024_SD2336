$ErrorActionPreference = "Stop"

#region Settings
# ACC Account Name
$hubName = ""
# ACC Project Name
$projectName = ""
# ACC Submittal Status
$submittalStatus = "Closed"

# APS Client Id
$clientId = ""
# APS Client Secret
$clientSecret = ""
# APS Scope
$scope = "data:read data:write account:read"
# APS Callback URL
$callbackUrl = "http://localhost:8080/api/auth/callback"
# APS Username
$username = ""
#region APS Password
$password = ""
#endregion

# Odoo Database
$odooDatabase = ""
# Odoo Username
$odooUsername = ""
#region Odoo Password
$odooPassword = ""
#endregion

# Log file
$logFile = "C:\temp\ACC Ducts Export to Odoo.txt"

# Standard length of duct pipes in inches
$standardLength = 120
#endregion

#region Logging Functions
Function Write-Log($message) {
    Write-Host $message
    $isWritten = $false
    do {
        try {
            Add-Content -Path $logFile -Value "$(Get-Date -Format o) $message" -ErrorAction Stop
            $isWritten = $true
        }
        catch {
        }
    } until ( $isWritten )
}
#endregion

#region APS Functions
# https://aps.autodesk.com/en/docs/data/v2/reference/http/hubs-GET
function Get-ApsAccHubs {
    # Get ACC Hubs
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/project/v1/hubs?filter[extension.type]=hubs:autodesk.bim360:Account"
        "Method" = "Get"
        "Headers" = $ApsConnection.RequestHeaders
    }
    $response = Invoke-RestMethod @parameters
    return $response.data
}

# https://aps.autodesk.com/en/docs/data/v2/reference/http/hubs-hub_id-projects-GET
function Get-ApsProjects($hub) {
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/project/v1/hubs/$($hub.id)/projects"
        "Method" = "Get"
        "Headers" = $ApsConnection.RequestHeaders
    }
    $response = Invoke-RestMethod @parameters
    return $response.data
}

# https://aps.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-items-item_id-GET/
function Get-ApsItem($project, $itemId) {
    # Get folder contents (items and subfolders)
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/data/v1/projects/$($project.id)/items/$([System.Web.HttpUtility]::UrlEncode($itemId))"
        "Method" = "Get"
        "Headers" = $ApsConnection.RequestHeaders
    }
    $response = Invoke-RestMethod @parameters
    return $response.data
}

# https://aps.autodesk.com/en/docs/data/v2/reference/http/projects-project_id-items-item_id-PATCH
# https://stackoverflow.com/questions/73286387/is-there-an-autodesk-forge-api-available-to-lock-files-in-bim-360-or-acc
function Update-ApsItemLocked($project, $item, $locked = $false) {

    $body = @"
{
    "jsonapi": {
        "version": "1.0"
    },
    "data": {
        "type": "items",
        "id": "$($item.id)",
        "attributes": {
            "reserved": $($locked.ToString().ToLower())
        }
    }
}
"@
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/data/v1/projects/$($project.id)/items/$([System.Web.HttpUtility]::UrlEncode($item.id))"
        "Method" = "Patch"
        "Headers" = $ApsConnection.RequestHeaders
        "ContentType" = "application/vnd.api+json"
        "Body" = $body
    }
    $response = Invoke-RestMethod @parameters
    return $response.data
}

# https://aps.autodesk.com/en/docs/acc/v1/reference/http/submittals-metadata-GET/
function Get-ApsAccSubmittalMetadata($project) {
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/construction/submittals/v2/projects/$($project.id.TrimStart("b."))/metadata"
        "Method" = "Get"
        "Headers" = $ApsConnection.RequestHeaders
    }
    $response = Invoke-RestMethod @parameters
    return $response
}

# https://aps.autodesk.com/en/docs/acc/v1/reference/http/submittals-items-GET/
function Get-ApsAccSubmittalItems($project) {
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/construction/submittals/v2/projects/$($project.id.TrimStart("b."))/items"
        "Method" = "Get"
        "Headers" = $ApsConnection.RequestHeaders
    }
    $response = Invoke-RestMethod @parameters
    return $response.results
}

# https://aps.autodesk.com/en/docs/acc/v1/reference/http/submittals-items-GET/
function Get-ApsAccSubmittalItemRelationships($project, $submittal) {
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/bim360/relationship/v2/containers/$($project.id.TrimStart("b."))/relationships:search?domain=autodesk-construction-submittals&type=submittalitem&Id=$($submittal.id)&withDomain=autodesk-bim360-documentmanagement&withType=documentlineage"
        "Method" = "Get"
        "Headers" = $ApsConnection.RequestHeaders
    }
    $response = Invoke-RestMethod @parameters
    return $response.relationships
}
#endregion

#region GraphQL Functions
function Invoke-GraphQLQuery($query, $variables) {
    $body = @"
{
    "query": "$($query.Replace("`r`n", "\n"))",
    "variables": $variables
}
"@
    $parameters = @{
        "Uri" = "https://developer.api.autodesk.com/aec/graphql"
        "Method" = "Post"
        "ContentType" = "application/json; charset=utf-8"
        "Body" = $body
        "Headers" = $ApsConnection.RequestHeaders
    }
    $parameters["Headers"].Add("Accept", "application/json; charset=utf-8")
    $response = Invoke-RestMethod @parameters #returns Json in ISO-8859-1
    $json = $response | ConvertTo-Json -Depth 10
    $bytes = [System.Text.Encoding]::GetEncoding("ISO-8859-1").GetBytes($json)
    $response = [System.Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json
    return $response.data
}

function Get-ApsAecDmHub($hubName) {
    $query = @"
    query GetHubs(`$hubName: String!) {
        hubs(filter: {name: `$hubName}) {
        pagination {
            cursor
        }
        results {
            name
            id
        }
        }
    }
"@

    $variables = @"
    {
        "hubName": "$hubName"
    }
"@
    return Invoke-GraphQLQuery $query $variables
}

function Get-ApsAecDmProject($hubId, $projectName) {
    $query = @"
    query GetProjects(`$hubId: ID!, `$projectName: String!) {
        projects(hubId: `$hubId, filter: { name: `$projectName }) {
            results {
                id
                name
                alternativeIdentifiers{
                dataManagementAPIProjectId
                }
            }
        }
    }
"@

    $variables = @"
    {
        "hubId":"$hubId",
        "projectName": "$projectName"
    }
"@
    return Invoke-GraphQLQuery $query $variables
}

# Doesn't work: message: "The following ID is malformed: {ACC_PROJECT_ID}"
function Get-ApsAecDmProjectByDataManagementApiId($project) {
    $query = @"
    query GetProjectByDataManagementAPIId(`$dataManagementAPIProjectId: ID!) {
        projectByDataManagementAPIId(dataManagementAPIProjectId: `$dataManagementAPIProjectId) {
            id
            name
            alternativeIdentifiers {
                dataManagementAPIProjectId
            }
            hub {
                id
                name
            }
        }
    }
"@

    $variables = @"
    {
        "dataManagementAPIProjectId": "$($project.id.TrimStart("b."))"
    }
"@

    return Invoke-GraphQLQuery $query $variables
}

function Get-ApsAecDmElementGroup($projectId, $fileUrn) {
    $query = @"
    query GetElementGroupByProjectAndFileUrn(`$projectId: ID!, `$fileUrn: [String!]) {
        elementGroupsByProject(projectId: `$projectId, filter: { fileUrn: `$fileUrn }) {
            pagination {
                cursor
            }
            results {
                name
                id
                alternativeIdentifiers {
                    fileUrn
                    fileVersionUrn
                }
            }
        }
    }
"@

    $variables = @"
    {
        "projectId": "$projectId",
        "fileUrn": "$fileUrn"
    }
"@

    return Invoke-GraphQLQuery $query $variables
}

function Get-ApsAecDmDuctsElements($elementGroupId, $cursor) {
    $query = @"
    query GetDuctsFromElementGroup(`$elementGroupId: ID!, `$elementsFilter: String!, `$cursor: String) {
        elementsByElementGroup(
            elementGroupId: `$elementGroupId
            filter: {query: `$elementsFilter}
            pagination: {limit: 100, cursor: `$cursor}
        ) {
            pagination {
                cursor
                pageSize
            }
            results {
                name
                properties(filter: {names: [\"Diameter\", \"Length\", \"Size\", \"Element Name\", \"Family Name\"]}) {
                    results {
                        name
                        value
                        definition {
                            units {
                                name
                            }
                        }
                    }
                }
            }
        }
    }
"@
    if ($cursor) {
        $cursorString = """$cursor"""
    } else {
        $cursorString = "null"
    }

    $variables = @"
    {
    "elementGroupId": "$elementGroupId",
    "elementsFilter": "(property.name.category==Ducts or property.name.category=='Flex Ducts' or property.name.category=='Duct Fittings') and 'property.name.Element Context'==Instance",
    "cursor": $cursorString
    }
"@

    return Invoke-GraphQLQuery $query $variables
}
#endregion

#region ODOO Functions
function Invoke-OdooAuthentication {
    $body = @"
    {
        "jsonrpc": "2.0",
        "method": "call",
        "params": {
            "db": "$odooDatabase",
            "login": "$odooUsername",
            "password": "$odooPassword",
            "context": {}
        },
        "id": 1
    }
"@
    $parameters = @{
        "Uri" = "https://$odooDatabase.odoo.com/web/session/authenticate"
        "Method" = "Post"
        "ContentType" = "application/json; charset=utf-8"
        "Body" = $body
    }
    $response = Invoke-WebRequest @parameters -SessionVariable odooSession
    $json = $response.Content | ConvertFrom-Json
    Write-Log "Connected user '$($json.result.username)'. Session valid until: $($odooSession.Cookies.GetCookies($parameters.Uri).Expires)" | Out-Null
    return $odooSession
}

function Invoke-OdooDataSetCall($body) {
    $parameters = @{
        "Uri" = "https://$odooDatabase.odoo.com/web/dataset/call_kw"
        "Method" = "Post"
        "ContentType" = "application/json; charset=utf-8"
        "Body" = $body
    }
    $response = Invoke-RestMethod @parameters -WebSession $odooSession #returns Json in UTF-8
    return $response.result
}

function Get-OdooProducts {
    $body = @"
    {
        "jsonrpc": "2.0",
        "method": "call",
        "params": {
            "model": "product.product",
            "method": "search_read",
            "args": [
            [["id", ">", 0]],
            ["id", "name", "list_price", "qty_available", "uom_id", "uom_po_id", "categ_id"]
            ],
            "kwargs": {
            "limit": 1000
            }
        },
        "id": 2
    }
"@

    return Invoke-OdooDataSetCall $body
}

function Add-OdooProduct($name) {
    $name = $name.Replace("""", "\""")
    $body = @"
    {
        "jsonrpc": "2.0",
        "method": "call",
        "params": {
            "model": "product.product",
            "method": "create",
            "args": [
            {
                "name": "$name",
                "list_price": 0
            }
            ],
            "kwargs": {
            }
        },
        "id": 3
    }
"@

    return Invoke-OdooDataSetCall $body
}

function Add-OdooDelivery($sourceDocument) {
    $body = @"
    {
        "jsonrpc": "2.0",
        "method": "call",
        "params": {
            "model": "stock.picking",
            "method": "create",
            "args": [
                {
                    "partner_id": false,
                    "picking_type_id": 2,
                    "location_id": 8,
                    "location_dest_id": 5,
                    "origin": "$sourceDocument"
                }
            ],
            "kwargs": {
            }
        },
        "id": 4
    }

"@

    return Invoke-OdooDataSetCall $body
}

function Add-OdooStockMove ($odooDeliveryId, $odooProductId, $count, $description = "") {
    $body = @"
        {
        "jsonrpc": "2.0",
        "method": "call",
        "params": {
            "model": "stock.move",
            "method": "create",
            "args": [
            {
                "picking_id": $odooDeliveryId,
                "product_id": $odooProductId,
                "product_uom_qty": $count,
                "product_uom": 1,
                "location_id": 8,
                "location_dest_id": 5,
                "name": "COOLORANGE",
                "description_picking": "$description"
            }
            ],
            "kwargs": {
            }
        },
    "id": 5
    }
"@

    return Invoke-OdooDataSetCall $body
}
#endregion

Write-Log "Starting ACC Ducts Export to Odoo Deliveries..."

Import-Module powerAPS

# 3-legged OAuth
$apsParameters = @{
    "ClientId" = $clientId
    "ClientSecret" = $clientSecret
    "CallbackUrl" = $callbackUrl
    "Scope" = $scope
    "Username" = $username
    "Password" = $password
}

$result = Open-APSConnection @apsParameters -Express #-Visible
if (-not $result) {
    throw "Cannot open connection"
}

$hubs = Get-ApsAccHubs
$hub = $hubs | Where-Object { $_.attributes.name -eq $hubName }
$projects = Get-ApsProjects $hub
$project = $projects | Where-Object { $_.attributes.name -eq $projectName }

$aecDmHub = Get-ApsAecDmHub $hub.attributes.name
$aecDmProject = Get-ApsAecDmProject $aecDmHub.hubs.results[0].id $project.attributes.name

$aecDmProjectId = $aecDmProject.projects.results[0].id
Write-Log "Project: $($project.attributes.name)"

$metadata = Get-ApsAccSubmittalMetadata $project
$submittals = Get-ApsAccSubmittalItems $project
foreach ($submittal in $submittals) {
    $state = $metadata.statuses | Where-Object { $_.id -eq $submittal.statusId }
    if ($state.value -eq $submittalStatus) {
        Write-Log "Submittal: $($submittal.title) - State: $($state.value)"
        $relationships = Get-ApsAccSubmittalItemRelationships $project $submittal

        $ductObjects = @()
        foreach ($relationship in $relationships) {
            $reference = $relationship.entities | Where-Object { $_.domain -eq "autodesk-bim360-documentmanagement" -and $_.type -eq "documentlineage" }
            $item = Get-ApsItem $project $reference.id
            if ($item.attributes.displayName -notlike "*.rvt") {
                Write-Log "Skipping file '$($item.attributes.displayName)'. It is not a Revit file!"
                continue
            }

            if ($item.attributes.reserved) {
                Write-Log "Skipping reserved file '$($item.attributes.displayName)'. Reserved files have already been transferred to the ERP system!"
                continue
            }

            $elementGroupResult = Get-ApsAecDmElementGroup $aecDmProjectId $reference.id
            $fileId = $elementGroupResult.elementGroupsByProject.results.id
            $fileName = $elementGroupResult.elementGroupsByProject.results.name
            Write-Log "Starting APS AEC Data Model extraction for file: $fileName..."

            $precision = 5
            $cursor = $null
            do {
                if ($cursor) { $continued = " (cursor: $($cursor))" } else { $continued = "" }
                Write-Log "Extracting granular data from: $($item.attributes.displayName)$($continued)..."
                $elementsResult = Get-ApsAecDmDuctsElements $fileId $cursor
                $cursor = $elementsResult.elementsByElementGroup.pagination.cursor
                foreach ($element in $elementsResult.elementsByElementGroup.results) {
                    $props = @{}
                    foreach ($prop in $element.properties.results) {
                        if ($prop.definition.units) {
                            $value = $prop.value
                            if ($prop.definition.units.name -eq "Meters") {
                                $valueInInches = [Math]::Round([double]$value * 100 / 2.54, $precision)
                            }
                            elseif ($prop.definition.units.name -eq "Centimeters") {
                                $valueInInches = [Math]::Round([double]$value / 2.54, $precision)
                            }
                            elseif ($prop.definition.units.name -eq "Millimeters") {
                                $valueInInches = [Math]::Round([double]$value / 25.4, $precision)
                            }
                            elseif ($prop.definition.units.name -eq "Inches") {
                                $valueInInches = [Math]::Round([double]$value, $precision)
                            }
                            elseif ($prop.definition.units.name -eq "Feet") {
                                $valueInInches = [Math]::Round([double]$value * 12, $precision)
                            }
                            $props += @{$prop.name = $valueInInches }
                        }
                        else {
                            $props += @{$prop.name = $prop.value }
                        }
                    }
            
                    $props += @{"Name" = "" }
                    $props += @{"StandardLength" = 0 }
                    $props += @{"Count" = 1 }
                    $ductObject = New-Object PsObject -Property $props
                    $ductObjects += $ductObject
                }
            } while ($cursor)

            Update-ApsItemLocked $project $item $true | Out-Null
        }

        if ($ductObjects.Count -eq 0) {
            Write-Log "No ducts found in the submittal!"
            continue
        }

        # Add the standard length, the leftover and the name to each duct object
        $diameterSymbol = [Convert]::ToString([System.Text.Encoding]::Default.GetString([byte]248)) # -> ø
        foreach ($ductObject in $ductObjects) {
            $length = $ductObject.Length
            if ($length) {
                $ductObject.Length = $length
                $ductObject.StandardLength = $standardLength
            }

            if ($ductObject.Diameter) {
                $name = "$($ductObject.'Family Name') / $($ductObject.'Element Name') $($ductObject.Diameter)""$diameterSymbol"
            }
            elseif ($ductObject.Size) {
                $name = "$($ductObject.'Family Name') / $($ductObject.'Element Name') $($ductObject.Size)"
            }
            else {
                $name = "$($ductObject.'Family Name') / $($ductObject.'Element Name')"
            }
            $ductObject.Name = $name
        }

        Write-Log "Total ducts: $($ductObjects.Count)"

        # Group results to get the right order, the right length, the total count and the waste
        $groupedDuctObjects = $ductObjects | 
        Group-Object 'Name', 'StandardLength' |
        ForEach-Object {
            if ($_.Group[0].StandardLength -eq 0) {
                $count = ($_.Group | Measure-Object -Property Count -Sum).Sum
                $waste = 0
                $length = $null
                $name = $_.Group[0].Name
            }
            else {
                $count = [Math]::Ceiling(($_.Group | Measure-Object -Property Length -Sum).Sum / $_.Group[0].StandardLength)
                $waste = $count * $_.Group[0].StandardLength - ($_.Group | Measure-Object -Property Length -Sum).Sum
                $length = ($_.Group | Measure-Object -Property Length -Sum).Sum
                $name = if ($_.Group[0].StandardLength -gt 0) { "$($_.Group[0].Name) ($($_.Group[0].StandardLength)`")" } else { $_.Group[0].Name }
            } 

            [PSCustomObject]@{
                'Name'   = $name
                'Length' = $length
                'Count'  = $count
                'Waste'  = [Math]::Round($waste, 2)
            }
        } | Sort-Object 'Name', 'StandardLength'
        $groupedDuctObjects | Format-Table -AutoSize

        Write-Log "Total duct objects: $($ductObjects.Count)"
        Write-Log "Unique duct objects: $($groupedDuctObjects.Count)"
        Write-Log "Total duct pipe length: $(($ductObjects | Measure-Object -Property Length -Sum).Sum)`""
        Write-Log "Total duct pipe waste: $(($groupedDuctObjects | Measure-Object -Property Waste -Sum).Sum)`""

        Write-Log "$($groupedDuctObjects.Count) unique ducts found in $($relationships.Count) file(s)!"

        $creatingCount = 0;
        $existingCount = 0;

        $global:odooSession = Invoke-OdooAuthentication
        $odooDeliveryId = Add-OdooDelivery "ACC Submittal: $($submittal.title)"
        Write-Log "Delivery created in Odoo: $($submittal.title)"
        $odooProducts = Get-OdooProducts
        
        foreach ($d in $groupedDuctObjects) {
            $odooProduct = @($odooProducts | Where-Object { $_.name -eq $d.Name })[0]
            if (-not $odooProduct) {
                $odooProductId = Add-OdooProduct($d.Name)
                $creatingCount++
            } else {
                $odooProductId = $odooProduct.id
                $existingCount++
            }

            $d | Add-Member -MemberType NoteProperty -Name "OdooProductId" -Value $odooProductId -Force
            
            $odooStockMoveId = Add-OdooStockMove $odooDeliveryId $d.OdooProductId $d.Count
            $odooStockMoveId | Out-Null
        }
        Write-Log "$creatingCount new product(s) created and $existingCount product(s) existed in Odoo"
        Write-Log "$($groupedDuctObjects.Count) product(s) added to the new delivery (Id $($odooDeliveryId))"
    }
}

Close-APSConnection

Write-Log "Finished APS Task Scheduler Job"
