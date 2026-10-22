#define AppVersion "0.2.0"
[Setup]
AppId={{19F2CA6C-0536-420C-834C-962B9F351E76}
AppName=ÁudioLivre
AppVersion={#AppVersion}
AppPublisher=arilsonbc
AppPublisherURL=https://github.com/arilsonbc/AudioLivre
DefaultDirName={localappdata}\Programs\AudioLivre
DefaultGroupName=ÁudioLivre
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist\packages
OutputBaseFilename=AudioLivre-{#AppVersion}-win-x64-setup
SetupIconFile=..\src\AudioLivre.Desktop\Assets\audiolivre.ico
UninstallDisplayIcon={app}\AudioLivre.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
CloseApplications=yes
[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; Flags: unchecked
[Files]
Source: "..\dist\0.2.0\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\ÁudioLivre"; Filename: "{app}\AudioLivre.exe"; IconFilename: "{app}\AudioLivre.exe"
Name: "{userdesktop}\ÁudioLivre"; Filename: "{app}\AudioLivre.exe"; Tasks: desktopicon; IconFilename: "{app}\AudioLivre.exe"
[Run]
Filename: "{app}\AudioLivre.exe"; Description: "Abrir ÁudioLivre"; Flags: nowait postinstall skipifsilent
