param([string]$ReferenceDirectory = "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2")
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) { $csc = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe" }
if (!(Test-Path $csc)) { throw '.NET Framework 4.7.2 oder neuer wird benötigt.' }
New-Item dist -ItemType Directory -Force | Out-Null
$version = [regex]::Match((Get-Content src\Version.cs -Raw),'Version\s*=\s*"(\d+\.\d+\.\d+)"').Groups[1].Value
if (!$version) { throw 'Versionsnummer in src/Version.cs fehlt.' }
$installer = "MAC-Setup-$version.exe"
if (!(Test-Path (Join-Path $ReferenceDirectory 'mscorlib.dll'))) { throw '.NET Framework 4.7.2 Developer Pack fehlt. Alternativ -ReferenceDirectory mit dem Ordner der Microsoft net472-Referenzassemblies angeben. Berater brauchen nur die Laufzeit, kein Developer Pack.' }
$refs = @('/noconfig','/nostdlib+')
foreach ($assembly in @('mscorlib','System','System.Core','System.Xml','System.Windows.Forms','System.Drawing','System.Xml.Linq','System.IO.Compression','System.IO.Compression.FileSystem','System.Web.Extensions','System.Security')) {
 $refs += '/r:' + (Join-Path $ReferenceDirectory ($assembly + '.dll'))
}
& $csc /nologo /codepage:65001 /target:winexe /out:dist\MAC-Kundenklaerung.exe /win32icon:assets\mac.ico @refs src\App.cs src\Auth.cs src\AuthUi.cs src\LocalState.cs src\ModernControls.cs src\Theme.cs src\MultiSelect.cs src\Workbook.cs src\Updater.cs src\SelfTest.cs src\Version.cs
if ($LASTEXITCODE) { throw 'App-Build fehlgeschlagen.' }
New-Item build -ItemType Directory -Force | Out-Null
& $csc /nologo /codepage:65001 /target:exe /out:build\ApiCompatibility.exe @refs tests\ApiCompatibility.cs
if ($LASTEXITCODE) { throw 'API-Prueftool konnte nicht gebaut werden.' }
& .\build\ApiCompatibility.exe dist\MAC-Kundenklaerung.exe
if ($LASTEXITCODE) { throw 'Nicht unterstuetzter String-Aufruf in der App.' }
New-Item dist\assets -ItemType Directory -Force | Out-Null
Copy-Item assets\* dist\assets -Force
$test = Start-Process dist\MAC-Kundenklaerung.exe -ArgumentList '--self-test' -Wait -PassThru
if ($test.ExitCode) { throw 'Selbsttests fehlgeschlagen.' }
Copy-Item ANLEITUNG.md dist\ANLEITUNG.md -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$stage = Join-Path $env:TEMP ('MAC-Build-' + [guid]::NewGuid())
New-Item $stage -ItemType Directory | Out-Null
try {
 Copy-Item dist\MAC-Kundenklaerung.exe,dist\ANLEITUNG.md $stage
 Copy-Item assets (Join-Path $stage assets) -Recurse
 $payload = Join-Path $PSScriptRoot ('dist\MAC-Programm-' + $version + '.zip')
 if (Test-Path $payload) {Remove-Item $payload -Force}
 [IO.Compression.ZipFile]::CreateFromDirectory($stage,$payload)
 $hash = (Get-FileHash $payload -Algorithm SHA256).Hash.ToLower()
 New-Item build -ItemType Directory -Force | Out-Null
 $config = 'public static class PayloadInfo { public const string Url = "https://github.com/Tunfisch77/MAC/releases/download/v' + $version + '/MAC-Programm-' + $version + '.zip"; public const string Hash = "' + $hash + '"; }'
 $config | Set-Content build\PayloadInfo.cs -Encoding UTF8
 & $csc /nologo /codepage:65001 /target:winexe "/out:dist\$installer" /win32icon:assets\mac.ico @refs src\Installer.cs src\ModernControls.cs src\Theme.cs src\Updater.cs src\Version.cs build\PayloadInfo.cs
 if ($LASTEXITCODE) { throw 'Installer-Build fehlgeschlagen.' }
 & .\build\ApiCompatibility.exe "dist\$installer"
 if ($LASTEXITCODE) {throw 'Nicht unterstuetzter String-Aufruf im Setup.'}
} finally { Remove-Item $stage -Recurse -Force }
(Get-FileHash "dist\$installer" -Algorithm SHA256).Hash.ToLower() + '  ' + $installer | Set-Content dist\SHA256SUMS.txt -Encoding ASCII
$check = Start-Process "dist\$installer" -ArgumentList '--self-test' -Wait -PassThru
if ($check.ExitCode) {throw 'Setup-Selbsttest fehlgeschlagen.'}
(Get-FileHash $payload -Algorithm SHA256).Hash.ToLower() + '  ' + [IO.Path]::GetFileName($payload) | Add-Content dist\SHA256SUMS.txt -Encoding ASCII
& $csc /nologo /codepage:65001 /target:exe /out:dist\AuthIntegration.exe @refs /r:dist\MAC-Kundenklaerung.exe tests\AuthIntegration.cs
if ($LASTEXITCODE) {throw 'Client-Integrationstest konnte nicht gebaut werden.'}
$appSources = @('src\App.cs','src\Auth.cs','src\AuthUi.cs','src\LocalState.cs','src\ModernControls.cs','src\Theme.cs','src\MultiSelect.cs','src\Workbook.cs','src\Updater.cs','src\SelfTest.cs','src\Version.cs')
& $csc /nologo /codepage:65001 /target:exe /main:UpdateUiSmoke /out:dist\UpdateUiSmoke.exe @refs @appSources tests\UpdateUiSmoke.cs
if ($LASTEXITCODE) {throw 'Update-Oberflaechentest konnte nicht gebaut werden.'}
& $csc /nologo /codepage:65001 /target:exe /main:InstallerUiSmoke /out:dist\InstallerUiSmoke.exe @refs src\Installer.cs src\ModernControls.cs src\Theme.cs src\Updater.cs src\Version.cs build\PayloadInfo.cs tests\InstallerUiSmoke.cs
if ($LASTEXITCODE) {throw 'Installer-Oberflaechentest konnte nicht gebaut werden.'}
Write-Host 'Fertig: Setup und Programmpaket gemeinsam als Release hochladen.' 
