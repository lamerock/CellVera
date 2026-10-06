```ini
; ============================================================
; CellVera Installer
; Inno Setup 6
; ============================================================

#define MyAppName "CellVera"
#define MyAppPublisher "CellVera"
#define MyAppExeName "CellVera.exe"
#define MyAppDescription "Battery health and power insights for Windows laptops"

; ------------------------------------------------------------
; Version
;
; GitHub Actions can override this using:
; /DMyAppVersion=1.0.0
;
; When compiling manually, this fallback is used.
; ------------------------------------------------------------

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

; ------------------------------------------------------------
; Published application folder
;
; GitHub Actions can override this using:
; /DPublishDir="D:\...\publish"
;
; When compiling manually, this fallback is used.
; ------------------------------------------------------------

#ifndef PublishDir
  #define PublishDir "publish"
#endif


[Setup]

; IMPORTANT:
; Keep this AppId unchanged between releases.
; Changing it makes Windows treat the installer as a different app.
AppId={{D9D13A6B-47B8-4DE2-9B3D-8F2D0D63A001}

AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}

AppPublisher={#MyAppPublisher}

VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}
VersionInfoProductName={#MyAppName}

; ------------------------------------------------------------
; Installation
;
; Per-user installation avoids unnecessary UAC/admin prompts.
; ------------------------------------------------------------

DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}

PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DisableProgramGroupPage=yes

; ------------------------------------------------------------
; Architecture
; ------------------------------------------------------------

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; ------------------------------------------------------------
; Installer appearance
; ------------------------------------------------------------

WizardStyle=modern

SetupIconFile=Assets\CellVera.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

; ------------------------------------------------------------
; Installer output
; ------------------------------------------------------------

OutputDir=installer
OutputBaseFilename=CellVera-Setup-{#MyAppVersion}

Compression=lzma2/max
SolidCompression=yes

; ------------------------------------------------------------
; Uninstall behavior
; ------------------------------------------------------------

UninstallDisplayName={#MyAppName}
CreateUninstallRegKey=yes

; ------------------------------------------------------------
; Running-instance handling
; ------------------------------------------------------------

CloseApplications=yes
RestartApplications=no

; ------------------------------------------------------------
; Miscellaneous
; ------------------------------------------------------------

AllowNoIcons=yes
ShowLanguageDialog=no


[Languages]

Name: "english"; MessagesFile: "compiler:Default.isl"


[Tasks]

Name: "desktopicon"; \
    Description: "Create a desktop shortcut"; \
    GroupDescription: "Additional shortcuts:"; \
    Flags: unchecked


[Files]

; Copy the entire self-contained .NET publish directory.
Source: "{#PublishDir}\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs


[Icons]

; Start Menu shortcut
Name: "{autoprograms}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; \
    WorkingDir: "{app}"; \
    Comment: "{#MyAppDescription}"

; Optional desktop shortcut
Name: "{autodesktop}\{#MyAppName}"; \
    Filename: "{app}\{#MyAppExeName}"; \
    WorkingDir: "{app}"; \
    Comment: "{#MyAppDescription}"; \
    Tasks: desktopicon


[Run]

Filename: "{app}\{#MyAppExeName}"; \
    Description: "Launch {#MyAppName}"; \
    WorkingDir: "{app}"; \
    Flags: nowait postinstall skipifsilent


[Code]

function InitializeSetup(): Boolean;
begin
  Result := True;
end;
```