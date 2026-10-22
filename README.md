# ÁudioLivre

Conversor local de vídeos e áudios para MP3, WAV, FLAC, M4A, Opus e OGG. C#/.NET 10, Avalonia e FFmpeg. Interface em português, conversão em lote, progresso e cancelamento. Nenhum arquivo é enviado para a internet.

## Usar

Baixe os pacotes atuais em https://github.com/arilsonbc/AudioLivre/actions (abra a execução aprovada mais recente e a seção Artifacts). As releases anteriores continuam disponíveis.

No artefato Windows há duas opções: `AudioLivre-0.2.0-win-x64-setup.exe` instala para o usuário atual, cria atalho no menu Iniciar e oferece desinstalação; `AudioLivre-0.2.0-win-x64-portable.zip` funciona após extrair. O instalador também oferece atalho opcional na área de trabalho e não requer administrador. Ambos incluem o mesmo aplicativo e FFmpeg.

- Windows 10/11 x64: extraia **todo** o ZIP e abra `AudioLivre.exe`. FFmpeg e runtime .NET incluídos; sem instalador.
- Linux x64 com glibc e ambiente gráfico X11/XWayland: extraia o `.tar.gz`, instale FFmpeg pelo gerenciador da distribuição (`sudo apt install ffmpeg` no Ubuntu/Debian) e execute `./AudioLivre`. O runtime .NET está incluído. Não execute com sudo. Use `chmod +x AudioLivre start.sh` se seu gerenciador de arquivos remover as permissões.

Adicione ou arraste arquivos, escolha formato e pasta e clique em Converter. A primeira faixa de áudio é usada. Arquivos existentes são preservados com nomes numerados. Clique duas vezes numa falha para ver detalhes. Para converter novamente um arquivo concluído, limpe a fila e adicione-o novamente.

MP3/M4A/Opus/OGG usam compressão com perda. WAV e FLAC não recuperam qualidade perdida na origem. WAV gera arquivos maiores. Arquivos sem áudio, protegidos ou danificados podem falhar; a fila continua.

## Desenvolvimento

Requer SDK .NET 10 e PowerShell 7 (`pwsh`) para os scripts. O SDK não é necessário para usar os pacotes.

- `dotnet build src/AudioLivre.Desktop`
- `pwsh -File package.ps1 -Runtime win-x64` baixa FFmpeg com SHA256 verificado e gera ZIP.
- No Linux, `pwsh -File package.ps1 -Runtime linux-x64` gera tar.gz com permissões de execução.
- `pwsh -File test.ps1` testa todos os formatos, perfis, codecs, duração, arquivos existentes, erros, cancelamento e a fila Avalonia.
- `dotnet run --project src/AudioLivre.Desktop` inicia a interface (FFmpeg precisa estar no PATH ou junto ao executável).

O motor está em `src/AudioLivre.Core`; a interface, em `src/AudioLivre.Desktop`. A implementação WinForms original permanece na tag `v0.1.0`. Os workflows testam e empacotam Windows e Linux; macOS não faz parte desta versão.

No Linux, FFmpeg é procurado primeiro junto ao aplicativo e depois no PATH. Dependências gráficas usuais incluem libX11, libICE, libSM, libfontconfig e libfreetype; consulte https://docs.avaloniaui.net/docs/supported-platforms . Distribuições musl/Alpine, ARM e sessões Wayland sem XWayland não foram incluídas nos pacotes.

Atualizações devem usar commits pequenos, com uma mudança coerente por commit, enviados ao GitHub após validação. Consulte VALIDATION.md e THIRD-PARTY.md.
