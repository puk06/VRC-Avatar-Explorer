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
#define MyAppDisplayName "VRC Avatar Explorer"
#define MyAppExeName "AvatarExplorer.exe"
#define MyRepoRoot "..\\.."

[Setup]
AppId={{7BF331AB-1B3F-4497-BA2A-B34AEE7C90C7}
AppName={#MyAppDisplayName}
AppVersion={#MyAppVersion}
DefaultDirName={localappdata}\Programs\{#MyAppName}
PrivilegesRequired=lowest
DefaultGroupName={#MyAppDisplayName}
DisableProgramGroupPage=yes
UsePreviousTasks=no
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir={#MyRepoRoot}\{#MyOutputDir}
OutputBaseFilename={#MyAppName}_{#MyAppVersion}-{#MyAppRuntime}_setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#MyRepoRoot}\AvatarExplorer.UI\Assets\SoftwareIcon.ico
AppMutex=AvatarExplorerV2.SingleInstance

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "registerblm"; Description: "Overwrite Booth Library Manager scheme"; GroupDescription: "URL scheme registration:"; Flags: unchecked

[Files]
Source: "{#MyRepoRoot}\{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Registry]
; Register vrcae:// URLs for the current user. This matches PrivilegesRequired=lowest.
Root: HKCU; Subkey: "Software\Classes\vrcae"; ValueType: string; ValueName: ""; ValueData: "URL:vrcae Protocol"
Root: HKCU; Subkey: "Software\Classes\vrcae"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""
Root: HKCU; Subkey: "Software\Classes\vrcae\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKCU; Subkey: "Software\Classes\booth-library-manager"; ValueType: string; ValueName: ""; ValueData: "URL:booth-library-manager Protocol"; Tasks: registerblm
Root: HKCU; Subkey: "Software\Classes\booth-library-manager"; ValueType: string; ValueName: "URL Protocol"; ValueData: ""; Tasks: registerblm
Root: HKCU; Subkey: "Software\Classes\booth-library-manager\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""; Tasks: registerblm

[Icons]
Name: "{autoprograms}\{#MyAppDisplayName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppDisplayName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppDisplayName}}"; Flags: nowait postinstall skipifsilent

[Code]
procedure RemoveSchemeIfOwned(const Scheme: string; const ExpectedCommand: string);
var
  RegisteredCommand: string;
begin
  { Do not remove a handler that another application registered after us. }
  if RegQueryStringValue(
       HKCU,
       'Software\Classes\' + Scheme + '\shell\open\command',
       '',
       RegisteredCommand) and
     SameText(RegisteredCommand, ExpectedCommand) then
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\Classes\' + Scheme);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ExpectedCommand: string;
begin
  if CurUninstallStep <> usUninstall then
    exit;

  ExpectedCommand := '"' + ExpandConstant('{app}\{#MyAppExeName}') + '" "%1"';
  RemoveSchemeIfOwned('vrcae', ExpectedCommand);
  RemoveSchemeIfOwned('booth-library-manager', ExpectedCommand);
end;
