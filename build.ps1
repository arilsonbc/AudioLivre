$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$destination = Join-Path $projectRoot 'dist\AudioLivre'
New-Item -ItemType Directory -Force $destination | Out-Null
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /utf8output /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll /out:"$destination\AudioLivre.exe" "$projectRoot\src\AudioLivre.cs"
if ($LASTEXITCODE -ne 0) { throw 'Falha na compilação.' }
@'
<?xml version="1.0" encoding="utf-8" ?>
<configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" /></startup></configuration>
'@ | Set-Content -Encoding UTF8 "$destination\AudioLivre.exe.config"
Copy-Item "$projectRoot\README.md" $destination
Copy-Item "$projectRoot\LICENSE" $destination
foreach ($tool in @('ffmpeg.exe','ffprobe.exe')) {
    if (Test-Path "$projectRoot\tools\$tool") { Copy-Item "$projectRoot\tools\$tool" $destination }
}
if (Test-Path "$projectRoot\THIRD-PARTY.md") { Copy-Item "$projectRoot\THIRD-PARTY.md" $destination }
if (Test-Path "$projectRoot\licenses") { Copy-Item "$projectRoot\licenses" $destination -Recurse -Force }
Write-Output $destination
