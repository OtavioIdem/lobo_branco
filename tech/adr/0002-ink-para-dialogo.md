# ADR 0002 — Usar Ink para dialogo e narrativa ramificada

Data: 2026-09-09
Status: aceita

## Contexto
O jogo precisa de dialogo ramificado com condicoes sobre estado de mundo, e o volume de
texto estimado e de 12 a 18 mil palavras no protótipo (doc 06). Construir um editor de
grafo de dialogo proprio e um subprojeto de cerca de dois meses.

## Decisao
Usar Ink (inkle) com o pacote Ink Unity Integration, ligando variaveis externas do Ink
diretamente ao `WorldState`.

## Consequencias
- Texto em arquivos versionaveis em Git, com diff legivel.
- Narrativa testavel fora da Unity, no editor Inky.
- Escritor e programador podem trabalhar nos mesmos arquivos sem conflito de cena.
- Dependencia de terceiro (MIT, risco baixo).
- Nao ha editor visual de grafo; a estrutura vive no texto.

## Alternativas consideradas
- **Editor de grafo proprio com GraphView**: descartado pelo custo.
- **Yarn Spinner**: equivalente e valido; Ink escolhido pela integracao mais direta com
  variaveis externas e pelo historico em RPGs narrativos.
- **Dialogue System for Unity (pago)**: descartado por acoplar o projeto a um framework
  grande que seria dificil de remover.
