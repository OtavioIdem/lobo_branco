# ADR 0005 — Sistemas agnosticos de propriedade intelectual

Data: 2026-09-09
Status: aceita

## Contexto
O projeto e derivado de uma IP de terceiros (CD PROJEKT e Andrzej Sapkowski). Ele existe
por tolerancia, nao por direito (doc 10). Ao mesmo tempo, o valor real do trabalho esta na
engenharia e no design, nao nos nomes.

## Decisao
Nenhum nome do universo original aparece em codigo, nome de classe, enum, constante ou
nome de arquivo de script. Esses nomes existem **apenas** em assets de dados
(ScriptableObject) e em tabelas de localizacao.

Exemplos:
- `class PlayerCharacter`, nao `class GeraltController`
- `class KnockbackSign`, com o nome "Aard" vindo de um asset
- `MonsterDef` asset "Barghest", nao `enum MonsterType { Barghest }`
- Chave de localizacao `class.witcher.name`, nao a string "Bruxo" no codigo

## Consequencias
- Se houver pedido de remocao, trocar uma pasta de dados e um arquivo de localizacao
  produz um jogo proprio, preservando toda a engenharia.
- Se o projeto virar comercial, o caminho existe sem reescrever codigo.
- E melhor engenharia de qualquer forma: dados fora do codigo e o principio central do doc 07.
- Custo: disciplina de nomenclatura constante. Praticamente zero em esforco.

## Alternativas consideradas
- **Nomear tudo pelo original**: mais rapido de escrever, e destruiria a possibilidade de
  reaproveitamento. Descartado.
