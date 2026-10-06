#define MyAppName "CellVera"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CellVera"
#define MyAppExeName "CellVera.exe"
#define PublishDir "dist\CellVera-20261006-110223"

[Setup]
AppId={{D9D13A6B-47B8-4DE2-9B3D-8F2D0D63A001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\CellVera
DefaultGroupName=CellVera
DisableProgramGroupPage=yes
OutputDir=installer
OutputBaseFilename=CellVera-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=Assets\CellVera.ico
UninstallDisplayIcon={app}\CellVera.exe

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\CellVera"; Filename: "{app}\CellVera.exe"
Name: "{autodesktop}\CellVera"; Filename: "{app}\CellVera.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Run]
Filename: "{app}\CellVera.exe"; Description: "Launch CellVera"; Flags: nowait postinstall skipifsilent