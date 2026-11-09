; Build with Inno Setup 6.7.3 or later. All payload paths are relative to this file.
#ifndef PayloadDir
  #define PayloadDir "..\dist\enestv-win-x64"
#endif
#ifndef OutputPath
  #define OutputPath "..\dist"
#endif
#define AppVersion "1.4.1"

[Setup]
AppId={{6A85B19C-301B-48D1-A92F-17E3679DF57B}
AppName=enestv
AppVersion={#AppVersion}
AppVerName=enestv {#AppVersion}
AppPublisher=Enes
DefaultDirName={localappdata}\Programs\enestv
DefaultGroupName=enestv
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
OutputDir={#OutputPath}
OutputBaseFilename=enestv-Setup-{#AppVersion}-x64
SetupIconFile=..\EnesTV.App\Assets\enestv.ico
UninstallDisplayIcon={app}\enestv.exe
VersionInfoVersion={#AppVersion}
VersionInfoDescription=enestv Windows Kurulumu
VersionInfoProductName=enestv
WizardStyle=modern dark includetitlebar
WizardSizePercent=110
; App logo centred on the dark welcome/finish panel (not stretched); Setup picks the size that fits the display DPI.
WizardImageFile=wizard-logo-120.png,wizard-logo-240.png
WizardImageStretch=no
WizardImageBackColor=#101216
; Small logo on the inner pages; sized copies so it is not lost by scaling a 1254 px image down.
WizardSmallImageFile=wizard-small-58.png,wizard-small-116.png
WizardSmallImageBackColor=#101216
LicenseFile=..\LICENSE
; Third-party notices are shown before installing (after the licence), where "ready to install" wording applies.
InfoBeforeFile=..\THIRD-PARTY-NOTICES.md
DisableWelcomePage=no
DisableDirPage=no
Compression=lzma2
SolidCompression=yes
SetupLogging=yes
CloseApplications=yes
RestartApplications=no
Uninstallable=yes

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Messages]
WelcomeLabel1=enestv'ye hoş geldin.
WelcomeLabel2=Kendi yayınların, kendi ekranın.%n%nBu sihirbaz enestv'yi bilgisayarına kuracak. .NET ve video oynatıcı bileşenleri pakete dahildir.
FinishedHeadingLabel=enestv hazır.
FinishedLabel=enestv bilgisayarına kuruldu.%nKendi yasal IPTV kaynağını ekleyip izlemeye başlayabilirsin.

[Tasks]
Name: "desktopicon"; Description: "Masaüstü kısayolu oluştur"; GroupDescription: "Kısayollar:"; Flags: unchecked

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "LICENSE,THIRD-PARTY-NOTICES.md,SOURCE-ACCESS.md,licenses\*"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\THIRD-PARTY-NOTICES.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\SOURCE-ACCESS.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\licenses\*"; DestDir: "{app}\licenses"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\enestv"; Filename: "{app}\enestv.exe"; WorkingDir: "{app}"; IconFilename: "{app}\enestv.exe"; AppUserModelID: "Enes.enestv"
Name: "{autodesktop}\enestv"; Filename: "{app}\enestv.exe"; WorkingDir: "{app}"; IconFilename: "{app}\enestv.exe"; Tasks: desktopicon; AppUserModelID: "Enes.enestv"

[Code]
{ The finish page has two options drawn as the same kind of check box, so they line up exactly.
  "Open enestv" is not a [Run] postinstall entry: Inno's run list draws its box flush left with no gap before the
  text, which never aligned with a regular check box. Both options are handled on Finish below. }
var
  OpenCheckBox, ThanksCheckBox: TNewCheckBox;

function CreateFinishCheckBox(const Caption: String): TNewCheckBox;
begin
  Result := TNewCheckBox.Create(WizardForm);
  Result.Parent := WizardForm.FinishedPage;
  Result.Caption := Caption;
  Result.Checked := True;
  Result.Visible := False;
end;

procedure InitializeWizard;
begin
  OpenCheckBox := CreateFinishCheckBox('enestv''yi şimdi aç');
  ThanksCheckBox := CreateFinishCheckBox('enes''e teşekkür et');
end;

{ "enes'e teşekkür et": leave a local marker; enestv shows a short thank-you note on its next start and deletes it.
  Nothing is sent anywhere. Written before enestv is started, so the note appears on this first launch.
  Not reached in silent installs (no wizard), which therefore neither open the app nor leave the marker. }
function NextButtonClick(CurPageID: Integer): Boolean;
var
  DataDir: String;
  ErrorCode: Integer;
begin
  Result := True;
  if CurPageID <> wpFinished then Exit;
  if ThanksCheckBox.Checked then
  begin
    DataDir := ExpandConstant('{localappdata}\enestv');
    if ForceDirectories(DataDir) then
      SaveStringToFile(DataDir + '\thanks.flag', 'thanks', False);
  end;
  if OpenCheckBox.Checked then
    ShellExec('', ExpandConstant('{app}\enestv.exe'), '', ExpandConstant('{app}'), SW_SHOWNORMAL, ewNoWait, ErrorCode);
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  OpenCheckBox.Visible := CurPageID = wpFinished;
  ThanksCheckBox.Visible := CurPageID = wpFinished;
  if CurPageID = wpFinished then
  begin
    OpenCheckBox.SetBounds(
      WizardForm.FinishedLabel.Left,
      WizardForm.FinishedLabel.Top + WizardForm.FinishedLabel.Height + ScaleY(20),
      WizardForm.FinishedLabel.Width,
      ScaleY(20));
    ThanksCheckBox.SetBounds(
      WizardForm.FinishedLabel.Left,
      OpenCheckBox.Top + OpenCheckBox.Height + ScaleY(8),
      WizardForm.FinishedLabel.Width,
      ScaleY(20));
  end;
end;
