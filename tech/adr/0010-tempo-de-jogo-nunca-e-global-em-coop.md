# ADR 0010 — Tempo de jogo nunca é global em coop

Data: 2026-09-14
Status: aceita

## Contexto
O `docs/03` §11 pede dois efeitos que, num jogo solo, se fazem mexendo na escala de tempo:
hitstop de 0,04 a 0,08 s quando um golpe conecta, e câmera lenta de 0,25 s a 0,4x na morte
do último inimigo de um encontro (tarefas 1.25 e 1.28).

`Time.timeScale` é global na máquina. No cliente, ele congela só aquela tela. No host, ele
congela a simulação inteira: a IA de todas as criaturas, a vida e o vigor de todos os
jogadores, e a contagem da janela de Fluxo de todo mundo. Um bruxo acertando um golpe forte
travaria por 0,08 s o combate dos outros três.

Congelar só a máquina de quem bateu também não serve. O host mede a janela de Fluxo de
0,22 s pelo próprio relógio (tarefa 1.12). Um dono que congela 0,08 s termina o golpe 0,08 s
depois do que o host acha, e perde um terço da janela de encadear, sem nada no Console.

## Decisão
**Nenhum efeito de sensação mexe em `Time.timeScale`.** Os dois casos se resolvem assim:

| Efeito | Como | Onde vale |
|---|---|---|
| Hitstop | O golpe de quem bateu fica parado pelo mesmo tempo nas duas pontas | Simulação do golpe, no host e no dono |
| Câmera lenta (1.28) | Só apresentação: câmera, partículas e som de cada máquina | Tela de cada jogador |

O hitstop funciona em três passos:

1. O host resolve o golpe e sabe que ele conectou e em qual postura. No primeiro alvo do
   golpe, ele soma o hitstop da postura à própria contagem do golpe.
2. O host confirma o acerto para o dono, com o dano e a postura.
3. O dono segura a linha do tempo do golpe pela mesma duração, e dispara o tremor e o soco
   de câmera na tela dele.

A confirmação chega com atraso de meia ida e volta, e isso não importa: a extensão total é a
mesma nos dois lados, então o golpe termina no mesmo instante relativo. É um hitstop por
golpe, e não por alvo, para a postura Grupo não congelar quatro vezes.

## Consequências
- A janela de Fluxo sobrevive ao hitstop. O teste `AttackTimelineHoldTests` garante que o
  golpe dura o tempo dele mais o tempo segurado, nem um passo a mais.
- O hitstop não congela o mundo, só o golpe. Em greybox isso é quase invisível no corpo; a
  sensação vem do tremor e do soco de câmera. Quando houver `Animator` (M4), a pose congela
  junto com a linha do tempo, sem mudar nada da rede.
- O golpe é a única coisa que para. A criatura atingida não congela, porque congelar a IA
  de uma criatura atacada por dois jogadores só pelo golpe de um deles é justamente o erro
  global que esta ADR evita.
- A câmera lenta da 1.28 fica decidida antes de existir: ela nunca vai tocar em simulação.

## Alternativas consideradas
- **`Time.timeScale`.** Descartada pelo motivo do contexto: no host, trava a sessão inteira.
- **Congelar só no dono.** Descartada: desalinha o fim do golpe entre dono e host e come a
  janela de Fluxo de quem bateu.
- **O host congela e manda todo mundo congelar.** Descartada: é o `timeScale` global com
  ida e volta a mais, e ainda congela quem não tem nada a ver com o golpe.
