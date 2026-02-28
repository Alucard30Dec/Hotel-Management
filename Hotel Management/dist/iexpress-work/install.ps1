$ErrorActionPreference = "Stop"

function Test-DotNet48OrNewer {
    $release = 0
    if (Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release -ErrorAction SilentlyContinue) {
        $release = [int](Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release)
    }
    elseif (Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release -ErrorAction SilentlyContinue) {
        $release = [int](Get-ItemPropertyValue -Path "HKLM:\SOFTWARE\WOW6432Node\Microsoft\NET Framework Setup\NDP\v4\Full" -Name Release)
    }
    return ($release -ge 528040)
}

Add-Type -AssemblyName System.Windows.Forms

if (-not (Test-DotNet48OrNewer)) {
    [System.Windows.Forms.MessageBox]::Show(
        ".NET Framework 4.8+ is required. Please install it and run setup again.",
        "Hotel Management Setup",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
    exit 1
}

$appName = "Hotel Management"
$exeName = "Hotel Management.exe"
$defaultDir = Join-Path $env:ProgramFiles $appName
$fallbackDir = Join-Path $env:LOCALAPPDATA "Programs\Hotel Management"
$installDir = $defaultDir
$payload = Join-Path $PSScriptRoot "payload.zip"

try {
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}
catch {
    $installDir = $fallbackDir
    New-Item -ItemType Directory -Path $installDir -Force | Out-Null
}

$existingConfig = Join-Path $installDir "Hotel Management.exe.config"
$configBackup = Join-Path $env:TEMP ("hm_cfg_" + [guid]::NewGuid().ToString("N") + ".config")
if (Test-Path $existingConfig) {
    Copy-Item -LiteralPath $existingConfig -Destination $configBackup -Force
}

Expand-Archive -Path $payload -DestinationPath $installDir -Force

if (Test-Path $configBackup) {
    Copy-Item -LiteralPath $configBackup -Destination $existingConfig -Force
    Remove-Item -LiteralPath $configBackup -Force -ErrorAction SilentlyContinue
}

$exePath = Join-Path $installDir $exeName
if (-not (Test-Path $exePath)) {
    [System.Windows.Forms.MessageBox]::Show(
        "Install failed: app executable not found after extraction.",
        "Hotel Management Setup",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
    exit 1
}

$shell = New-Object -ComObject WScript.Shell
$startMenuPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Hotel Management.lnk"
$desktopPath = Join-Path ([Environment]::GetFolderPath("Desktop")) "Hotel Management.lnk"

$startShortcut = $shell.CreateShortcut($startMenuPath)
$startShortcut.TargetPath = $exePath
$startShortcut.WorkingDirectory = $installDir
$startShortcut.IconLocation = $exePath
$startShortcut.Save()

$desktopShortcut = $shell.CreateShortcut($desktopPath)
$desktopShortcut.TargetPath = $exePath
$desktopShortcut.WorkingDirectory = $installDir
$desktopShortcut.IconLocation = $exePath
$desktopShortcut.Save()

[System.Windows.Forms.MessageBox]::Show(
    "Install completed successfully.`nLocation: $installDir",
    "Hotel Management Setup",
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Information
) | Out-Null
exit 0
