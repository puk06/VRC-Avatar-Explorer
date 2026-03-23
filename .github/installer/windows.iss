#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif

#ifndef MyAppRuntime
  #define MyAppRuntime "win-x64"
#endif

#ifndef MySourceDir
  #define MySourceDir "publish"
#endif

#ifndef MyOutputDir
  #define MyOutputDir "."
#endif

#define MyAppName "VRC-Avatar-Explorer"
#define MyAppExeName "AvatarExplorer.exe"
#define MyRepoRoot "..\\.."

[Setup]
AppId={{7BF331AB-1B3F-4497-BA2A-B34AEE7C90C7}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={code:GetDefaultDirName}
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UsePreviousTasks=no
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir={#MyRepoRoot}\{#MyOutputDir}
OutputBaseFilename={#MyAppName}_{#MyAppVersion}-{#MyAppRuntime}_setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#MyRepoRoot}\AvatarExplorer.UI\Assets\SoftwareIcon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checked

[Files]
Source: "{#MyRepoRoot}\{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Code]
function GetDefaultDirName(Param: string): string;
begin
  if IsAdminInstallMode then
    Result := ExpandConstant('{autopf}\\{#MyAppName}')
  else
    Result := ExpandConstant('{localappdata}\\Programs\\{#MyAppName}');
end;
