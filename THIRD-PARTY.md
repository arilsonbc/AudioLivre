# FFmpeg incluído

FFmpeg 9.0.2, essentials build x64 de Gyan Doshi, distribuído sob GPL versão 3. O ÁudioLivre usa os executáveis como processos separados.

- Build: https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.2-essentials_build.zip
- SHA256 do ZIP original: `60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba`
- Código-fonte FFmpeg correspondente: https://github.com/FFmpeg/FFmpeg/tree/946fcce07b
- Página do fornecedor, bibliotecas, configuração e fontes: https://www.gyan.dev/ffmpeg/builds/
- A licença integral está em `licenses/FFmpeg-GPLv3.txt`.
- A documentação original, versões de bibliotecas e opções do build estão em `licenses/FFmpeg-build.txt`.

O código do ÁudioLivre está disponível no repositório https://github.com/arilsonbc/AudioLivre . Os direitos de marcas e licenças das dependências permanecem com seus titulares.

## Interface multiplataforma 0.2.0

Avalonia 11.3.21 e Inter são obtidos via NuGet. Avalonia usa licença MIT: https://github.com/AvaloniaUI/Avalonia/blob/11.3.21/licence.md . Inter usa SIL Open Font License: https://github.com/rsms/inter/blob/master/LICENSE.txt . .NET: https://github.com/dotnet/runtime/blob/main/LICENSE.TXT . As dependências nativas de renderização têm seus próprios avisos nos pacotes NuGet.

O build FFmpeg Windows descrito acima acompanha apenas o ZIP Windows. No Linux, o aplicativo usa FFmpeg instalado pela distribuição ou fornecido pelo usuário, cuja licença e fontes dependem desse pacote.
