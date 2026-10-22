param([string]$FixtureRoot)
$ErrorActionPreference = 'Stop'
if (!$FixtureRoot) { $FixtureRoot = Join-Path $PSScriptRoot 'test-results' }
New-Item -ItemType Directory -Force $FixtureRoot | Out-Null
& "$PSScriptRoot\build.ps1"
$appRoot = Join-Path $PSScriptRoot 'dist\AudioLivre'
$video = Join-Path $FixtureRoot 'vídeo de teste.mp4'
$silent = Join-Path $FixtureRoot 'sem-audio.mp4'
& "$appRoot\ffmpeg.exe" -hide_banner -loglevel error -f lavfi -i 'color=c=blue:s=160x90:r=15:d=3' -f lavfi -i 'sine=frequency=440:duration=3' -c:v mpeg4 -c:a aac -shortest -y $video
if ($LASTEXITCODE -ne 0) { throw 'Fixture failed' }
& "$appRoot\ffmpeg.exe" -hide_banner -loglevel error -f lavfi -i 'color=c=black:s=160x90:r=15:d=2' -c:v mpeg4 -an -y $silent
if ($LASTEXITCODE -ne 0) { throw 'Fixture failed' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:"$appRoot\AudioLivre.exe" /out:"$appRoot\Integration.exe" "$PSScriptRoot\tests\Integration.cs"
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed' }
& "$appRoot\Integration.exe" $appRoot $video $silent (Join-Path $FixtureRoot ([guid]::NewGuid().ToString('N')))
if ($LASTEXITCODE -ne 0) { throw 'Integration tests failed' }
Remove-Item -LiteralPath "$appRoot\Integration.exe"
