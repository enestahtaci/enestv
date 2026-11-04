; Build with Inno Setup 6.7.3 or later. All payload paths are relative to this file.
#ifndef PayloadDir
  #define PayloadDir "..\dist\enestv-win-x64"
#endif
#ifndef OutputPath
  #define OutputPath "..\dist"
#endif
#define AppVersion "1.3.0"

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
WizardImageFile=
WizardImageBackColor=#101216
WizardSmallImageFile=..\EnesTV.App\Assets\enestv-icon.png
WizardSmallImageBackColor=#101216
LicenseFile=..\LICENSE
InfoAfterFile=..\THIRD-PARTY-NOTICES.md
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

[Run]
Filename: "{app}\enestv.exe"; Description: "enestv'yi şimdi aç"; Flags: nowait postinstall skipifsilent unchecked

[Code]
var
  ThanksCheckBox: TNewCheckBox;

procedure InitializeWizard;
begin
  ThanksCheckBox := TNewCheckBox.Create(WizardForm);
  ThanksCheckBox.Parent := WizardForm.FinishedPage;
  ThanksCheckBox.Caption := 'enes''e teşekkür et';
  ThanksCheckBox.Checked := False;
  ThanksCheckBox.Visible := False;
  ThanksCheckBox.TabOrder := WizardForm.RunList.TabOrder + 1;
  { Deliberately no OnClick handler, no persistence and no action. Just a joke. }
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  ThanksCheckBox.Visible := CurPageID = wpFinished;
  if CurPageID = wpFinished then
  begin
    WizardForm.RunList.Height := ScaleY(32);
    ThanksCheckBox.SetBounds(
      WizardForm.FinishedLabel.Left,
      WizardForm.RunList.Top + WizardForm.RunList.Height + ScaleY(12),
      WizardForm.FinishedLabel.Width,
      ScaleY(25));
  end;
end;
