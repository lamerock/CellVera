#define MyAppName "CellVera"
#define MyAppPublisher "CellVera"
#define MyAppExeName "CellVera.exe"
#define MyAppDescription "Battery health and power insights for Windows laptops"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "publish"
#endif

[Setup]
AppId={{D9D13A6B-47B8-4DE2-9B3D-8F2D0D63A001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}

VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}
VersionInfoProductName={#MyAppName}

DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}

PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DisableProgramGroupPage=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

WizardStyle=modern

SetupIconFile=Assets\CellVera.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

OutputDir=installer
OutputBaseFilename=CellVera-Setup-{#MyAppVersion}

Compression=lzma2/max
SolidCompression=yes

UninstallDisplayName={#MyAppName}
CreateUninstallRegKey=yes

CloseApplications=yes
RestartApplications=no

AllowNoIcons=yes
ShowLanguageDialog=no


[Languages]

Name: "english"; MessagesFile: "compiler:Default.isl"


[Tasks]

Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked


[Files]

Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]

Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "{#MyAppDescription}"

Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Comment: "{#MyAppDescription}"; Tasks: desktopicon


[Run]

Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent