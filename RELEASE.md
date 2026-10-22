Interface em português migrada para Avalonia, preservando o motor FFmpeg, seis formatos, qualidade, fila, progresso, cancelamento e proteção dos arquivos existentes.

- **Windows 10/11 x64:** extraia todo o ZIP e abra AudioLivre.exe. FFmpeg e runtime .NET incluídos.
- **Linux x64 (glibc, ambiente gráfico X11/XWayland):** extraia o tar.gz e execute `./AudioLivre`. Runtime .NET incluído; instale FFmpeg pelo gerenciador da distribuição (Ubuntu/Debian: `sudo apt install ffmpeg`). Não execute com sudo.
- Pacotes e SHA256 disponibilizados como artefatos no GitHub Actions somente após compilação, conversões reais, testes da fila, inicialização nativa e conversão do pacote extraído passarem nos dois runners.

Mantidos os arquivos anteriores na versão 0.1.0. macOS e Linux ARM não estão incluídos. A instalação em distribuições diferentes do Ubuntu 22.04 e a interação manual com seletores, arrastar arquivos e múltiplas escalas de tela ainda precisam de validação.
