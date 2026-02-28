# Build installer for other PCs

This project is a .NET Framework 4.8 WinForms app.  
The installer flow below creates a single `Setup.exe` that you can copy to another Windows PC.

## Prerequisites on build machine (Windows)

1. Visual Studio 2022 Build Tools (or Visual Studio) with `.NET desktop build tools`.
2. Inno Setup 6 (includes `ISCC.exe`) - script can auto-install via `winget` if missing.
3. PowerShell 5+.

## One-command build

From repository root, run:

```bat
tools\installer\build-installer.bat
```

Or with PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\installer\build-installer.ps1
```

## Output

Installer file is generated at:

```text
E:\Project\Hotel-Management\Install\HotelManagement_Setup_v<version>.exe
```

To override output folder:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\installer\build-installer.ps1 -InstallerOutputDir "E:\Project\Hotel-Management\Install"
```

## Notes

1. Installer checks for `.NET Framework 4.8+` on target machine.
2. Installer keeps existing `Hotel Management.exe.config` on upgrade (does not overwrite).
3. Packaging includes app binaries plus `Address`, `tessdata`, and `diaban.json`.
4. If script cannot find `ISCC.exe`, it will auto-run `winget install JRSoftware.InnoSetup`.
5. If Inno Setup install still fails, script will automatically fallback to Windows `IExpress`.
6. If `IExpress` is also blocked by machine policy, script will fallback again to a managed self-extracting `Setup.exe` (compiled by `csc`).
7. To disable auto-install and fail fast:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\installer\build-installer.ps1 -NoAutoInstallInno
```
