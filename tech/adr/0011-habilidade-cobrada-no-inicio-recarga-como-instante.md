# ADR 0011 — Habilidade: o host cobra no início, pode recusar, e a recarga viaja como instante

Data: 2026-09-14
Status: aceita

## Contexto
A tarefa 1.32 cria a habilidade com custo e recarga como dado. É a infraestrutura dos sinais
da tarefa 1.18 e das escolas do `docs/13` §5. Em coop, três perguntas de rede vêm antes de
qualquer pergunta de combate:

1. **Quem decide se a habilidade sai.** Vigor e recarga são recursos, e o `docs/13` §6 põe
   recurso no host.
2. **Como a recarga chega ao dono.** Ele precisa dela para saber se pede, e o HUD do
   `docs/02` §7 mostra "sinal selecionado, com cooldown".
3. **O que acontece quando host e dono discordam.**

O golpe já respondeu parte disso, e a resposta dele não serve aqui. O host cobra o vigor do
golpe e nunca recusa (`CharacterVitals.TrySpendStamina`), porque recusar um golpe que já saiu
na tela do dono troca um problema invisível por um visível. Para um sinal a conta muda. O
`docs/03` §8 faz dos sinais a única forma consistente de abrir um inimigo blindado: um sinal
de graça é uma janela de graça, e isso quebra o dilema do vigor do `docs/03` §7.

## Decisão
**O dono confere e pede. O host confere de novo, cobra no início, arma a recarga e pode
recusar. A recarga viaja como o instante em que volta, no relógio do servidor.**

| Pergunta | Resposta |
|---|---|
| Quando cobra | No início da conjuração: vigor e recarga juntos. Se a esquiva cortar, o que foi pago fica pago |
| Quem decide | `AbilityRules.Check`, uma função só, usada pelo dono antes de pedir e pelo host antes de aceitar |
| Quem dispara o efeito | O host, quando chega o pedido de efeito do dono. São três pedidos por habilidade: começou, efeito, cortou |
| Como a recarga viaja | Um struct de cinco `double`, copiado byte a byte, escrito pelo host só quando aceita uma conjuração. Cada máquina calcula quanto falta pelo `ServerTime` |
| Folga | 0,1 s de recarga para o pedido que chega pela rede, porque o relógio do servidor no cliente é estimativa |
| Recusa | O host responde só ao dono, com o motivo. O dono corta a conjuração, e o efeito não sai |
| Previsão | Ao pedir, o dono arma a própria cópia da recarga. A cópia replicada sobrescreve quando chega, e a recusa também |

## Consequências
- **A recusa quase nunca acontece, e o motivo não é sorte.** O dono conferiu com a mesma regra,
  contra a cópia replicada. Durante a meia ida e volta até o host, o vigor do host só sobe
  (apanhar não gasta vigor) e a recarga só anda. A recusa fica para o erro de estimativa do
  relógio além da folga, e para asset mal configurado.
- **Nenhuma mensagem por quadro.** São 40 bytes por conjuração aceita, e o painel do dono conta
  para baixo junto com o do host.
- **O compromisso do estado de sinal é parte da solução de rede.** Enquanto a conjuração roda,
  o dono não pede outra, e até lá a recarga do host chegou. Uma habilidade de duração zero
  quebra isso, então o `AbilityDef` trata duração zero como problema e há teste para isso.
- **Cinco vagas é teto de rede, não só de design.** São os cinco sinais do `docs/03` §8. Uma
  escola com seis habilidades exige mudar o struct, e o `SchoolDef` avisa antes.
- **O relógio muda quando a sessão começa.** Sem rede, a recarga conta no relógio local; no
  spawn, o host a reposiciona no relógio do servidor sem mudar quanto falta.
- **O efeito da 1.18 pendura no evento `CastTriggered`**, que só dispara em quem resolve.
- **É o primeiro caso em que o host recusa um pedido do dono.** Esquiva (1.10) e aparo (1.11)
  vão fazer a mesma pergunta, e o aparo de 0,18 s não cabe em "o host recusa depois": este
  arranjo serve a ações de centenas de milissegundos, não a janelas menores que o ping.

## Alternativas consideradas
- **Host sempre aceita, como no vigor do golpe.** O dono decidiria sozinho a recarga, o que é
  declarar recurso, e um relógio adiantado no cliente viraria sinal antes da hora.
- **Recarga como contagem replicada.** Ela muda todo quadro. Ou viaja com epsilon, como o
  vigor, e o HUD anda aos saltos, ou cada máquina conta a sua e elas divergem.
- **`NetworkList<double>`.** Aloca memória nativa que precisa ser liberada, e manda cada vaga
  como mudança separada. O struct fixo não aloca e cabe numa mensagem.
- **Cobrar no efeito, e não no início.** A esquiva cortaria a conjuração antes da conta, e todo
  sinal viraria uma finta sem custo.
- **O host conta sozinho o instante do efeito a partir do asset**, como conta o fim do golpe
  para abrir a janela de Fluxo. Economizaria uma mensagem, mas uma esquiva no último centésimo
  antes do efeito chegaria depois da conta do host, e o sinal sairia com o bruxo já rolando na
  tela do dono. Com o efeito pedido, as mensagens chegam na ordem em que o dono as mandou.
