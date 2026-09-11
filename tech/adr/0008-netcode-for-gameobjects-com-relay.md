# ADR 0008 — Netcode for GameObjects com Relay, e autoridade dividida

Data: 2026-09-10
Status: aceita

## Contexto

O [doc 13](../../docs/13_COOP_E_REDE.md) transformou o slice do Capítulo I em uma caçada
cooperativa de 2 a 4 jogadores, em sessão privada aberta por código de convite. Isso obriga
a escolher uma biblioteca de rede e um modelo de autoridade antes das tarefas 1.10 e 1.11 do
[doc 12](../../docs/12_BACKLOG_VERTICAL_SLICE.md), que são esquiva, aparo e riposte.

O momento importa por duas razões. A primeira é que a máquina de estados tem hoje **dois**
estados, `Locomotion` e `Attack`. Converter dois estados para o modelo de autoridade é
barato; converter os doze que o M1 ainda vai criar, não.

A segunda é o aparo. A janela de 0,18 s do [doc 03 §5](../../docs/03_COMBATE.md) é o sistema
mais sensível a latência do projeto inteiro: ela é menor do que a viagem de ida e volta de
muitas conexões domésticas. Quem decide se um aparo aconteceu, e contra qual quadro do
ataque inimigo, é uma decisão de rede, não de combate. Construir aparo antes de responder
isso é construí-lo duas vezes.

As restrições que pesam:

1. **É cooperativo contra IA.** Não há PvP, não há ranking, não há nada a ganhar trapaceando.
2. **É um dev sozinho, com 12 h por semana, cujo objetivo declarado é aprender.** Uma solução
   que exige entender rollback e reconciliação antes de ver dois bonecos andando juntos
   gasta o orçamento inteiro antes da primeira recompensa.
3. **O código existente já separa simulação de input.** `PlayerLocomotion` recebe direção por
   propriedade e `DamagePipeline` é classe pura. A regra 4 do CLAUDE.md, escrita para
   testabilidade, entrega de graça a separação que rede exige.
4. **Sessão privada entre amigos, pela internet.** Ninguém vai configurar redirecionamento de
   porta no roteador. Precisa de travessia de NAT.

## Decisão

**Netcode for GameObjects (NGO 2.x) sobre Unity Transport, com Unity Relay para o código de
convite, e autoridade dividida: o dono simula o próprio personagem, o host resolve todo o dano.**

Pacotes a instalar: `com.unity.netcode.gameobjects`, `com.unity.transport`,
`com.unity.services.core`, `com.unity.services.authentication`, `com.unity.services.relay` e
`com.unity.multiplayer.playmode`. Versões resolvidas na instalação, contra o editor 6000.6.0f1.

### A divisão de autoridade, e por que ela é o ponto desta ADR

| Camada | Autoridade | Justificativa |
|---|---|---|
| Posição, rotação, estado da FSM | **Dono** | Movimento responde ao input local sem esperar viagem de ida e volta |
| Pedido de ataque, esquiva, sinal | **Dono pede** | Vira RPC para o host |
| `DamagePipeline`, acerto, morte | **Host** | Uma única fonte de verdade. Os 11 estágios rodam uma vez, no host |
| `StatSheet` de todo mundo | **Host** | Replicado. Cliente lê, nunca escreve |
| IA, behavior tree, attack token | **Host** | Já é peça única por natureza (doc 07 §6) |

Cliente com autoridade de movimento é uma decisão deliberada, e é ela que torna o projeto
viável. Predição no cliente com reconciliação no servidor é o problema mais difícil de
netcode de ação, e existe para impedir trapaça de movimento. Num jogo cooperativo contra
IA, esse problema não precisa ser resolvido: quem quiser trapacear está trapaceando contra
si mesmo e contra amigos que convidou.

O dano é a exceção porque é onde a divergência aparece. Se dois clientes resolverem o mesmo
golpe, o monstro leva dano duas vezes ou nenhuma. Por isso o `MeleeHitbox` continua sendo a
mesma classe, mas só o host a chama.

## Consequências

### Fica mais fácil
- O movimento continua respondendo na hora, sem predição, sem rollback, sem buffer de estado.
- `PlayerLocomotion`, `DamagePipeline` e os 115 testes existentes sobrevivem sem alteração.
- NGO é de primeira parte. A documentação, os exemplos e as mensagens de erro do editor falam
  a mesma língua, o que importa quando o objetivo é aprender.
- Relay resolve travessia de NAT e entrega um código de convite pronto, que é literalmente a
  funcionalidade pedida.
- Multiplayer Play Mode roda jogadores virtuais dentro do mesmo editor. Testar coop sozinho
  deixa de exigir duas builds e duas janelas, o que ataca o risco X10 do doc 13.

### Fica mais difícil
- Todo estado que importa para o jogo passa a precisar de uma decisão explícita de quem manda.
  Isso é trabalho a mais em cada sistema novo, para sempre.
- O host tem vantagem de latência. Em PvE isso é aceitável; em qualquer futuro com PvP, não.
- Depurar passa a ter uma dimensão nova. O painel de debug precisa mostrar autoridade e papel
  desde o primeiro dia, senão erro de rede parece bug de gameplay.
- Relay depende de um projeto no Unity Gaming Services e de conexão. Por isso o transporte
  direto por IP fica disponível em desenvolvimento, e a rede não pode depender da nuvem para
  ser testada.

### Fica irreversível
- Trocar de biblioteca depois que a FSM, o combate e a IA estiverem em rede é reescrever a
  camada de rede inteira. A escolha vale pelo resto do protótipo.
- A autoridade de movimento no cliente fecha a porta para PvP competitivo neste código.
  Isso é aceito: PvP está fora do escopo no doc 13 §8.

## Alternativas consideradas

**Fish-Networking.** Tem a melhor predição e reconciliação do ecossistema Unity, de graça.
Descartada porque a predição é exatamente o problema que este projeto não precisa resolver, e
porque documentação de terceiro é um obstáculo a mais para quem está aprendendo. Seria a
escolha certa se houvesse PvP.

**Mirror.** Madura, muito usada, boa documentação. Descartada por ser herdeira da UNet antiga
e por não ter integração de primeira parte com Relay. A vantagem sobre NGO hoje é pequena o
bastante para não compensar sair do caminho oficial.

**Photon Fusion ou PUN.** Resolveria hospedagem e travessia sem esforço. Descartada porque
amarra o projeto a um serviço pago com teto de jogadores simultâneos, e porque esconde a
camada que o projeto existe para ensinar.

**Lobby do Steam via Facepunch.Steamworks.** Entregaria convite pelo overlay da Steam, que é a
melhor experiência possível para jogar com amigos. Descartada porque exige um App ID da Steam,
que custa dinheiro e não faz sentido para um protótipo não-comercial.

**Só IP direto, sem Relay.** Zero dependência de nuvem e é o que vai ser usado no dia a dia de
desenvolvimento. Descartada como solução final porque exige redirecionamento de porta para
jogar com alguém de fora da rede local, e o pedido original era entrar por código.

**Autoridade total no host, inclusive movimento.** É o modelo correto para qualquer jogo com
competição. Descartada porque sem predição o movimento fica com atraso perceptível, e com
predição o custo de implementação consome o orçamento do milestone.
