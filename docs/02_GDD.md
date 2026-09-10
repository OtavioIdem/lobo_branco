# 02 — Documento de Design (GDD)

Documento consolidado. Os sistemas grandes têm doc próprio; aqui fica a visão de conjunto
e tudo que não merece arquivo separado.

## 1. Resumo em uma linha

Um caçador de monstros mutado investiga uma conspiração que roubou os segredos da sua
ordem, numa terra em guerra civil onde nenhum lado merece vencer.

## 2. O loop de jogo

### Loop macro (por capítulo, 3 a 6 horas)
```
Chegar na região  →  Ler o clima político  →  Aceitar contratos e a quest principal
      ↓                                                        ↓
Escolha estrutural do capítulo  ←  Confrontar o antagonista local  ←  Investigar
      ↓
Consequência semeada (colhida 1 a 2 capítulos depois)
```

### Loop médio (por contrato, 20 a 45 minutos) — este é o coração do jogo
```
1. ACEITAR   Quadro de contratos ou NPC. Pagamento negociável.
2. INVESTIGAR  Cena do crime. Sentidos de Bruxo. Colher 3 de 5 pistas possíveis.
3. DEDUZIR   Cruzar pistas no Bestiário. Identificar a espécie. Erro é possível.
4. PREPARAR  Óleo, poções, bombas, sinal escolhido. Meditar.
5. CAÇAR     Rastrear até o covil. Combate.
6. RESOLVER  Prova (troféu), pagamento, e às vezes uma escolha moral no fim.
```
A etapa 3 é o que diferencia este jogo. **Você pode deduzir errado**, comprar o óleo
errado e descobrir isso no meio da luta. Não há confirmação automática.

### Loop micro (combate, 10 a 90 segundos)
```
Ler o inimigo → escolher postura e arma → abrir com sinal → janela de dano
    → gerenciar vigor e toxicidade → reagir ao tell → repetir
```

## 3. Estados do jogo e telas

| Estado | Descrição |
|---|---|
| `Exploration` | Câmera livre, terceira pessoa, movimento completo |
| `Combat` | Trava opcional em alvo, HUD expandido, postura visível |
| `Dialogue` | Câmera cinemática, tempo pausado, opções |
| `Meditation` | Menu de alquimia, talentos, descanso, avanço de tempo |
| `Investigation` | Sentidos de Bruxo ativos, tempo real, pistas destacadas |
| `Menu` | Inventário, bestiário, diário, mapa, opções |

## 4. Controles (teclado e mouse, e gamepad)

| Ação | Teclado/Mouse | Gamepad |
|---|---|---|
| Mover | WASD | Analógico esquerdo |
| Câmera | Mouse | Analógico direito |
| Ataque leve | Botão esquerdo | X / Quadrado |
| Ataque forte | Botão direito | Y / Triângulo |
| Esquiva | Espaço | B / Círculo (toque) |
| Rolamento | Espaço duplo | B / Círculo (segurar) |
| Aparo / bloqueio | Shift esquerdo | LB / L1 |
| Lançar sinal | Q | RB / R1 |
| Roda de sinais | Segurar Q | Segurar RB |
| Trocar espada | 1 (aço) / 2 (prata) | Direcional esquerda/direita |
| Trocar postura | Roda do mouse | Direcional cima/baixo |
| Sentidos de Bruxo | Ctrl esquerdo | Analógico direito (clique) |
| Consumível rápido | R | Direcional baixo |
| Interagir | E | A / X |
| Meditar | M | — (via menu) |

Tudo via **Unity Input System** com rebind em runtime desde o começo. Retrofitar isso é
sofrimento; fazer certo no dia 1 custa duas horas.

## 5. Atributos e estatísticas do personagem

Quatro atributos, herdados do original mas com funções redesenhadas para que cada um
mude uma decisão de jogo, não só um número.

| Atributo | Governa |
|---|---|
| **Força** | Dano de postura Forte, poder de aparo, capacidade de carga |
| **Destreza** | Dano de postura Rápida, janela de esquiva, chance de crítico |
| **Vigor** | Vigor máximo e regeneração, resistência a atordoamento |
| **Inteligência** | Intensidade de sinais, limite de toxicidade, qualidade de dedução |

Estatísticas derivadas:

| Estatística | Base | Fonte de crescimento |
|---|---|---|
| Vitalidade | 100 | Nível, Lua Cheia, mutações |
| Vigor | 100 | Vigor (atributo), Coruja |
| Toxicidade máx. | 60 | Inteligência, talentos de alquimia |
| Adrenalina | 0 a 3 cargas | Ganha em combate, gasta em finalizações |
| Peso de carga | 60 | Força |

## 6. Progressão de nível

- 20 níveis no jogo completo, 5 no protótipo.
- XP vem de: quests (70%), contratos (20%), monstros novos no bestiário (10%).
  **Matar monstro repetido não dá XP.** Isso mata o grind e reforça o pilar P1.
- Por nível: 1 ponto de atributo + 1 ponto de trilha.
- Cada 4 níveis: 1 slot de mutagênio.

## 7. Interface

### HUD em combate (canto inferior esquerdo, minimalista)
- Barra de vitalidade, barra de vigor sobreposta
- Anel de toxicidade em volta do retrato (verde → âmbar → vermelho)
- Ícone de espada ativa (aço ou prata) e postura ativa
- Sinal selecionado, com cooldown
- Cargas de adrenalina

### Fora de combate
HUD oculto por padrão. Aparece por 3 segundos ao mudar de estado. Bússola no topo apenas
se ativada nas opções — o padrão é navegar pelo mundo, não pela seta.

### Bestiário — a tela mais importante do jogo
Cada entrada tem quatro seções, e as três últimas ficam **bloqueadas** até você colher
as pistas correspondentes:
1. **Aparência** (destravada ao ver)
2. **Comportamento** (destravada por observação ou 2 pistas)
3. **Vulnerabilidades** (destravada por 3 pistas ou por um livro) — **isto concede o
   bônus de dano mecanicamente, não só informação**
4. **Origem** (lore, opcional, para colecionadores)

Um monstro com Vulnerabilidades destravadas recebe de você um multiplicador de dano
adicional. Ler o bestiário é literalmente uma mecânica de dano.

## 8. Economia

Moeda: **orens**.

| Fonte | Faixa por hora |
|---|---|
| Contratos | 150 a 400 orens |
| Venda de peles e troféus | 50 a 120 |
| Loot de humanos | 30 a 80 |
| Achados no mundo | 20 a 60 |

| Sumidouro | Custo típico |
|---|---|
| Base alcoólica de qualidade | 30 a 90 |
| Receita de alquimia (comprada) | 100 a 250 |
| Reparo de espada | 40 a 80 |
| Suborno / informação | 50 a 300 |
| Espada nova | 400 a 1200 |

A economia é apertada de propósito no Capítulo I e afrouxa a partir do III. O jogador
deve sentir que aceitar um contrato ruim por dinheiro é uma decisão real.

## 9. Progressão de dificuldade

Três dificuldades, e elas mudam **regras**, não só números:

| Dificuldade | Diferença |
|---|---|
| **Caminho Fácil** | Poções podem ser bebidas em combate. Pistas brilham. Dano reduzido. |
| **A Trilha** (padrão) | Regras descritas neste GDD. |
| **Caminho do Bruxo** | Sem marcador de quest. Vulnerabilidade errada causa dano 0,25x. Morte de aliado é permanente. |

## 10. Estrutura de conteúdo do jogo completo (referência de longo prazo)

| Capítulo | Zona | Horas | Contratos | Escolha estrutural |
|---|---|---|---|---|
| Prólogo | Kaer Morhen | 1 | 0 | — |
| I | Arredores de Vizima | 5 | 3 | Abigail |
| II | Bairro do Templo + Pântano | 7 | 5 | Ordem / Scoia'tael / Neutro |
| III | Bairro do Comércio | 6 | 4 | Adda: curar ou matar |
| IV | Águas Turvas + Lago | 5 | 4 | Destino de Alvin |
| V | Vizima Antiga | 5 | 2 | Alinhamento final |
| Epílogo | Palácio | 1 | 0 | — |

Total alvo: **30 horas**, contra as 40 do original. O corte vem de D4 e D9.

## 11. Mudanças autorais principais (o "seu" jogo)

Lista consolidada do que este projeto faz e o original não fazia:

1. **Combate de ação real** com esquiva, aparo, riposte e Fluxo opcional (`03`).
2. **Sistema de Fluxo** — o clique ritmado do original volta como bônus opcional de dano,
   não como requisito. Homenagem sem a prisão.
3. **Cinco trilhas de habilidade novas**, com nós que abrem opções em vez de porcentagens (`04`).
4. **Mutações com slots** — build reconfigurável em meditação.
5. **Toxicidade como recurso de poder**, não só como punição (`05`).
6. **Bestiário mecanicamente relevante** — pesquisa concede dano.
7. **Dedução falível** — você pode errar a identificação da espécie.
8. **Ecos de escolha** — sistema formal de consequência atrasada com flags de mundo (`06`).
9. **Reputação sistêmica visível** — a cidade muda de comportamento (`06`).
10. **Quadro de contratos com contratos gerados**, para eliminar backtracking vazio.
11. **Vínculos** substituem cartas de sexo — relacionamentos com estado e consequência.
12. **Sem XP por monstro repetido** — remove o grind.

## 12. Perguntas de design ainda abertas

Registradas de propósito. Serão respondidas por protótipo, não por discussão.

| # | Pergunta | Como responder |
|---|---|---|
| Q1 | O Fluxo confunde quem não conhece o original? | Playtest com 3 pessoas, greybox |
| Q2 | Toxicidade como buff incentiva jogo suicida chato? | Protótipo de combate isolado |
| Q3 | Dedução falível frustra ou engaja? | Playtest do contrato da Besta |
| Q4 | Trava de alvo ajuda ou atrapalha contra grupos? | Protótipo com 5 barghests |
| Q5 | Câmera isométrica opcional vale o custo? | Adiado — decidir após o slice |
