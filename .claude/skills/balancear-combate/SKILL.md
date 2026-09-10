---
name: balancear-combate
description: Ajusta números de combate preservando a razão de 5,3 vezes entre jogador preparado e despreparado, que é o pilar central do jogo expresso em matemática. Use sempre que for mexer em dano, vida, armadura, custo de vigor, multiplicador de postura, bônus de óleo ou de bestiário, tempo de morte, ou quando alguém disser "está muito difícil", "está fácil demais", "esse inimigo é esponja", "o combate está estranho". Use antes de mudar qualquer valor: uma mudança isolada em um pipeline multiplicativo de onze estágios propaga de formas não óbvias.
---

# Balancear combate

## A conta que o jogo inteiro protege

O dano passa por onze estágios multiplicativos, na ordem do `docs/03` seção 9. As duas
pontas que importam, contra um barghest:

```
preparado    prata, postura Rápida, óleo de besta, bestiário pesquisado, Fluxo 3
             1,0 × 0,75 × 1,3 × 1,0 × 1,20 × 1,5 × 1,25 = 2,19x

despreparado aço, postura Forte, sem óleo, sem pesquisa
             1,0 × 1,45 × 0,8 × 0,35 × 1,0 × 1,0 × 1,0 = 0,41x
```

**A razão é 5,3.** Ela não é um número arbitrário: é o pilar 2 escrito em matemática.
Quem faz o trabalho de bruxo vence, quem improvisa apanha.

Antes e depois de qualquer ajuste, recalcule essa razão. Se ela caiu muito, o jogo virou
um jogo de reflexo e a preparação virou decoração. Se subiu demais, jogar sem preparo
deixa de ser difícil e passa a ser impossível, o que também é ruim: o jogador precisa
conseguir errar e sentir o erro, não bater em parede.

## Onde cada multiplicador vive

| # | Estágio | Faixa | Onde ajustar |
|---|---|---|---|
| 1 | dano base da arma | — | `WeaponDef` |
| 2 | postura | 0,75 a 1,45 | `docs/03` seção 4 |
| 3 | afinidade de postura | 0,8 / 1,0 / 1,3 | `docs/03` seção 4 |
| 4 | material aço ou prata | 0,35 a 1,0 | `docs/03` seção 3 |
| 5 | Fluxo | 1,0 a 1,35 | `docs/03` seção 6 |
| 6 | óleo | 1,0 ou 1,5 | `OilDef` |
| 7 | bestiário pesquisado | 1,0 ou 1,25 | `docs/02` seção 7 |
| 8 | poções | varia | `PotionDef` |
| 9 | crítico | 2,0 | `docs/03` |
| 10 | armadura do alvo | subtração plana | `MonsterDef` |
| 11 | resistência por tipo | varia | `MonsterDef` |

A armadura é **subtração plana com mínimo de 1**, não percentual. Isso significa que ela
pune desproporcionalmente a postura Rápida, que bate fraco e rápido. É intencional e é o
que dá função à postura Forte contra alvos blindados. Não "conserte" isso sem perceber
que está removendo uma decisão do combate.

## Alvos de tempo de morte

Do `docs/03` seção 12:

| Situação | Alvo |
|---|---|
| Inimigo comum, preparado | 3 a 5 golpes |
| Inimigo comum, despreparado | 12 a 18 golpes |
| Jogador morre, sem armadura | 5 golpes |
| Jogador morre, armadura leve | 8 golpes |
| Boss, preparado | 90 a 150 s |

Os 12 a 18 golpes do despreparado **devem parecer errados**. Essa sensação é o sinal que
ensina o jogador a se preparar. Quando reclamarem que um inimigo é esponja, a primeira
pergunta é se o jogador estava preparado. Se estava e ainda assim demorou, aí sim há
problema de número.

## Antes de mudar um valor

1. **Reproduza.** Rode o cenário na cena `Sandbox_Combate` e olhe o log do pipeline de
   dano, que registra cada multiplicador aplicado. O problema quase sempre aparece ali,
   e quase nunca é onde a intuição aponta.
2. **Isole.** Em um pipeline multiplicativo, mudar dois estágios de uma vez torna
   impossível saber qual resolveu. Mude um, meça, depois o próximo.
3. **Prefira mexer no específico.** Ajustar `MonsterDef` de uma criatura afeta uma luta.
   Ajustar um multiplicador de postura afeta o jogo inteiro e todos os outros números
   passam a estar errados.

## Antes de fechar

1. **Recalcule a razão preparado contra despreparado.**
2. **Rode os testes do pipeline de dano.** Os dois cenários de referência, 2,19x e 0,41x,
   são testados. Se eles falharam, ou o balanceamento mudou de propósito e o teste precisa
   ser atualizado com justificativa, ou a mudança quebrou algo.
   ```bash
   .claude/skills/unity-batch/scripts/unity.sh test all
   ```
3. **Atualize `design/combate/balanceamento.csv`.** O CSV é a fonte para conferência
   rápida; se ele divergir dos assets, ninguém confia mais em nenhum dos dois.
4. **Verifique os cinco arquétipos de build** do `docs/04` seção 10. Todos precisam
   conseguir vencer A Besta. Se um deixou de conseguir, o ajuste fechou uma porta.
5. **Linha no `CHANGELOG.md`**, dizendo o que mudou e por quê.

## Quando o problema não é número

Setenta por cento da sensação de combate vem do game feel, não do balanceamento
(`docs/03` seção 11). Se a reclamação é "o combate parece estranho", "não tem peso" ou
"parece que não estou acertando", verifique estes antes de tocar em qualquer valor:

- Buffer de input de 0,2 s existe e funciona
- Hitstop de 0,08 s no golpe forte, 0,04 no leve
- Screen shake proporcional ao dano
- Partícula e som por material do alvo
- Anticipação em toda animação de golpe
- Tell de inimigo entre 0,4 e 0,9 s, visualmente distinto por ataque

Um combate com números perfeitos e sem nenhum destes parece quebrado. Um combate com
números medianos e todos estes parece bom.
