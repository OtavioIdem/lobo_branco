# 13 — Coop e Rede

Data da decisão: 2026-09-10. Substitui a linha "Multiplayer" da lista de fora-do-escopo
do [doc 00 §5](00_VISAO_E_ESCOPO.md) e altera o portão M1 do [doc 09](09_ROADMAP_E_MILESTONES.md).

## 1. O que mudou

O protótipo continua sendo **o Capítulo I e nada mais**. O que muda é que ele passa a ser
jogado por **2 a 4 pessoas na mesma caçada**, em sessão privada aberta por código de convite.

O que **não** mudou, e é importante dizer para não reabrir discussão depois:

- A zona continua sendo os Arredores de Vizima.
- O contrato continua sendo A Besta dos Arredores.
- O combate continua sendo o do [doc 03](03_COMBATE.md), com as três posturas, aço e prata,
  Fluxo, Vigor e sinais. Nenhum número de balanceamento foi jogado fora.
- Continua projeto solo, não-comercial, de aprendizado, com orçamento de 12 h por semana.
- Continua valendo a regra de ouro: nada extraído de jogo original entra no repositório.

## 2. O jogo em uma frase

Dois a quatro bruxos de escolas diferentes aceitam o mesmo contrato, se preparam de formas
distintas e caçam juntos uma criatura que nenhum deles derruba sozinho.

## 3. Por que o Capítulo I aguenta coop

Não foi sorte. Três coisas que já estavam no design servem coop sem alteração:

1. **A tabela de encontros do [doc 03 §13](03_COMBATE.md) já tem matilha.** Quatro barghests
   pedindo postura Grupo e Aard é, literalmente, um encontro desenhado para mais de uma frente.
2. **O pipeline de dano já tem arquétipo e afinidade de postura.** `StanceAffinityStage` já
   existe e já pune a postura errada. Isso é diferenciação de papel pronta, de graça.
3. **A zona é fechada e pequena.** Vilarejo, floresta, cemitério e cripta. Não há mundo aberto
   para replicar, e isso é o que torna a rede viável para um dev sozinho.

## 4. Os quatro pilares sob coop

| Pilar | Situação | O que acontece com ele |
|---|---|---|
| P1 — Bruxo é investigador | **Revisado** | Ver §4.1. Investigação vira coletiva, não paralela |
| P2 — Preparação > reflexo | **Reforçado** | Fica melhor em coop. Erro de preparo agora é visível ao grupo |
| P3 — Escolha sem moral limpa | **Adiado** | Ver §4.2. Sai do slice, volta depois do portão M1 |
| P4 — Vizima é um personagem | **Adiado** | Reação sistêmica da cidade depende de estado persistente por jogador |

### 4.1 O problema da investigação em coop, e a saída

Investigação em grupo tem um modo de falha conhecido: um jogador acha a pista, fala em voz
alta, e os outros três viraram plateia. Isso mata o P1 em vez de servi-lo.

A saída é **dividir os sentidos por escola**. Cada escola lê um tipo de vestígio e nenhuma lê
todos. O grupo completo enxerga a cena inteira; um jogador sozinho enxerga um terço dela e
entra na caçada com informação incompleta, que é exatamente o que o P2 quer punir.

| Escola | Lê | Não lê |
|---|---|---|
| Lobo | Rastro e pegada, direção e número | Resíduo mágico, vestígio de sangue |
| Grifo | Resíduo mágico, marca de ritual, ancoragem de etéreo | Rastro físico |
| Gato | Sangue, cheiro, restos, hora da morte | Marca de ritual |

Isso não é uma mecânica nova e cara: é um filtro por tipo em cima do sistema de investigação
que o [doc 02](02_GDD.md) já previa.

### 4.2 Por que a escolha da Abigail sai do slice

O P3 depende de uma escolha sem informação completa e com consequência atrasada. Em coop,
a pergunta "quem decide" não tem resposta boa dentro do orçamento:

- **Host decide** transforma três jogadores em espectadores do momento mais importante do jogo.
- **Votação** dilui exatamente a coisa que o pilar quer, que é carregar uma escolha pessoal.
- **Eco por jogador** é a resposta certa e custa um sistema de save divergente por participante.

A decisão é adiar, não cancelar. O slice entrega o ciclo de caçada. A escolha volta como
primeiro item depois do portão M1, e a terceira opção é o caminho quando voltar.

## 5. Escolas, não personagens nomeados

A ideia original era ter o elenco principal jogável. Isso não cabe aqui por dois motivos: o
Capítulo I tem **um** bruxo, e a regra 3 do [doc 07 §3](07_ARQUITETURA_TECNICA.md) proíbe nome
de IP em código, registrada na [ADR 0005](../tech/adr/0005-sistemas-agnosticos-de-ip.md).

Escolas de bruxo resolvem os dois e são melhores de projetar, porque cada uma já tem uma
identidade de luta coerente.

| Escola | Fantasia | Viés de atributo | Postura favorecida | Papel no grupo |
|---|---|---|---|---|
| **Lobo** | Equilibrado, espada e sinal | nenhum | Rápida | Referência. É o kit que já existe |
| **Grifo** | Sinais intensos, controle | Vontade | Grupo | Segura o campo, ancora etéreo, quebra guarda |
| **Gato** | Velocidade e veneno | Destreza | Rápida | Dano alto, sobrevive mal |
| **Urso** | Armadura pesada, dano bruto | Vigor | Forte | Absorve o boss enquanto os outros trabalham |
| **Víbora** | Duas espadas, execução | Destreza | Rápida | Fecha alvo ferido |

**No slice entram duas: Lobo e Grifo.** Não três, não cinco. Lobo porque já está construído.
Grifo porque força o sistema de habilidade com custo e recarga a existir de verdade, e esse
sistema é a infraestrutura que as outras três reaproveitam depois.

### 5.1 Escola é dado, não código

Uma escola é a combinação de três alavancas que **já existem no projeto**:

1. `StatBlockDef` diferente, que é o viés de atributo.
2. ~~Afinidade de postura, que `StanceAffinityStage` já aplica.~~ **Corrigido na tarefa 1.31**, abaixo.
3. Intensidade e custo de sinal, que o [doc 03 §8](03_COMBATE.md) já parametriza.

Uma escola nova é um `SchoolDef` apontando para esses três assets, mais o filtro de vestígio
do §4.1. Nenhum `if` por escola em código de combate. Se aparecer um, a escola está errada.

**Correção de 2026-09-14, escrita ao implementar a tarefa 1.31.** O item 2 estava errado. O
`StanceAffinityStage` compara a postura do golpe com o arquétipo do **alvo**, e a escola de
quem bate não entra nessa conta. A "postura favorecida" da tabela do §5 virou outra coisa,
mais simples e sem multiplicador novo:

- **A escola é dona dos três golpes, um por postura.** A postura favorecida é a postura com
  que o bruxo entra na luta, e a vaga em que a escola põe o golpe mais bem feito. Como o que
  muda é o `AttackDef`, e não um bônus de dano, a razão de 5,3 vezes do doc 03 fica intacta.
- **A intensidade de sinal é o atributo `SignIntensity`**, e por isso mora no `StatBlockDef`
  da escola, e não num campo próprio.
- **Custo de sinal e filtro de vestígio ficam fora do `SchoolDef` por enquanto**, porque sinais
  e investigação ainda não existem (tarefa 1.18 e M3).

Cada golpe tem que declarar a postura da vaga em que está. O golpe viaja pela rede como
postura e o host resolve o asset pela postura; um golpe na vaga errada faria dono e host
desferirem golpes diferentes. Há teste para isso sobre os assets reais.

## 6. Modelo de autoridade, em resumo

Detalhe e justificativa na [ADR 0008](../tech/adr/0008-netcode-for-gameobjects-com-relay.md).

| Camada | Quem manda | Motivo |
|---|---|---|
| Movimento e câmera | O dono do personagem | Coop contra IA. Trapaça não importa, e isso elimina predição e reconciliação |
| Máquina de estados do jogador | O dono | O estado é consequência do input do dono |
| Resolução de dano | **O host** | Um único `DamagePipeline` decide. Cliente nunca declara dano próprio |
| Vida, Vigor, Adrenalina, Fluxo | **O host** | Replicados para todos. Cliente lê, não escreve |
| IA, encontro, attack token | **O host** | O coordenador de token do doc 07 §6 já é peça única por natureza |
| Efeito visual e som de impacto | Todos, localmente | Reagem ao evento do host |
| Telegrafo de ataque de inimigo | **O host** decide o instante; todos desenham | Um carimbo de tempo por golpe, no relógio do servidor. Cada máquina calcula o aviso a partir do mesmo `AttackDef`, e o aviso do cliente com ping termina junto com a janela de dano do host (tarefa 1.23) |
| Hitstop | **O host** soma ao golpe; o dono segura o dele pelo mesmo tempo | Nunca `Time.timeScale`, que no host congelaria a sessão inteira. A extensão do golpe é igual nas duas pontas, e a janela de Fluxo sobrevive ([ADR 0010](../tech/adr/0010-tempo-de-jogo-nunca-e-global-em-coop.md)) |
| Tremor e soco de câmera | Só o dono, na tela dele | Reagem ao acerto confirmado pelo host e à vida do próprio bruxo caindo |

A frase que resolve noventa por cento das dúvidas de implementação: **o cliente pede, o host
decide, todo mundo assiste.**

## 7. O que o código atual aguenta

Auditoria dos 43 scripts existentes. O resultado é melhor do que o esperado, porque a regra 4
do CLAUDE.md ("sistemas testáveis não conhecem input nem câmera") já é, sem ter sido escrita
para isso, a preparação exata que rede exige.

| Módulo | Veredito | Observação |
|---|---|---|
| `Stats/*` | **Integral** | Passa a viver no host e replicar. A classe não muda |
| `Combat/DamagePipeline`, `DamageStages`, `DamageContext` | **Integral** | Classe pura, sem MonoBehaviour. Roda só no host |
| `Combat/AttackDef`, `CombatTuningDef`, `MeleeWeaponDef`, `CombatTypes` | **Integral** | São dados |
| `Combat/MeleeHitbox` | **Integral** | A classe não muda. Muda quem a chama: só o host |
| `Player/PlayerLocomotion`, `ILocomotionDriver` | **Integral** | Já recebe direção por propriedade. É a peça mais valiosa do projeto agora |
| `Player/InputBuffer` | **Integral** | Vira o buffer do dono |
| `Camera/ThirdPersonCameraRig` | **Integral** | Local por natureza |
| `Player/PlayerInputReader` | **Ajuste** | Passa a rodar só no dono |
| `Player/PlayerMeleeAttacker` | **Ajuste** | Dispara pedido; quem resolve o acerto é o host |
| `Combat/CombatDummy` | **Ajuste** | Perde a vida para um `CharacterVitals` ao lado. Ver nota abaixo |
| `Player/PlayerBrain` | **Reescrever** | É o único que junta input, câmera e FSM. É onde a autoridade entra |
| `Tests/*` | **Integral** | Os 115 testes continuam valendo. Testam lógica pura, que é justamente o que não muda |

Nada é jogado fora. Um arquivo é reescrito.

**Nota de 2026-09-11, escrita ao implementar as tarefas 1.9e e 1.9f.** A auditoria previa que
o `CombatDummy` virasse `NetworkBehaviour`. Ele não virou, e a razão é que a vida replicada
não é um problema do alvo de sandbox: é o mesmo problema do bruxo, do barghest e da Besta.
Ela saiu para um componente próprio, `Combat/CharacterVitals`, que carrega a folha de
atributos e a vida atual, e que o jogador e a cápsula usam sem diferença nenhuma. Quem vira
`NetworkBehaviour` é ele. O `CombatDummy` continua sendo o que sempre foi, um `MonoBehaviour`
que classifica a criatura e pisca quando apanha, e o `MonsterDef` da tarefa 1.20 vai herdar
a vida replicada de graça.

Os valores base não são replicados, e isso não é economia de banda: eles saem do mesmo
`StatBlockDef` em todas as máquinas. O que viaja é o que diverge, e hoje isso é só a vida.
Quando poções e talentos entrarem no M2, eles nascem no host e os modificadores passam a
viajar junto.

## 8. Escopo revisto

**Entra:**
- Sessão privada de 2 a 4 jogadores, host mais Relay, entrada por código de convite
- Duas escolas jogáveis, Lobo e Grifo
- Vestígio por escola, conforme §4.1
- Tudo que já estava dentro do escopo do doc 00 §5, exceto o que sai abaixo

**Sai do slice (não do projeto):**
- A escolha estrutural da Abigail e o sistema de Ecos
- Reação sistêmica do vilarejo ao alinhamento
- Diálogo ramificado em Ink durante sessão coop
- As outras três escolas

**Continua fora, agora com mais motivo ainda:**
- PvP, servidor dedicado, matchmaking público, crossplay, progressão persistente entre sessões
- Capítulos II a V, romance, minijogos, montaria, dublagem

## 9. O portão M1 mudou

Antes: *você luta contra 4 cápsulas por 10 minutos e quer continuar.*

Agora: **duas pessoas lutam contra 4 cápsulas por 10 minutos e querem continuar, e pelo menos
uma vez alguma delas fez algo que a outra não conseguiria fazer sozinha.**

A segunda metade da frase é o teste real. Coop onde dois jogadores fazem a mesma coisa mais
rápido não é coop, é um jogo solo com testemunha. Se o portão falhar nessa metade, o problema
está nos kits, não na rede.

## 10. Custo honesto

| Frente | Horas estimadas |
|---|---|
| Rede: NGO, Relay, código de convite, dois jogadores andando na mesma cena | 12 a 16 h |
| Converter FSM, ataque e hitbox para o modelo de autoridade do §6 | 8 a 12 h |
| Sistema de habilidade com custo e recarga (hoje só existe ataque leve) | 6 a 8 h |
| Segunda escola, Grifo, como dado | 6 a 8 h |
| Vestígio por escola | 4 h |

Entre 36 e 48 horas somadas às 40 h que ainda faltam do M1. O M1 sai de 4 semanas para algo
entre **7 e 8 semanas**. Esse é o preço do pivô e ele deve ser aceito de olho aberto, não
descoberto no meio do caminho.

## 11. Riscos novos

| # | Risco | Mitigação |
|---|---|---|
| X6 | Rede entra tarde e vira retrofit caro | Entra **agora**, antes das tarefas 1.10 e 1.11. A FSM tem 2 estados hoje e vai ter 12 no fim do M1 |
| X6b | Aparo de 0,18 s não sobrevive à latência | É o primeiro sistema a ser projetado em rede, não o último. Ver ADR 0008 |
| X7 | Escopo volta a crescer pela porta das escolas | Duas escolas. A terceira só depois do portão M1, e só se o portão passar |
| X8 | Depender de serviço de nuvem trava o desenvolvimento | Transporte direto por IP em desenvolvimento. Relay só para jogar com gente de fora |
| X9 | Balanceamento do doc 03 foi feito para um alvo e um jogador | Rebalancear é tarefa explícita do M1, não descoberta. O `CombatTuningDef` já centraliza |
| X10 | Depurar problema de rede sozinho é lento e desanimador | Duas instâncias na mesma máquina, com log de autoridade no painel de debug desde o primeiro dia |
