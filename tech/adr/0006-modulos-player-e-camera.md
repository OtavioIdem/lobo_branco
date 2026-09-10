# ADR 0006 — Modulos Player e Camera separados

Data: 2026-09-09
Status: aceita

## Contexto
O doc 07 previa 12 modulos e nenhum deles cobria o personagem jogavel nem a camera.
Ao implementar movimento e camera de terceira pessoa (tarefas 0.9 a 0.11 do doc 12),
duas perguntas apareceram: onde vive o controlador do jogador, e onde vive o pivo de camera.

As opcoes eram colocar os dois em `Combat` (que ja abriga a maquina de estados do jogador
segundo o doc 07 secao 4.4), ou criar modulos proprios.

## Decisao
Criar `TW1R.Player` e `TW1R.Camera`.

- `Camera` referencia apenas `Core` e o pacote Cinemachine. Nao le input: recebe deltas
  por `ApplyLook`.
- `Player` referencia `Core`, `Stats`, `Combat` e `Camera`. E o dono do leitor de input e,
  no futuro, da FSM.
- `Player` conhece `Camera`; o inverso nunca.

## Consequencias
- O pivo de camera fica reutilizavel por cutscene, camera de dialogo e camera livre de
  debug, sem arrastar o jogador como dependencia.
- O pivo fica testavel em EditMode, porque sem input e sem loop de jogo ele e matematica
  pura. Existem 11 testes em `ThirdPersonCameraRigTests`.
- `Combat` fica reservado para dano, hitbox e resistencias, sem ficar sendo um deposito.
- O grafo de dependencia do doc 07 secao 3 ganhou duas linhas e foi reescrito como tabela
  de niveis, que e mais facil de verificar que o diagrama anterior.
- Custo: dois `.asmdef` a mais, e a regra "Player conhece Camera" precisa ser respeitada
  na mao. Um ciclo entre asmdefs e erro de compilacao, entao a engine cobra a regra.

## Alternativas consideradas
- **Tudo em `Combat`**: mais rapido agora, e transformaria `Combat` no modulo que sabe
  tudo sobre o jogador. Descartado.
- **Camera dentro de `Player`**: impediria reuso do pivo por cutscene, e criaria a
  tentacao de a camera ler o estado do jogador direto.
