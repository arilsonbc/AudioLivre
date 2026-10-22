param([ValidateSet('win-x64','linux-x64')][string]$Runtime = $(if ($IsLinux) { 'linux-x64' } else { 'win-x64' }))
$ErrorActionPreference = 'Stop'
$destination = Join-Path $PSScriptRoot "dist/0.2.0/$Runtime"
& dotnet publish "$PSScriptRoot/src/AudioLivre.Desktop/AudioLivre.Desktop.csproj" -c Release -r $Runtime --self-contained true -o $destination
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Copy-Item "$PSScriptRoot/README.md","$PSScriptRoot/LICENSE","$PSScriptRoot/THIRD-PARTY.md","$PSScriptRoot/VALIDATION.md" $destination -Force
Copy-Item "$PSScriptRoot/licenses" $destination -Recurse -Force
if ($Runtime -eq 'win-x64') {
    foreach ($tool in @('ffmpeg.exe','ffprobe.exe')) {
        if (Test-Path "$PSScriptRoot/tools/$tool") { Copy-Item "$PSScriptRoot/tools/$tool" $destination -Force }
    }
} else { Copy-Item "$PSScriptRoot/scripts/start.sh" $destination -Force }
Write-Output $destination
