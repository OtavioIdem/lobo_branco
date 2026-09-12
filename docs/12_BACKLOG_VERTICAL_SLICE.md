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
| 1.9h | Relay: autenticação anônima, criar sessão, gerar e entrar por código de convite | G | ADR 0008 |
| 1.9i | UI mínima de sala: hospedar, colar código, entrar, ver quem está dentro | M | 13 §8 |

**Portão da rede:** duas pessoas em máquinas diferentes entram na mesma `Sandbox_Combate`
por código, batem no mesmo `CombatDummy`, e o dano bate igual nas duas telas. Só depois disso
a tarefa 1.10 começa.

| # | Tarefa | Tam. | Doc |
|---|---|---|---|
| 1.10 | Estados `Dodge` e `Roll` com frames de invulnerabilidade | M | 03 §5 |
| 1.11 | Estados `Parry` e `Riposte` com a janela de 0,18 s | G | 03 §5 |
| 1.12 | Sistema de Fluxo com a janela de 0,22 s e os cinco níveis de bônus | M | 03 §6 |
| 1.13 | Indicador visual do Fluxo (brilho na lâmina, via Shader Graph) | M | 03 §6 |
| 1.14 | Três posturas, com troca e afinidade de arquétipo | M | 03 §4 |
| 1.15 | Aço e prata, com a troca de 0,7 s não-cancelável | M | 03 §3 |
| 1.16 | Vigor: consumo, regeneração, atraso de 1,5 s | M | 03 §7 |
| 1.17 | Adrenalina: ganho, três gastos | M | 03 §7 |
| 1.18 | Cinco sinais com custo, cooldown e efeito | G | 03 §8 |
| 1.19 | Quebra de guarda e ancoragem de etéreos (regras que dão sentido aos sinais) | M | 03 §8 |
| 1.20 | `MonsterDef` como ScriptableObject | P | 07 §4.1 |
| 1.21 | Behavior tree base do inimigo (patrulha, detecção, engajamento, ataque) | G | 07 §6 |
| 1.22 | Coordenador de encontro com attack token (máximo 2) | M | 07 §6 |
| 1.23 | Telegrafo de ataque: animação de anticipação de 0,4 a 0,9 s | M | 03 §10 |
| 1.24 | Ataques `Unblockable` com tell vermelho | P | 03 §5 |
| 1.25 | Hitstop, screen shake por Cinemachine Impulse, camera punch | M | 03 §11 |
| 1.26 | Partículas de impacto por material do alvo | M | 03 §11 |
| 1.27 | Knockback proporcional ao peso do alvo | P | 03 §11 |
| 1.28 | Slow-motion no último inimigo morto | P | 03 §11 |
| 1.29 | Painel de debug: vitalidade, vigor, postura, Fluxo, e o log de dano | M | 11 §6 |
| 1.30 | Balancear com os números do doc 03 §12 e ajustar até o TTD alvo | G | 03 §12 |

| 1.31 | `SchoolDef`: junta bloco de atributos, afinidade de postura e intensidade de sinal | M | 13 §5.1 |
| 1.32 | Habilidade com custo e recarga, como dado. É a infraestrutura das escolas futuras | G | 13 §5 |
| 1.33 | Escola Grifo: sinais intensos, postura Grupo, viés de Vontade. Zero `if` por escola | G | 13 §5 |
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
