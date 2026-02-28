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
TargetName=E:\Project\Hotel-Management\Install\HotelManagement_Setup_v1.0.0.0.exe
FriendlyName=Hotel Management Setup v1.0.0.0
AppLaunched=powershell.exe -NoProfile -ExecutionPolicy Bypass -File install.ps1
FILE0=payload.zip
FILE1=install.ps1

[SourceFiles]
SourceFiles0=E:\Project\Hotel-Management\Hotel Management\dist\iexpress-work

[SourceFiles0]
%FILE0%=
%FILE1%=
