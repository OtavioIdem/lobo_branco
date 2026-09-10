# Changelog

Formato: uma linha por mudanca que o jogador ou o dev perceberia.

## [Nao lancado]

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
- Arvore `Assets/_Project` criada com 15 assembly definitions e o grafo de dependencias
  do doc 07.
- 7 tags e 10 layers de usuario definidas; 36 pares de colisao desativados.
- Fisica a 60 Hz, 8 iteracoes de solver.
- 6 cenas criadas e registradas no Build Settings: Boot, MainMenu, Sandbox_Combate,
  Zone_Vilarejo, Zone_Floresta, Zone_Cripta.
- `Assets/Editor/ProjectSetup.cs`: setup reproduzivel pelo menu Lobo Branco.
- Build de verificacao para Windows x64 gerada com sucesso, 0 erros de compilacao.
- Git inicializado com LFS e o merge driver UnityYAMLMerge para cenas e prefabs.

### 2026-09-09 — Pre-producao
- Estrutura do projeto criada.
- Levantamento completo: 13 documentos em `docs/`.
- ADRs 0001 a 0005 registradas.
- Planilhas de balanceamento, trilhas e ecos em `design/`.
