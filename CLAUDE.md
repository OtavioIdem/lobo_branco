# CLAUDE.md — Projeto Lobo Branco

Protótipo de RPG de ação **cooperativo** em Unity, remaster autoral de The Witcher (2007).
De 2 a 4 jogadores caçam juntos no Capítulo I, em sessão privada por código de convite.
Projeto solo, não-comercial, feito para aprender desenvolvimento de jogos.

O pivô para coop é de 2026-09-10 e está no `docs/13`. Onde o doc 13 contradiz um doc
anterior, **o doc 13 vence** até que o doc antigo seja reescrito.

## Onde as coisas estão

| Caminho | O que é |
|---|---|
| `docs/00` a `docs/13` | **A fonte da verdade.** Design, arquitetura, roadmap |
| `docs/13` | Coop, escolas, modelo de autoridade. **Prevalece sobre os anteriores** |
| `tech/adr/` | Decisões de arquitetura com justificativa |
| `design/*.csv` | Planilhas de balanceamento, trilhas, ecos, tracking semanal |
| `unity/LoboBranco/` | O projeto Unity. **Aponte o Unity Hub para esta pasta**, não para a raiz |
| `unity/LoboBranco/Assets/_Project/` | Tudo que é nosso. Nada nosso fica fora daqui |
| `unity/Builds/` | Saída de build, fora do controle de versão |
| `research/` | Rascunhos e anotações. Não é canônico |

Antes de propor qualquer coisa, leia o documento relevante. Os docs se referenciam
entre si por número de seção, e o `docs/01` tem o inventário de defeitos numerado (D1 a D13)
que cada sistema novo precisa resolver.

## Ambiente

| Item | Valor |
|---|---|
| Unity | `6000.6.0f1`, URP 17.6.0, Forward+ |
| Editor externo | VS Code, extensão `visualstudiotoolsforunity.vstuc` |
| Plataforma | Windows x64, Mono no desenvolvimento, IL2CPP nos builds de playtest |
| Shell | Git Bash e PowerShell, Windows 11 |
| Versionamento | Git com LFS, merge driver UnityYAMLMerge |

Rodar o editor sem interface, compilar, testar e buildar: use a skill `unity-batch`.
Nunca chame `Unity.exe` na mão sem ler ela antes, porque há armadilhas (o projeto trava
enquanto outra instância está aberta, e `-quit` não pode ser combinado com `-runTests`).

## Pilares de design (docs/00 seção 3)

Toda feature precisa servir a pelo menos um destes. Se não serve, ela não entra.
Este é o único freio contra escopo infinito num projeto solo de 12 horas por semana.

1. **O bruxo é investigador, não tanque.** Matar é o clímax, não a atividade.
   Em coop, cada escola lê um tipo de vestígio e nenhuma lê todos (docs/13 seção 4.1).
2. **Preparação vale mais que reflexo.** Óleo e poção certos valem mais que esquiva perfeita.
3. ~~Escolha sem moral limpa~~ — **adiado** para depois do portão M1 (docs/13 seção 4.2).
4. ~~Vizima é um personagem~~ — **adiado**, depende de save divergente por jogador.

## Escopo travado

O protótipo é **só o Capítulo I**, os Arredores de Vizima, jogado por **2 a 4 pessoas**.
Capítulos II a V, romance, minijogos, montaria e dublagem seguem fora (docs/00 seção 5).
Também fora, agora com mais motivo: PvP, servidor dedicado, matchmaking público, crossplay
e progressão persistente entre sessões (docs/13 seção 8).

**Duas escolas jogáveis no slice: Lobo e Grifo.** A terceira só depois do portão M1 passar.
Essa é a porta pela qual o escopo vai tentar crescer, e ela fica fechada.

Quando alguém pedir algo fora do escopo, diga que está fora e onde isso está registrado,
antes de implementar. Escopo crescendo é o risco X1 do docs/10, o que mais mata projeto solo.

## Regras de arquitetura (docs/07)

Estas não são preferências de estilo, são o que mantém o projeto navegável em seis meses.

1. **Dados em ScriptableObject, comportamento em MonoBehaviour.** Nenhum número de
   balanceamento vive em código. Balancear é editar asset, não recompilar.
2. **Grafo de asmdef só aponta para baixo.** A tabela de níveis está em `docs/07` seção 3.
   `Core` não depende de nada. Ciclo entre asmdefs é erro de compilação, então a engine cobra.
3. **Nomes da IP nunca aparecem em código.** `PlayerCharacter`, não `GeraltController`.
   `KnockbackSign`, com o nome "Aard" vindo de um asset. Motivo em `tech/adr/0005`.
4. **Sistemas testáveis não conhecem input nem câmera.** `PlayerLocomotion` recebe direção
   por propriedade. É isso que permite testar sem simular teclado.
5. **Zero alocação por frame em combate.** Sem LINQ em `Update`, sem concatenação de string,
   `NonAlloc` em toda query de física. Um pico de GC no meio de um riposte é bug de jogabilidade.
6. **O cliente pede, o host decide, todo mundo assiste** (ADR 0008). O dono simula o próprio
   movimento e a própria FSM. Dano, vida, vigor e IA são do host, sempre. Nenhum cliente
   declara dano. Sistema novo começa respondendo "quem tem autoridade aqui", e a resposta
   vai no cabeçalho do arquivo.
7. **Escola é dado, não código.** Um `if` por escola em código de combate é erro de revisão.
   Escola é `StatBlockDef` mais afinidade de postura mais intensidade de sinal (docs/13 seção 5.1).

## Definição de pronto (docs/00 seção 7)

1. Funciona no build de Windows, não só no editor.
2. Dados em ScriptableObject, não hardcoded.
3. Sobrevive a save, sair e carregar.
4. Sem exceção no Console em 5 minutos de jogo.
5. Uma linha no `CHANGELOG.md`.

## Convenções de código

- `.editorconfig` em `unity/LoboBranco/` manda. Campos privados com `_camelCase`,
  constantes em `PascalCase`, `using` fora do namespace, linha de 120 colunas.
- Comentário explica **por que**, não o que. Um comentário que descreve o código é ruído.
- Comentários e nomes de teste em português. Nomes de tipo e membro em inglês.
- Todo arquivo `.cs` tem um `.meta` irmão. Nunca ignore `.meta` no Git, e nunca renomeie
  um script fora do editor sem levar o `.meta` junto: isso desconecta referências de cena.

## Estado atual

M0 fechado. Do M1 estão escritas as tarefas 1.1 a 1.9, a camada de rede inteira (1.9a a
1.9i), o Fluxo (1.12), as três posturas (1.14), aço e prata (1.15), o Vigor (1.16), a
Adrenalina parcial (1.17), o `MonsterDef` (1.20), a IA de inimigo parcial (1.21), o
coordenador de encontro (1.22), o telegrafo de ataque replicado (1.23), hitstop, tremor e
soco de câmera (1.25), a escola como dado (1.31), a habilidade com custo e recarga (1.32), os
estados de controle do inimigo (1.18a, primeira das oito partes da 1.18, que vem antes da 1.33) e
o efeito de sinal como dado, com área, intensidade e variante por escola (1.18b).
Existe jogador com câmera de terceira pessoa, folha de atributos com modificadores, pipeline
de dano de 11 estágios, máquina de estados com buffer de input de 0,2 s, golpes por postura
que vêm da escola do bruxo (só o Lobo, por enquanto) com hitbox sem alocação, um sinal que
cobra vigor, recarrega e acha quem está no cone, mas ainda não empurra (tarefa 1.18c), sessão em que o dono simula o próprio bruxo enquanto o host resolve
vida e dano (por IP direto ou por código de convite), e uma criatura que percebe por cone e
por som, persegue por NavMesh, golpeia com telegrafo de 0,65 s visível em todas as máquinas e
espera a vez rondando quando o alvo já tem dois atacantes, e que pode ser atordoada, derrubada
e lentificada pelo host sem depender do grafo. 346 testes passando, build gerando.

**Nada de sensação mexe em `Time.timeScale`** (`tech/adr/0010`). A escala é global na máquina,
e no host ela congelaria a sessão de todo mundo. Hitstop estende o golpe de quem bateu pelo
mesmo tempo no host e no dono; câmera lenta, quando vier, é só apresentação.

Duas coisas estão pendentes e **nenhuma delas é código**.

A primeira é o **grafo de behavior tree** da tarefa 1.21. O asset de árvore do
`com.unity.behavior` é authoring do editor gráfico e o tipo dele é interno ao pacote, então
ele é a única coisa do projeto que não pode ser montada por script. Os cinco nós customizados,
o prefab `Enemy_Barghest`, o coordenador de encontro e a malha de navegação já existem: falta
abrir o editor, montar a árvore com eles e apontar o grafo no prefab. A receita está no
apêndice da `tech/adr/0009`.

A segunda é o **portão da rede** do `docs/12`: duas pessoas em máquinas diferentes entram na
mesma `Sandbox_Combate`, batem na mesma cápsula, e o dano bate igual nas duas telas. Ele não
fecha sozinho, porque é teste manual com duas pessoas, e o caminho do Relay ainda depende de
ligar o projeto ao Unity Gaming Services em Project Settings > Services. Esquiva, aparo e
riposte só depois disso. Dois motivos: a FSM tem poucos estados hoje e vai ter doze no fim do
M1, e a janela de aparo de 0,18 s é menor que o ping de muita gente, o que faz de "quem
decide se o aparo aconteceu" uma pergunta de rede e não de combate.

A regra prática de quem escreve sistema novo: o cliente pede, o host decide, todo mundo
assiste. Um sistema novo começa respondendo quem tem autoridade, e a resposta vai no
cabeçalho do arquivo. Estado que só diverge em jogo é o que viaja; o que sai de asset já
está nas duas máquinas e replicar seria mandar o que o outro lado já tem.

A janela de dano é dirigida por tempo decorrido, não por evento de animação, porque ainda
não existe `Animator` no projeto. O `AttackDef` tem uma chave para inverter isso quando as
animações entrarem no M4 (`tech/adr/0007`).

**M1 é um portão, e ele mudou** (`docs/13` seção 9). Duas pessoas lutam contra 4 cápsulas
por 10 minutos e querem continuar, **e** pelo menos uma vez uma delas fez algo que a outra
não conseguiria sozinha. A segunda metade é o teste de verdade: coop em que dois jogadores
fazem a mesma coisa mais rápido não é coop. Se falhar só na segunda metade, o problema está
nos kits e não na rede. Se falhar na primeira, o projeto para e reprojeta o combate.
Não construa conteúdo em cima de um combate ruim.

## Agentes e skills

Sete agentes em `.claude/agents/` cobrem os setores: engenharia, design, narrativa,
arte técnica, QA, revisão de arquitetura e pesquisa do jogo original.
Seis skills em `.claude/skills/` cobrem os procedimentos que se repetem.

Rode o Claude Code com `E:\Unity_Games\TW1-Remaster` como diretório, senão nada disso carrega.
