# 00 — Visão e Escopo

## 1. O contexto real (setembro/2026)

Antes de tudo, um ajuste factual, porque isso muda como você posiciona o projeto:

O remake oficial de The Witcher 1 (codinome interno **Canis Majoris**, anunciado em
outubro de 2022, feito pela **Fool's Theory** em **Unreal Engine 5** sob supervisão da
CD PROJEKT RED) **não foi cancelado**. Em 3 de setembro de 2026 a CDPR confirmou que ele
está **em pausa**, por dois motivos:

1. A Fool's Theory foi realocada para liderar a DLC **Songs of the Past** de The Witcher 3
   (prevista para 2027) e para apoiar The Witcher 4 (previsto para 2028).
2. O remake é tecnicamente **dependente** de The Witcher 4 — ele espera as ferramentas,
   os assets e os pipelines de UE5 que estão sendo construídos para o 4.

A retomada só é esperada depois de 2028. Ou seja: a vaga que você identificou existe, mas
ela é uma **pausa longa**, não um abandono. Isso não invalida nada do projeto — só evita
que você repita uma informação errada ao descrever o que está fazendo.

## 2. O que é este projeto

Um **protótipo jogável de RPG de ação em terceira pessoa** que usa a estrutura, o
bestiário e o arco narrativo de The Witcher (2007) como esqueleto, e substitui os
sistemas datados por sistemas próprios.

Não é um remake fiel. É um **remaster autoral**: mesma espinha dorsal, músculo novo.

### O que "autoral" significa em termos concretos

| Camada | Fidelidade ao original | Justificativa |
|---|---|---|
| Estrutura de capítulos | Alta | Funciona e é o que dá identidade |
| Arco narrativo principal | Alta, com mudanças de ritmo | O mistério da Salamandra é bom |
| Bestiário e ecologia | Alta | É o coração da fantasia "bruxo" |
| Combate | **Reescrito** | O original é o defeito mais citado |
| Progressão / talentos | **Reescrito** | Pedido explícito do escopo |
| Alquimia | Reescrita, mesma gramática | Mantém substâncias, muda o loop |
| Quests secundárias | Reescritas | Muitas são fetch quests |
| Diálogo e romance | Reescrito | Cartas de sexo saem |

## 3. Pilares de design

Todo recurso que entra no jogo tem que servir a pelo menos um destes quatro pilares.
Se não serve, não entra. Isso é a única defesa que um dev solo tem contra escopo infinito.

### P1 — O bruxo é um investigador, não um tanque
Matar o monstro é o clímax, não a atividade. O ciclo é: aceitar o contrato → investigar a
cena → identificar a espécie → ler o bestiário → **preparar** → caçar. Quem pula a
preparação enfrenta um inimigo com 3x a vida efetiva.

### P2 — Preparação vale mais que reflexo
Óleo correto na lâmina, poção certa no sangue e sinal adequado devem valer mais do que
executar um dodge perfeito. O jogo tem que premiar quem pensa antes, não só quem clica bem.
Isso é o que separa esta fantasia de um Souls-like genérico.

### P3 — Escolha sem moral limpa, consequência atrasada
A escolha da Abigail no Capítulo I é o melhor momento de design do jogo original: você
escolhe entre duas opções ruins, sem informação completa, e a consequência aparece horas
depois. Toda escolha grande do jogo segue esse molde. Nenhum indicador de "boa/má opção".

### P4 — Vizima é um personagem
O hub urbano reage ao seu alinhamento de forma sistêmica e visível: patrulhas, preços,
quem fala com você, quem atravessa a rua. A cidade é o placar do jogo.

## 4. Público e referências

- **Público:** quem jogou The Witcher 3 e nunca conseguiu terminar o 1.
- **Referências de combate:** Witcher 3 (base), Kingdom Come: Deliverance (peso e
  compromisso da animação), Dragon's Dogma 2 (fraquezas de monstro que importam).
- **Referências de investigação:** Return of the Obra Dinn (dedução real, não trilha de
  migalhas), Pentiment (consequência de interpretação).
- **Referências de estrutura:** Gothic 2 (mundo pequeno, denso, hostil no começo).

## 5. Escopo do protótipo (o que você vai realmente construir)

O protótipo é o **Capítulo I — Arredores de Vizima** e nada mais.

**Dentro do escopo:**
- 1 zona de vilarejo (hub social, ~5 NPCs com diálogo funcional)
- 1 zona selvagem (floresta + cemitério + cripta)
- 1 contrato completo, com o ciclo investigativo inteiro: **A Besta dos Arredores**
- 3 espécies de monstro (barghest, afogado, ghoul) + 1 boss
- Combate completo: 2 espadas, 3 posturas, esquiva, aparo, 3 sinais
- Alquimia funcional: 6 ervas, 4 poções, 2 óleos, toxicidade
- 1 árvore de habilidade completa das 5 planejadas (Esgrima)
- Save/load, menu, HUD, bestiário, inventário
- 1 escolha estrutural com consequência dentro do próprio protótipo

> **Atualização de 2026-09-10 ([doc 13](13_COOP_E_REDE.md)):** o protótipo passou a ser
> cooperativo, de 2 a 4 jogadores em sessão privada por código de convite, com duas escolas
> jogáveis. A zona, o contrato e o combate não mudaram. Saíram do slice a escolha da Abigail,
> os Ecos e o diálogo ramificado em sessão. Onde esta seção contradiz o doc 13, vale o doc 13.

**Fora do escopo (explicitamente):**
- Capítulos II a V, epílogo, prólogo de Kaer Morhen
- Romance, minijogos (dados, briga, bebida)
- Montaria, natação, escalada
- ~~Multiplayer~~ (ver doc 13). PvP, servidor dedicado, matchmaking público e crossplay
  continuam fora
- Console, mobile, localização além de pt-BR
- Dublagem (texto apenas)
- Crafting de equipamento, encantamentos
- Ciclo de dia/noite dinâmico completo (haverá dois estados fixos: dia e noite)

## 6. Critério de sucesso

O protótipo está pronto quando: um jogador que nunca viu o projeto consegue, sozinho e
sem instrução externa, em **25 a 40 minutos**, aceitar o contrato da Besta, descobrir por
investigação que ela é vulnerável a prata e a Igni, preparar-se para isso, e vencer —
**e** falha de forma perceptível se tentar sem preparo.

Se ele vencer sem preparo, o pilar P2 falhou e o combate precisa ser rebalanceado.

## 7. Definição de pronto (DoD) para qualquer feature

1. Funciona no build de Windows, não só no editor.
2. Tem dados em ScriptableObject, não valores hardcoded em MonoBehaviour.
3. Sobrevive a um ciclo de save → sair → carregar.
4. Não gera exceção no Console durante 5 minutos de jogo.
5. Tem uma linha no CHANGELOG.
