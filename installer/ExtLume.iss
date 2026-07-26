#define MyAppName "ExtLume"
#define MyAppVersion "0.3.0-beta.1"
#define MyAppNumericVersion "0.3.0.0"
#define MyAppExeName "ExtLume.exe"

[Setup]
AppId={{A09EFA0D-F3B2-4741-95B8-5974E489D708}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher=ExtLume contributors
DefaultDirName={localappdata}\Programs\ExtLume
DefaultGroupName=ExtLume
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\artifacts\installer
OutputBaseFilename=ExtLume-{#MyAppVersion}-Setup
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UsePreviousAppDir=yes
VersionInfoVersion={#MyAppNumericVersion}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppNumericVersion}
LicenseFile=..\LICENSE

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Start with Windows"; GroupDescription: "Startup"; Flags: unchecked

[Files]
Source: "..\artifacts\ExtLume.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.zh-CN.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\PRIVACY.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\COMPATIBILITY.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\COMPATIBILITY.zh-CN.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\VALIDATION_REPORT_v0.3.0-beta.1.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\assets\app-logo.png"; DestDir: "{app}\assets"; Flags: ignoreversion
Source: "..\assets\ui-preview.png"; DestDir: "{app}\assets"; Flags: ignoreversion

[Icons]
Name: "{group}\ExtLume"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\ExtLume"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "ExtLume"; ValueData: """{app}\{#MyAppExeName}"" --startup"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,ExtLume}"; Flags: nowait postinstall skipifsilent
