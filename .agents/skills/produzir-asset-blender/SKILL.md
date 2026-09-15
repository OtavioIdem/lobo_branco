---
name: produzir-asset-blender
description: Produzir ou revisar um asset 3D game-ready no Blender para o Projeto Lobo Branco, incluindo malha, UV, bake, rig quando necessario, LOD, colisao e exportacao FBX. Exige brief aprovado; nao configura o prefab final na Unity.
---

# Produzir asset no Blender

Leia `AGENTS.md`, `CLAUDE.md`, `docs/08_PIPELINE_ARTE_E_AUDIO.md`, `docs/10_LEGAL_E_RISCOS.md` e o brief aprovado. O executável validado nesta máquina é `E:\Blender\blender.exe` (Blender 5.2.1 LTS); registre qualquer versão diferente usada na entrega.

Se não houver brief com status aprovado, aprovadores, data, caminhos exatos e critérios mensuráveis, pare a produção e use `criar-brief-asset`. Se o portão M1 ainda não estiver fechado, produza proxy ou prova de pipeline, não arte final. Mesmo depois de M1, respeite as fases de arte do `docs/08`.

## Contrato geométrico

- 1 unidade Blender = 1 metro = 1 unidade Unity;
- origem e pivô conforme a função, com assets de personagem nos pés, chão e centro;
- escala e rotação aplicadas antes da exportação;
- normais, hard edges, smoothing e tangentes revisados;
- UV0 sem sobreposição indevida e UV de lightmap quando exigida;
- materiais e slots dentro do orçamento do brief;
- LODs e colisão separados e nomeados quando aplicáveis;
- armature, weights, sockets e clips validados para assets skinned quando aplicáveis.

Use `SM_`, `SK_`, `T_` e `M_` conforme o tipo. Não invente prefixos novos silenciosamente: se o brief precisar de convenção ainda ausente, registre a decisão antes de expandir o padrão.

Exporte FBX com `Y up` e `-Z forward`, somente collections autorizadas. Não inclua controles de rig, high-poly, cages ou geometria auxiliar no FBX final.

## Entrega

Entregue `.blend`, FBX e os demais itens exigidos pelo brief, além da ficha técnica e relatório de validação. Mapas, LODs, colisão, rig ou animação podem ser `não aplicável` para proxies, com justificativa. Informe versão do Blender, escala, contagem de triângulos, materiais, resolução de texturas e pendências. Referências visuais oficiais permanecem externas; nenhuma geometria, textura, rig, animação ou material do jogo/REDkit pode ser reutilizado. Qualquer recurso externo incorporado exige procedência e licença verificáveis.
