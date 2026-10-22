param([string]$FixtureRoot = (Join-Path $PSScriptRoot 'test-results'))
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $FixtureRoot | Out-Null
$ffmpeg = if ($IsLinux) { (Get-Command ffmpeg -ErrorAction Stop).Source } else { "$PSScriptRoot/tools/ffmpeg.exe" }
$video = Join-Path $FixtureRoot 'vídeo de teste.mp4'
$silent = Join-Path $FixtureRoot 'sem-audio.mp4'
& $ffmpeg -hide_banner -loglevel error -f lavfi -i 'color=c=blue:s=160x90:r=15:d=3' -f lavfi -i 'sine=frequency=440:duration=3' -c:v mpeg4 -c:a aac -shortest -y $video
if ($LASTEXITCODE -ne 0) { throw 'Fixture failed' }
& $ffmpeg -hide_banner -loglevel error -f lavfi -i 'color=c=black:s=160x90:r=15:d=2' -c:v mpeg4 -an -y $silent
if ($LASTEXITCODE -ne 0) { throw 'Fixture failed' }
& dotnet run --project "$PSScriptRoot/tests/Integration.csproj" -c Release -- (Split-Path $ffmpeg) $video $silent (Join-Path $FixtureRoot ([guid]::NewGuid().ToString('N')))
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
& dotnet build "$PSScriptRoot/tests/DesktopTests.csproj" -c Release
if ($LASTEXITCODE -ne 0) { throw 'UI test build failed' }
if (!$IsLinux) { Copy-Item "$PSScriptRoot/tools/ffmpeg.exe","$PSScriptRoot/tools/ffprobe.exe" "$PSScriptRoot/tests/bin/DesktopTests/Release/net10.0" -Force }
& dotnet run --no-build --project "$PSScriptRoot/tests/DesktopTests.csproj" -c Release -- $silent $video (Join-Path $FixtureRoot ([guid]::NewGuid().ToString('N')))
if ($LASTEXITCODE -ne 0) { throw 'UI tests failed' }
