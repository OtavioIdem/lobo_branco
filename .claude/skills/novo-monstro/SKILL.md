---
name: novo-monstro
description: Cria um monstro completo, do MonsterDef aos tells de ataque, entrada de bestiário, vulnerabilidades e composição de encontro. Use sempre que for adicionar qualquer inimigo ou criatura ao jogo (barghest, ghoul, afogado, alghoul, boss), quando pedirem "adiciona o monstro X", "cria um inimigo novo", ou quando um contrato precisar de uma criatura para caçar. Um monstro aqui não é só vida e dano: sem vulnerabilidade pesquisável e sem tell legível ele quebra os dois pilares centrais do jogo.
---

# Criar um monstro

## Por que um monstro é mais que estatísticas

Dois dos quatro pilares do jogo dependem de como o monstro é montado.

O pilar 1 diz que o bruxo é investigador. Isso só é verdade se a criatura tiver
vulnerabilidades **descobríveis por investigação** que concedem dano de verdade. Um
monstro sem pistas associadas é só um saco de vida.

O pilar 2 diz que preparação vale mais que reflexo. Isso só é verdade se enfrentar a
criatura despreparado for perceptivelmente pior. A conta está no `docs/03` seção 9 e a
razão entre preparado e despreparado é de 5,3 vezes.

Um monstro que não participa dos dois pilares não deveria entrar no jogo.

## As seis peças

### 1. Ficha, em `research/bestiario/`

Antes do asset, escreva a ficha: o que a criatura é, onde vive, o que ela deixa como
rastro, e o que a torna diferente das que já existem. Se a resposta da última pergunta
for "tem mais vida", volte e repense.

### 2. `MonsterDef`, em `Assets/_Project/Data/Monsters/`

```csharp
displayName          nome exibido, vindo de localização
monsterClass         Besta, Necrofago, Espectro, Insectoide, Maldito
archetype            define a postura ideal contra ele
baseVitality         ver docs/03 secao 12
baseDamage
armor                subtração plana, não percentual
vulnerableToOil      classe de óleo que multiplica por 1,5
vulnerableToSign     sinal com efeito ampliado
bestiaryEntry        as quatro seções da entrada
loot                 tabela, com o mutagênio quando houver
```

Arquétipo e postura ideal, do `docs/03` seção 4. Acertar concede 1,3x, errar 0,8x:

| Arquétipo | Postura ideal | Exemplos |
|---|---|---|
| Blindado, pesado, lento | Forte | golem, alghoul, cavaleiro |
| Ágil, esquivo | Rápida | barghest, ladino, afogado |
| Enxame, três ou mais | Grupo | matilha, corvos |

### 3. Tells de ataque

Cada ataque precisa de uma pose de anticipação de 0,4 a 0,9 s, **visualmente distinta**
das outras. Se dois ataques têm o mesmo tell, o jogador não está lendo, está adivinhando,
e o combate parece injusto mesmo com números perfeitos.

Ataques marcados `Unblockable` recebem tell vermelho e não podem ser aparados, só
esquivados. Use isso para forçar variedade de resposta, não para punir.

### 4. Entrada de bestiário, com quatro seções

As três últimas ficam bloqueadas até o jogador colher as pistas. Este é o gancho
mecânico do pilar 1:

| Seção | Como destrava | Efeito |
|---|---|---|
| Aparência | ao ver | nenhum |
| Comportamento | observação ou 2 pistas | nenhum |
| **Vulnerabilidades** | 3 pistas ou um livro | **concede 1,25x de dano** |
| Origem | opcional | lore |

Escreva também as pistas que levam até ela, e faça uma dedução errada ser plausível.
Uma pista deve apontar para duas espécies, e a terceira desempata. Detalhe no `docs/06` seção 3.

### 5. Comportamento, com behavior tree

Use `com.unity.behavior` (`tech/adr/0004`) e NavMesh. Papéis de matilha: `Solo`,
`Flanqueador`, `Cercador`, `Arremessador`.

**O coordenador de encontro concede no máximo dois tokens de ataque simultâneos.** Sem
isso, cinco barghests atacam ao mesmo tempo e o combate fica ilegível e injusto. Esse é o
truque de IA de combate mais importante que existe, e quase todo jogo de ação bom faz
alguma versão dele.

### 6. Composição de encontro

Um monstro sozinho não é conteúdo. Ele entra em pelo menos uma composição, e a regra é
que **nunca há dois encontros seguidos com a mesma composição** (defeito D13).

Cada composição ensina uma coisa. As do protótipo estão no `docs/03` seção 10:

| Nome | Composição | Ensina |
|---|---|---|
| Batedor | 1 barghest | tells e esquiva |
| Matilha | 4 barghests | postura Grupo, Aard |
| Emboscada | 2 barghests, 2 bandidos | troca de espada sob pressão |
| Guarda | 1 alghoul | quebra de guarda, postura Forte |

## Boss

Um boss precisa de fases que exijam **respostas diferentes**, não só mais vida. A Besta,
no `docs/03` seção 10, é o modelo:

1. Luta com a matilha, exige postura Grupo e Aard.
2. Fica sozinha e ganha investida `Unblockable`, exige leitura de tell e rolamento.
   Aparar aqui mata o jogador.
3. Invoca reforços e ganha regeneração **que só Igni interrompe**.

A fase 3 é o ponto: quem não trouxe Igni enfrenta uma luta injustamente longa, e a pista
disso estava na investigação. É o pilar 2 em forma de boss.

## Registrar

- Linha em `design/combate/balanceamento.csv` com vida, dano, armadura, arquétipo,
  óleo e sinal vulneráveis
- Entrada de bestiário e pistas
- Composição de encontro e onde ela aparece
- Linha no `CHANGELOG.md`

## Verificar

Os cinco arquétipos de build do `docs/04` seção 10 precisam conseguir vencer o encontro.
Se um não consegue, o problema é do monstro, não da build.

Depois: `.claude/skills/unity-batch/scripts/unity.sh test all`.
