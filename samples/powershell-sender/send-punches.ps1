<#
.SYNOPSIS
    Reads a CSV of punches and POSTs them to the attendance API webhook endpoint.

.DESCRIPTION
    Maps to one of the 3 ingestion modes — Mode 3, the PowerShell webhook.
    This script mirrors how a college without a direct biometric API integration
    would push attendance data to us: a scheduled PowerShell task on a building
    PC that watches a folder, reads the CSV the biometric machine dropped, and
    POSTs an envelope.

.EXAMPLE
    .\send-punches.ps1 -ApiBase http://localhost:5080 -TenantId tenant-a -CsvPath .\sample.csv

.NOTES
    Requires PowerShell 7+. No external modules.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ApiBase,
    [Parameter(Mandatory=$true)][string]$TenantId,
    [Parameter(Mandatory=$true)][string]$CsvPath,
    [string]$BatchId = "ps-$(Get-Date -Format yyyyMMddHHmmss)"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $CsvPath)) {
    throw "CSV not found: $CsvPath"
}

$rows = Import-Csv -Path $CsvPath
$punches = $rows | ForEach-Object {
    [PSCustomObject]@{
        externalRef = $_.external_ref
        punchAt     = $_.punch_at
        deviceId    = $_.device_id
        direction   = $_.direction.ToUpper()
    }
}

$envelope = [PSCustomObject]@{
    sourceBatchId = $BatchId
    punches       = $punches
}

$body = $envelope | ConvertTo-Json -Depth 4

Write-Host "Sending $($punches.Count) punches as batch $BatchId to tenant $TenantId..."

$response = Invoke-RestMethod `
    -Uri "$ApiBase/api/v1/punches/webhook" `
    -Method Post `
    -Headers @{ 'X-Tenant-Id' = $TenantId; 'Content-Type' = 'application/json' } `
    -Body $body

Write-Host "Response:" -ForegroundColor Green
$response | ConvertTo-Json -Depth 4 | Write-Host
