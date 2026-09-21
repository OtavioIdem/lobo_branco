---
name: criar-brief-asset
description: Converter uma necessidade de gameplay em brief visual e tecnico aprovavel para personagem, criatura, prop ou ambiente do Projeto Lobo Branco. Use antes de qualquer modelagem Blender; nao use quando ja existe brief aprovado e a tarefa e somente produzir.
---

# Criar brief de asset

Leia `AGENTS.md`, `CLAUDE.md`, `docs/08_PIPELINE_ARTE_E_AUDIO.md`, `docs/10_LEGAL_E_RISCOS.md`, `docs/13_COOP_E_REDE.md` e o documento de gameplay que exige o asset.

Não comece pela aparência. Comece pela função do asset no loop, distância de leitura, contexto de câmera e interação em coop. Confirme se é proxy, placeholder, prova de pipeline ou arte final; antes do portão M1, arte final não é a saída padrão.

## Estrutura do brief

- identificador, dono, estágio e prioridade;
- status (`rascunho`, `em revisão`, `aprovado` ou `bloqueado`), aprovador e data;
- função de gameplay e pilar servido;
- contexto espacial, câmera e distância de leitura;
- escala em metros e referências internas;
- silhueta, linguagem de formas, materiais e desgaste;
- vistas necessárias e partes móveis;
- variantes, dano/estado e modularidade;
- animação, rig, sockets e colisão quando aplicável;
- orçamento preliminar de triângulos, materiais, texturas e LODs;
- caminhos exatos para fonte `.blend`, exportações, texturas e destino Unity;
- referências permitidas com licença e anti-referências;
- critérios de aceite visual, técnico e de gameplay;
- dúvidas abertas que impedem produção.

Use nomes agnósticos de IP. Salve briefs novos em `art/briefs/<categoria>/` somente quando a tarefa autorizar escrita. Um brief só libera produção quando contém `status: aprovado`, aprovador, data, caminhos exatos e critérios mensuráveis. O `art_director` aprova o visual e o `unity_technical_artist` aprova o contrato técnico.

Use [o template de handoff](references/asset-handoff-template.md) ao criar um brief. Campos sem relevância para proxy podem ser `não aplicável`, com justificativa.

