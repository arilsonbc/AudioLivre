# Validação 0.2.0

Motor reaproveitado da versão 0.1.0. As 34 verificações de integração de conversão foram preservadas: seis formatos, perfis, codec real, duração, nomes com espaço e acento, proteção de arquivos, erros, cancelamento e limpeza.

A interface Avalonia tem teste próprio de duplicatas, continuidade após falha, progresso e conclusão. O workflow Windows/Linux também abre o aplicativo nativo e converte usando o pacote extraído, antes de disponibilizar os artefatos.

Os testes locais passaram no Windows x64. O resultado Linux e a execução nativa dos dois sistemas ficam registrados em https://github.com/arilsonbc/AudioLivre/actions . Os pacotes 0.2.0 ficam nos artefatos de cada job aprovado; o workflow não publica releases automaticamente.

Amostras sintéticas não abrangem todos os codecs ou durações. Seletores de arquivo/pasta, arrastar arquivos, acessibilidade, múltiplos monitores e diferentes escalas de DPI ainda precisam de uma rodada manual. Linux validado pelo workflow no Ubuntu 22.04 com Xvfb; isso não garante todas as distribuições e ambientes gráficos.
