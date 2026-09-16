# 03 — Combate

Resolve os defeitos **D1** (metrônomo), **D6** (sinais irrelevantes), **D8** (bestiário
passivo) e **D13** (encontros repetitivos).

## 1. O problema a resolver

No original, o input é uma autorização. Aqui ele é uma ação com compromisso: toda animação
tem tempo de recuperação durante o qual você não pode cancelar. Isso é o que transforma
"clicar" em "decidir".

A regra de ouro: **nenhum input cancela uma animação já iniciada, exceto a esquiva, e a
esquiva custa vigor.** Todo o peso tático nasce dessa única regra.

## 2. Camadas do combate

```
Camada 1  ARMA      Aço ou prata          decisão binária, alto impacto
Camada 2  POSTURA   Forte / Rápida / Grupo  decisão por inimigo
Camada 3  AÇÃO      Ataque, esquiva, aparo, sinal, item   decisão por segundo
Camada 4  FLUXO     Timing opcional para bônus   decisão de maestria
Camada 5  PREPARO   Óleo, poção, bomba   decisão pré-combate, a mais importante
```

Camadas 1, 2 e 5 são herança direta do original. As camadas 3 e 4 são o trabalho novo.

## 3. Arma: aço e prata

| Arma | Contra humano/humanoide | Contra monstro |
|---|---|---|
| **Aço** | 1,0x | **0,35x** |
| **Prata** | **0,5x** | 1,0x |

Trocar de espada leva **0,7 s** com animação e não pode ser cancelado. Em uma luta mista
(bandidos com um cão amaldiçoado), essa troca é um risco real.

Cada espada tem **durabilidade** e um slot de óleo ativo. Óleo dura 60 golpes.

## 4. Postura

Trocar de postura leva **0,25 s** e pode ser feito durante o deslocamento, mas não durante
um ataque.

| Postura | Dano | Tempo do golpe | Recuperação | Alvos | Custo de vigor |
|---|---|---|---|---|---|
| **Forte** | 1,45x | 0,55 s | 0,45 s | 1 | 8 |
| **Rápida** | 0,75x | 0,25 s | 0,15 s | 1 | 4 |
| **Grupo** | 0,90x | 0,40 s | 0,55 s | até 4 em arco de 180° | 12 |

**Afinidade de postura** — cada arquétipo de inimigo tem uma postura ideal. Acertar
concede 1,3x adicional; errar concede 0,8x.

| Arquétipo | Postura ideal | Exemplos |
|---|---|---|
| Blindado / pesado / lento | Forte | golem, alghoul, cavaleiro da Ordem |
| Ágil / esquivo | Rápida | barghest, ladino, drowner |
| Enxame (3+) | Grupo | matilha de barghests, corvos, nekkers |

Isso é o motor de decisão mais importante do combate segundo a segundo, e é herança
direta e intencional do original.

**Nota de implementação, 2026-09-11 (tarefa 1.14).** A tabela acima tem uma linha por
postura, e não uma por botão: é a postura que decide qual golpe sai. Por isso os dois
botões de ataque do [doc 02 §4](02_GDD.md) atacam na postura corrente, em vez de o esquerdo
dar um golpe Rápido e o direito um Forte. Se o botão direito desse um golpe Forte com a
postura Rápida valendo, escolher postura não seria decisão nenhuma e a camada 2 do §2
deixaria de existir na prática.

Fica em aberto o que vai distinguir os dois botões **dentro** de uma mesma postura. As
opções óbvias são um segundo golpe por postura, mais lento e mais caro, ou o botão direito
virar outra coisa por completo. A decisão não é urgente e não deve ser tomada no escuro:
ela pede o playtest do portão M1, com as animações do M4 ainda por cima. Até lá os dois
botões são o mesmo golpe, o que não tira nada de quem joga.

## 5. Ações defensivas

### Esquiva (toque)
- Deslocamento curto, 2,5 m, 0,35 s
- Custo: 15 de vigor
- **Frames de invulnerabilidade:** 0,10 s a 0,22 s do início
- Única ação que cancela outra animação

### Rolamento (segurar)
- 5 m, 0,6 s
- Custo: 25 de vigor
- Invulnerabilidade de 0,12 s a 0,30 s
- Reposiciona; ruim para contra-ataque, bom para escapar de área

### Aparo (parry)
- Segurar bloqueia ataques leves com custo de vigor igual a 40% do dano
- **Não bloqueia** ataques marcados como `Unblockable` (tell vermelho) nem ataques de
  monstros grandes
- **Janela de riposte:** apertar aparo entre 0 e 0,18 s antes do impacto anula o dano,
  atordoa o atacante por 1,2 s e concede 1 carga de adrenalina

O riposte é o teto de habilidade do combate. Nunca é obrigatório.

## 6. Sistema de Fluxo — a homenagem ao original

Esta é a peça que mantém a identidade do jogo de 2007 sem restaurar o defeito.

**Como funciona:** ao encadear ataques, existe uma janela de 0,22 s no fim da recuperação
de cada golpe. Atacar dentro dela mantém a **Corrente de Fluxo**.

| Elos na corrente | Bônus |
|---|---|
| 1 | — |
| 2 | +10% de dano |
| 3 | +20% de dano, −20% de custo de vigor |
| 4 | +30% de dano, animação alternativa |
| 5+ | +35% de dano (teto), gera 1 adrenalina a cada 2 elos |

**A diferença crucial:** atacar fora da janela **não te pune**. Você só perde o bônus e a
corrente reinicia. Um jogador que ignora o Fluxo inteiro ainda joga o jogo — só faz menos
dano. Um jogador que o domina se sente um esgrimista.

Um indicador discreto (um brilho na lâmina, não um ícone de HUD) marca a janela.

## 7. Vigor e adrenalina

**Vigor** — 100 base. Regenera 18/s fora de combate, 6/s em combate, 0/s durante
1,5 s após gastar. Sinais e defesa competem pelo mesmo recurso, e isso é o dilema central.

**Adrenalina** — 3 cargas. Ganha com riposte, Fluxo alto e mortes. Gasta em:
- **Finalização** (1 carga): execução instantânea em inimigo abaixo de 20% de vida
- **Sinal reforçado** (1 carga): dobra a intensidade do sinal
- **Segundo suspiro** (2 cargas): recupera 40% do vigor instantaneamente

**Nota de implementação, 2026-09-12 (tarefa 1.17).** O recurso existe e os ganhos funcionam:
corrente de Fluxo alta e morte causada rendem carga, e o riposte vai render quando existir
(tarefa 1.11). Dos três gastos, só o **segundo suspiro** foi implementado, porque é o único
que não depende de sistema ausente: a finalização precisa de um estado de execução e o sinal
reforçado precisa dos sinais (tarefa 1.18). Os dois entram junto com o que eles gastam.

Falta também decidir **por onde o jogador gasta**. A tabela de controles do
[doc 02 §4](02_GDD.md) não tem tecla para nenhum dos três, e inventar uma agora seria
decidir no escuro uma coisa que o playtest do portão M1 responde melhor. Até lá o segundo
suspiro é uma chamada que ninguém dispara, e o painel de debug mostra as cargas.

## 8. Sinais reequilibrados (resolve D6)

Custo em vigor, escalado por Inteligência. Os sinais deixam de ser dano e passam a ser
**criação de janela** — a única forma consistente de abrir um inimigo blindado.

| Sinal | Custo | Cooldown | Efeito | Papel |
|---|---|---|---|---|
| **Aard** | 30 | 4 s | Cone 6 m, derruba leves, atordoa médios 1,5 s, quebra guarda | Abridor |
| **Igni** | 35 | 5 s | Cone 5 m, dano 0,8x, aplica Queimadura (4/s por 5 s) | Dano contínuo e fraquezas |
| **Quen** | 25 | 6 s | Absorve 1 golpe por 8 s; ao quebrar devolve 30% do dano | Compromisso ofensivo |
| **Axii** | 40 | 12 s | Inimigo humanoide luta ao seu lado por 8 s | Controle e **diálogo** |
| **Yrden** | 35 | 8 s | Armadilha 4 m por 12 s, lentidão de 60%, ancora seres etéreos | Área e counter-hard |

Regras que dão relevância aos sinais:
1. **Inimigos com guarda** (escudos, carapaças) tomam 0,3x de dano de espada até que a
   guarda seja quebrada. Só Aard, Igni ou um riposte quebram guarda.
2. **Seres etéreos** (espectros, wraiths) são intangíveis para lâminas até serem ancorados
   por Yrden. Isso torna um sinal obrigatório, não opcional.
3. **Axii em diálogo** aparece em cerca de 15% das conversas, gastando vigor, e às vezes é
   a única forma de obter uma pista.

**Nota de implementação, 2026-09-14 (tarefa 1.32).** Custo e recarga existem como dado, no
`AbilityDef`, e o Aard é o primeiro asset: 30 de vigor e 4 s, como na tabela. A recarga conta a
partir do início da conjuração, e o vigor é cobrado no mesmo instante. O efeito é da tarefa 1.18,
e até lá o sinal cobra, recarrega e não faz nada.

Esta seção não dá dois números que o sistema precisa, e eles foram decididos na tarefa: **0,3 s
de conjuração** até o efeito e **0,4 s de recuperação**, 0,7 s no total, o mesmo da troca de
espada. A tarefa 1.30 revê os dois. O "escalado por Inteligência" do primeiro parágrafo também
não tem fórmula, e fica para a 1.18 decidir junto com a intensidade.

**Nota de implementação, 2026-09-15 (tarefa 1.18b).** O efeito virou dado: cada sinal tem uma
área e uma lista de efeitos, e só o host os aplica
([ADR 0012](../tech/adr/0012-efeito-de-sinal-em-asset-aplicado-pelo-host.md)). Três decisões
que esta seção não trazia:

- **"Escalado por Inteligência" é a intensidade, e não o custo.** A intensidade é
  `SignIntensity × Inteligência ÷ 10`, e o bruxo de nível 1 tem 10 de Inteligência: com ele, todo
  sinal sai com os números desta tabela. O custo fica fixo, porque os 30 de vigor são o número
  contra o qual o dilema do §7 foi medido. O `SignIntensity` é o multiplicador neutro em 1,0 em
  que o sinal reforçado do §7 escreve.
- **A intensidade escala potência, e nunca forma.** Duração, dano e força crescem; alcance,
  abertura, custo e recarga não. Em coop, o cone é o que o companheiro aprende a ler.
- **O cone do abridor tem 90 graus de abertura.** A tabela dá os 6 m e não a abertura. 90 graus
  pega os dois barghests que flanqueiam e ainda obriga a mirar. A tarefa 1.30 revê.

O sinal continua sem efeito em jogo até a 1.18c: ele cobra, recarrega, acha quem está no cone, e
o painel de debug mostra quantos.

**Nota de implementação, 2026-09-15 (tarefa 1.18c).** O abridor passou a controlar. "Leve" e
"médio" não existiam como dado, e o arquétipo da §4 não serve para isso: um barghest e um bandido
são ambos ágeis, e só um deles voa com um empurrão. Toda criatura declara agora um **porte**
(leve, médio ou pesado) no `MonsterDef`. Quatro decisões que esta seção não trazia:

- **A derrubada dura 2 s.** A tabela dá só o atordoamento de 1,5 s. Derrubar é o controle mais
  forte, então dura mais.
- **O barghest é leve.** A §12 não diz o porte dele, e a composição Matilha da §10 existe para
  ensinar o abridor. Com o barghest médio, o abridor só atordoaria a matilha.
- **Sem porte declarado, a criatura é média:** atordoada, nunca derrubada.
- **A intensidade alonga o controle e não troca o tipo.** Um Grifo intenso segura o barghest no
  chão por mais tempo, e nunca derruba o que esta tabela diz que só atordoa.

Ficam de fora, com dono: **quebrar guarda** é a 1.19, e o **empurrão** da §11 é sensação e entra
com ela. Um risco fica registrado para a 1.35: dois bruxos alternando o abridor a cada 2 s mantêm
um barghest no chão para sempre, porque o controle nunca soma mas pode ser reaplicado quando
acaba. Pode ser exatamente a sinergia que o portão M1 procura, ou pode ser controle eterno; o
playtest decide antes de qualquer imunidade entrar.

**Nota de implementação, 2026-09-15 (tarefa 1.18d).** O fogo fere e queima. Esta seção dá "cone
5 m, dano 0,8x, Queimadura 4/s por 5 s", e cinco coisas precisaram de decisão:

- **O 0,8x é da espada na mão.** Com 12 de base, o fogo sai 9,6 antes da armadura. Um número de
  dano próprio seria esquecido quando a espada melhorar.
- **O dano do sinal passa por 4 dos 11 estágios da §9:** bestiário, poção, armadura e resistência.
  Postura, afinidade, material, Fluxo, óleo e crítico são de lâmina; com o material na conta, o
  fogo sairia 0,35x contra monstro com aço na mão. É outra lista de estágios, na mesma ordem, e
  os onze do golpe e a razão de 5,3 vezes não mudam.
- **A Queimadura ignora a armadura e respeita a resistência a fogo.** Com a subtração plana, 4
  por tique viraria 1 contra o alghoul de 12 de armadura, e o fogo seria pior justamente contra
  quem ele deveria abrir. São tiques de 1 s, 20 de dano no total, e a intensidade escala o dano
  por segundo e não a duração.
- **Reaplicar não soma:** a Queimadura mais forte manda, como a lentidão. Dois bruxos com fogo
  no mesmo barghest não dobram o dano.
- **O cone tem 60 graus**, mais estreito que os 90 do abridor: o abridor derruba a matilha em
  volta, e o fogo é mirado no alvo que tem a fraqueza. A tarefa 1.30 revê.

Quebrar guarda continua na 1.19, e interromper a regeneração da Besta (§10) espera a Besta. O
Lobo recebe o fogo na segunda vaga, e ele só pode ser conjurado quando a roda da 1.18g existir.

**Nota de implementação, 2026-09-15 (tarefa 1.18e).** O escudo existe, e ele trouxe duas peças
que faltavam. A primeira é uma **forma de área nova**, em quem conjura: o escudo é o único sinal
cujo alvo já é conhecido antes de qualquer consulta, e ele não custa física nenhuma. A segunda é
**quem bateu**, que agora viaja no resultado de dano: sem isso o alvo sabe que apanhou e não de
quem, e não havia para onde devolver os 30%.

- **Absorve um evento de dano inteiro**, de 8 ou de 80, e quebra. É o que faz do escudo uma
  decisão de quando erguer, e não um colete de vida extra.
- **O troco não passa pelo pipeline de novo.** Aquele dano já foi resolvido uma vez; rodar os
  estágios outra vez aplicaria a armadura de quem bateu a um dano que já é o que ele causou.
- **A intensidade escala o troco, e não a duração.** Como o escudo absorve o golpe inteiro, o
  retorno é a única potência que existe nele, e uma duração que crescesse tiraria o compromisso.
- **Erguer de novo substitui**, nunca soma: dois escudos não absorvem dois golpes.

O escudo entra na frente da vida por um contrato, e não por conhecer sinais: a poção de pele de
pedra do M2 entra pela mesma porta. Ele absorve qualquer dano que chegue por ela, inclusive um
tique de Queimadura — o que só vai importar quando alguma criatura puser fogo no bruxo.

## 9. Pipeline de dano

A ordem importa, e ela é o que faz o pilar P2 funcionar. Implementada como uma struct
`DamageRequest` que passa por estágios.

```
1. dano_base          da arma
2. × multiplicador de postura        Forte 1,45 / Rápida 0,75 / Grupo 0,90
3. × afinidade de postura            1,3 acerto / 1,0 neutro / 0,8 erro
4. × material da arma                aço-prata conforme a tabela
5. × bônus de Fluxo                  1,0 a 1,35
6. × óleo aplicado                   1,0 ou 1,5 se a classe casar
7. × conhecimento do bestiário       1,0 ou 1,25 se Vulnerabilidades destravadas
8. × buffs de poção                  Trovão, etc.
9. × crítico                         2,0 se ocorrer
10. − armadura do alvo               subtração plana, mínimo de 1
11. × resistência do alvo ao tipo    corte / contusão / fogo / prata
```

**O cálculo que justifica tudo:** um jogador preparado, contra um barghest, com prata,
postura Rápida, óleo de besta, bestiário pesquisado e Fluxo 3:

```
1,0 × 0,75 × 1,3 × 1,0 × 1,20 × 1,5 × 1,25 = 2,19x
```

Um jogador despreparado, com aço, postura Forte, sem óleo e sem pesquisa:

```
1,0 × 1,45 × 0,8 × 0,35 × 1,0 × 1,0 × 1,0 = 0,41x
```

A diferença é de **5,3 vezes**. É isso que garante que o critério de sucesso do protótipo
(seção 6 do doc 00) seja alcançável. O jogador despreparado não perde por reflexo ruim,
perde por não ter feito o trabalho de bruxo.

## 10. Design de inimigos (resolve D13)

Todo inimigo é definido por um `MonsterDef` com:
- Arquétipo (define a postura ideal)
- Vulnerabilidades: classe de óleo, tipo de sinal, tipo de dano
- Conjunto de tells: cada ataque tem um telegrafo de 0,4 a 0,9 s, com animação distinta
- Comportamento de matilha: `Solo`, `Flanqueador`, `Cercador`, `Arremessador`

### Composições de encontro
Regra: **nunca dois encontros seguidos com a mesma composição.** As composições do
protótipo:

| Nome | Composição | Ensina |
|---|---|---|
| Batedor | 1 barghest | Tells e esquiva |
| Matilha | 4 barghests | Postura Grupo, Aard |
| Emboscada | 2 barghests + 2 bandidos | Troca de espada sob pressão |
| Guarda | 1 alghoul (blindado) | Quebra de guarda, postura Forte |
| Chefe da matilha | A Besta + 3 barghests | Tudo junto |

### A Besta (boss do protótipo)
Três fases, cada uma exigindo uma resposta diferente:
1. **Fase 1 (100–65%)** — luta com a matilha. Você precisa afinar em Grupo e usar Aard.
2. **Fase 2 (65–30%)** — fica sozinha, ganha investida `Unblockable`. Exige leitura de tell
   e rolamento. Aparo aqui te mata.
3. **Fase 3 (30–0%)** — invoca reforços e ganha regeneração que **só é interrompida por
   Igni**. Se o jogador não trouxe Igni, a luta é injustamente longa — e a pista disso
   está na investigação.

## 11. Feedback e game feel (o que faz o combate parecer bom)

Sem isso, os números acima não valem nada. Lista de implementação obrigatória:

- **Hitstop:** 0,08 s de congelamento na conexão de um golpe forte, 0,04 s no leve
- **Screen shake:** amplitude proporcional ao dano, com decaimento; via Cinemachine Impulse
- **Camera punch:** deslocamento de 2° no impacto
- **Partículas:** faísca em armadura, sangue em carne, poeira em pedra (por material do alvo)
- **Áudio em três camadas:** whoosh da lâmina, impacto por material, reação vocal do alvo
- **Animação:** anticipação, contato, recuperação; nunca um golpe sem anticipação
- **Knockback:** empurrão proporcional; inimigos leves voam, pesados nem se movem
- **Slow-motion:** 0,25 s a 0,4x na morte do último inimigo de um encontro

Regra prática: 70% da sensação de bom combate vem desta seção, não do balanceamento.

## 12. Balanceamento inicial (números para o primeiro playtest)

| Entidade | Vitalidade | Dano por golpe | Armadura |
|---|---|---|---|
| Jogador nível 1 | 100 | 12 base | 4 |
| Aldeão hostil | 40 | 8 | 0 |
| Bandido | 70 | 14 | 3 |
| Barghest | 55 | 16 | 2 |
| Afogado | 90 | 20 | 4 |
| Ghoul | 65 | 15 | 3 |
| Alghoul | 140 | 26 | 12 |
| A Besta | 400 | 34 | 8 |

Alvos de tempo de morte (TTD):
- Inimigo comum, jogador preparado: **3 a 5 golpes**
- Inimigo comum, jogador despreparado: 12 a 18 golpes (deve parecer errado, e é o ponto)
- Jogador morre em: 5 golpes sem armadura, 8 com armadura leve
- Boss preparado: 90 a 150 segundos
