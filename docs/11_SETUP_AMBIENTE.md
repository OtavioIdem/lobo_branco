# 11 — Setup do Ambiente

Passo a passo verificado contra o que está instalado nesta máquina em 09/09/2026.

> **Este documento já foi executado.** As seções 2 a 6 foram aplicadas no projeto
> `unity/LoboBranco` em 09/09/2026, e o checklist da seção 7 passou, com build de Windows
> gerado e sem erros de compilação. O documento fica como referência para reproduzir o
> ambiente em outra máquina, e as etapas automatizáveis viraram
> `Assets/Editor/ProjectSetup.cs`, no menu **Lobo Branco → Setup**.

## 1. O que já está pronto

| Item | Estado |
|---|---|
| Unity Hub | Instalado em `C:\Program Files\Unity Hub` |
| Unity Editor `6000.6.0f1` | Instalado |
| Módulo Windows Build Support | Instalado |
| Módulo WebGL | Instalado (não usaremos) |
| Template URP 17.2.1 | Disponível |
| Git 2.49.0 | Instalado |
| Git LFS 3.6.1 | Instalado |
| Visual Studio 2022 | Instalado |
| VS Code | Instalado |

## 2. O que falta instalar

### 2.1 Módulos do Unity Editor
Abra o Unity Hub → Installs → engrenagem no `6000.6.0f1` → Add Modules. Confirme:
- **Windows Build Support (IL2CPP)** — necessário para builds de playtest
- **Documentation** — opcional, mas útil offline

### 2.2 Ferramentas de desenvolvimento C#
Escolha **um** dos dois:

**Opção A — Visual Studio 2022 (recomendada, já instalada)**
Abra o Visual Studio Installer e confirme que a carga de trabalho
**Game development with Unity** está marcada. Sem ela, o IntelliSense para Unity não
funciona bem.

**Opção B — VS Code**
Instale as extensões: `C# Dev Kit` (Microsoft) e `Unity` (Microsoft).

Depois, na Unity: Edit → Preferences → External Tools → External Script Editor → escolha
sua opção, e clique em **Regenerate project files**.

O IntelliJ IDEA instalado não serve para C# em Unity. Se você preferir a experiência
JetBrains, é o **Rider** (gratuito para uso não-comercial desde 2024).

### 2.3 Blender
Baixe em blender.org. Versão 4.2 LTS ou superior. Grátis.

### 2.4 Ink
Duas partes:
- **Inky** (editor de Ink) em github.com/inkle/inky/releases
- **Ink Unity Integration** — instalar via Package Manager por Git URL:
  `https://github.com/inkle/ink-unity-integration.git#upm`

## 3. Criar o projeto Unity

### Pelo Unity Hub (recomendado)
1. Unity Hub → Projects → New Project
2. Editor Version: `6000.6.0f1`
3. Template: **Universal 3D** (o `urp-blank`)
4. Project name: `LoboBranco`
5. Location: `E:\Unity_Games\TW1-Remaster\unity`
6. Create

O primeiro import leva de 5 a 15 minutos. Normal.

### Pela linha de comando (alternativa)
```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Unity.exe" -createProject "E:/Unity_Games/TW1-Remaster/unity/LoboBranco" -batchmode -quit -logFile -
```
Isso cria um projeto vazio **sem** URP configurado; você teria que adicionar o pacote e
criar os assets de pipeline à mão. Prefira o Hub.

## 4. Configuração obrigatória do projeto (antes de escrever código)

Faça tudo isto na primeira sessão. Cada item que você deixar para depois custa dez vezes mais.

### 4.1 Serialização em texto — o mais importante
`Edit → Project Settings → Editor → Asset Serialization → Mode: Force Text`

Sem isso, cenas e prefabs são binários, e qualquer merge no Git é uma perda total de trabalho.

### 4.2 Configurações do Player
`Edit → Project Settings → Player`
- Company Name, Product Name: `LoboBranco`
- Scripting Backend: **Mono** (durante o desenvolvimento)
- Api Compatibility Level: **.NET Standard 2.1**
- **Allow unsafe code:** desmarcado, a menos que necessário

### 4.3 Qualidade e URP
`Project Settings → Graphics` → confirme que o URP asset está atribuído.
No `UniversalRenderPipelineAsset`:
- Rendering Path: **Forward+**
- **GPU Resident Drawer:** Instanced Drawing
- **GPU Occlusion Culling:** ativado
- Depth Texture: ativado
- Opaque Texture: ativado (necessário para o efeito de Sentidos de Bruxo)
- Shadow Distance: 60, Cascades: 4

### 4.4 Física
`Project Settings → Physics`
- Default Solver Iterations: 8
- Layer Collision Matrix: configure as layers antes de criar prefabs

Layers a criar agora:
```
Player, Enemy, EnemyHitbox, PlayerHitbox, Environment, Interactable,
Clue, Water, NPC, Projectile, IgnoreCamera
```

### 4.5 Tempo
`Project Settings → Time` → Fixed Timestep: **0.01666** (60 Hz).
O padrão de 0,02 (50 Hz) causa inconsistência em detecção de hitbox. Isso importa em um
jogo de ação.

### 4.6 Tags
```
Player, Enemy, NPC, Interactable, Herb, Clue, Container, Merchant
```

## 5. Configurar o Git

Da raiz `E:\Unity_Games\TW1-Remaster`:

```bash
git init
git lfs install
git add .gitignore .gitattributes README.md docs/
git commit -m "Levantamento inicial de pre-producao"
```

### Merge driver para cenas e prefabs
```bash
git config merge.unityyamlmerge.name "Unity YAML Merge"
git config merge.unityyamlmerge.driver '"C:/Program Files/Unity/Hub/Editor/6000.6.0f1/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %A %B %A'
```
O `.gitattributes` já criado aponta `.unity` e `.prefab` para esse driver.

### Repositório remoto
Se for usar GitHub: crie o repositório **privado**. Um repositório público com o nome
"The Witcher" atrai atenção que você não quer neste estágio (doc 10).

Atenção ao limite de LFS gratuito do GitHub: 1 GB de armazenamento e 1 GB de banda por mês.
Um projeto Unity com assets estoura isso rápido. Alternativas: GitLab (10 GB grátis),
Azure DevOps (LFS ilimitado em repositórios privados), ou um Git local com backup em disco
externo.

## 6. Cena de sandbox — faça isto no primeiro dia

Crie `Assets/_Project/Scenes/Sandbox_Combate.unity` com:
- Um plano de 50 × 50 m
- Iluminação simples
- O jogador
- Botões de debug para spawnar cada tipo de inimigo
- Um painel de debug mostrando vitalidade, vigor, toxicidade, postura, elos de Fluxo
- Um console imprimindo o log do pipeline de dano

Essa cena é onde você vai passar 60% do tempo de desenvolvimento. Investir duas horas nela
em M0 economiza dezenas depois. Nunca teste combate na cena do vilarejo.

## 7. Verificação final

Antes de considerar M0 concluído:

```
[ ] O projeto abre sem erros no Console
[ ] Build de Windows gera e o .exe roda
[ ] git status limpo, e um arquivo binário grande foi rastreado pelo LFS
[ ] Uma cena salva e reaberta mantém tudo
[ ] O IntelliSense funciona no seu editor
[ ] Sandbox_Combate abre e o personagem se move
[ ] Gamepad e teclado funcionam
[ ] Asset Serialization está em Force Text
```

## 8. Recursos de aprendizado (curados, não exaustivos)

Para não se perder em tutoriais infinitos, estes cobrem exatamente o que este projeto precisa:

| Assunto | Recurso |
|---|---|
| Unity 6 e URP | Unity Learn — Pathway "Junior Programmer" e a documentação de URP |
| Input System | Documentação oficial do pacote; comece por Action Assets |
| Cinemachine 3 | Documentação oficial; a API mudou muito da 2.x, ignore tutoriais antigos |
| ScriptableObjects como arquitetura | Palestra "Game Architecture with Scriptable Objects" (Unite Austin 2017, Ryan Hipple) — ainda é a melhor referência sobre o assunto |
| Combate de ação | Canal "iHeartGameDev" e a série de combate do "Bardent" |
| Behavior trees | Documentação do pacote `com.unity.behavior` |
| Ink | Documentação oficial de Ink, "Writing with Ink" |
| Game feel | Palestra "Juice it or lose it" (Martin Jonasson e Petri Purho) |

**Aviso sobre tutoriais:** a maior parte do conteúdo de Unity no YouTube é de 2019 a 2022 e
usa o Input Manager antigo, Cinemachine 2 e padrões de singleton. Funciona, mas te ensina
hábitos que você vai ter que desaprender. Prefira a documentação oficial para APIs, e
tutoriais para conceitos.
