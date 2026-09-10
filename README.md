# TW1-Remaster — Codinome "Projeto Lobo Branco"

Protótipo de RPG de ação em Unity, tomando **The Witcher (2007)** como base de design
e reinterpretando seus sistemas com mecânicas próprias.

> **Status:** M0 em andamento — projeto Unity criado e configurado. Sem código de jogo ainda.
> **Engine:** Unity `6000.6.0f1` com URP 17.6.0
> **Projeto Unity:** `unity/LoboBranco` — abra pelo Unity Hub por esse caminho
> **Plataforma alvo do protótipo:** Windows x64
> **Natureza:** projeto pessoal, não-comercial, de aprendizado.

---

## Por onde começar a ler

| Ordem | Documento | O que responde |
|---|---|---|
| 1 | [docs/00_VISAO_E_ESCOPO.md](docs/00_VISAO_E_ESCOPO.md) | O que é o jogo, pilares, o que fica fora |
| 2 | [docs/01_ANALISE_TW1_ORIGINAL.md](docs/01_ANALISE_TW1_ORIGINAL.md) | Desmontagem do original: sistemas, estrutura, defeitos |
| 3 | [docs/02_GDD.md](docs/02_GDD.md) | Documento de design consolidado |
| 4 | [docs/03_COMBATE.md](docs/03_COMBATE.md) | Combate novo, com números |
| 5 | [docs/04_PROGRESSAO_SKILLTREES.md](docs/04_PROGRESSAO_SKILLTREES.md) | Árvores de habilidade novas |
| 6 | [docs/05_ALQUIMIA_SINAIS_ITENS.md](docs/05_ALQUIMIA_SINAIS_ITENS.md) | Alquimia, toxicidade, sinais, economia |
| 7 | [docs/06_NARRATIVA_E_QUESTS.md](docs/06_NARRATIVA_E_QUESTS.md) | Adaptação da história e estrutura de quests |
| 8 | [docs/07_ARQUITETURA_TECNICA.md](docs/07_ARQUITETURA_TECNICA.md) | Como o código se organiza na Unity |
| 9 | [docs/08_PIPELINE_ARTE_E_AUDIO.md](docs/08_PIPELINE_ARTE_E_AUDIO.md) | De onde vêm os assets |
| 10 | [docs/09_ROADMAP_E_MILESTONES.md](docs/09_ROADMAP_E_MILESTONES.md) | Cronograma realista para solo dev |
| 11 | [docs/10_LEGAL_E_RISCOS.md](docs/10_LEGAL_E_RISCOS.md) | Propriedade intelectual e riscos do projeto |
| 12 | [docs/11_SETUP_AMBIENTE.md](docs/11_SETUP_AMBIENTE.md) | Passo a passo de instalação e configuração |
| 13 | [docs/12_BACKLOG_VERTICAL_SLICE.md](docs/12_BACKLOG_VERTICAL_SLICE.md) | Tarefas concretas, ordenadas, do primeiro dia |

## Estrutura de pastas

```
TW1-Remaster/
├── docs/          Levantamentos e design docs (a fonte da verdade)
├── design/        Planilhas de balanceamento, tabelas, grafos de quest
├── research/      Anotações do original, bestiário, mapas de referência
├── tech/          ADRs (decisões de arquitetura) e protótipos descartáveis
├── art/           Concepts, greybox, placeholders (nada extraído do jogo original)
├── audio/         Referências e assets licenciados
├── tools/         Scripts de build, importadores, geradores
└── unity/
    ├── LoboBranco/    O projeto Unity
    └── Builds/        Saída de build (fora do controle de versão)
```

## Estado do projeto Unity

Configurado em 09/09/2026. O que já está feito:

- Projeto URP criado a partir do template `urp-blank 17.2.1`, resolvido para URP 17.6.0
- Forward+, GPU Resident Drawer, GPU Occlusion Culling, Adaptive Probe Volumes
- Sete pacotes adicionados, dois removidos, mais Ink por Git URL
- 15 assembly definitions com o grafo de dependências do doc 07
- 8 tags e 10 layers de usuário, com 36 pares de colisão desativados
- Física a 60 Hz, 8 iterações de solver
- 6 cenas criadas e registradas no Build Settings
- Git com LFS e o merge driver da Unity para cenas e prefabs

Reproduzir em outra máquina: abra o projeto e use o menu **Lobo Branco → Setup**.

## Regra de ouro deste repositório

Nenhum asset extraído de `The Witcher (2007)` entra aqui — nem malha, nem textura,
nem áudio, nem texto de diálogo copiado. Ver [docs/10_LEGAL_E_RISCOS.md](docs/10_LEGAL_E_RISCOS.md).
