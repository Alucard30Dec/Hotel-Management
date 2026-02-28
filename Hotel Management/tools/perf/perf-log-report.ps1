param(
    [Parameter(Mandatory = $true)]
    [string]$LogPath,
    [string]$OutDir = "./tools/perf/out"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LogPath)) {
    throw "Log file not found: $LogPath"
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Get-PercentileValue {
    param(
        [double[]]$Values,
        [double]$Percentile
    )

    if ($Values.Count -eq 0) { return 0 }
    $sorted = $Values | Sort-Object
    $position = [Math]::Ceiling(($Percentile / 100.0) * $sorted.Count) - 1
    if ($position -lt 0) { $position = 0 }
    if ($position -ge $sorted.Count) { $position = $sorted.Count - 1 }
    return [double]$sorted[$position]
}

$rows = New-Object System.Collections.Generic.List[object]

Get-Content -LiteralPath $LogPath -Encoding UTF8 | ForEach-Object {
    $line = $_
    if ($line -notmatch 'PerfScope:\s*(?<Operation>[^|]+)') { return }

    $operation = ($Matches.Operation).Trim()
    if ([string]::IsNullOrWhiteSpace($operation)) { return }

    $context = @{}
    $parts = $line -split '\s\|\s'
    foreach ($part in $parts) {
        if ($part -notmatch '^(?<Key>[A-Za-z0-9_]+)=(?<Value>.*)$') { continue }
        $key = $Matches.Key.Trim()
        $value = $Matches.Value.Trim()
        $context[$key] = $value
    }

    $elapsed = 0.0
    if ($context.ContainsKey('ElapsedMs')) {
        [double]::TryParse($context['ElapsedMs'], [ref]$elapsed) | Out-Null
    }

    $alloc = 0.0
    if ($context.ContainsKey('MemoryDeltaBytes')) {
        [double]::TryParse($context['MemoryDeltaBytes'], [ref]$alloc) | Out-Null
    }

    $gc0 = 0
    if ($context.ContainsKey('Gc0Delta')) {
        [int]::TryParse($context['Gc0Delta'], [ref]$gc0) | Out-Null
    }

    $gc1 = 0
    if ($context.ContainsKey('Gc1Delta')) {
        [int]::TryParse($context['Gc1Delta'], [ref]$gc1) | Out-Null
    }

    $gc2 = 0
    if ($context.ContainsKey('Gc2Delta')) {
        [int]::TryParse($context['Gc2Delta'], [ref]$gc2) | Out-Null
    }

    $rows.Add([pscustomobject]@{
        Operation = $operation
        ElapsedMs = $elapsed
        MemoryDeltaBytes = $alloc
        Gc0Delta = $gc0
        Gc1Delta = $gc1
        Gc2Delta = $gc2
    })
}

if ($rows.Count -eq 0) {
    throw "No PerfScope lines found in $LogPath"
}

$rawPath = Join-Path $OutDir 'perfscope-raw.csv'
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $rawPath

$summary = $rows |
    Group-Object Operation |
    ForEach-Object {
        $values = @($_.Group | ForEach-Object { [double]$_.ElapsedMs })
        [pscustomobject]@{
            Operation = $_.Name
            Calls = $_.Count
            TotalMs = [Math]::Round(($values | Measure-Object -Sum).Sum, 2)
            AvgMs = [Math]::Round(($values | Measure-Object -Average).Average, 2)
            P95Ms = [Math]::Round((Get-PercentileValue -Values $values -Percentile 95), 2)
            MaxMs = [Math]::Round(($values | Measure-Object -Maximum).Maximum, 2)
            AvgAllocBytes = [Math]::Round((($_.Group | Measure-Object -Property MemoryDeltaBytes -Average).Average), 2)
            TotalGc0 = ($_.Group | Measure-Object -Property Gc0Delta -Sum).Sum
            TotalGc1 = ($_.Group | Measure-Object -Property Gc1Delta -Sum).Sum
            TotalGc2 = ($_.Group | Measure-Object -Property Gc2Delta -Sum).Sum
        }
    } |
    Sort-Object TotalMs -Descending

$summaryPath = Join-Path $OutDir 'perfscope-summary.csv'
$summary | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $summaryPath

$top10Path = Join-Path $OutDir 'hotspots-top10.csv'
$summary | Select-Object -First 10 | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $top10Path

Write-Host "Raw CSV: $rawPath"
Write-Host "Summary CSV: $summaryPath"
Write-Host "Top10 CSV: $top10Path"
