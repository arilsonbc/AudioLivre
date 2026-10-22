param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist'))
$ErrorActionPreference = 'Stop'
$version = '0.1.0'
$toolsRoot = Join-Path $PSScriptRoot 'tools'
New-Item -ItemType Directory -Force $toolsRoot,$OutputDirectory | Out-Null
if (!(Test-Path "$toolsRoot\ffmpeg.exe") -or !(Test-Path "$toolsRoot\ffprobe.exe")) {
    $archive = Join-Path $toolsRoot 'ffmpeg-9.0.2.zip'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.2-essentials_build.zip' -OutFile $archive
    $expected = '60F467265B1E312373DBCD92200C2618A74850F98D3D078E94296BB3FA2047BA'
    if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne $expected) { throw 'FFmpeg checksum mismatch' }
    $unpacked = Join-Path $toolsRoot 'unpacked'
    Expand-Archive -LiteralPath $archive -DestinationPath $unpacked -Force
    $build = Join-Path $unpacked 'ffmpeg-9.0.2-essentials_build'
    Copy-Item "$build\bin\ffmpeg.exe","$build\bin\ffprobe.exe" $toolsRoot
}
& "$PSScriptRoot\build.ps1"
$appRoot = Join-Path $PSScriptRoot 'dist\AudioLivre'
$destination = Join-Path $OutputDirectory "AudioLivre-$version-win-x64-portable.zip"
$files = Get-ChildItem -LiteralPath $appRoot | Where-Object { $_.Name -ne 'Render.exe' -and $_.Name -ne 'Integration.exe' }
Compress-Archive -LiteralPath $files.FullName -DestinationPath $destination -Force
$hash = (Get-FileHash $destination -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($destination))" | Set-Content -Encoding ASCII (Join-Path $OutputDirectory 'SHA256SUMS.txt')
Write-Output $destination
