#define MyAppName "Hotel Management"
#define MyAppPublisher "MANHPC.COM"
#define MyAppExeName "Hotel Management.exe"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef SourceDir
  #define SourceDir "..\..\bin\Release"
#endif

[Setup]
AppId={{D57C892D-E5F8-426C-AB1D-E884C583FFAF}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
CloseApplications=yes
DisableProgramGroupPage=yes
OutputBaseFilename=HotelManagement_Setup

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "vietnamese"; MessagesFile: "compiler:Languages\Vietnamese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion; Excludes: "Hotel Management.exe.config,*.pdb,*.xml,logs\*"
Source: "{#SourceDir}\Hotel Management.exe.config"; DestDir: "{app}"; Flags: onlyifdoesntexist ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet48OrNewerInstalled: Boolean;
var
  Release: Cardinal;
begin
  Result :=
    (RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040)) or
    (RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040));
end;

function InitializeSetup: Boolean;
begin
  if not IsDotNet48OrNewerInstalled then
  begin
    MsgBox('This application requires .NET Framework 4.8 or newer. Please install it and run setup again.', mbCriticalError, MB_OK);
    Result := False;
  end
  else
    Result := True;
end;
