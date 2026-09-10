# ADR 0001 — Usar URP em vez de HDRP

Data: 2026-09-09
Status: aceita

## Contexto
O projeto e um RPG de acao em terceira pessoa feito por uma pessoa que esta aprendendo
Unity. A maquina tem o template `urp-blank 17.2.1` instalado e o Editor 6000.6.0f1.
HDRP entrega qualidade visual superior mas custa muito mais em tempo de aprendizado, de
iteracao e de build.

## Decisao
Usar Universal Render Pipeline com Forward+, GPU Resident Drawer, GPU Occlusion Culling
e Adaptive Probe Volumes.

## Consequencias
- Iteracao rapida, builds curtos, roda em hardware modesto.
- Teto de qualidade visual mais baixo, mas suficiente para o alvo do protótipo.
- Migrar para HDRP depois e possivel mas doloroso: materiais, luzes e volumes teriam que
  ser refeitos. Reavaliar somente apos o vertical slice.

## Alternativas consideradas
- **HDRP**: descartada pelo custo de aprendizado e iteracao para um dev solo iniciante.
- **Built-in**: descartada por estar em fim de vida e nao receber as features do Unity 6.
