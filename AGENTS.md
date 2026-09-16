# AGENTS.md — Projeto Lobo Branco

Este repositório usa uma célula de design especializada do Codex. Antes de propor ou alterar qualquer coisa, leia `CLAUDE.md` e o documento canônico relacionado à tarefa.

## Fonte de verdade

- `docs/00` a `docs/13`: visão, design, arquitetura e roadmap.
- `docs/13_COOP_E_REDE.md` prevalece quando contradiz documentos anteriores.
- `design/**/*.csv`: números de balanceamento e tracking.
- `research/`: rascunho, nunca regra canônica.

P1 (investigação) e P2 (preparação) são os pilares ativos do slice. P3 e P4 estão adiados. O escopo é Capítulo I, 2–4 jogadores, escolas Lobo e Grifo.

## Restrição de produção visual

Enquanto o portão manual de rede de M1 não estiver fechado, permita apenas briefs, provas de pipeline, proxies, placeholder e greybox. Não inicie produção ampla de arte final. A ordem é greybox → placeholder → direção de arte → polimento; a fase de direção de arte definitiva só começa depois do vertical slice, conforme `docs/08`.

Nenhum modelo, textura, áudio, animação, diálogo ou outro asset extraído do jogo ou do REDkit pode entrar no repositório. Personagens fan-made podem reproduzir a identidade visual da obra desde que malha, UV, materiais, texturas e animações sejam criados do zero para este projeto não comercial. Referências oficiais permanecem externas e não são versionadas como assets. Todo asset externo incorporado precisa de licença verificável e registro em `art/CREDITS.md` no mesmo commit.

## Equipe de design Codex

- `design_lead`: coordena decisões, escopo, documentação e handoffs.
- `systems_designer`: mecânicas, balanceamento e planilhas.
- `art_director`: direção visual, art bible e aprovação de briefs.
- `level_designer`: greybox, fluxo espacial, encontros e coop.
- `blender_artist`: modelagem, UV, bake, rig, LOD, colisão e exportação.
- `unity_technical_artist`: importação, materiais URP, prefabs, iluminação, VFX e orçamento técnico.

Não delegue a dois agentes a edição do mesmo arquivo. O agente principal integra e valida o resultado.

## Skills do projeto

Use as skills em `.agents/skills/` conforme a tarefa: `avaliar-design-jogo`, `definir-direcao-visual`, `criar-brief-asset`, `produzir-asset-blender`, `integrar-asset-unity` e `greybox-zona-coop`.
