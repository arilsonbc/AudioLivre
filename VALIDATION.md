# Validação da versão 0.1.0

Executada em 22/10/2026 no Windows x64, com o executável C# compilado pelo .NET Framework e FFmpeg 9.0.2.

37 verificações de integração aprovadas:

- Vídeo de teste gerado localmente, com imagem e tom de 440 Hz; caminho com espaços e acentos.
- Conversões reais para MP3, WAV, FLAC, M4A, Opus e OGG.
- Três perfis de qualidade dos formatos com perda; duração de saída e codec verificados pelo ffprobe.
- Progresso final, preservação de arquivo existente e numeração de nomes repetidos.
- Rejeição de mídia corrompida e vídeo sem áudio.
- Cancelamento durante processamento e limpeza dos arquivos temporários.
- Fila da interface: evita duplicatas, continua após duas falhas e conclui o arquivo válido seguinte.

A janela também foi renderizada para inspeção visual. Arrastar arquivos, o seletor de pasta e diferentes monitores/escalas de DPI ainda precisam de uma rodada de uso manual. As amostras sintéticas não abrangem todos os codecs, durações ou arquivos existentes.

O ZIP do fornecedor FFmpeg teve o SHA256 conferido contra o publicado. O pacote final acompanha SHA256SUMS.txt.
