# Changelog

Formato: uma linha por mudanca que o jogador ou o dev perceberia.

## [Nao lancado]

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
