# ÁudioLivre

Conversor local de vídeos e áudios para MP3, WAV, FLAC, M4A, Opus e OGG. Interface em português, conversão em lote, progresso e cancelamento. Nenhum arquivo é enviado para a internet.

## Usar

Windows 10/11 x64 com .NET Framework 4.8 (incluído nas versões atuais do Windows). Extraia **todo** o ZIP e abra `AudioLivre.exe`. Adicione ou arraste arquivos, escolha formato e pasta e clique em Converter. A primeira faixa de áudio é usada. Arquivos existentes são preservados com nomes numerados.

MP3/M4A/Opus/OGG usam compressão com perda. WAV e FLAC não recuperam qualidade perdida na origem. WAV gera arquivos maiores. Arquivos sem áudio, protegidos ou danificados podem falhar; a fila continua.

## Compilar

Execute `powershell -ExecutionPolicy Bypass -File .\build.ps1`. O compilador do .NET Framework já existente no Windows é utilizado; nenhum pacote NuGet é necessário. Coloque `ffmpeg.exe` e `ffprobe.exe` em `tools/` antes de empacotar. O build gera `dist/AudioLivre/`.

## Dependência

FFmpeg é executado como processo separado. A distribuição deve acompanhar os avisos de licença, a identificação exata do build e o acesso ao código-fonte correspondente. Consulte `THIRD-PARTY.md` do pacote.

## Desenvolvimento

Atualizações devem ser feitas em commits pequenos, com uma mudança coerente por commit. Valide compilação e os formatos afetados antes de enviar ao GitHub.