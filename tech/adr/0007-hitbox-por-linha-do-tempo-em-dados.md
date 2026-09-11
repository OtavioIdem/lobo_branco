# ADR 0007 — Hitbox por linha do tempo em dados, nao por evento de animacao

Data: 2026-09-10
Status: aceita

## Contexto

O doc 07 secao 4.5 e a tarefa 1.9 do doc 12 especificam a janela de dano por **evento de
animacao**: o clipe chama `HitboxOn` no quadro 8 e `HitboxOff` no quadro 14.

Isso nao e implementavel hoje. O jogador e uma capsula greybox sem `Animator`, e as
animacoes de Mixamo entram no M4 (tarefa 4.5). Nao existe clipe onde pendurar um evento.

Havia tambem uma lacuna nos numeros. O doc 03 secao 4 fixa "tempo do golpe" (0,55 s no
forte) e "recuperacao" (0,45 s), mas nao diz quanto do tempo do golpe e anticipacao e
quanto e janela de dano. O doc 03 secao 11 exige que exista anticipacao, sem quantificar.

As opcoes eram tres:

1. Adiar a tarefa 1.9 ate o M4, e ate la aceitar `OnTriggerEnter` em um colisor de espada.
2. Importar uma animacao qualquer so para ter onde pendurar o evento.
3. Dirigir a janela por tempo decorrido, com os tempos em asset.

## Decisao

A janela de dano e dirigida por **tempo decorrido**, a partir de um `AttackDef`
(ScriptableObject) que grava `strikeTime`, `recovery`, e a janela como **fracao** de
`strikeTime` (`hitboxOpenAt`, `hitboxCloseAt`).

O `AttackDef` carrega tambem `hitboxFromAnimationEvent`. Ligado, o `AttackState` para de
contar tempo e a janela passa a ser aberta e fechada de fora — pelos eventos do clipe.
`IMeleeAttacker` ja expoe exatamente `OpenHitbox` e `CloseHitbox`, que sao os dois eventos
do doc 07 secao 4.5 com outro nome.

Divisao decidida aqui, por nao estar em nenhum documento:

| Golpe | Tempo do golpe | Abre em | Fecha em | Anticipacao | Janela |
|---|---|---|---|---|---|
| Leve (Rapida) | 0,25 s | 55% | 95% | 0,14 s | 0,10 s |
| Forte | 0,55 s | 72% | 96% | 0,40 s | 0,13 s |
| Grupo | 0,40 s | 62% | 95% | 0,25 s | 0,13 s |

A anticipacao de 0,40 s do golpe forte foi escolhida para bater com o piso da faixa de
telegrafo de inimigo do doc 03 secao 10 (0,4 a 0,9 s): jogador e inimigo passam a ser
legiveis na mesma escala de tempo.

## Consequencias

- A tarefa 1.9 fica pronta no M1 em vez de esperar o M4, e o portao do M1 ("voce luta
  contra 4 capsulas por 10 minutos e quer continuar") pode ser avaliado de verdade.
- A janela ser fracao e nao segundos e o que torna a migracao barata: fracao de
  `strikeTime` e exatamente o *normalized time* de um clipe. No M4, o trabalho e ligar o
  booleano e chamar dois metodos que ja existem, nao reescrever a linha do tempo.
- A tabela do doc 03 secao 4 continua sendo a fonte: `strikeTime` e `recovery` sao os
  numeros do documento, sem transformacao. So a divisao interna e nova.
- A linha do tempo inteira fica verificavel em EditMode com passos fixos, sem `Animator`
  e sem fisica. Sao 17 testes que continuam validos depois da migracao, porque testam o
  estado e nao o clipe.
- Custo: enquanto nao houver animacao, a janela nao esta sincronizada com nada visual, e
  o alcance so pode ser julgado pelo gizmo do editor e pela capsula do inimigo piscando.
- Custo: quando a animacao chegar, os numeros de fracao precisam ser reconferidos contra o
  clipe real. Um asset com fracao errada nao quebra nada — so faz o golpe acertar antes de
  a lamina sair, e o sintoma e "o combate parece errado".

## Decisao secundaria: `MeleeWeaponDef`

O estagio 1 do pipeline precisa do dano cru da arma, e o doc 03 secao 12 diz 12 para o
jogador nivel 1. Nao existe sistema de itens ate o M2 (tarefas 2.1 e 2.4), e o
`StatBlock_Player` grava `AttackDamage = 0` porque aquele campo e bonus, nao dano de arma.

Criado `MeleeWeaponDef` com tres campos: dano base, material e tipo de dano. Ele e
provisorio e sera absorvido pela hierarquia de `ItemDef` no M2. A alternativa era o
numero 12 dentro de um `MonoBehaviour`, o que quebraria a regra 1 do CLAUDE.md no
primeiro golpe do jogo.

## Alternativas consideradas

- **`OnTriggerEnter` em colisor de espada.** Descartado pelo motivo que o doc 07 secao 4.5
  ja da: o trigger dispara quando a fisica quer, a ordem e imprevisivel, e uma lamina que
  atravessa o alvo em dois quadros gera dois eventos. A lista de ja-atingidos resolveria o
  segundo problema, mas nao o controle de quadro.
- **Importar uma animacao antes da hora.** Antecipa uma decisao de arte (tarefa 4.4, rig
  humanoide) para resolver um problema de codigo, e amarra a linha do tempo de combate a
  um clipe temporario que sera jogado fora.
- **Tempos em constantes no estado.** Quebra a regra 1 do CLAUDE.md e mata o ajuste: os
  tempos de golpe sao dos numeros que mais precisam ser sentidos com o jogo rodando.
