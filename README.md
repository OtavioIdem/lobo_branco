# TW1-Remaster — Codinome "Projeto Lobo Branco"

Protótipo de RPG de ação **cooperativo** em Unity, tomando **The Witcher (2007)** como base
de design e reinterpretando seus sistemas com mecânicas próprias.

De 2 a 4 bruxos de escolas diferentes aceitam o mesmo contrato no Capítulo I e caçam juntos,
em sessão privada aberta por código de convite. O pivô para coop é de 2026-09-10 e está no
[docs/13](docs/13_COOP_E_REDE.md), que prevalece sobre os documentos anteriores.

> **Status:** M0 fechado. M1 em andamento — tarefas 1.1 a 1.9 prontas, 115 testes passando.
> O próximo passo é a camada de rede, antes da tarefa 1.10.
> **Engine:** Unity `6000.6.0f1` com URP 17.6.0
> **Projeto Unity:** `unity/LoboBranco`
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
| 14 | [docs/13_COOP_E_REDE.md](docs/13_COOP_E_REDE.md) | **Coop, escolas e autoridade. Prevalece sobre os anteriores** |

## Agentes e skills

Sete agentes especializados por setor, em `.claude/agents/`. Chame pelo nome, ou deixe
que sejam escolhidos pela descrição.

| Agente | Setor |
|---|---|
| `unity-engenheiro` | Implementa sistemas de gameplay em C# |
| `game-designer` | Projeta e balanceia mecânicas, mantém as planilhas |
| `narrativa-ink` | Diálogo, quests, contratos, sistema de Ecos |
| `arte-tecnica` | Greybox, shaders, VFX, iluminação, áudio, licenças |
| `qa-unity` | Compila, testa, builda, diagnostica |
| `revisor-arquitetura` | Confere o código contra o doc 07 e os ADRs |
| `pesquisador-tw1` | Pesquisa o jogo original e referências de design |

Seis skills para os procedimentos que se repetem, em `.claude/skills/`.

| Skill | Quando dispara |
|---|---|
| `unity-batch` | Compilar, testar, buildar, rodar método de editor |
| `novo-sistema` | Criar um módulo ou sistema de gameplay do zero |
| `novo-monstro` | Adicionar um inimigo completo, com bestiário e encontro |
| `novo-contrato` | Montar uma quest nas dez etapas, com Eco |
| `balancear-combate` | Mexer em qualquer número de combate |
| `adr` | Registrar uma decisão de arquitetura |

O `CLAUDE.md` é lido automaticamente a cada sessão e carrega o essencial do projeto.
**Abra o Claude Code com `E:\Unity_Games\TW1-Remaster` como diretório**, senão nada
disso é encontrado.

## Estrutura de pastas

```
TW1-Remaster/
├── CLAUDE.md      Contexto carregado em toda sessão
├── .claude/       Agentes e skills do projeto
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

## Abrir no Unity Hub

No Hub, em **Add**, aponte para esta pasta e não para a raiz do repositório:

```
E:\Unity_Games\TW1-Remaster\unity\LoboBranco
```

O Hub só aceita a pasta que contém diretamente `Assets` e `ProjectSettings`, ou uma cujos
filhos imediatos sejam projetos. Ele não desce dois níveis, então selecionar
`TW1-Remaster` falha com "No valid Unity projects found".

O projeto fica em uma subpasta de propósito, para que `docs/`, `design/` e `tech/` fiquem
no mesmo repositório sem virar lixo dentro de `Assets`.

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
