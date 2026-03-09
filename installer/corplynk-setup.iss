; CorpLynk Call Center - InnoSetup Installer Script
; Kullanim:
;   1. dotnet publish src/CallCenter.Windows -c Release -p:PublishProfile=win-x64
;   2. iscc installer/corplynk-setup.iss
;
; Gereksinimler:
;   - InnoSetup 6.x (https://jrsoftware.org/isinfo.php)
;   - Publish ciktisi: publish/win-x64/ klasorunde

#define MyAppName "CorpLynk"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "CorpLynk"
#define MyAppURL "https://corplynk.com"
#define MyAppExeName "CorpLynk.exe"
#define MyAppDescription "CorpLynk Call Center - Profesyonel cagri merkezi uygulamasi"

[Setup]
AppId={{7E2F8A3B-4C5D-6E7F-8A9B-0C1D2E3F4A5B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\publish\installer
OutputBaseFilename=CorpLynk-{#MyAppVersion}-setup
SetupIconFile=..\src\CallCenter.Windows\Assets\corplynk.ico
UninstallDisplayIcon={app}\Assets\corplynk.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppDescription}
VersionInfoProductName={#MyAppName}

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startup"; Description: "Windows baslangicindan otomatik baslat"; GroupDescription: "Ek secenekler:"

[Files]
; Self-contained single-file publish ciktisi
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
; Otomatik baslatma (kullanici sectiginde)
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startup

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
// Eski versiyon yukluyse otomatik kaldirma
function InitializeSetup(): Boolean;
var
  UninstallKey: String;
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := True;
  UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{7E2F8A3B-4C5D-6E7F-8A9B-0C1D2E3F4A5B}_is1';
  if RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', UninstallString) or
     RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', UninstallString) then
  begin
    if MsgBox('CorpLynk zaten yuklu. Onceki surumu kaldirip yenisini yuklemek ister misiniz?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    end
    else
      Result := False;
  end;
end;
