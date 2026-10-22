param([string]$OutputDirectory = (Join-Path $PSScriptRoot 'dist/packages'), [ValidateSet('win-x64','linux-x64')][string]$Runtime = $(if ($IsLinux) { 'linux-x64' } else { 'win-x64' }))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
if ($Runtime -eq 'win-x64') {
    $toolsRoot = Join-Path $PSScriptRoot 'tools'
    New-Item -ItemType Directory -Force $toolsRoot | Out-Null
    if (!(Test-Path "$toolsRoot/ffmpeg.exe") -or !(Test-Path "$toolsRoot/ffprobe.exe")) {
        $archive = Join-Path $toolsRoot 'ffmpeg-9.0.2.zip'
        Invoke-WebRequest -Uri 'https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.2-essentials_build.zip' -OutFile $archive
        if ((Get-FileHash $archive -Algorithm SHA256).Hash -ne '60F467265B1E312373DBCD92200C2618A74850F98D3D078E94296BB3FA2047BA') { throw 'FFmpeg checksum mismatch' }
        Expand-Archive -LiteralPath $archive -DestinationPath "$toolsRoot/unpacked" -Force
        Copy-Item "$toolsRoot/unpacked/ffmpeg-9.0.2-essentials_build/bin/ffmpeg.exe","$toolsRoot/unpacked/ffmpeg-9.0.2-essentials_build/bin/ffprobe.exe" $toolsRoot
    }
} elseif (!$IsLinux) { throw 'Package Linux archives on Linux to preserve executable permissions.' }
& "$PSScriptRoot/build.ps1" -Runtime $Runtime
$appRoot = Join-Path $PSScriptRoot "dist/0.2.0/$Runtime"
$name = "AudioLivre-0.2.0-$Runtime-portable"
if ($Runtime -eq 'win-x64') {
    $destination = Join-Path $OutputDirectory "$name.zip"
    Compress-Archive -Path "$appRoot/*" -DestinationPath $destination -Force
} else {
    & chmod +x "$appRoot/AudioLivre" "$appRoot/start.sh"
    $destination = Join-Path $OutputDirectory "$name.tar.gz"
    & tar -czf $destination -C $appRoot .
    if ($LASTEXITCODE -ne 0) { throw 'tar failed' }
}
$hash = (Get-FileHash $destination -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([IO.Path]::GetFileName($destination))" | Set-Content -Encoding ASCII "$destination.sha256"
Write-Output $destination
