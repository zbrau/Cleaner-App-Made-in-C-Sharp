#define MyAppName "AppCleaner Pro"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "brau"
#define MyAppURL "https://github.com/zbrau/Cleaner-App-Made-in-C-Sharp"
#define MyAppExeName "AppCleanerPro.exe"

[Setup]
AppId={{6B5F76F0-0695-46D9-B1A8-DFA9BC202611}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
OutputDir=F:\PROYECTOS\C+\AppCleanerPro\dist
OutputBaseFilename=AppCleaner_Setup_v2.0.0
SetupIconFile=F:\PROYECTOS\C+\AppCleanerPro\Assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "F:\PROYECTOS\C+\AppCleanerPro\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
