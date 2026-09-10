# ADR 0004 — Usar com.unity.behavior para IA de inimigos

Data: 2026-09-09
Status: aceita

## Contexto
Inimigos precisam de patrulha, deteccao, papeis de matilha e coordenacao por attack token.
Escrever uma FSM por tipo de inimigo nao escala; escrever um sistema de behavior tree
proprio e um subprojeto.

## Decisao
Usar o pacote oficial `com.unity.behavior` com editor de grafo, mais
`com.unity.ai.navigation` para NavMesh.

## Consequencias
- Comportamentos reutilizaveis e visualizaveis, com debug em runtime.
- Um pacote oficial relativamente novo; risco de mudancas de API.
- A FSM escrita a mao fica reservada apenas para o **jogador**, onde o controle de frame
  precisa ser exato (ADR implicita: jogador nao usa behavior tree).

## Alternativas consideradas
- **FSM propria para inimigos**: descartada por nao escalar com o numero de arquetipos.
- **Behavior Designer (pago)**: descartado; o pacote oficial e gratuito e suficiente.
- **GOAP**: descartado, complexidade injustificada nesta escala.
