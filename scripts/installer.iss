#define AppName "AltF4 Cinema"
[Setup]
AppId={{CE6E4E54-E4C7-44DA-A9EF-123DE7A95620}
AppName={#AppName}
AppVersion=1.0.0
AppPublisher=Nhóm AltF4
DefaultDirName={localappdata}\Programs\AltF4Cinema
DefaultGroupName=AltF4 Cinema
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\deliverables
OutputBaseFilename=AltF4-Cinema-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\AltF4.Cinema.WinForms.exe
[Files]
Source: "..\deliverables\portable\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\AltF4 Cinema"; Filename: "{app}\AltF4.Cinema.WinForms.exe"
Name: "{group}\AltF4 Cinema Demo"; Filename: "{app}\AltF4.Cinema.WinForms.exe"; Parameters: "--demo"
Name: "{autodesktop}\AltF4 Cinema"; Filename: "{app}\AltF4.Cinema.WinForms.exe"
[Run]
Filename: "{app}\AltF4.Cinema.WinForms.exe"; Description: "Mo AltF4 Cinema"; Flags: nowait postinstall skipifsilent
