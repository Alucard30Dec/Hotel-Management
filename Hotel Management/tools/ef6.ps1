param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("add", "update", "list", "script")]
    [string]$Command,

    [Parameter(Position = 1)]
    [string]$MigrationName,

    [string]$Configuration = "Debug",

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\Hotel Management.csproj"
$projectPath = [System.IO.Path]::GetFullPath($projectPath)

$projectDir = [System.IO.Path]::GetDirectoryName($projectPath)
$exePath = Join-Path $projectDir ("bin\" + $Configuration + "\Hotel Management.exe")
$configPath = Join-Path $projectDir "App.config"
$efPath = Join-Path $projectDir "packages\EntityFramework.6.5.1\tools\net45\any\ef6.exe"

if (!(Test-Path $projectPath)) {
    throw "Cannot find project file: $projectPath"
}
if (!(Test-Path $efPath)) {
    throw "Cannot find EF6 tool: $efPath"
}

function Resolve-MSBuildPath {
    $msbuildCmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($msbuildCmd) {
        return $msbuildCmd.Path
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $installationPath = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
        if ($installationPath) {
            $candidates = @(
                (Join-Path $installationPath "MSBuild\Current\Bin\MSBuild.exe"),
                (Join-Path $installationPath "MSBuild\15.0\Bin\MSBuild.exe")
            )
            foreach ($candidate in $candidates) {
                if (Test-Path $candidate) { return $candidate }
            }
        }
    }

    $fallbacks = @(
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
    )
    foreach ($candidate in $fallbacks) {
        if (Test-Path $candidate) { return $candidate }
    }

    return $null
}

if (-not $SkipBuild) {
    $msbuildPath = Resolve-MSBuildPath
    if (-not $msbuildPath) {
        throw "msbuild not found. Install Visual Studio Build Tools or run in Developer PowerShell. You can also try -SkipBuild if bin\$Configuration\Hotel Management.exe already exists."
    }

    Write-Host "Building project ($Configuration)..." -ForegroundColor Cyan
    & $msbuildPath $projectPath /t:Build /p:Configuration=$Configuration /nologo /v:m
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed. Fix build errors first."
    }
}

if (!(Test-Path $exePath)) {
    throw "Built assembly not found: $exePath"
}

$common = @(
    "--assembly", $exePath,
    "--project-dir", $projectDir,
    "--language", "C#",
    "--root-namespace", "Hotel_Management",
    "--config", $configPath,
    "--migrations-config", "Hotel_Management.Migrations.Configuration"
)

switch ($Command) {
    "add" {
        if ([string]::IsNullOrWhiteSpace($MigrationName)) {
            throw "Please provide migration name. Example: .\tools\ef6.ps1 add AddRoomPrice"
        }
        & $efPath migrations add $MigrationName @common
    }
    "update" {
        & $efPath database update @common
    }
    "list" {
        & $efPath migrations list @common
    }
    "script" {
        & $efPath database update --script @common
    }
}

if ($LASTEXITCODE -ne 0) {
    throw "EF command failed."
}

Write-Host "Done." -ForegroundColor Green
