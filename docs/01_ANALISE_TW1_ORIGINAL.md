# 01 — Análise do Original: The Witcher (2007)

Este é o documento mais importante da pré-produção. Você não pode remasterizar o que não
entendeu. Aqui está desmontado o que o jogo faz, como faz, e onde falha.

## 1. Ficha técnica

| Item | Valor |
|---|---|
| Lançamento | Outubro de 2007 |
| Estúdio | CD PROJEKT RED (primeiro jogo do estúdio) |
| Engine | **Aurora Engine** (BioWare, do Neverwinter Nights) fortemente modificada |
| Reedição | Enhanced Edition (2008) — retrabalho de diálogo, load times, dublagem |
| Câmera | 3 modos: isométrica, over-the-shoulder, e ação/livre |
| Estrutura | Prólogo + 5 capítulos + epílogo |
| Duração | ~40h para a história principal |

A escolha da Aurora explica quase todos os defeitos: era uma engine de RPG isométrico
tático baseado em D&D 3.5. Combate em tempo real com timing foi enxertado nela, e é por
isso que ele parece indireto — você não controla a espada, você autoriza o próximo ataque.

## 2. Estrutura narrativa completa

### Prólogo — Kaer Morhen
Geralt, amnésico, é encontrado pelos bruxos. A fortaleza é atacada pela **Salamandra**,
liderada em campo pelo mago **Savolla** e pelo **Professor**, com o mago **Azar Javed**
por trás. Eles roubam os segredos de mutação dos bruxos. Boss: o **Frightener**.
- Função: tutorial de combate, sinais e alquimia. Estabelece o McGuffin (as mutações).

### Capítulo I — Arredores de Vizima (Podgrodzie)
Vilarejo rural sob ataque de **A Besta** (uma matilha de barghests com um líder).
NPCs-chave: **Abigail** (curandeira/bruxa), **Odo**, **Haren Brogg**, o **Reverendo**,
**Mikul**, o coveiro. A Salamandra opera de um esconderijo na cripta.
- **A escolha:** quando a Besta é derrotada, a vila culpa Abigail e forma uma turba.
  Você defende Abigail (e mata aldeões) ou entrega ela (e ela é linchada). Nenhuma opção
  é limpa, e a consequência retorna no Capítulo V.
- **Por que funciona:** informação incompleta, ambos os lados têm razão parcial, e o
  jogo nunca diz qual foi a escolha "certa".

### Capítulo II — Vizima (Bairro do Templo) e o Pântano
Introduz o conflito político central: **Ordem da Rosa Flamejante** (humanos, ordem,
xenofobia) contra os **Scoia'tael** (elfos e anões, resistência, terrorismo).
Representantes: **Siegfried** (Ordem) e **Yaevinn** (Scoia'tael).
NPCs-chave: **Kalkstein** (alquimista obsessivo), **Raymond Maarloeve** (detetive — morto
e substituído por um **doppler** a serviço de Azar Javed; o jogador trabalha com o impostor
por horas sem saber). No pântano: olheiros, druidas, o culto de **Dagon**, o **Golem** e
os obeliscos.
- **A escolha:** aliar-se à Ordem, aos Scoia'tael, ou permanecer neutro. Essa escolha se
  repete em III e V e determina o final.

### Capítulo III — Vizima (Bairro do Comércio)
Núcleo político. **Triss Merigold**, o **Rei Foltest**, a **Princesa Adda** (a strzyga),
**Coleman**, os esgotos e o **Zeugl**. O laboratório de Kalkstein na torre do mago.
Revelação do Grão-Mestre **Jacques de Aldersberg** como o verdadeiro antagonista ideológico.
- **A escolha:** curar ou matar Adda. Curá-la é mais difícil e tem peso político.
- **Ritmo:** melhor capítulo do jogo. Investigação real, alta densidade, pouco filler.

### Capítulo IV — Águas Turvas (Murky Waters) e o Lago
Aldeia de pescadores, **Alvin** (a criança Fonte, com dom profético), a **Dama do Lago**,
**Jaskier/Dandelion**, o Rei Pescador, o culto vodyanoi.
- **A escolha:** o destino de Alvin e a quem entregar a criança.
- **Ritmo:** o pior capítulo. Longo, pastoral, com muito ir-e-vir e pouca pressão
  narrativa. É aqui que a maioria dos jogadores abandona o jogo.

### Capítulo V — Vizima Antiga
Zona de guerra e epidemia. A cidade em chamas, Ordem contra Scoia'tael em combate aberto.
Bosses: **Azar Javed** e depois **Jacques de Aldersberg**, na visão gelada do futuro dele.
- **A escolha final:** apoiar a Ordem, os Scoia'tael, ou matar os dois líderes (neutro).
- **Ritmo:** claramente cortado. Curto, corredor, resolve rápido demais o que foi
  construído por 30 horas.

### Epílogo
Tentativa de assassinato contra Foltest. Geralt intervém. Gancho direto para o TW2.

## 3. Sistemas do original, destrinchados

### 3.1 Combate — o "clique ritmado"

Como funciona de fato:
1. Você escolhe a **arma**: espada de **aço** (humanos e não-monstros) ou de **prata**
   (monstros). Usar a errada corta o dano drasticamente.
2. Você escolhe a **postura**, três por arma:
   - **Forte** — dano alto, lento, contra inimigos blindados ou lentos
   - **Rápida** — dano baixo, cadência alta, contra inimigos ágeis
   - **Grupo** — ataque em área, contra três ou mais inimigos
3. Você clica no inimigo. Geralt inicia uma sequência automática. Quando o cursor
   **inflama**, você clica de novo para continuar o combo. Errar o tempo quebra a corrente.
4. Cada estilo (seis combinações) sobe de nível separadamente e ganha novos golpes.

O que isso acerta: a fantasia de um esgrimista treinado, não de um brigão. A leitura de
inimigo (postura certa) é decisão real e tem profundidade.

O que erra: o input é **autorização, não ação**. Você não erra um golpe, você erra um
clique de metrônomo. Não há esquiva, aparo ou posicionamento significativo. Depois de
cinco horas seu corpo automatiza o ritmo e o combate se torna ruído.

### 3.2 Sinais

Cinco, custeados por **Vigor**:

| Sinal | Efeito | Uso tático |
|---|---|---|
| **Aard** | Rajada telecinética, derruba ou atordoa | Abre o inimigo para finalização |
| **Igni** | Explosão de fogo em cone | Dano direto, aplica queimadura |
| **Quen** | Escudo temporário | Absorve um golpe |
| **Axii** | Confunde ou domina a mente | Remove um inimigo do combate, e diálogo |
| **Yrden** | Armadilha no chão | Controle de área |

No original os sinais são fracos e subutilizados: o dano de espada domina. Axii em
diálogo é a melhor ideia do sistema e é usada pouquíssimo.

### 3.3 Alquimia

O sistema mais original do jogo e o pior comunicado.

**Gramática:** cada ingrediente carrega uma ou mais de nove **substâncias**.
- Primárias: **Vitriol**, **Rebis**, **Aether**, **Quebrith**, **Hydragenum**, **Vermilion**
- Secundárias: **Rubedo**, **Nigredo**, **Albedo**

Uma receita exige N substâncias específicas mais uma **base alcoólica** (Alcohest,
Espírito Anão, vodka de Rivia). A qualidade da base determina se a poção sai comum,
aprimorada ou excelente.

**Ervas relevantes:** celandina, balisse, verbena, fibra-han, aloé-do-lobo, briônia,
berbercano, flor de cravo-de-bruxa, pétalas de ginátia, cogumelo sewant, raiz de
mandrágora, mirto branco, mofo verde, olho-de-corvo, cortinário, pétalas de heléboro.

**Poções canônicas** (as que importam):

| Poção | Efeito |
|---|---|
| **Andorinha** | Regeneração de vitalidade |
| **Gato** | Visão noturna |
| **Coruja** | Regeneração de vigor |
| **Nevasca** | Reflexos acelerados, slow-motion |
| **Trovão** | Mais dano de ataque |
| **Lua Cheia** | Mais vitalidade máxima |
| **Lobo** | Bônus de estilo rápido |
| **Papa-figo** | Imunidade a veneno |
| **Filtro de Petri** | Mais intensidade de sinais |
| **Decocção de Raffard Branco** | Cura forte instantânea, toxicidade alta |
| **Salgueiro** | Resistência a atordoamento |
| **Sangue Negro** | Dano refletido em vampiros e necrófagos |

**Toxicidade:** cada poção adiciona toxicidade. Acima de um limite, você perde vida. O
limite sobe com talentos.

**Fricção crítica:** poções só podem ser bebidas em **meditação**, ou seja, fora de
combate e em pontos específicos. Se você entrar em uma luta despreparado, não há
recuperação — você morre ou foge. Isso é ao mesmo tempo a melhor ideia do sistema
(preparação importa) e a pior execução (punição sem sinalização prévia).

Existem também **óleos de lâmina** por classe de monstro, e **bombas**.

### 3.4 Progressão

- Ganha-se **Talentos** ao subir de nível, em três raridades: **Bronze**, **Prata** e **Ouro**.
- Gastos em quatro categorias:
  1. **Atributos** — Força, Destreza, Vigor, Inteligência
  2. **Estilos de espada** — seis árvores (aço e prata × forte, rápido e grupo)
  3. **Sinais** — cinco árvores
  4. **Perícias profissionais** — alquimia, resistências e afins
- Talentos são gastos em **meditação**, numa tela de constelações do corpo de Geralt.
- **Troféus** de monstros dão bônus passivos quando equipados.

O problema: é uma grade enorme de mais 2% e sem sabor. Quase nenhum talento muda **como**
você joga, quase todos mudam **quanto** você faz. Um talento bom deveria abrir uma opção
nova, não engrossar um número.

### 3.5 Diálogo, escolha e reputação

- Diálogo em lista de opções, com checagem ocasional de Axii ou de atributo.
- O eixo de alinhamento Ordem / Neutro / Scoia'tael é rastreado silenciosamente.
- Escolhas grandes têm **consequência atrasada** de propósito, às vezes capítulos depois.
  Esse é o traço de design mais valioso do jogo inteiro e precisa ser preservado.

### 3.6 Minijogos

Pôquer de dados, briga de punhos e competição de bebida. Simples, charmosos, opcionais.

### 3.7 Cartas de sexo

Recompensas colecionáveis por encontros sexuais. Datado, e tratado pela imprensa e pelos
próprios devs do remake como algo a remover. **Sai do projeto.** O sistema de vínculos que
substitui isso está em `06_NARRATIVA_E_QUESTS.md`.

## 4. Inventário de defeitos (a lista de trabalho do remaster)

Ordenado por impacto no jogador.

| # | Defeito | Gravidade | Onde é resolvido |
|---|---|---|---|
| D1 | Combate é metrônomo, não ação | Crítica | `03_COMBATE.md` |
| D2 | Talentos são grade de porcentagem sem sabor | Crítica | `04_PROGRESSAO_SKILLTREES.md` |
| D3 | Alquimia mal comunicada, preparação sem sinalização | Alta | `05_ALQUIMIA_SINAIS_ITENS.md` |
| D4 | Capítulo IV arrasta, muito backtracking | Alta | `06_NARRATIVA_E_QUESTS.md` |
| D5 | Capítulo V truncado | Alta | `06_NARRATIVA_E_QUESTS.md` |
| D6 | Sinais irrelevantes perto do dano de espada | Alta | `03_COMBATE.md` |
| D7 | Diário de quests confuso | Média | `07_ARQUITETURA_TECNICA.md` |
| D8 | Bestiário é enciclopédia passiva, não ferramenta | Média | `03_COMBATE.md` |
| D9 | Gestão de inventário tediosa | Média | `05_ALQUIMIA_SINAIS_ITENS.md` |
| D10 | Cartas de sexo | Média | Removido |
| D11 | Sem movimento vertical | Baixa | Fora de escopo |
| D12 | Load times e transições de zona | Baixa | Addressables |
| D13 | Encontros repetem as mesmas três composições | Média | `03_COMBATE.md` |

## 5. Ativos de design a preservar intactos

Cuidado para não modernizar isso fora do jogo.

1. **A escolha da Abigail** e o molde dela.
2. **Aço contra prata** como decisão constante.
3. A **gramática de substâncias** da alquimia.
4. **Consequência atrasada** de escolhas.
5. **Axii em diálogo** — expandir, não cortar.
6. O **doppler que se passa por Raymond**, a melhor reviravolta do jogo.
7. O tom: fantasia suja, política, adulta e sem heróis.
