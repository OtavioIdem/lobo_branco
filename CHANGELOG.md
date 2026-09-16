# Changelog

Formato: uma linha por mudanca que o jogador ou o dev perceberia.

## [Nao lancado]

- 2026-09-14: PlayerWolf v03 de acabamento no Blender, com rosto e materiais detalhados, cota volumetrica e fonte de alta resolucao; v02 preservada e FBX validado.

- 2026-09-12: PlayerWolf proxy v02 com rosto definido, barba curta, corpo e membros revistos; fonte v01 preservada, 14.698 triangulos e round-trip FBX validado.

### 2026-09-15 — M1 tarefa 1.18g: da para escolher o sinal
- **Os quatro sinais viraram jogaveis.** Ate aqui o Q usava sempre a primeira vaga, entao fogo,
  escudo e armadilha existiam como asset e ninguem conseguia lanca-los. Segurar Q abre a roda, e as
  teclas 3 a 7 escolhem direto.
- **A roda nunca desacelera o tempo** (`tech/adr/0010`), ao contrario do Witcher 3: em coop a escala
  e global na maquina, e no host ela congelaria a sessao de todo mundo enquanto um jogador escolhe.
- **O sinal passa a sair quando Q e solto.** Lancar e escolher dividem a tecla, e sem o toque
  segurar para abrir a roda lancaria o sinal antes. Toque e espera usam o mesmo 0,3 s.
- **Fechar a roda nao conjura:** o sinal escolhido sai no proximo toque, e soltar a roda nunca gasta
  vigor sem o jogador mandar.
- `SignWheelSelection`: a regra da escolha, sem input e sem tela, com a conta de angulo e a zona
  morta que impede trocar de sinal sem querer. Escolher e local do dono: nada disso viaja.
- A roda de greybox e IMGUI e mostra recarga por vaga, ate o HUD do docs/02 secao 7 existir.
- 441 testes passando, contra 429 antes. Build de Windows gerando.

### 2026-09-15 — M1 tarefa 1.18f: a armadilha segura o campo
- **O Yrden existe como asset**, com os numeros do docs/03 secao 8: 35 de vigor, 8 s de recarga, e
  uma armadilha de 4 m que fica 12 s no chao lentificando em 60% quem estiver dentro. O Lobo o tem
  na quarta vaga, e ele so pode ser conjurado com a roda da tarefa 1.18g.
- **O primeiro objeto de rede que nasce em jogo.** O host o faz nascer e destroi, e ele esta
  registrado no `NetworkManager` da cena: sem registro nos dois lados, a conexao e recusada, o que e
  melhor que armadilha invisivel. Sem rede, nasce local e funciona igual.
- **Viaja so o instante em que ela some.** Raio e lentidao vem do prefab, igual nas duas maquinas, e
  a lentidao de cada criatura ja viaja no estado dela.
- **A lentidao e reaplicada em pulsos curtos**, e nao pelos 12 s de uma vez: quem sai da armadilha
  volta a correr sozinho.
- A intensidade estica o tempo de campo, e nao os 60%: escalar a lentidao seria imobilizar.
- **Ancorar etereos ficou de fora**, com motivo escrito no docs/03 secao 8: nao existe criatura
  eterea no slice, e a regra precisa nascer inteira junto com o primeiro espectro.
- Consertado antes de existir em jogo: uma armadilha que ninguem armou se destruia no primeiro
  quadro, porque "sem prazo" estava sendo lido como "prazo vencido".
- 429 testes passando, contra 416 antes. Build de Windows gerando.

### 2026-09-15 — M1 tarefa 1.18e: o escudo absorve um golpe e devolve o troco
- **O Quen existe como asset**, com os numeros do docs/03 secao 8: 25 de vigor, 6 s de recarga,
  absorve um golpe por 8 s e devolve 30% do dano a quem bateu. O Lobo o tem na terceira vaga, e ele
  so pode ser conjurado com a roda da tarefa 1.18g.
- **Forma de area nova, em quem conjura.** O escudo e o unico sinal cujo alvo ja e conhecido antes
  de qualquer consulta, e ele nao custa fisica nenhuma.
- **O resultado de dano passou a carregar quem bateu.** Sem isso, o alvo sabia que apanhou e nao de
  quem, e nao havia para onde devolver o troco.
- `WardState` e `WardStatus`: o escudo como instante de queda replicado, erguido e quebrado so pelo
  host. **Aqui o estado viaja, e na Queimadura nao**: o escudo e decisao do jogador, e ele precisa
  ver na propria tela se ainda esta protegido.
- **O escudo entra na frente da vida por contrato** (`IDamageAbsorber`), e o `DamageReceiver` nao
  conhece sinal nenhum: a pocao de pele de pedra do M2 entra pela mesma porta.
- Absorve um golpe de 8 ou de 80 pelo mesmo preco, o troco nao passa pelo pipeline de novo, e a
  intensidade escala o que volta e nao a duracao. O painel de debug mostra o escudo de pe.
- **Consertado um tique de Queimadura que sumia por arredondamento** (tarefa 1.18d). O compasso
  chega ao instante somando, e quem pergunta calcula de uma vez; em ponto flutuante os dois podem
  diferir no ultimo bit, e a queimadura tirava 4 em vez de 8 dependendo de que horas a partida
  comecou. O teste que pegou isso falhava so as vezes.
- 416 testes passando, contra 392 antes. Build de Windows gerando.

### 2026-09-15 — M1 tarefa 1.18d: o fogo fere e queima
- **O Igni existe como asset, e ainda nao sai do Q.** Custo 35, recarga de 5 s, cone de 5 m: dano
  de 0,8 vezes a espada na mao, como fogo, e Queimadura de 4 por segundo por 5 s (docs/03 secao 8).
  O Lobo o tem na segunda vaga, e ele so pode ser conjurado com a roda da tarefa 1.18g.
- `DamageEffectDef`: dano de sinal pelo pipeline, mas so por 4 dos 11 estagios: bestiario, pocao,
  armadura e resistencia. Postura, material, Fluxo, oleo e critico sao de lamina. E outra lista de
  estagios; os onze do golpe e a razao de 5,3 vezes nao mudam.
- `BurnState` e `BurnStatus`: a Queimadura em tiques de 1 s, contada so no host, e o dano de cada
  tique viaja na vida. **Ignora armadura e respeita resistencia a fogo.** Reaplicar nunca soma; a
  mais forte manda. Morte pela queimadura rende carga de adrenalina a quem acendeu.
- Capsulas paradas e cacadores da sandbox queimam.
- **Decididos aqui, fora do documento:** base na espada na mao, queimadura sem armadura e cone de 60
  graus. A nota no docs/03 secao 8 explica cada um.
- 392 testes passando, contra 361 antes. Build de Windows gerando.

### 2026-09-15 — M1 tarefa 1.18c: o abridor derruba a matilha
- **Q passa a controlar.** O abridor derruba criaturas leves por 2 s, atordoa as medias por 1,5 s e
  nao move as pesadas (docs/03 secao 8). O barghest e leve: o sinal deita os dois cacadores da
  sandbox, em todas as maquinas.
- `BodyWeight`: toda criatura declara porte no `MonsterDef`. O arquetipo de postura nao servia,
  porque "agil" nao diz se a criatura e leve ou media. Sem porte, ela conta como media.
- `ControlEffectDef`: o primeiro efeito de sinal, com uma resposta por porte. A intensidade alonga o
  controle e nunca troca o tipo.
- **Decididos aqui, fora do documento:** derrubada de 2 s e barghest leve. A nota no docs/03 secao 8
  explica, e registra o risco de dois bruxos alternando o abridor para manter um barghest no chao.
- As capsulas paradas da sandbox nao tem controle e ignoram o abridor. Quebrar guarda e a 1.19.
- 361 testes passando, contra 346 antes.

### 2026-09-15 — M1 tarefa 1.18b: o efeito de sinal vira dado
- `SignEffectDef`: um efeito de sinal e um asset, subclasse deste tipo. O `AbilityDef` ganhou uma
  area (`SignArea`, cone ou raio) e uma lista de efeitos. Os efeitos de verdade sao das tarefas
  1.18c a 1.18f (`tech/adr/0012`).
- `SignResolver`: uma consulta de fisica sem alocacao, filtro de abertura, alvos do mais perto para
  o mais longe. Quem conjurou e quem esta abatido ficam de fora, e uma criatura de dois colisores
  recebe o efeito uma vez.
- `PlayerSignEffects`: **so o host aplica**, no instante do efeito que a 1.32 ja pedia, com a
  posicao que o host ve. O resultado viaja no estado que cada alvo ja replica: nenhum byte novo na
  rede.
- **Gancho de variante por escola.** O `SchoolDef` aponta o sinal especializado e os efeitos a mais,
  que sao somados depois dos do sinal. O Lobo aponta o abridor, com a variante vazia.
- **Intensidade de sinal = `SignIntensity` x Inteligencia / 10**, no `CombatTuningDef`. Escala
  potencia e nunca area, custo ou recarga. O "escalado por Inteligencia" do docs/03 secao 8 foi lido
  como intensidade, e nao como custo; a nota de la explica. O bloco do jogador nao tinha
  `SignIntensity`, e o setup acrescenta; um teste falha para escola com intensidade zero.
- O abridor ganhou o cone de 6 m do documento e **90 graus de abertura, decididos aqui**. Ele continua
  sem efeito: o painel de debug mostra quantos alvos o cone pegou, e a area aparece na Scene view.
- 346 testes passando, contra 308 antes. Build de Windows gerando.

### 2026-09-14 — M1 tarefa 1.18a: o inimigo pode ser atordoado, derrubado e lentificado
- **A 1.18 foi dividida em oito partes e vem antes da 1.33.** Sem efeito de sinal, o Grifo seria
  um Lobo com outra postura inicial. O Axii ficou adiado ate existir inimigo humanoide.
- **Todas as escolas terao os cinco sinais, e cada uma e especializada em um**, com uma variante
  so dela: Lobo no Aard, Grifo no Igni. As escolas fora do slice e o conflito da Lince com a
  ADR 0005 estao registrados no docs/13 secao 5.
- `ControlState`: atordoamento, derrubada e lentidao guardados como o instante em que acabam, no
  relogio do servidor. E a regra e o formato de rede ao mesmo tempo. Reaplicar nunca soma, entao
  dois bruxos nao deixam uma criatura atordoada para sempre; na lentidao, a mais forte manda.
- `ControlStatus`: o host aplica, todos veem. Sem controle, a criatura para no lugar, nao gira,
  corta o golpe em andamento (o telegrafo para em todas as maquinas) e devolve a vez de golpear,
  para outra poder atacar. Mora em componente, e nao no grafo, entao funciona antes de o grafo do
  barghest existir.
- A lentidao vira modificador de `MoveSpeed` em todas as maquinas. **O bloco do barghest nao tinha
  `MoveSpeed`**, e um multiplicador sobre zero nao lentifica nada: o setup agora acrescenta o que
  falta, e um teste falha para qualquer criatura sem o atributo.
- `ControlStatusView`: em greybox, a capsula derrubada deita e a atordoada balanca. No editor, o
  menu de contexto do `ControlStatus` atordoa, derruba e lentifica, para conferir sem sinal.
- No `Enemy_Barghest`, nada muda ate um sinal aplicar controle (tarefas 1.18c e 1.18f).
- 308 testes passando, contra 292 antes. Build de Windows gerando.

### 2026-09-14 — M1 tarefa 1.32: habilidade com custo e recarga
- `AbilityDef`: habilidade em asset, com custo de vigor, recarga, tempo de conjurar e
  recuperacao. A escola lista as dela por vaga, e a vaga e o que viaja pela rede.
- **Q passa a conjurar, e o sinal ainda nao faz nada.** O Lobo ganhou o abridor do docs/03
  secao 8, com os 30 de vigor e os 4 s de recarga do documento. Ele cobra, recarrega e
  compromete o bruxo por 0,7 s; o efeito e da tarefa 1.18. Os 0,3 s de conjuracao e os 0,4 s
  de recuperacao nao estao no documento e foram decididos aqui.
- **O host cobra no inicio e pode recusar** (`tech/adr/0011`). Ao contrario do vigor do golpe,
  que o host cobra sempre, um sinal de graca e uma janela de graca. O dono confere com a mesma
  regra antes de pedir, entao a recusa quase nunca acontece; quando acontece, a conjuracao corta
  e o efeito nao sai.
- **A recarga viaja como o instante em que volta**, no relogio do servidor, e cada maquina
  calcula quanto falta. Nenhuma mensagem por quadro: 40 bytes por conjuracao aceita.
- `CastState`: conjuracao comprometida, como o golpe. A esquiva corta, e vigor e recarga ficam
  pagos. Recarga que volta dentro dos 0,2 s do buffer solta o sinal sozinha.
- O painel de debug mostra o sinal selecionado, a recarga, o instante do efeito e a recusa do host.
- 292 testes passando, contra 248 antes. Build de Windows gerando.

### 2026-09-14 — M1 tarefa 1.31: a escola vira dado
- `SchoolDef`: uma escola de bruxo em asset. Bloco de atributos, os tres golpes por postura e
  a postura favorecida. E a regra 7 do CLAUDE.md feita tipo: nenhum `if` de escola em codigo.
- **O docs/13 secao 5.1 estava errado sobre a afinidade de postura, e foi corrigido.** Ele
  dizia que o `StanceAffinityStage` ja aplicava a afinidade da escola. Nao aplica: aquele
  estagio compara a postura do golpe com o arquetipo do alvo, e a escola de quem bate nao
  entra na conta. A postura favorecida virou a postura inicial e a vaga do golpe mais bem
  feito. E dado de golpe e nao multiplicador novo, entao a razao de 5,3 vezes fica intacta.
- A intensidade de sinal ja era o atributo `SignIntensity`, e mora no bloco de atributos da
  escola. Custo de sinal e filtro de vestigio ficaram de fora: sinais e investigacao ainda
  nao existem, e campo para sistema inexistente e promessa.
- **Cada golpe tem que declarar a postura da vaga em que esta.** O golpe viaja pela rede como
  postura e o host resolve o asset pela vaga; um golpe Forte na vaga Rapida faria dono e host
  desferirem golpes diferentes, sem erro nenhum. O asset avisa no Inspector, e um teste
  percorre as escolas reais do projeto.
- `PlayerSchool`: o unico lugar do personagem que sabe a escola. O atacante pergunta os
  golpes, o cerebro pergunta a postura inicial, e a folha de atributos pergunta por uma
  interface do modulo de combate, porque o grafo de asmdef nao deixa o combate apontar para o
  jogador.
- `School_Wolf.asset` aponta para os mesmos assets que o jogador ja usava. **Nada muda em jogo**:
  criar a escola nao pode mudar o jogo, e a escola que muda e o Grifo, da tarefa 1.33.
- **Um teste falha se aparecer uma terceira escola.** O slice e Lobo e Grifo, e uma escola nova
  e so um asset, a porta mais facil para o escopo crescer (risco X1 do docs/10).
- 248 testes passando, contra 238 antes.

### 2026-09-14 — M1 tarefa 1.25: o golpe que conecta se sente, sem parar o tempo de ninguem
- **O hitstop nao mexe em `Time.timeScale`, e isso e regra do projeto agora.** A escala de
  tempo e global na maquina: no host, 0,08 s de congelamento de um golpe forte travaria a IA,
  a vida e o Fluxo dos outros tres jogadores. Registrado em `tech/adr/0010`.
- **O hitstop estende o golpe de quem bateu pelo mesmo tempo nas duas pontas da rede.** O host
  soma os segundos a contagem do golpe e confirma o acerto para o dono, que segura a propria
  linha do tempo pelo mesmo numero. A confirmacao chega com meia ida e volta de atraso, e isso
  nao importa: a extensao total e igual dos dois lados. Congelar so o dono faria o golpe
  terminar depois do que o host acha, e comeria um terco da janela de Fluxo de 0,22 s.
- `AttackTimeline.Hold`: o tempo segurado e gasto antes, e a sobra do passo avanca o golpe no
  mesmo quadro. Sem a sobra, cada hitstop arredondaria para um quadro inteiro, e a duracao do
  golpe dependeria da taxa de quadros, que e diferente no host e no dono.
- **Um hitstop por golpe, e nao por alvo.** A postura Grupo acerta ate quatro, e somar quatro
  congelamentos faria dela o golpe mais lento do jogo.
- Uma confirmacao que chega depois de a esquiva cortar o golpe e descartada. Guardada, ela
  congelaria o golpe seguinte, que nao acertou nada.
- `HitFeedbackDef`: 0,08 s no Forte e 0,04 s no Rapido, os numeros do docs/03 secao 11. Grupo
  fica em 0,05 s, porque o documento nao da numero e congelar muito com varios alvos parece
  travamento. Tremor proporcional ao dano, com teto, e soco de 2 graus.
- `PlayerHitFeedback`: tremor e soco so na tela de quem bateu. Apanhar tambem treme, e nao
  empurra, porque o soco e a assinatura de quem acerta. Sem a pergunta "este personagem e o
  desta tela", o golpe de um companheiro sacudiria a camera de todo mundo.
- `CameraPunch`: o soco e somado so na hora de escrever a rotacao, e nunca no pitch do
  jogador. Dez golpes seguidos nao deixam a camera vinte graus mais baixa.
- 238 testes passando, contra 218 antes. Um deles garante que o golpe dura exatamente o tempo
  dele mais o tempo segurado, nem um passo a mais: e a conta que faz host e dono concordarem.

### 2026-09-14 — M1 tarefa 1.23: o aviso do golpe chega a todo mundo
- **O telegrafo era, antes de tudo, um problema de rede.** A linha do tempo do golpe roda
  so no host, entao no cliente nao existia anticipacao nenhuma: quem hospedava via o tell,
  e o companheiro apanhava sem aviso.
- `EnemyTelegraph`: o host manda um unico instante por golpe, no relogio do servidor, e cada
  maquina calcula o aviso sozinha a partir do mesmo `AttackDef`, que ja esta no disco dela.
  Mandar a fase por quadro seria mandar pela rede uma conta que o outro lado sabe fazer.
- **Com ping, o cliente comeca o aviso atrasado, mas termina no tempo certo.** O tempo e
  medido contra o relogio do servidor, entao o aviso de quem tem latencia fecha junto com a
  janela de dano do host. Recomecar do zero ao receber daria a todo cliente um aviso que
  acaba depois de o golpe ja ter acertado, e ensinaria o tempo errado a quem joga com ping.
- O golpe cortado no meio tambem viaja, para o cliente nao continuar avisando um golpe que o
  host cancelou. O fim natural nao viaja: cada maquina chega nele pela mesma conta.
- `TelegraphCurve`: **o aviso enche ate a janela abrir, em vez de so ligar.** Um aviso que
  liga diz que o golpe vem; um aviso que enche diz quando, e quando e o que a esquiva precisa.
  Ele tambem nasce ja visivel, porque um aviso que comeca apagado desperdica metade da
  anticipacao.
- `TelegraphStyleDef`: ambar e nunca vermelho. Vermelho e o tell de `Unblockable` da tarefa
  1.24, e um aviso comum vermelho ensinaria ao jogador a resposta errada.
- Sem Animator, a pose de anticipacao e a capsula que se abaixa com o pe no chao. Encolher
  pelo centro a faria flutuar, e flutuar antes de atacar parece erro de fisica, nao pose.
- **Corrigido: um barghest que caia no meio da garrada terminava o golpe e acertava.** Quem
  dirige o golpe e a arvore, e uma arvore que continua rodando deixava a criatura morta
  concluir o ataque. Agora o golpe e encerrado no quadro em que ela cai.
- 218 testes passando, contra 206 antes. Dois deles sao sobre rede: relogio do cliente um
  pouco atras do carimbo do host nao apaga o aviso, e aviso recebido tarde ja comeca adiantado.

### 2026-09-12 — M1 tarefa 1.22: quem tem a vez de golpear
- `AttackTokenPool`: no maximo dois atacantes por alvo ao mesmo tempo. E a regra que
  transforma um amontoado em combate: nao ha esquiva que resolva cinco golpes simultaneos, e
  nao ha como ler cinco telegrafos de uma vez.
- **O teto e por alvo, e nao por encontro, e isso corrige o docs/07 secao 6 para coop.** O
  numero 2 de la foi pensado para um jogador. Com quatro, um teto de encontro faria um grupo
  de oito criaturas ter seis paradas assistindo, e o segundo, o terceiro e o quarto jogador
  nunca seriam atacados. O documento foi atualizado.
- `EncounterCoordinator`: o dono dos tokens, no mesmo objeto do `NetworkManager` porque ele e
  do host pelo mesmo motivo que o resto dali. No cliente ninguem pede token, porque no
  cliente ninguem decide atacar.
- **Cena sem coordenador vira erro no Console, e nao golpe recusado.** Recusar todos os
  golpes deixaria o combate sem inimigos, que e pior do que deixa-lo desorganizado. O aviso
  sai uma vez so.
- `CircleTargetAction`: quem e recusado ronda o alvo a distancia de engajamento em vez de
  esperar parado. Sem este no o token pioraria o combate: a terceira criatura colaria no
  bruxo sem bater, e inimigo imovel a meio metro parece travamento. Travado e pior do que
  injusto. Metade ronda para um lado e metade para o outro, pelo identificador da propria
  criatura, sem sorteio e sem estado guardado.
- **A vez volta no fim de cada golpe, e nao no fim da recarga.** Uma criatura segurando o
  token durante a pausa de 1,2 s ocuparia uma das duas vagas sem bater, e o combate ficaria
  vazio pela metade. Quem perde o alvo de vista, cai, ou sai de cena tambem devolve.
- `GetInstanceID` saiu de circulacao no editor 6000.6 e virou erro de compilacao. As chaves
  do pool passaram a ser o `EntityId` convertido por `ToULong`, que e a ponte oficial entre o
  identificador da engine e uma classe pura sem Unity.
- 206 testes passando, contra 193 antes. Treze deles sao sobre devolver o token e nao sobre
  conceder, de proposito: um token que vaza deixa uma vaga presa e o sintoma e um encontro
  que para de atacar no meio, sem nada no Console.

### 2026-09-12 — M1 tarefa 1.21 (parcial): o inimigo percebe, persegue e golpeia
- **O jogador passou a ser atingivel.** Ate aqui o pipeline de dano so tinha alvo de um
  lado: a capsula de sandbox implementava `IDamageable` e o bruxo nao implementava nada.
  Um inimigo que persegue e golpeia atravessaria o jogador sem tirar um ponto de vida, e
  sem uma linha no Console. Isso nao era item do backlog e deveria ter sido.
- `DamageReceiver`: a porta do dano, agora um componente so, usado pelo bruxo e pela
  criatura. Ele saiu de dentro do `CombatDummy`, que e greybox de sandbox: copiar a ponte
  para o lado do jogador criaria duas implementacoes do mesmo contrato, e a segunda
  esqueceria de recusar dano resolvido fora do host.
- `Combatant_Witcher.asset`: o perfil de combate do bruxo. Humanoide nao e detalhe de
  fantasia, e o numero mais pesado que incide sobre o jogador: sem ele o alvo vira besta
  agil e o aco de um bandido cai de 1,0x para 0,35x, o que deixaria o bruxo quase
  invulneravel a metade dos inimigos do capitulo.
- `AttackTimeline`: a linha do tempo de um golpe virou classe propria no modulo de combate,
  porque o bruxo e o monstro desferem o mesmo golpe. Ela estava dentro do `AttackState`, e
  copia-la para a IA daria duas contagens que envelheceriam separadas: no dia em que o
  Animator entrar, so uma das duas mudaria.
- `EnemySenses`: cone de visao, circulo de audicao e memoria, em classe pura e testada.
  As tres regras que importam sao as tres decisoes de jogo. Contornar um grupo funciona,
  porque a visao e um cone. Colar pelas costas nao e invisibilidade, porque o ouvido nao
  depende de estar olhando. E perder de vista nao e esquecer: ela caca por mais 4 s, o que
  impede o comportamento que mais denuncia IA ruim, o inimigo que desiste no instante em
  que voce quebra a linha de visao com ele a dois metros.
- `EnemyAgent`: busca de alvo, linha de visao, giro e velocidade. A percepcao roda no
  `Update` dele e nao dentro de um no da arvore, e esse e o ponto: um no so executa
  enquanto o galho dele esta ativo, e uma criatura que so percebesse no ramo de patrulha
  ficaria cega justamente enquanto persegue. A busca acontece 4 vezes por segundo, e nao
  todo quadro: e mais reacao do que qualquer um percebe, por um quarto do custo.
- `EnemyMeleeAttacker`: o golpe da criatura, pela mesma hitbox sem alocacao e pelo mesmo
  pipeline de onze estagios do jogador. Ele e bem menor que o do bruxo porque nao ha
  autoridade dividida: quem decide, quem conta e quem resolve e o host, sempre.
- **Em coop, ela persegue quem chegou mais perto, e nao quem viu primeiro.** Sem isso, dois
  jogadores dividiriam a atencao de um monstro pela ordem de chegada em vez de pelo que
  estao fazendo, e flanquear deixaria de significar alguma coisa.
- `Attack_Barghest_Claw.asset`: 0,65 s de anticipacao antes da janela de 0,20 s. O numero
  esta na faixa de 0,4 a 0,9 s que o docs/03 secao 10 pede, e ele e o tempo que a esquiva
  da tarefa 1.10 vai ter para acontecer. Encurtar isso torna o combate injusto, nao dificil.
- **A criatura so gira durante a anticipacao.** E a regra que faz o telegrafo significar
  alguma coisa: um monstro que corrige a mira ate o ultimo instante transforma o tell em
  decoracao, porque sair de linha nao adianta. Parando de girar quando a lamina compromete,
  ler o tell vira a resposta certa.
- `Weapon_BarghestClaws.asset` bate zero de dano cru, e isso esta certo: os 16 da tabela do
  docs/03 secao 12 ja moram no `AttackDamage` do bloco de atributos, e o estagio 1 soma os
  dois. Repetir os 16 no asset dobraria o dano da criatura, e o sintoma apareceria so no
  playtest.
- Quatro nos customizados de behavior tree, prefab `Enemy_Barghest` com autoridade de
  posicao no servidor, malha de navegacao da sandbox assada por script, e dois cacadores na
  cena, longe o bastante para o jogador nascer fora do campo de visao deles.
- 193 testes passando, contra 165 antes.
- **Parcial, e falta uma coisa so: o grafo.** O asset de arvore do `com.unity.behavior` e
  authoring do editor grafico, e o tipo dele e interno ao pacote, entao nao ha como monta-lo
  por script como o resto do projeto e montado. Os nos, o prefab, o NavMesh e todos os
  numeros estao prontos; falta arrastar os nos uma vez e apontar o grafo no prefab. A
  divisao entre o que vive no grafo e o que vive em componente esta na
  `tech/adr/0009`.

### 2026-09-12 — Primeiro blockout 3D do personagem Lobo
- Criado no Blender 5.2.1 LTS um fan model de Geralt inteiramente novo, sem reutilizar
  geometria, textura, rig, animação ou material do jogo/REDkit.
- O proxy tem 1,85 m, 6.500 triângulos, cinco materiais planos, pivô no chão e volumes
  simplificados para cabelo, barba, armadura, cota de malha, medalhão e duas espadas.
- O FBX passou no round-trip do Blender sem alterar altura ou pivô e foi importado pela
  Unity com escala 1. A compilação batch terminou limpa.
- Ainda não há rig, UV, texturas, LOD, animação ou prefab jogável; esta entrega valida
  apenas a modelagem inicial e o pipeline Blender → Unity.

### 2026-09-12 — M1 tarefa 1.20: MonsterDef
- `MonsterDef`: a especie em asset. Classe da criatura, arquetipo de postura, oleo que casa
  e resistencia por tipo de dano. O `CombatDummy` parou de declarar essas quatro coisas em
  codigo e passou a le-las de la.
- Duas diferencas para o esboco do doc 07 secao 4.1, as duas de proposito. Vitalidade, dano
  e armadura nao entraram: eles ja moram no `StatBlockDef`, e repetir os mesmos numeros em
  dois assets criaria dois barghests diferentes, com a divergencia aparecendo so quando
  alguem editasse um dos dois. Entrada de bestiario, tabela de loot e vulnerabilidade a
  sinal tambem nao entraram, porque bestiario, loot e sinais nao existem: campo de dado
  para sistema inexistente e promessa, nao dado.
- `Monster_Barghest.asset` criado pelo setup de dados, apontando para o bloco de atributos
  que ja tinha os 55 de vitalidade do doc 03 secao 12.
- As resistencias saem vazias, e o vazio e neutro. Quais criaturas resistem a que e decisao
  de balanceamento, e balanceamento e a tarefa 1.30, com o jogo rodando. O que ficou pronto
  aqui e o lugar onde esses numeros vao morar, e o teste que garante que tipo nao listado
  vale 1,0: sem ele, toda criatura seria imune a tudo que ninguem escreveu.
- O `CombatDummy` ficou sem nenhum numero e sem nenhuma classificacao proprios. O que
  sobrou nele e o que e mesmo de sandbox: piscar ao apanhar e levantar sozinho.
- 165 testes passando, contra 162 antes.
- **Ordem alterada**: a 1.20 entrou antes da 1.18. Quase todo sinal e um efeito sobre o
  inimigo, e capsula nao cai, nao queima e nao muda de lado. Construir os sinais antes do
  inimigo da 1.21 seria construi-los duas vezes. Registrado no doc 12.

### 2026-09-12 — M1 tarefa 1.17 (parcial): Adrenalina
- `AdrenalinePool`: tres cargas que nao regeneram sozinhas. A diferenca para o Vigor e o
  que define o recurso: Vigor volta com o tempo, Adrenalina so entra quando o bruxo faz
  alguma coisa bem feita, entao gastar uma carga e gastar uma luta que ja aconteceu.
- Ganha com corrente de Fluxo alta, uma carga no quinto elo e uma a cada dois dali para
  frente, e com morte causada. O ganho por riposte entra junto com o riposte (tarefa 1.11).
- Cair leva a adrenalina junto: a conta da luta encerra quando o bruxo encerra.
- Replicada como a vida e o vigor, em um byte, sem epsilon: ela muda de um em um e cada
  mudanca importa.
- **Dos tres gastos, so o segundo suspiro existe**: duas cargas viram 40 por cento do vigor
  maximo, na hora. Finalizacao precisa de um estado de execucao e sinal reforcado precisa
  dos sinais, entao os dois entram junto com o que eles gastam (tarefas 1.11 e 1.18).
- **E nenhum dos tres tem tecla.** A tabela de controles do doc 02 secao 4 nao tem botao
  para adrenalina, e inventar um agora seria decidir no escuro o que o playtest do portao
  M1 responde melhor. Ficou registrado no doc 03 secao 7, e a tarefa 1.17 esta marcada
  como parcial no doc 12 em vez de fechada.
- `IDamageable` ganhou `IsDown`. Quem desfere o golpe precisa saber se o alvo caiu, porque
  morte causada e uma das tres fontes de adrenalina.
- Painel F1 mostra as cargas.
- 162 testes passando, contra 153 antes.

### 2026-09-12 — M1 tarefa 1.16: Vigor
- `StaminaPool`: classe pura com os numeros do doc 03 secao 7. Cem de base, 18 por segundo
  descansando, 6 em combate, e nada durante 1,5 s depois de cada gasto.
- O atraso de 1,5 s e o sistema inteiro. Sem ele o recurso vira um contador que sempre
  volta e gastar deixa de ser escolha; com ele, cada gasto abre uma janela sem rede de
  protecao. Sinal e defesa saem do mesmo bolso, e e esse o dilema que o documento chama
  de central.
- O custo de vigor dos golpes estava escrito nos tres assets de postura desde a tarefa 1.8
  e nunca tinha sido cobrado. Agora e: 4 na Rapida, 8 na Forte, 12 na Grupo.
- Com tres elos de Fluxo o golpe custa 20 por cento menos, a outra metade do bonus do
  doc 03 secao 6 que estava esperando o vigor existir.
- Faltar vigor nao perde o input. O golpe fica guardado no buffer de 0,2 s e sai sozinho
  quando o vigor voltar, entao faltar vigor parece atraso e nao parece clique ignorado.
- O dono pergunta se da, o host cobra. Ele cobra o que tem em vez de recusar um golpe que
  ja saiu na tela de quem pediu: trocar um problema invisivel por um bem visivel seria
  piorar. Quem tenta gastar vigor fora do host leva erro no Console.
- O vigor vai replicado como a vida, mas so quando anda meio ponto, porque ele muda todo
  quadro e escrever todo quadro encheria a rede com diferenca de 0,3. O cheio e o zero
  sempre passam, que sao os dois valores em que a barra muda de significado.
- "Em combate" e uma heuristica por enquanto: alguns segundos depois de gastar ou de
  apanhar. Quem vai saber isso de verdade e o coordenador de encontro da tarefa 1.22, e
  o numero esta em asset com esse aviso.
- Apanhar conta como combate tanto quanto bater. Sem isso, quem so defende regeneraria na
  taxa de descanso no meio da briga.
- Painel F1 mostra o vigor e o estado da regeneracao: parado, em combate ou descansando.
- 153 testes passando, contra 143 antes.

### 2026-09-11 — M1 tarefa 1.15: aco e prata
- `WeaponSwapState`: 0,7 s guardando uma espada e sacando a outra, sem andar, sem atacar e
  sem esquivar. A espada nova so entra em vigor no fim: trocar e compromisso inteiro.
- E a unica excecao a excecao do jogo. A regra de ouro do doc 03 secao 1 deixa a esquiva
  cortar qualquer acao comprometida, e a secao 3 declara esta troca nao-cancelavel. Sem
  isso, esquivar viraria o jeito de pagar meio preco pela troca e a camada 1 do combate,
  que e a decisao de maior impacto, deixaria de custar. Tem teste com esse nome.
- Atordoamento, morte e dialogo continuam interrompendo, porque nao sao input do jogador:
  sao o mundo agindo sobre ele. Interrompido no meio, o bruxo fica com a espada que ja tinha.
- Ao contrario da postura, a troca de espada para o bruxo. Postura se troca andando; espada
  ocupa as duas maos. O contraste e o que da peso a decisao.
- `IWeaponHolder` novo, separado do `IMeleeAttacker`: desferir golpe e saber o que esta
  empunhado sao perguntas diferentes, e o teste da troca nao precisa fingir que abre hitbox.
- Teclas 1 e 2, e direcional esquerda e direita, conforme o doc 02 secao 4. Pedir a espada
  que ja esta na mao nao faz nada, senao um apertao distraido custaria 0,7 s parado.
- A troca nao passa pelo buffer de input, pelo mesmo motivo da postura: guardar a troca
  faria a espada mudar sozinha depois do golpe.
- O dono avisa o host quando troca, uma mensagem por troca e nao uma por golpe. O material
  e o estagio 4 do pipeline, e sem o aviso o host resolveria o golpe com a espada errada:
  0,35x onde deveria ser 1,0x. Ninguem mais precisa saber ainda, entao nada e replicado
  alem disso ate a lamina ter modelo (M4).
- A espada de prata saiu do limbo: o asset existia desde a tarefa 1.9 e nao tinha como ser
  empunhado. Com as duas na mao, os dois cenarios de referencia do doc 03 secao 9 viraram
  alcancaveis dentro do jogo, e nao so dentro do teste.
- `weaponSwapSeconds` no `PlayerTuningDef`, em asset.
- Painel F1 mostra a espada empunhada e avisa quando esta trocando.
- 143 testes passando, contra 137 antes.

### 2026-09-11 — M1 tarefa 1.14: as tres posturas
- `StanceSelector`: classe pura com a postura corrente e os 0,25 s que a troca leva. Ate a
  troca terminar, quem vale e a postura velha: trocar no meio da luta e aposta, nao punicao.
- Roda do mouse e direcional cima e baixo andam na roda das tres posturas. Duas voltas
  rapidas andam dois passos, porque a roda conta a partir da postura que ja esta a caminho.
- Trocar durante um golpe nao acontece, e a regra mora no `PlayerStateRules`, junto da
  regra de ouro. Dois lugares decidindo o que interrompe o que viram duas regras diferentes.
- O tempo da troca virou dado, em `stanceSwitchSeconds` no `PlayerTuningDef`. Perto de zero
  ele apaga a camada 2 do combate, entao e um numero para julgar jogando.
- A troca de postura nao passa pelo buffer de input, de proposito. O buffer existe para um
  input chegar cedo demais e ainda valer; guardar uma troca faria a postura mudar sozinha
  depois, que e o oposto de decisao tomada.
- Os dois botoes de ataque passam a dar o golpe da postura corrente. A tabela do doc 03
  secao 4 tem uma linha por postura e nao uma por botao: se o botao direito desse um golpe
  Forte com a postura Rapida valendo, escolher postura nao seria decisao nenhuma. O que vai
  distinguir os dois botoes dentro de uma mesma postura ficou registrado como pergunta em
  aberto no doc 03 secao 4, para ser respondida com playtest e nao no escuro.
- O golpe em grupo saiu do limbo: ele existia em asset desde a tarefa 1.8 e nao tinha como
  ser usado. Agora ele e a postura Grupo, com os quatro alvos em arco de 180 graus.
- O golpe passou a viajar pela rede como postura, e nao como indice de catalogo. As duas
  maquinas tem o mesmo prefab, entao a mesma postura da no mesmo asset e nao existe como
  elas discordarem de qual golpe foi.
- `IsCommitted` subiu para o contrato `IPlayerState`. Perguntar isso com um cast abriria a
  porta para uma segunda definicao de "comprometido", e ja sao dois os interessados.
- A afinidade de arquetipo nao precisou de codigo novo: o estagio 3 do pipeline ja punia a
  postura errada desde a tarefa 1.4, e so faltava o jogador poder escolher a postura.
- Painel F1 mostra a postura e a troca em andamento.
- 137 testes passando, contra 129 antes.

### 2026-09-11 — M1 tarefa 1.12: a Corrente de Fluxo
- `FlowChain`: classe pura que conta os elos e a janela de 0,22 s que se abre no fim de
  cada golpe. Encadear dentro dela soma um elo, ate o teto de 1,35x do doc 03 secao 6.
- Atacar fora da janela nao pune: a corrente reinicia em um elo e nada mais acontece. Tem
  teste com esse nome, porque e a regra que separa homenagem de defeito restaurado.
- A janela e dado, nao codigo: `flowWindowSeconds` entrou no `CombatTuningDef`, ao lado da
  tabela de bonus que ja estava la desde a tarefa 1.4.
- Quem conta e o host, porque a corrente muda o dano. O host mede o intervalo entre dois
  pedidos do mesmo jogador, e nao o proprio relogio: a latencia atrasa os dois pedidos
  junto, entao o intervalo sobrevive ao ping.
- Os elos viajam replicados para o dono ver. Quem decide se ataca agora ou espera e ele, e
  decidir sem ver a corrente seria adivinhar.
- O golpe interrompido nunca chega ao fim, entao a janela dele nunca abre e o proximo golpe
  ja recomeca do primeiro elo. `Break` existe so para o numero na tela concordar na hora.
- `DamageRequest` parou de receber zero fixo no campo de Fluxo. O cenario de referencia do
  doc 03 secao 9, o que define a razao de 5,3 vezes, pressupoe tres elos: ate hoje nenhum
  golpe dentro do jogo podia ter mais que zero, entao metade da conta de balanceamento era
  inalcancavel na pratica. Rebalancear com isso ligado e a tarefa 1.30.
- Painel F1 mostra elos e a janela aberta. E o unico retorno de Fluxo ate o brilho na
  lamina da tarefa 1.13.
- 129 testes passando, contra 121 antes.

### 2026-09-11 — M1 tarefas 1.9h e 1.9i: convite por codigo e a sala
- `NetRelaySession`: entra anonimo no Unity Gaming Services, cria a alocacao no Relay e
  devolve um codigo de convite. Quem recebe o codigo troca ele por um endereco de
  retransmissao e entra. Ninguem precisa saber o IP de ninguem nem mexer no roteador.
- Prefere DTLS quando o Relay oferece, e cai para UDP quando nao. Pegar o primeiro endereco
  da lista funcionaria ate o dia em que a ordem mudasse.
- O tamanho da sala sai do `NetSpawnRing`, que e onde ele ja era cobrado na aprovacao de
  conexao. A alocacao pede conexoes e nao lugares: quatro na sala sao tres conexoes.
- Falha de nuvem vira frase explicada em vez de excecao no Console. Projeto nao ligado ao
  UGS e perguntado antes de sair de casa, porque esse erro chega de la como configuracao
  generica e ninguem descobre por ele o que fazer.
- `NetRoomPanel`, em F3: hospedar e gerar codigo, copiar, colar codigo, entrar, ver quem
  esta dentro e sair. O IP direto continua na mesma tela, embaixo, porque e o caminho do
  dia a dia e ele nao depende de nuvem nenhuma (risco X8).
- A sala e IMGUI de proposito. A fundacao de UI Toolkit comeca na tarefa 2.3, com o
  inventario, e o menu da 4.12 vai substituir esta tela de qualquer jeito. Montar UXML e
  USS agora seria fundacao para uma tela descartavel.
- `NetDebugHud` perdeu os botoes de conectar e virou so leitura, que e o que a tarefa 1.9g
  pedia. Duas telas fazendo a mesma coisa e uma a mais para manter.
- **O caminho do Relay nao foi testado de ponta a ponta.** O projeto ainda nao esta ligado
  ao Unity Gaming Services, entao o que da para afirmar e que compila, que o caminho de
  erro responde com a instrucao certa, e que o IP direto continua subindo. Ligar o projeto
  em Project Settings > Services e o pre-requisito do portao da rede.

### 2026-09-11 — M1 tarefas 1.9e e 1.9f: a vida e o golpe passam a ser do host
- `CharacterVitals`: componente novo no modulo `Combat` que carrega a folha de atributos e
  a vida atual de um personagem. A vida so muda em quem tem autoridade e chega replicada
  nos outros. Cliente le e nunca escreve, e quem cobra isso e o proprio NGO.
- O bruxo e a capsula usam o mesmo componente. Vida replicada e um problema so, resolvido
  uma vez, e o `MonsterDef` da tarefa 1.20 vai herdar ele pronto.
- Os valores base nao viajam pela rede: saem do mesmo `StatBlockDef` em todas as maquinas.
  O que viaja e o que diverge, e hoje isso e so a vida. Pocoes e talentos entram no M2.
- `CombatDummy` perdeu a vida propria. Piscar, tombar e levantar viraram reacao local ao
  numero que o host mudou, o que custa zero RPC e vale para as quatro telas.
- O ataque virou pedido: tres mensagens por golpe, comecou, abriu e fechou, em vez de uma
  por quadro. Entre abrir e fechar, quem consulta a fisica e o host, no proprio `Update`,
  usando a posicao que o `NetworkTransform` ja traz do dono.
- `MeleeHitbox` e `DamagePipeline` rodam so no host. Duas maquinas resolvendo o mesmo golpe
  tiram vida duas vezes, ou nenhuma.
- Um golpe cujo abrir e fechar chegam no mesmo quadro do host ainda produz exatamente uma
  consulta. Sem isso, quem joga com ping alto teria golpe que as vezes nao sai.
- `IMeleeAttacker` nao mudou uma linha: o `AttackState` continua sem saber que rede existe,
  e os eventos de animacao do M4 (ADR 0007) entram por onde sempre iam entrar.
- Sem rede, o caminho e o de antes: quem pede e quem resolve sao o mesmo objeto, e a maquina
  de estados dirige a janela quadro a quadro. A `Sandbox_Combate` continua jogavel sozinha.
- O prefab de jogador estava sendo gravado com identificador de rede zero. No editor isso
  nao aparece, porque o `OnValidate` do `NetworkObject` conserta em memoria ao abrir o
  asset; no build nao ha `OnValidate`, e o jogador simplesmente nao nasceria no executavel.
  O `SandboxSetup` agora forca o calculo depois de gravar o prefab.
- As tres capsulas da sandbox ganharam `NetworkObject`. Sem isso cada participante mataria
  a propria copia do alvo e o dano nao bateria entre as telas.
- Painel F1 mostra a vida e de onde ela vem, `[eu resolvo]` ou `[o host manda]`.
- `docs/07` secao 3: o pacote do Netcode nao e modulo nosso e nao entra na tabela de niveis.
  Modulo com estado replicado referencia ele direto, sem passar pelo `Net`. `Combat` e o
  primeiro caso, e a seta continua apontando para o mesmo lado.
- `docs/13` secao 7: nota de implementacao sobre por que o `CombatDummy` nao virou
  `NetworkBehaviour` e a vida saiu para um componente proprio.
- 121 testes passando, contra 115 antes.

### 2026-09-10 — M1 tarefas 1.9a a 1.9d e 1.9g: a rede entra
- Pacotes de rede instalados contra o editor 6000.6.0f1: NGO 2.13.2, Transport 6.6.0,
  Services Core 1.18.0, Authentication 3.7.4, Relay 1.2.0 e Multiplayer Play Mode 3.0.0.
- Modulo `Net` novo, no nivel 1 do grafo de asmdef, logo acima de `Core`. Ele hospeda a
  sessao e nao conhece nenhum sistema de jogo: quem conhece rede e o sistema, nunca o
  contrario. `Net` referenciando `Player` ou `Combat` e dependencia invertida.
- `NetLauncher`: transporte direto por IP, sem depender de nuvem para testar (risco X8).
  A instancia principal sobe como host e a virtual entra com `-lb-client` nos argumentos.
- `NetSpawnRing`: o host aprova a conexao e escolhe onde cada bruxo nasce, em circulo.
  Sem isso quatro personagens nascem dentro um do outro e o `CharacterController` chuta
  todo mundo para fora, o que parece bug de rede e nao e.
- `NetDebugHud` em F2: papel, cliente, ida e volta, e por personagem quem move e quem
  resolve dano. Pedido pelo risco X10 desde o primeiro dia, e nao no fim.
- `PlayerBrain` reescrito como `NetworkBehaviour`, o unico arquivo que a rede obrigou a
  reescrever. O dono liga input, movimento, camera e cursor; o companheiro desliga
  locomocao e input e e desenhado pela posicao que chega do dono dele.
- Sem rede ligada, o jogador local e dono de si mesmo. A `Sandbox_Combate` continua
  jogavel sozinha: rede nao pode virar pre-requisito para testar combate.
- O jogador saiu da cena e virou prefab com `NetworkObject` e `NetworkTransform` em
  autoridade de dono. Quem cria e o host, um por conexao. O pivo de camera fica na cena,
  porque e local por natureza, e o dono se prende a ele quando nasce.
- `ThirdPersonCameraRig` parou de reclamar da falta de alvo no `Awake`. Com rede, o alvo
  so existe quando o personagem do dono nasce, e um aviso que aparece sempre ninguem le.
- Sete assets de dados que nunca tinham sido versionados entraram no repositorio: os dois
  golpes, o golpe em grupo, as duas espadas, a afinacao do jogador e o bloco do barghest.
  Sem eles, um clone novo abria a sandbox com o `PlayerMeleeAttacker` se desligando sozinho
  no `Awake`, ou seja, sem ataque e sem erro visivel.

### 2026-09-10 — Pivo para cooperativo (documentacao)
- O slice do Capitulo I passa a ser jogado por 2 a 4 pessoas em sessao privada, com
  entrada por codigo de convite. Zona, contrato e combate seguem os mesmos.
- `docs/13`: pilares sob coop, escolas em vez de personagens nomeados, modelo de
  autoridade, auditoria de reuso dos 43 scripts e custo estimado do pivo.
- Investigacao vira coletiva: cada escola le um tipo de vestigio e nenhuma le todos.
  Sem isso um jogador acha a pista e os outros tres viram plateia.
- Pilares 3 e 4 adiados. A escolha da Abigail nao tem resposta boa para quatro pessoas
  dentro do orcamento, e adiar e melhor do que entregar votacao.
- Duas escolas no slice, Lobo e Grifo. Escola e dado: bloco de atributos, afinidade de
  postura e intensidade de sinal. Nenhum `if` por escola em codigo de combate.
- ADR 0008: Netcode for GameObjects com Relay. Autoridade dividida, o dono simula o
  proprio personagem e o host resolve todo o dano. Sem predicao nem reconciliacao,
  porque e cooperativo contra IA e esse problema nao precisa ser resolvido aqui.
- A rede entra antes da tarefa 1.10, nao depois do M1. A FSM tem dois estados hoje e
  vai ter doze, e a janela de aparo de 0,18 s e menor que o ping de muita gente.
- Nove tarefas de rede (1.9a a 1.9i) e cinco de escola (1.31 a 1.35) no `docs/12`.
- O portao M1 mudou: duas pessoas querem continuar depois de 10 minutos, **e** uma delas
  fez algo que a outra nao conseguiria sozinha. A segunda metade e o teste de verdade.
- Custo assumido: 36 a 48 h a mais no M1, que sai de 4 para 7 ou 8 semanas.

### 2026-09-10 — M1 tarefas 1.6 a 1.9: FSM, buffer de input, ataques e hitbox
- Maquina de estados do jogador escrita a mao, com contrato de quatro metodos e tempo
  por parametro. Estados `Locomotion` e `Attack`.
- `PlayerStateRules`: a regra de ouro do combate em um lugar so. Nenhum input cancela
  acao comprometida, exceto esquiva e rolamento, mais atordoamento, morte e dialogo,
  que sao o mundo agindo sobre o jogador e nao input.
- `InputBuffer` de 0,2 s. Sem ele o combate parece irresponsivo mesmo com numeros certos.
- `AttackDef`: anticipacao, janela de dano e recuperacao em asset, com os tempos do
  doc 03 secao 4. Golpes leve, forte e de grupo criados.
- `MeleeHitbox`: consulta de capsula sem alocacao, filtrada por arco, com lista de
  ja-atingidos por golpe. Sem `OnTriggerEnter` em colisor de espada.
- `CombatDummy`: alvo de sandbox que recebe dano, pisca e revive, para dar o que bater.
- Painel de debug mostra estado, fase do golpe, buffer e alvos atingidos.
- ADR 0007: a janela de dano e dirigida por tempo decorrido e nao por evento de animacao,
  porque nao existe Animator no projeto ainda. `AttackDef` tem chave para inverter isso
  quando as animacoes entrarem no M4.
- 115 testes passando, contra 55 antes.

### 2026-09-10 — M1 tarefas 1.1 a 1.5: folha de atributos e pipeline de dano
- `StatSheet`: valores base mais modificadores Flat, PercentAdd e PercentMult, com cache
  por atributo e evento de mudanca. Formula independente da ordem de chegada.
- `StatModifier` carrega a origem, para que uma pocao que vence leve embora exatamente
  os proprios bonus e nenhum outro.
- `StatBlockDef`: valores base em asset.
- Pipeline de dano com os 11 estagios do doc 03 secao 9, cada um um objeto testavel,
  com log passo a passo que mostra cada multiplicador aplicado.
- `CombatTuningDef`: todos os multiplicadores de combate em um asset. E a superficie
  de balanceamento do combate inteiro.
- Assets criados: `CombatTuning` e `StatBlock_Player` com os numeros do doc 03 secao 12.
- 55 testes passando. Os dois cenarios de referencia batem: preparado 2,19x,
  despreparado 0,41x, razao de 5,3 vezes entre eles.
- `PlayerLocomotion.Tick(deltaTime)`: o tempo passou a entrar por parametro. Os testes
  de movimento simulam passos fixos de 1/60 e deixaram de ser intermitentes.
- Nomenclatura: o atributo Vigor virou `Endurance` e o recurso Vigor virou `Stamina`
  em codigo, porque os dois colidiam. Registrado no doc 07 secao 4.2.

### 2026-09-09 — Agentes e skills de projeto
- `CLAUDE.md`: contexto carregado em toda sessao, com regras de arquitetura, pilares,
  escopo travado e definicao de pronto.
- Sete agentes em `.claude/agents/`, um por setor: unity-engenheiro, game-designer,
  narrativa-ink, arte-tecnica, qa-unity, revisor-arquitetura e pesquisador-tw1.
- Seis skills em `.claude/skills/`: unity-batch, novo-sistema, novo-monstro,
  novo-contrato, balancear-combate e adr.
- `scripts/unity.sh`: roda o editor sem interface para compilar, testar, buildar e
  executar metodo, com deteccao de versao, guarda de lockfile e leitura dos resultados.

### 2026-09-09 — Movimento e camera de terceira pessoa (tarefas 0.9 a 0.12)
- `PlayerControls.inputactions`: 17 acoes e 37 bindings, teclado, mouse e gamepad,
  conforme o doc 02 secao 4.
- Modulos novos `TW1R.Player` e `TW1R.Camera` (ADR 0006). O grafo de dependencia do
  doc 07 virou tabela de niveis.
- `PlayerInputReader`: unico ponto do jogo que fala com o Input System.
- `PlayerLocomotion`: caminhada a 2,0 m/s, corrida a 5,5 m/s, gravidade, rampas e giro
  suavizado. Sem nenhuma referencia a input ou camera, o que o torna testavel.
- `ThirdPersonCameraRig`: pivo com yaw livre e pitch limitado entre -35 e 70 graus.
  Nao le input, recebe deltas.
- `PlayerBrain`: liga os tres e trava o cursor. E onde a FSM vai morar.
- `PlayerDebugOverlay`: painel IMGUI com FPS, velocidade, posicao e angulos. F1 esconde.
- `CM_Exploration`: CinemachineCamera com follow amortecido, mira dura e deoccluder
  contra parede.
- `GameLayers`: nomes e mascaras de layer centralizados.
- Cena `Sandbox_Combate` montada por script (`SandboxSetup`), nao a mao.
- 19 testes automatizados passando: 11 em EditMode para a camera, 8 em PlayMode para
  o movimento.
- `.editorconfig`, `.vscode/settings.json`, `extensions.json` e `launch.json`.
  VS Code definido como editor externo, com a extensao vstuc instalada.
- `.gitattributes` passa a fixar LF em codigo, para concordar com o `.editorconfig`.

### 2026-09-09 — M0 parcial: projeto Unity criado e configurado
- Modulo Windows Build Support (IL2CPP) instalado no editor 6000.6.0f1.
- Projeto Unity criado em `unity/LoboBranco` a partir do template URP, resolvido para
  URP 17.6.0.
- Pacotes adicionados: Cinemachine 6.6.0, Behavior 1.0.16, Addressables 4.0.1,
  Localization 1.5.13, Splines 2.9.1, ProBuilder 6.1.2, Newtonsoft Json 3.2.2.
- Pacote Ink (inkle) instalado por Git URL.
- Pacotes removidos: Visual Scripting e Collab Proxy.
- URP configurada: Forward+, GPU Resident Drawer, GPU Occlusion Culling,
  Adaptive Probe Volumes, sombra a 60 m com 4 cascatas, depth e opaque texture ligadas.
- Arvore `Assets/_Project` criada com assembly definitions e o grafo do doc 07.
- 7 tags e 10 layers de usuario definidas; 36 pares de colisao desativados.
- Fisica a 60 Hz, 8 iteracoes de solver.
- 6 cenas criadas e registradas no Build Settings.
- `Assets/Editor/ProjectSetup.cs`: setup reproduzivel pelo menu Lobo Branco.
- Build de verificacao para Windows x64 gerada com sucesso, 0 erros de compilacao.
- Git inicializado com LFS e o merge driver UnityYAMLMerge para cenas e prefabs.

### 2026-09-09 — Pre-producao
- Estrutura do projeto criada.
- Levantamento completo: 13 documentos em `docs/`.
- ADRs 0001 a 0005 registradas.
- Planilhas de balanceamento, trilhas e ecos em `design/`.
