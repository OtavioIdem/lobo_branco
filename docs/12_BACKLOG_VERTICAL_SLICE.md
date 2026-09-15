# 12 — Backlog do Vertical Slice

Tarefas ordenadas. Faça de cima para baixo. Não pule para baixo porque parece mais
divertido — a ordem existe porque cada tarefa depende das anteriores.

Estimativas em horas, já com o multiplicador de aprendizado do doc 09.

Legenda de tamanho: **P** até 2 h, **M** de 2 a 5 h, **G** de 5 a 12 h.

---

## M0 — Fundação (24 h)

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| ~~0.1~~ ✅ | Instalar módulo IL2CPP; configurar editor externo | P | 11 §2 |
| ~~0.2~~ ✅ | Criar projeto `LoboBranco` com template Universal 3D | P | 11 §3 |
| ~~0.3~~ ✅ | Aplicar toda a configuração obrigatória (Force Text, layers, tags, física, tempo) | M | 11 §4 |
| ~~0.4~~ ✅ | `git init`, LFS, YAMLMerge, primeiro commit | P | 11 §5 |
| ~~0.5~~ ✅ | Criar a árvore de pastas `_Project` completa | P | 07 §3 |
| ~~0.6~~ ✅ | Criar os 12 `.asmdef` com o grafo de dependências correto | M | 07 §3 |
| ~~0.7~~ ✅ | Instalar pacotes do manifest (Input, Cinemachine, AI Nav, Behavior, Addressables, Newtonsoft, ProBuilder, Test Framework) | M | 07 §2 |
| ~~0.8~~ ✅ | Instalar Ink Unity Integration por Git URL | P | 11 §2.4 |
| ~~0.9~~ ✅ | Input Action Asset com as 17 ações do doc 02 §4 | M | 02 §4 |
| ~~0.10~~ ✅ | Controlador de movimento: andar, correr, gravidade, slope, `CharacterController` | G | 08 §2 |
| ~~0.11~~ ✅ | Cinemachine: `CM_Exploration` com deoccluder e damping | M | 07 §7 |
| ~~0.12~~ ✅ | Cena `Sandbox_Combate` com plano, luz e painel de debug | M | 11 §6 |
| 0.13 | Build de Windows e verificação do checklist de saída | P | 11 §7 |

**Portão M0:** cápsula anda com teclado e gamepad, câmera acompanha, build roda.

---

## M1 — Combate (48 h) — não avance sem passar pelo portão

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| ~~1.1~~ ✅ | `StatType`, `ModifierOp`, `StatModifier`, `StatSheet` com evento de mudança | M | 07 §4.2 |
| ~~1.2~~ ✅ | Testes unitários do `StatSheet` (ordem de operações, remoção por Source) | P | 07 §9 |
| ~~1.3~~ ✅ | `IDamageable`, `IDamageDealer`, `DamageContext` com o `Log` | M | 07 §4.3 |
| ~~1.4~~ ✅ | Os 11 `IDamageStage` do pipeline, na ordem | G | 03 §9 |
| ~~1.5~~ ✅ | Testes unitários do pipeline: os dois cenários de 2,19x e 0,41x | M | 03 §9 |
| ~~1.6~~ ✅ | FSM do jogador: contrato de estado e transições | G | 07 §4.4 |
| ~~1.7~~ ✅ | Buffer de input de 0,2 s | P | 07 §4.4 |
| ~~1.8~~ ✅ | Estados `Locomotion`, `Attack` leve e forte | G | 03 §4 |
| ~~1.9~~ ✅ | Hitbox por evento de animação com `OverlapCapsule` e lista de já-atingidos | M | 07 §4.5 |

**A rede entra aqui.** Não depois do M1. A FSM tem dois estados hoje e vai ter doze no fim
do milestone, e a janela de aparo de 0,18 s da tarefa 1.11 é menor que o ping de muita gente.
Construir esquiva e aparo antes de decidir quem tem autoridade é construí-los duas vezes.
Justificativa completa na [ADR 0008](../tech/adr/0008-netcode-for-gameobjects-com-relay.md).

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| ~~1.9a~~ ✅ | Instalar NGO, Transport, Services Core, Authentication, Relay e Multiplayer Play Mode | P | ADR 0008 |
| ~~1.9b~~ ✅ | `NetworkManager` na cena de bootstrap, transporte direto por IP, duas cápsulas na mesma cena | M | ADR 0008 |
| ~~1.9c~~ ✅ | Prefab de jogador em rede: `NetworkObject`, spawn por conexão, câmera e input só do dono | M | 13 §6 |
| ~~1.9d~~ ✅ | `PlayerBrain` reescrito com autoridade. É o único arquivo que a rede obriga a reescrever | G | 13 §7 |
| ~~1.9e~~ ✅ | `StatSheet` autoritativo no host e replicado. Cliente lê, nunca escreve | G | 13 §6 |
| ~~1.9f~~ ✅ | Ataque vira pedido: RPC do dono, `MeleeHitbox` e `DamagePipeline` rodando só no host | G | 13 §6 |
| ~~1.9g~~ ✅ | Painel de debug mostra papel, dono, autoridade e ida-e-volta de cada personagem | P | 13 §11 |
| ~~1.9h~~ ✅ | Relay: autenticação anônima, criar sessão, gerar e entrar por código de convite | G | ADR 0008 |
| ~~1.9i~~ ✅ | UI mínima de sala: hospedar, colar código, entrar, ver quem está dentro | M | 13 §8 |

**Portão da rede:** duas pessoas em máquinas diferentes entram na mesma `Sandbox_Combate`
por código, batem no mesmo `CombatDummy`, e o dano bate igual nas duas telas. Só depois disso
a tarefa 1.10 começa.

O código das nove tarefas está escrito, mas **o portão continua fechado**, e ele não fecha
sozinho: ele é um teste manual com duas pessoas. Falta também ligar o projeto ao Unity
Gaming Services em Project Settings > Services, senão o Relay responde que o projeto não
existe. Até isso acontecer, o caminho testável é o IP direto, que é exatamente o que o
risco X8 do [doc 13 §11](13_COOP_E_REDE.md) previu.

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| 1.10 | Estados `Dodge` e `Roll` com frames de invulnerabilidade | M | 03 §5 |
| 1.11 | Estados `Parry` e `Riposte` com a janela de 0,18 s | G | 03 §5 |
| ~~1.12~~ ✅ | Sistema de Fluxo com a janela de 0,22 s e os cinco níveis de bônus | M | 03 §6 |
| 1.13 | Indicador visual do Fluxo (brilho na lâmina, via Shader Graph) | M | 03 §6 |
| ~~1.14~~ ✅ | Três posturas, com troca e afinidade de arquétipo | M | 03 §4 |
| ~~1.15~~ ✅ | Aço e prata, com a troca de 0,7 s não-cancelável | M | 03 §3 |
| ~~1.16~~ ✅ | Vigor: consumo, regeneração, atraso de 1,5 s | M | 03 §7 |
| 1.17 ⚠️ | Adrenalina: ganho, três gastos. **Parcial**: recurso e ganhos prontos, e dos três gastos só o segundo suspiro. Nota no 03 §7 | M | 03 §7 |
| ~~1.18a~~ ✅ | Estados de controle no inimigo: atordoado, derrubado, lento. Host decide, todos veem. Em componente, sem depender do grafo | M | 07 §6 |
| ~~1.18b~~ ✅ | Efeito de sinal como dado: área em cone ou raio sem alocação, escala por `SignIntensity`, gancho de variante por escola. Nota abaixo | M | 03 §8 |
| ~~1.18c~~ ✅ | Aard: cone de 6 m, derruba leves, atordoa médios por 1,5 s. Porte da criatura no `MonsterDef`. Nota no 03 §8 | M | 03 §8 |
| 1.18d | Igni: cone de 5 m, dano 0,8x pelo pipeline, Queimadura de 4/s por 5 s | M | 03 §8 |
| 1.18e | Quen: absorve um golpe por 8 s e devolve 30% do dano ao quebrar | M | 03 §8 |
| 1.18f | Yrden: armadilha replicada de 4 m por 12 s, lentidão de 60% | M | 03 §8 |
| 1.18g | Escolher o sinal: roda segurando Q, sem desacelerar o tempo, e teclas diretas | M | 02 §4 |
| 1.18h ⏸ | Axii. **Adiado** até existir inimigo humanoide: nota abaixo | M | 03 §8 |
| 1.19 | Quebra de guarda e ancoragem de etéreos (regras que dão sentido aos sinais) | M | 03 §8 |
| ~~1.20~~ ✅ | `MonsterDef` como ScriptableObject | P | 07 §4.1 |
| 1.21 ⚠️ | Behavior tree base do inimigo. **Parcial**: sentidos, aquisição de alvo, golpe, nós customizados, prefab e NavMesh prontos; falta desenhar o grafo no editor gráfico. Nota abaixo | G | 07 §6 |
| ~~1.22~~ ✅ | Coordenador de encontro com attack token. **Máximo 2 por alvo**, e não por encontro: nota abaixo | M | 07 §6 |
| ~~1.23~~ ✅ | Telegrafo de ataque de 0,4 a 0,9 s. Em greybox, cor e pose; o aviso é replicado por instante de início. Nota abaixo | M | 03 §10 |
| 1.24 | Ataques `Unblockable` com tell vermelho | P | 03 §5 |
| ~~1.25~~ ✅ | Hitstop, screen shake por Cinemachine Impulse, camera punch. **Sem `timeScale`**: nota abaixo | M | 03 §11 |
| 1.26 | Partículas de impacto por material do alvo | M | 03 §11 |
| 1.27 | Knockback proporcional ao peso do alvo | P | 03 §11 |
| 1.28 | Slow-motion no último inimigo morto | P | 03 §11 |
| 1.29 | Painel de debug: vitalidade, vigor, postura, Fluxo, e o log de dano | M | 11 §6 |
| 1.30 | Balancear com os números do doc 03 §12 e ajustar até o TTD alvo | G | 03 §12 |

**Sobre a ordem, 2026-09-12.** A 1.20 foi feita antes da 1.18 de propósito, e vale escrever
por quê antes que pareça capricho. Os cinco sinais são, quase todos, efeitos sobre o inimigo:
Aard derruba e atordoa, Igni queima, Yrden lentifica, Axii vira o lado. Cápsula de sandbox
não cai, não queima e não muda de lado, então construir os sinais agora seria construir os
efeitos duas vezes: uma contra a cápsula e outra quando o inimigo da 1.21 existir. A 1.18
entra depois da 1.21, junto da 1.19, que é a regra que dá sentido a ela.

**Sobre a 1.21 ficar parcial, 2026-09-12.** Tudo que é código está escrito e testado: o
cone de visão com memória, a busca de alvo, o golpe com telegrafo de 0,65 s, os quatro nós
customizados, o prefab do barghest e a malha de navegação da sandbox. O que falta é o
próprio grafo, e ele **não pode ser montado por script**: o tipo de asset de autoria do
`com.unity.behavior` é interno ao pacote. Resta uma sessão no editor gráfico, arrastando os
nós que já existem, e apontar o grafo resultante no prefab `Enemy_Barghest`. A divisão entre
o que vive no grafo e o que vive em componente está na
[ADR 0009](../tech/adr/0009-percepcao-em-componente-arvore-no-grafo.md).

O jogador passou a ser atingível na mesma tarefa, pelo `DamageReceiver`. Isso não era um
item do backlog e deveria ter sido: até aqui, o pipeline de dano só tinha alvo de um lado.

**Sobre o token da 1.22 ser por alvo, 2026-09-12.** O `docs/07` §6 escreve "um coordenador
por encontro concede no máximo 2 tokens", e esse número foi pensado para um jogador. Em coop
de quatro, um teto global de dois faria um encontro de oito criaturas ter seis paradas
assistindo, e o segundo, o terceiro e o quarto jogador nunca seriam atacados. O teto passou a
valer **por alvo**: cada bruxo enfrenta no máximo dois de cada vez, e um grupo grande continua
sendo um grupo grande. O número em si está no `CombatTuningDef`.

O token só melhora o combate com o `CircleTargetAction` do lado dele. Sem ter para onde
esperar, a terceira criatura seria recusada, cairia no galho de perseguir e ficaria encostada
no bruxo sem bater, e inimigo parado a meio metro parece travado. Rondando, a espera vira
ameaça.

**Sobre o telegrafo da 1.23 ser de rede antes de ser de arte, 2026-09-14.** A linha do tempo
do golpe roda só no host, então no cliente não existe anticipação nenhuma para desenhar: sem
um aviso replicado, quem hospeda vê o tell e o companheiro apanha sem aviso. O host manda um
único instante por golpe, no relógio do servidor, e cada máquina calcula o aviso a partir do
mesmo `AttackDef`. Com ping, o cliente começa o aviso atrasado, mas termina junto com a
janela de dano do host. Recomeçar do zero ao receber daria a todo cliente um aviso que acaba
depois de o golpe já ter acertado.

Não há `Animator` ainda, então a "animação de anticipação" da tarefa é, em greybox, a cápsula
que muda de cor e se abaixa. O aviso **enche** até a janela abrir, em vez de só ligar: um
aviso que liga diz que o golpe vem, e um aviso que enche diz quando. A cor é âmbar e nunca
vermelha, porque vermelho é o tell de `Unblockable` da 1.24. Quando as animações entrarem no
M4, a pose substitui o encolher, e o instante que viaja pela rede continua o mesmo.

**Sobre o hitstop da 1.25 não congelar o tempo, 2026-09-14.** O jeito comum de fazer hitstop é
mexer em `Time.timeScale`, e em coop isso é proibido: a escala é global na máquina, e no host
ela congelaria a simulação de todos os jogadores. Congelar só o dono também quebra, porque o
host mede a janela de Fluxo de 0,22 s pelo próprio relógio, e um dono parado 0,08 s perderia um
terço dela. O hitstop estende o golpe de quem bateu pela mesma duração nas duas pontas: o host
soma à contagem dele, e o dono segura a própria linha do tempo quando a confirmação chega. É um
hitstop por golpe, e não por alvo. A regra vale também para a câmera lenta da 1.28, que fica
decidida antes de existir: só apresentação, nunca simulação
([ADR 0010](../tech/adr/0010-tempo-de-jogo-nunca-e-global-em-coop.md)).

**Sobre o `SchoolDef` da 1.31, 2026-09-14.** O `docs/13` §5.1 dizia que a afinidade de postura
de uma escola já era aplicada pelo `StanceAffinityStage`, e isso estava errado: aquele estágio
compara a postura do golpe com o arquétipo do alvo, e a escola de quem bate não entra na conta.
A escola passou a ser dona dos três golpes, e a postura favorecida é a postura inicial e a vaga
do golpe mais bem feito. É dado de golpe, não multiplicador novo, então a razão de 5,3 vezes
fica intacta. A intensidade de sinal já era atributo, e mora no bloco de atributos da escola.
Custo de sinal e filtro de vestígio ficaram de fora, porque sinais e investigação ainda não
existem. O Lobo sai idêntico ao kit que já existia; nada muda em jogo nesta tarefa.

Um teste falha se aparecer uma terceira escola em `Assets/_Project/Data`. O slice é Lobo e
Grifo, e uma escola nova é só um asset, que é a porta mais fácil para o escopo crescer.

**Sobre a habilidade da 1.32, 2026-09-14.** Uma habilidade é um `AbilityDef`: custo de vigor,
recarga, tempo de conjurar e recuperação. A escola lista as dela por vaga, e é a vaga que viaja
pela rede, como a postura viaja no golpe. O efeito ficou de fora, porque os efeitos dos sinais
são a 1.18: o host dispara um evento no instante do efeito, e é ali que o empurrão vai pendurar.

A autoridade tem uma diferença para o golpe, e ela é deliberada. O vigor do golpe o host cobra
sempre e nunca recusa. A habilidade ele pode recusar, porque sinal de graça é janela de graça,
e janela é o recurso mais caro do combate. A recusa quase nunca acontece, e quando acontece o
dono corta a conjuração. A recarga viaja como o instante em que volta, no relógio do servidor,
e não como contagem ([ADR 0011](../tech/adr/0011-habilidade-cobrada-no-inicio-recarga-como-instante.md)).

**O Lobo ganhou o primeiro sinal, e ele não faz nada ainda.** É o abridor do doc 03 §8, com os
30 de vigor e os 4 s de recarga do documento. Isso muda o jogo de propósito: Q passa a gastar
vigor. É o que permite conferir, no portão da rede, que a recarga do companheiro conta junto
com a do host. Os tempos de conjurar e recuperar não estão no doc 03, e a nota de lá diz quais
foram escolhidos. A roda de sinais do doc 02 §4 também não existe, e até ela existir Q usa a
primeira vaga.

**Sobre a 1.18 vir antes da 1.33, e dividida, 2026-09-14.** Sem efeito de sinal, o Grifo seria
um Lobo com outra postura inicial, e o portão M1 pede que um jogador faça o que o outro não
consegue. Por isso os sinais vêm antes da segunda escola. A 1.18 foi dividida em oito partes
porque cinco efeitos sobre um inimigo que ainda não sabe ficar atordoado não cabem numa tarefa.

O **Axii ficou adiado**. Ele faz um humanoide lutar do lado do bruxo, e hoje não existe inimigo
humanoide na sandbox. Fazer agora seria escrever troca de facção na busca de alvo sem nada para
testar.

A **escolha do sinal tem roda e teclas diretas**. A roda nunca desacelera o tempo, ao contrário
do Witcher 3, pela [ADR 0010](../tech/adr/0010-tempo-de-jogo-nunca-e-global-em-coop.md): o mundo
continua correndo enquanto o bruxo escolhe.

Todas as escolas têm os cinco sinais, e cada uma é especializada em um, com uma variante de
efeito só dela. No slice, Lobo no Aard e Grifo no Igni. A tabela e as escolas registradas fora
do slice estão no [doc 13 §5](13_COOP_E_REDE.md).

**Sobre o efeito de sinal da 1.18b, 2026-09-15.** Um efeito é um asset, subclasse de
`SignEffectDef`, e a habilidade ganhou uma área (cone ou raio) e uma lista deles. Só o host
aplica, no instante do efeito que a 1.32 já pedia, e o resultado chega nas outras máquinas pelo
estado que cada alvo já replica: nenhum byte novo na rede. Os alvos saem do mais perto para o
mais longe, e quem conjurou e quem está abatido ficam de fora
([ADR 0012](../tech/adr/0012-efeito-de-sinal-em-asset-aplicado-pelo-host.md)).

A **variante de escola** é o `SchoolDef` apontando o sinal especializado e os efeitos a mais, que
são somados depois dos do sinal. O Lobo aponta o abridor, com a variante vazia, porque o efeito
dela ainda não foi desenhado. A **intensidade** usa a Inteligência, e com o Lobo sai 1,0: a nota
do [doc 03 §8](03_COMBATE.md) diz o que ela escala e o que não escala. O abridor ganhou o cone de
6 m e 90 graus, e continua sem efeito: o painel de debug mostra quantos alvos o cone pegou, e o
empurrão é a 1.18c.

| ~~1.31~~ ✅ | `SchoolDef`: bloco de atributos, três golpes e postura favorecida. A intensidade de sinal é atributo. Nota acima | M | 13 §5.1 |
| ~~1.32~~ ✅ | Habilidade com custo e recarga, como dado. O host cobra no início e pode recusar; a recarga viaja como instante. Sem efeito até a 1.18. Nota acima | G | 13 §5 |
| 1.33 | Escola Grifo: sinais intensos, postura Grupo, viés de Inteligência, variante própria do Igni. Zero `if` por escola. Depois da 1.18 | G | 13 §5 |
| 1.34 | Seleção de escola na entrada da sala | P | 13 §8 |
| 1.35 | Rebalancear o doc 03 para dois jogadores. Os números foram feitos para um | G | 03 §12 |

**Portão M1:** duas pessoas lutam contra 4 cápsulas por 10 minutos e querem continuar, **e**
pelo menos uma vez uma delas fez algo que a outra não conseguiria fazer sozinha.

A segunda metade é o teste de verdade. Coop em que dois jogadores fazem a mesma coisa mais
rápido não é coop, é jogo solo com testemunha. Falhou só na segunda metade, o problema está
nos kits: volte para 1.31 a 1.33. Falhou na primeira, pare aqui e reprojete o combate.
Não construa conteúdo sobre um combate ruim.

---

## M2 — Sistemas de RPG (48 h)

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| 2.1 | Hierarquia completa de `ItemDef` | M | 05 §11 |
| 2.2 | Inventário com abas, peso seletivo, stacking | G | 05 §7 |
| 2.3 | UI de inventário em UI Toolkit | G | 08 §6 |
| 2.4 | Equipamento: slots, durabilidade, modificadores via `StatSheet` | M | 05 §8 |
| 2.5 | `SubstanceType`, `IngredientDef`, `RecipeDef` | M | 05 §11 |
| 2.6 | Resolvedor de receitas (dado o inventário, o que é produzível) | M | 05 §11 |
| 2.7 | Testes unitários do resolvedor de receitas | P | 07 §9 |
| 2.8 | Tela de alquimia de três painéis | G | 05 §3 |
| 2.9 | Seis ervas com pontos de colheita no mundo | M | 05 §2 |
| 2.10 | Quatro poções com efeitos ligados ao `StatSheet` | M | 05 §4 |
| 2.11 | Toxicidade: quatro zonas, decaimento, efeitos de tela | M | 05 §5 |
| 2.12 | Dois óleos, com aplicação de 2 s e cargas | M | 05 §4 |
| 2.13 | Duas bombas com arremesso em arco | M | 05 §6 |
| 2.14 | Meditação: tela, avanço de tempo, restauração | M | 04 §2 |
| 2.15 | `SkillNodeDef` e o grafo da trilha de Esgrima (13 nós) | M | 04 §3 |
| 2.16 | Tela de talentos com tiers e requisitos | G | 04 §2 |
| 2.17 | Implementar os efeitos dos 13 nós de Esgrima | G | 04 §3 |
| 2.18 | Mutagênios: dois slots, cores, regra de sinergia | M | 04 §8 |
| 2.19 | XP, níveis 1 a 5, sem XP por monstro repetido | M | 02 §6 |
| 2.20 | `ISaveable`, `SaveService`, JSON, versionamento | G | 07 §5 |
| 2.21 | GUIDs persistentes para objetos de mundo | M | 07 §5 |
| 2.22 | Teste automatizado de round-trip de save | M | 07 §9 |

**Portão M2:** nível 1 ao 5, três poções produzidas, óleo aplicado, oito pontos gastos,
save e load preservam tudo exatamente.

---

## M3 — Mundo e conteúdo (60 h)

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| 3.1 | Fixar e documentar as escalas de referência | P | 08 §2 |
| 3.2 | Greybox do vilarejo em ProBuilder (travessia em 45 s) | G | 08 §2 |
| 3.3 | Greybox da floresta e do cemitério | G | 08 §2 |
| 3.4 | Greybox da cripta | M | 08 §2 |
| 3.5 | NavMesh nas três zonas | M | 07 §6 |
| 3.6 | Addressables por zona, cena `Boot`, transição com fade | G | 07 §8 |
| 3.7 | `WorldState` com `WorldFlagDef` e log de escrita | M | 07 §4.7 |
| 3.8 | Integração Ink e ligação de variáveis externas ao `WorldState` | G | 07 §4.7 |
| 3.9 | UI de diálogo, com marcadores de mecânica e opções reentráveis | G | 06 §6 |
| 3.10 | Cinco NPCs escritos em Ink (Reverendo, Odo, Abigail, coveiro, Haren) | G | 06 §9 |
| 3.11 | `QuestDef` com estágios, condições e efeitos | G | 06 §3 |
| 3.12 | Diário com as três abas | M | 06 §7 |
| 3.13 | Renderer Feature dos Sentidos de Bruxo (contorno + dessaturação) | G | 08 §4 |
| 3.14 | `ClueDef`, pistas colhíveis, estado de coleta | M | 06 §3 |
| 3.15 | Tela de dedução: cruzar pistas, identificar espécie, permitir erro | G | 06 §3 |
| 3.16 | Bestiário com quatro seções e o bônus mecânico de 1,25x | G | 02 §7 |
| 3.17 | Barghest: `MonsterDef`, behavior, tells | M | 03 §10 |
| 3.18 | Ghoul e Afogado | M | 03 §10 |
| 3.19 | Alghoul, com guarda que exige quebra | M | 03 §10 |
| 3.20 | A Besta: três fases, investida `Unblockable`, regeneração só quebrada por Igni | G | 03 §10 |
| 3.21 | As cinco composições de encontro, distribuídas sem repetir em sequência | M | 03 §10 |
| 3.22 | Mercadores, preços, calibragem econômica do doc 05 §9 | M | 05 §9 |
| 3.23 | Contrato "A Besta dos Arredores" montado nas dez etapas | G | 06 §3 |
| 3.24 | A escolha da Abigail, com as três resoluções e o Eco `abigail_fate` | G | 06 §2 |
| 3.25 | Consequência do Eco visível dentro do próprio protótipo | M | 06 §4 |
| 3.26 | Estados de dia e noite fixos, com transição na meditação | M | 00 §5 |

**Portão M3:** um jogador externo joga do início ao fim sem sua intervenção.

---

## M4 — Vestir e validar (36 h)

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| 4.1 | Escolher e comprar um pacote de ambiente coerente | P | 08 §2 |
| 4.2 | Substituir greybox do vilarejo por assets | G | 08 §2 |
| 4.3 | Substituir floresta, cemitério e cripta | G | 08 §2 |
| 4.4 | Personagem jogável com rig humanoide | M | 08 §4 |
| 4.5 | Animações Mixamo: locomoção, ataques, esquiva, aparo, reações | G | 08 §4 |
| 4.6 | Modelos de inimigos e suas animações | G | 08 §4 |
| 4.7 | VFX Graph: Igni, Aard, Yrden, sangue | G | 08 §4 |
| 4.8 | Shader de aplicação de óleo e de distorção por toxicidade | M | 08 §4 |
| 4.9 | Áudio: matriz de impacto de três camadas | G | 08 §5 |
| 4.10 | Ambiência das três zonas e duas faixas de música | M | 08 §5 |
| 4.11 | Iluminação com Adaptive Probe Volumes; interiores escuros de verdade | G | 07 §1 |
| 4.12 | HUD final, menu principal, tela de morte, opções com rebind | G | 02 §7 |
| 4.13 | Localização em pt-BR com todas as chaves | M | 07 §2 |
| 4.14 | `CREDITS.md` de arte e áudio completos | P | 08 §7 |
| 4.15 | Passe de performance contra o orçamento do doc 07 §12 | M | 07 §12 |
| 4.16 | Build IL2CPP de release | P | 11 §4.2 |
| 4.17 | **Playtest com 3 pessoas**, observando sem ajudar, anotando cada travada | M | 00 §6 |
| 4.18 | Corrigir os travamentos encontrados | G | — |
| 4.19 | Segundo playtest para confirmar o critério de sucesso | M | 00 §6 |

**Portão M4:** critério de sucesso do doc 00 §6 atingido.

---

## As três primeiras coisas a fazer agora

Se você quiser começar hoje, nesta ordem:

1. **0.1 a 0.4** — instalar IL2CPP, criar o projeto, configurar (com Force Text) e commitar.
   Cerca de 3 horas, e resolve o risco X6 permanentemente.
2. **0.10 a 0.12** — a cápsula que anda, a câmera e a sandbox. Cerca de 8 horas, e é o
   primeiro momento em que existe algo para rodar.
3. **1.1 a 1.5** — `StatSheet` e o pipeline de dano, com testes. Cerca de 10 horas, e é a
   fundação sobre a qual todo o resto do jogo se apoia.

Depois disso você estará em M1, que é onde o projeto vive ou morre.
