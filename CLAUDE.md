# CLAUDE.md — Projeto Lobo Branco

Protótipo de RPG de ação em Unity, remaster autoral de The Witcher (2007).
Projeto solo, não-comercial, feito para aprender desenvolvimento de jogos.

## Onde as coisas estão

| Caminho | O que é |
|---|---|
| `docs/00` a `docs/12` | **A fonte da verdade.** Design, arquitetura, roadmap |
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
2. **Preparação vale mais que reflexo.** Óleo e poção certos valem mais que esquiva perfeita.
3. **Escolha sem moral limpa, consequência atrasada.** Nunca marcar opção como boa ou má.
4. **Vizima é um personagem.** A cidade reage de forma sistêmica ao seu alinhamento.

## Escopo travado

O protótipo é **só o Capítulo I**, os Arredores de Vizima. Capítulos II a V, romance,
minijogos, montaria, multiplayer e dublagem estão explicitamente fora (docs/00 seção 5).

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

M0 fechado. Existe um jogador que anda com câmera de terceira pessoa, 19 testes
automatizados passando, e build gerando. O próximo passo é o **M1**, que começa pela
folha de atributos e pelo pipeline de dano de 11 estágios (`docs/12`, tarefas 1.1 a 1.5).

**M1 é um portão.** Se o combate contra cápsulas cinzas não for divertido depois de
pronto, o projeto para e reprojeta o combate. Não construa conteúdo em cima de um
combate ruim.

## Agentes e skills

Sete agentes em `.claude/agents/` cobrem os setores: engenharia, design, narrativa,
arte técnica, QA, revisão de arquitetura e pesquisa do jogo original.
Seis skills em `.claude/skills/` cobrem os procedimentos que se repetem.

Rode o Claude Code com `E:\Unity_Games\TW1-Remaster` como diretório, senão nada disso carrega.
