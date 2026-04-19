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

[Files]
Source: "{#MyRepoRoot}\{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppDisplayName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppDisplayName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
