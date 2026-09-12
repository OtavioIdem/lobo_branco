# Changelog

Formato: uma linha por mudanca que o jogador ou o dev perceberia.

## [Nao lancado]

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
