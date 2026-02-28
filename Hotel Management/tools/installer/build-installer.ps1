param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SkipBuild,
    [string]$InstallerOutputDir = "",
    [string]$InnoCompilerPath = "",
    [switch]$NoAutoInstallInno
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Find-MSBuild {
    $cmd = Get-Command msbuild -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $vswhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vswhere)) {
        throw "Cannot find MSBuild. Install Visual Studio Build Tools with .NET desktop build tools."
    }

    $found = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
    if (-not $found) {
        throw "Cannot find MSBuild via vswhere."
    }

    return $found
}

function Find-InnoCompiler {
    param(
        [string]$PreferredPath = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($PreferredPath) -and (Test-Path $PreferredPath)) {
        return (Resolve-Path $PreferredPath).Path
    }

    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }
    $cmd = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $appPaths = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\ISCC.exe",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\ISCC.exe"
    )
    foreach ($key in $appPaths) {
        try {
            $item = Get-ItemProperty -Path $key -ErrorAction Stop
            if ($item.'(default)' -and (Test-Path $item.'(default)')) {
                return $item.'(default)'
            }
        }
        catch {
        }
    }

    $uninstallKeys = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 6_is1",
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1"
    )
    foreach ($key in $uninstallKeys) {
        try {
            $item = Get-ItemProperty -Path $key -ErrorAction Stop
            if ($item.InstallLocation) {
                $candidate = Join-Path $item.InstallLocation "ISCC.exe"
                if (Test-Path $candidate) {
                    return $candidate
                }
            }
        }
        catch {
        }
    }

    $paths = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe",
        "C:\Program Files\Inno Setup 5\ISCC.exe"
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw "Cannot find Inno Setup Compiler (ISCC.exe). Install Inno Setup (winget install JRSoftware.InnoSetup) or pass -InnoCompilerPath."
}

function Ensure-InnoCompiler {
    param(
        [string]$PreferredPath = "",
        [switch]$DisableAutoInstall
    )

    try {
        return Find-InnoCompiler -PreferredPath $PreferredPath
    }
    catch {
        if ($DisableAutoInstall) {
            throw
        }
    }

    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if (-not $winget) {
        throw "Cannot find ISCC.exe and winget is not available. Install Inno Setup manually, or pass -InnoCompilerPath."
    }

    Write-Host "ISCC.exe not found. Attempting to install Inno Setup via winget..."
    & $winget.Source install --id JRSoftware.InnoSetup -e --silent --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        Write-Host "winget attempt #1 failed with exit code $LASTEXITCODE. Retrying with --scope user..."
        & $winget.Source install --id JRSoftware.InnoSetup -e --scope user --silent --accept-package-agreements --accept-source-agreements
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Auto-install Inno Setup failed with exit code $LASTEXITCODE. Install Inno Setup manually or pass -InnoCompilerPath."
    }

    Start-Sleep -Seconds 2
    return Find-InnoCompiler -PreferredPath $PreferredPath
}

function Build-IExpressInstaller {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StageDir,
        [Parameter(Mandatory = $true)]
        [string]$OutputDir,
        [Parameter(Mandatory = $true)]
        [string]$OutputFileName,
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [string]$DistDir
    )

    $iexpressPath = Join-Path $env:WINDIR "System32\iexpress.exe"
    if (-not (Test-Path $iexpressPath)) {
        throw "IExpress is not available on this Windows machine."
    }

    $workDir = Join-Path $DistDir "iexpress-work"
    if (Test-Path $workDir) {
        Remove-Item -LiteralPath $workDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null

    $payloadZip = Join-Path $workDir "payload.zip"
    Compress-Archive -Path (Join-Path $StageDir "*") -DestinationPath $payloadZip -Force

    $installScript = Join-Path $workDir "install.ps1"
    @'
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
'@ | Set-Content -Path $installScript -Encoding ASCII

    $outputFile = Join-Path $OutputDir ($OutputFileName + ".exe")
    $sedPath = Join-Path $workDir "installer.sed"

    @"
[Version]
Class=IEXPRESS
SEDVersion=3

[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=0
HideExtractAnimation=1
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=
TargetName=%TargetName%
FriendlyName=%FriendlyName%
AppLaunched=%AppLaunched%
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles

[Strings]
TargetName=$outputFile
FriendlyName=Hotel Management Setup v$Version
AppLaunched=powershell.exe -NoProfile -ExecutionPolicy Bypass -File install.ps1
FILE0=payload.zip
FILE1=install.ps1

[SourceFiles]
SourceFiles0=$workDir

[SourceFiles0]
%FILE0%=
%FILE1%=
"@ | Set-Content -Path $sedPath -Encoding ASCII

    & $iexpressPath /N /Q $sedPath
    if ($LASTEXITCODE -ne 0) {
        throw "IExpress build failed with exit code $LASTEXITCODE."
    }
    if (-not (Test-Path $outputFile)) {
        throw "IExpress did not produce installer file."
    }

    return $outputFile
}

function Find-CSharpCompiler {
    $cmd = Get-Command csc -ErrorAction SilentlyContinue
    if ($cmd) {
        return $cmd.Source
    }

    $paths = @(
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe"
    )

    foreach ($path in $paths) {
        if (Test-Path $path) {
            return $path
        }
    }

    throw "Cannot find csc.exe for self-extract installer fallback."
}

function Build-SelfExtractingInstaller {
    param(
        [Parameter(Mandatory = $true)]
        [string]$StageDir,
        [Parameter(Mandatory = $true)]
        [string]$OutputDir,
        [Parameter(Mandatory = $true)]
        [string]$OutputFileName,
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [string]$DistDir
    )

    $workDir = Join-Path $DistDir "sfx-work"
    if (Test-Path $workDir) {
        Remove-Item -LiteralPath $workDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $workDir -Force | Out-Null

    $payloadZip = Join-Path $workDir "payload.zip"
    Compress-Archive -Path (Join-Path $StageDir "*") -DestinationPath $payloadZip -Force

    $stubCodePath = Join-Path $workDir "InstallerStub.cs"
    @'
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

internal static class Program
{
    private const string Marker = "HMSETUP1";
    private const string AppName = "Hotel Management";
    private const string ExeName = "Hotel Management.exe";
    private const string ConfigName = "Hotel Management.exe.config";

    [STAThread]
    private static void Main()
    {
        try
        {
            if (!IsDotNet48OrNewer())
            {
                MessageBox.Show(
                    ".NET Framework 4.8+ is required. Please install it and run setup again.",
                    "Hotel Management Setup",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var installerExe = Application.ExecutablePath;
            var payloadZip = ExtractEmbeddedPayload(installerExe);
            if (string.IsNullOrWhiteSpace(payloadZip) || !File.Exists(payloadZip))
            {
                MessageBox.Show("Setup payload was not found.", "Hotel Management Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var installDir = ResolveInstallDir();
            Directory.CreateDirectory(installDir);

            var extractedDir = Path.Combine(Path.GetTempPath(), "hm_extract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractedDir);
            ZipFile.ExtractToDirectory(payloadZip, extractedDir);

            string backupConfig = null;
            var existingConfig = Path.Combine(installDir, ConfigName);
            if (File.Exists(existingConfig))
            {
                backupConfig = Path.Combine(Path.GetTempPath(), "hm_cfg_" + Guid.NewGuid().ToString("N") + ".config");
                File.Copy(existingConfig, backupConfig, true);
            }

            CopyDirectory(extractedDir, installDir);

            if (!string.IsNullOrWhiteSpace(backupConfig) && File.Exists(backupConfig))
            {
                File.Copy(backupConfig, existingConfig, true);
                TryDeleteFile(backupConfig);
            }

            var appExe = Path.Combine(installDir, ExeName);
            if (!File.Exists(appExe))
            {
                MessageBox.Show("Install failed: app executable not found after extraction.", "Hotel Management Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            TryDeleteFile(payloadZip);
            TryDeleteDirectory(extractedDir);

            MessageBox.Show(
                "Install completed successfully.\nLocation: " + installDir,
                "Hotel Management Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Installer failed: " + ex.Message, "Hotel Management Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsDotNet48OrNewer()
    {
        return GetReleaseFromKey(@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full") >= 528040
            || GetReleaseFromKey(@"SOFTWARE\WOW6432Node\Microsoft\NET Framework Setup\NDP\v4\Full") >= 528040;
    }

    private static int GetReleaseFromKey(string keyPath)
    {
        try
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath))
            {
                if (key == null) return 0;
                var value = key.GetValue("Release");
                if (value == null) return 0;
                return Convert.ToInt32(value);
            }
        }
        catch
        {
            return 0;
        }
    }

    private static string ResolveInstallDir()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var preferred = Path.Combine(programFiles, AppName);
        try
        {
            Directory.CreateDirectory(preferred);
            return preferred;
        }
        catch
        {
            var fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                AppName);
            Directory.CreateDirectory(fallback);
            return fallback;
        }
    }

    private static string ExtractEmbeddedPayload(string installerExePath)
    {
        var markerBytes = System.Text.Encoding.ASCII.GetBytes(Marker);
        using (var fs = new FileStream(installerExePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            if (fs.Length < markerBytes.Length + 8) return null;

            fs.Seek(-(markerBytes.Length + 8), SeekOrigin.End);

            var markerTail = new byte[markerBytes.Length];
            ReadExactly(fs, markerTail, 0, markerTail.Length);
            if (!markerTail.SequenceEqual(markerBytes)) return null;

            var lengthBytes = new byte[8];
            ReadExactly(fs, lengthBytes, 0, 8);
            var payloadLength = BitConverter.ToInt64(lengthBytes, 0);
            if (payloadLength <= 0) return null;

            var payloadStart = fs.Length - markerBytes.Length - 8 - payloadLength;
            if (payloadStart < 0) return null;

            var zipPath = Path.Combine(Path.GetTempPath(), "hm_payload_" + Guid.NewGuid().ToString("N") + ".zip");
            fs.Seek(payloadStart, SeekOrigin.Begin);
            using (var outStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CopyBytes(fs, outStream, payloadLength);
            }
            return zipPath;
        }
    }

    private static void ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        var readTotal = 0;
        while (readTotal < count)
        {
            var read = stream.Read(buffer, offset + readTotal, count - readTotal);
            if (read <= 0) throw new EndOfStreamException();
            readTotal += read;
        }
    }

    private static void CopyBytes(Stream input, Stream output, long count)
    {
        var buffer = new byte[81920];
        long remaining = count;
        while (remaining > 0)
        {
            var read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0) throw new EndOfStreamException();
            output.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);

        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = dir.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
            Directory.CreateDirectory(Path.Combine(destinationDir, relative));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = file.Substring(sourceDir.Length).TrimStart(Path.DirectorySeparatorChar);
            var target = Path.Combine(destinationDir, relative);
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
            File.Copy(file, target, true);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
        }
    }
}
'@ | Set-Content -Path $stubCodePath -Encoding ASCII

    $stubExePath = Join-Path $workDir "InstallerStub.exe"
    $cscPath = Find-CSharpCompiler

    & $cscPath /nologo /target:winexe /optimize+ /out:$stubExePath /r:System.Windows.Forms.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll $stubCodePath
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $stubExePath)) {
        throw "Failed to compile self-extract installer stub."
    }

    $outputFile = Join-Path $OutputDir ($OutputFileName + ".exe")
    Copy-Item -LiteralPath $stubExePath -Destination $outputFile -Force

    [byte[]]$payloadBytes = [System.IO.File]::ReadAllBytes($payloadZip)
    [byte[]]$markerBytes = [System.Text.Encoding]::ASCII.GetBytes("HMSETUP1")
    [byte[]]$lengthBytes = [System.BitConverter]::GetBytes([Int64]$payloadBytes.Length)

    $stream = [System.IO.File]::Open($outputFile, [System.IO.FileMode]::Append, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
    try {
        $stream.Write($payloadBytes, 0, $payloadBytes.Length)
        $stream.Write($markerBytes, 0, $markerBytes.Length)
        $stream.Write($lengthBytes, 0, $lengthBytes.Length)
    }
    finally {
        $stream.Dispose()
    }

    return $outputFile
}

function Get-AppVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AssemblyInfoPath
    )

    $content = Get-Content -Path $AssemblyInfoPath -Raw
    $match = [regex]::Match($content, 'AssemblyFileVersion\("([^"]+)"\)')
    if ($match.Success) {
        return $match.Groups[1].Value
    }

    return "1.0.0"
}

function Copy-OptionalItem {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourcePath,
        [Parameter(Mandatory = $true)]
        [string]$DestinationRoot
    )

    if (-not (Test-Path $SourcePath)) {
        return
    }

    $item = Get-Item -LiteralPath $SourcePath
    if ($item.PSIsContainer) {
        $target = Join-Path $DestinationRoot $item.Name
        if (Test-Path $target) {
            Remove-Item -LiteralPath $target -Recurse -Force
        }
        Copy-Item -LiteralPath $SourcePath -Destination $target -Recurse -Force
    }
    else {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationRoot -Force
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptDir "..\.."))
$repoParent = Split-Path -Parent $repoRoot
$projectPath = Join-Path $repoRoot "Hotel Management.csproj"
$solutionPath = Join-Path $repoRoot "Hotel Management.sln"
$assemblyInfoPath = Join-Path $repoRoot "Properties\AssemblyInfo.cs"
$issPath = Join-Path $scriptDir "HotelManagement.iss"
$releaseDir = Join-Path $repoRoot "bin\$Configuration"
$distDir = Join-Path $repoRoot "dist"
$stageDir = Join-Path $distDir "staging"
if ([string]::IsNullOrWhiteSpace($InstallerOutputDir)) {
    $installerOutDir = Join-Path $repoParent "Install"
}
else {
    $installerOutDir = [System.IO.Path]::GetFullPath($InstallerOutputDir)
}
$appExePath = Join-Path $releaseDir "Hotel Management.exe"

if (-not (Test-Path $projectPath)) {
    throw "Cannot find project file: $projectPath"
}
if (-not (Test-Path $issPath)) {
    throw "Cannot find installer script: $issPath"
}

$appVersion = Get-AppVersion -AssemblyInfoPath $assemblyInfoPath
Write-Host "Version: $appVersion"

if (-not $SkipBuild) {
    $msbuildPath = Find-MSBuild
    Write-Host "Using MSBuild: $msbuildPath"

    & $msbuildPath $projectPath /restore /t:Build /p:Configuration=$Configuration /p:Platform="AnyCPU" /nologo /verbosity:minimal
    if ($LASTEXITCODE -ne 0) {
        $restoreExitCode = $LASTEXITCODE
        $nuget = Get-Command nuget -ErrorAction SilentlyContinue
        if (-not $nuget) {
            throw "Build failed (exit code $restoreExitCode). Install nuget.exe and retry, or run restore manually in Visual Studio."
        }

        Write-Host "MSBuild restore failed. Trying fallback with nuget restore..."
        if (Test-Path $solutionPath) {
            & $nuget.Source restore $solutionPath
        }
        else {
            & $nuget.Source restore $projectPath
        }
        if ($LASTEXITCODE -ne 0) {
            throw "NuGet restore failed with exit code $LASTEXITCODE."
        }

        & $msbuildPath $projectPath /t:Build /p:Configuration=$Configuration /p:Platform="AnyCPU" /nologo /verbosity:minimal
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed with exit code $LASTEXITCODE."
        }
    }
}

if (-not (Test-Path $appExePath)) {
    throw "Build output not found: $appExePath"
}

if (Test-Path $stageDir) {
    Remove-Item -LiteralPath $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

Copy-Item -Path (Join-Path $releaseDir "*") -Destination $stageDir -Recurse -Force

Copy-OptionalItem -SourcePath (Join-Path $repoRoot "Address") -DestinationRoot $stageDir
Copy-OptionalItem -SourcePath (Join-Path $repoRoot "tessdata") -DestinationRoot $stageDir
Copy-OptionalItem -SourcePath (Join-Path $repoRoot "diaban.json") -DestinationRoot $stageDir

Get-ChildItem -Path $stageDir -Recurse -File -Include *.pdb,*.xml | Remove-Item -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $stageDir "logs") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $stageDir "startup-*.stamp") -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $stageDir "startup-error.log") -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $installerOutDir -Force | Out-Null
$setupFileName = "HotelManagement_Setup_v$appVersion"
$installerPath = ""
$usedInnoSetup = $false

try {
    $isccPath = Ensure-InnoCompiler -PreferredPath $InnoCompilerPath -DisableAutoInstall:$NoAutoInstallInno
    Write-Host "Using Inno Setup: $isccPath"

    & $isccPath "/DSourceDir=$stageDir" "/DAppVersion=$appVersion" "/O$installerOutDir" "/F$setupFileName" $issPath
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup build failed with exit code $LASTEXITCODE."
    }

    $installerPath = Join-Path $installerOutDir "$setupFileName.exe"
    if (-not (Test-Path $installerPath)) {
        throw "Installer file not found after Inno Setup build."
    }

    $usedInnoSetup = $true
}
catch {
    Write-Warning ("Inno Setup path failed: " + $_.Exception.Message)
    try {
        Write-Host "Falling back to Windows IExpress to build setup..."
        $installerPath = Build-IExpressInstaller -StageDir $stageDir -OutputDir $installerOutDir -OutputFileName $setupFileName -Version $appVersion -DistDir $distDir
    }
    catch {
        Write-Warning ("IExpress fallback failed: " + $_.Exception.Message)
        Write-Host "Falling back to managed self-extracting Setup.exe..."
        $installerPath = Build-SelfExtractingInstaller -StageDir $stageDir -OutputDir $installerOutDir -OutputFileName $setupFileName -Version $appVersion -DistDir $distDir
    }
}

Write-Host ""
Write-Host "Installer created successfully:"
Write-Host $installerPath
if (-not $usedInnoSetup) {
    Write-Host "Packaging mode: IExpress fallback (no Inno Setup needed)."
}
