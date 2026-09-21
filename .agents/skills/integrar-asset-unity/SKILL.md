---
name: integrar-asset-unity
description: Importar e validar um asset Blender no Unity do Projeto Lobo Branco, configurando modelo, texturas, URP, rig, LOD, colisao e prefab com verificacao de build e desempenho. Nao use para modelar ou redefinir o brief.
---

# Integrar asset na Unity

Leia `AGENTS.md`, `CLAUDE.md`, `docs/07_ARQUITETURA_TECNICA.md`, `docs/08_PIPELINE_ARTE_E_AUDIO.md`, `docs/10_LEGAL_E_RISCOS.md`, o brief aprovado e a ficha técnica do Blender.

O projeto usa Unity 6000.6.0f1 e URP 17.6.0. Antes de importar, confirme que o brief aprovado contém os caminhos exatos de fonte, exportações, texturas e destino Unity. A documentação ainda não fixa um contrato global; se os caminhos estiverem ausentes, devolva o brief em vez de criar uma estrutura permanente por inferência.

## Validação

1. Verifique escala, orientação, pivô, transforms, normais e tangentes.
2. Configure `ModelImporter` de forma reproduzível; registre qualquer ajuste manual.
3. Confirme materiais e texturas no shader realmente usado. Não presuma `_MaskMap`: o URP Lit atual expõe `_MetallicGlossMap` e `_OcclusionMap`; use máscara empacotada somente com shader que a consuma.
4. Valide rig Humanoid, Avatar, clips e root motion. Jogador sem root motion; inimigos grandes e investidas apenas quando especificado.
5. Configure LODGroup, colliders, lightmap UVs e prefab quando aplicáveis. Aceite `não aplicável` apenas quando justificado no brief; não use geometria visual pesada como física por conveniência.
6. Revise licença e atualize `art/CREDITS.md` no mesmo commit de asset externo.
7. Meça custo contra 16,6 ms, menos de 1.200 draw calls e menos de 3 milhões de triângulos por frame.
8. Verifique as dimensões e tolerâncias definidas no brief, o `Transform` esperado, o pivô no chão, a comparação com a referência de jogador de 1,85 m e a estabilidade após reimportação.
9. Verifique Console e, quando autorizado, build Windows. Diferencie validação no editor de validação em build.

Se o asset violar o brief, devolva critérios objetivos ao `blender_artist`; não esconda erro de origem com correções frágeis de importação.
