# 05 — Alquimia, Toxicidade, Itens e Economia

Resolve os defeitos **D3** (alquimia mal comunicada) e **D9** (inventário tedioso).

## 1. Princípio

A alquimia do original é o sistema mais autoral do jogo e o mais mal explicado. O trabalho
aqui **não é simplificar** — é comunicar. A gramática de substâncias fica. O que muda é
que o jogo passa a te dizer o que você está fazendo.

## 2. A gramática de substâncias (mantida do original)

Nove substâncias. Toda erva, mineral e parte de monstro carrega de 1 a 3 delas.

| Substância | Símbolo | Associação temática |
|---|---|---|
| **Vitriol** | ♁ | Vitalidade, sangue, cura |
| **Rebis** | ⚏ | Equilíbrio, dualidade, sinais |
| **Aether** | ⚗ | Espírito, mente, percepção |
| **Quebrith** | ♄ | Resistência, endurecimento |
| **Hydragenum** | ☿ | Velocidade, fluidez, reflexo |
| **Vermilion** | ♂ | Força, fogo, agressão |
| **Rubedo** | 🜍 | Secundária — potência |
| **Nigredo** | 🜛 | Secundária — dissolução, veneno |
| **Albedo** | 🜔 | Secundária — purificação |

### Ervas do protótipo (6 apenas)

| Erva | Substâncias | Onde é encontrada | Bioma |
|---|---|---|---|
| **Celandina** | Vitriol, Aether | Comum, clareiras | Floresta |
| **Balisse** | Vitriol, Rubedo | Perto de água | Rio, pântano |
| **Verbena** | Hydragenum, Albedo | Comum, campo aberto | Vilarejo |
| **Aloé-do-lobo** | Quebrith, Vitriol | Rara, ruínas | Cemitério |
| **Mirto branco** | Aether, Nigredo | Rara, à noite | Cemitério |
| **Berbercano** | Vermilion, Rubedo | Comum, encostas | Floresta |

### Bases alcoólicas

| Base | Qualidade resultante | Preço | Onde |
|---|---|---|---|
| Vodka rústica | Comum (1,0x) | 15 | Qualquer taverna |
| Alcohest | Aprimorada (1,4x) | 45 | Alquimista |
| Espírito Anão | Excelente (1,9x) | 110 | Mercador anão, raro |

## 3. A tela de alquimia reescrita (resolve D3)

O original te dava uma lista e te deixava adivinhar. A tela nova tem três painéis:

```
┌──────────────┬───────────────────────┬──────────────┐
│  RECEITAS    │   BANCADA             │ INGREDIENTES │
│              │                       │              │
│ ▸ Andorinha  │  Precisa:             │ Celandina x4 │
│   Coruja     │   ♁♁  Vitriol x2      │  ♁ ⚗         │
│   Gato       │   ⚗   Aether  x1      │              │
│   Trovão     │                       │ Balisse x2   │
│              │  Você tem:            │  ♁ 🜍        │
│              │   ♁♁♁♁♁♁ ⚗⚗ 🜍🜍       │              │
│              │                       │ Verbena x7   │
│              │  ✔ PODE PRODUZIR      │  ☿ 🜔        │
│              │  Base: Alcohest ▾     │              │
│              │  Rende: 3 doses       │              │
└──────────────┴───────────────────────┴──────────────┘
```

Três regras de comunicação, e elas são o conserto do sistema:
1. **O que você tem em substâncias fica sempre visível**, não escondido no inventário.
2. **Receitas impossíveis mostram exatamente o que falta**, em vermelho.
3. **Auto-seleção de ingredientes** prioriza os mais abundantes e menos valiosos.
   Sem microgestão obrigatória, mas com override manual disponível.

## 4. Poções do protótipo (4)

| Poção | Receita | Efeito | Duração | Toxicidade |
|---|---|---|---|---|
| **Andorinha** | Vitriol x2, Aether x1 | Regenera 3 de vitalidade por segundo | 300 s | 15 |
| **Coruja** | Hydragenum x2, Albedo x1 | Regeneração de vigor +12/s | 240 s | 12 |
| **Gato** | Aether x2, Nigredo x1 | Visão noturna; cavernas e noite ficam navegáveis | 300 s | 10 |
| **Trovão** | Vermilion x2, Rubedo x1 | +30% de dano de ataque | 180 s | 25 |

### Óleos do protótipo (2)

| Óleo | Receita | Classe alvo | Bônus |
|---|---|---|---|
| **Óleo de Besta** | Vermilion x1, Quebrith x1 | Bestas (barghest, lobo, A Besta) | 1,5x |
| **Óleo de Necrófago** | Nigredo x2 | Necrófagos (ghoul, alghoul, afogado) | 1,5x |

Aplicar óleo leva 2 s e não pode ser feito em combate sem o nó *Trago Rápido*.

## 5. Toxicidade — de punição a recurso

No original a toxicidade é só um teto. Aqui ela é uma curva com três zonas, e a trilha de
Alquimia move a fronteira entre elas.

| Zona | Faixa | Efeito base | Com *Sangue de Bruxo* |
|---|---|---|---|
| **Limpo** | 0–30% | Nenhum | Nenhum |
| **Carregado** | 30–50% | Leve dessaturação da tela | Nenhum |
| **Envenenado** | 50–85% | −2 de vitalidade por segundo; vinheta escura | **+25% de dano**, sem perda de vitalidade |
| **Crítico** | 85–100% | −6 por segundo; visão embaçada, áudio abafado | Habilita **Frenesi** com *Êxtase* |

A toxicidade decai a 1 por segundo fora de combate, e a 0,3 por segundo em combate.
Meditar zera com o nó *Meditação Profunda*.

**Por que isso é melhor:** transforma uma restrição em uma decisão. "Bebo a quarta poção e
aceito viver perto da morte, ou entro na luta mais fraco e mais seguro?" O original só
permitia a segunda.

## 6. Bombas (2 no protótipo)

| Bomba | Receita | Efeito |
|---|---|---|
| **Fogo Grego** | Vermilion x2, Rubedo x1 | 40 de dano em área de 4 m, incendeia o chão por 8 s |
| **Pó Cegante** | Albedo x2, Aether x1 | Cega inimigos por 5 s; habilita *Predador* |

## 7. Inventário (resolve D9)

O que muda em relação ao original:

1. **Peso só se aplica a equipamento e loot de venda.** Ervas, poções, receitas e
   ingredientes de alquimia têm peso zero. O tédio de descartar ervas desaparece.
2. **Abas separadas:** Equipamento, Alquimia, Contratos, Loot, Missão. Nunca uma grade única.
3. **Stack infinito** para consumíveis.
4. **Botão "vender tudo de loot"** no mercador, com confirmação.
5. **Alforje do cavalo** não existe no protótipo, mas o baú da estalagem existe, para
   estocar loot valioso quando o peso aprertar.

## 8. Equipamento

O protótipo mantém deliberadamente simples: **não há crafting de equipamento.**

| Slot | Opções no protótipo |
|---|---|
| Espada de aço | 3 níveis: enferrujada, de bruxo, de Mahakam |
| Espada de prata | 2 níveis: de bruxo, prata rúnica |
| Armadura | 3 níveis: casaco de viagem, jaqueta de bruxo, gibão reforçado |
| Luvas, botas, calças | Fixos, cosméticos no protótipo |

Espadas e armaduras têm **durabilidade**. A zero, perdem 50% de eficácia. Reparo em
ferreiro ou com kit de reparo.

## 9. Economia detalhada do protótipo

O jogador começa com **80 orens**. Alvos de calibragem:

| Momento | Orens esperados | Decisão que o jogo força |
|---|---|---|
| Início | 80 | Comprar Alcohest (45) ou guardar? |
| Após primeiro contrato | 250 | Espada de bruxo (400) exige mais um contrato |
| Antes da Besta | 400 a 600 | Óleo, poções e reparo consomem cerca de 200 |
| Fim do protótipo | 700 a 1.000 | — |

Regra de calibragem: o jogador deve poder comprar **cerca de 70%** do que quer. Nunca 100%,
nunca 30%.

## 10. Mercadores do protótipo

| NPC | Vende | Compra |
|---|---|---|
| **Haren Brogg** (comerciante) | Bases alcoólicas, kits de reparo | Loot geral, peles |
| **Abigail** (curandeira) | Ervas raras, 2 receitas | Ervas, partes de monstro |
| **Ferreiro da vila** | Espadas de aço, armadura | Metal, armas |
| **Coveiro** | Nada; troca informação por orens | Troféus (bom preço) |

## 11. Estrutura de dados (adianta o doc 07)

Todos os itens são `ScriptableObject`, nunca prefabs com valores no Inspector.

```
ItemDef (abstract)
├── EquipmentDef        slot, durabilidade, modificadores de stat
│   ├── WeaponDef       material (aço/prata), dano, slot de óleo
│   └── ArmorDef        armadura, resistências
├── ConsumableDef
│   ├── PotionDef       efeitos, duração, toxicidade, receita
│   ├── OilDef          classe alvo, multiplicador, cargas
│   └── BombDef         raio, dano, efeito de área
├── IngredientDef       lista de SubstanceType, bioma, raridade
├── TrophyDef           mutagênio associado, valor de venda
└── QuestItemDef        flag associada, não vendável
```

`SubstanceType` é um `enum` com as nove substâncias. `RecipeDef` é uma lista de
`(SubstanceType, int)` mais um `BaseQualityRequirement`. Isso torna toda a alquimia
data-driven: adicionar uma poção nova é criar um asset, não escrever código.
