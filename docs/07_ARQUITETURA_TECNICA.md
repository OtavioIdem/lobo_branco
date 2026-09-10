# 07 — Arquitetura Técnica (Unity)

Ambiente verificado nesta máquina em 09/09/2026:

| Item | Detectado |
|---|---|
| Unity Editor | `6000.6.0f1` em `C:\Program Files\Unity\Hub\Editor\` |
| Módulos instalados | Windows Standalone, WebGL |
| Templates | `urp-blank 17.2.1`, `2d-cross-platform 7.0.0` |
| Git | 2.49.0 |
| Git LFS | 3.6.1 |
| IDEs | Visual Studio 2022 e 18, VS Code, IntelliJ IDEA 2025.2.4 |

**Faltando:** o módulo de suporte a IL2CPP costuma vir com o Windows Standalone; confirmar
no Unity Hub. Rider não está instalado (IntelliJ IDEA não substitui para C# em Unity) —
use **Visual Studio 2022** ou **VS Code com a extensão C# Dev Kit**.

## 1. Decisões de plataforma

### Render pipeline: URP

Você já tem o template URP instalado, e é a escolha correta aqui. Justificativa:

| | URP | HDRP |
|---|---|---|
| Qualidade visual máxima | Boa | Superior |
| Custo de aprendizado | Médio | Alto |
| Iteração e tempo de build | Rápido | Lento |
| Adequado a dev solo | **Sim** | Não |
| Roda em hardware modesto | Sim | Não |

Um protótipo de aprendizado não deve gastar seu tempo lutando com volumes de HDRP.
URP com **Forward+**, **Adaptive Probe Volumes** e pós-processamento bem calibrado produz
um resultado mais que suficiente. Se o projeto virar sério, a migração é possível mas dolorosa;
essa é uma decisão a registrar como ADR e reavaliar só depois do vertical slice.

Configuração inicial de URP:
- Rendering Path: **Forward+**
- **GPU Resident Drawer** ativado (Unity 6, ganho grande e gratuito em cenas com muita vegetação)
- **GPU Occlusion Culling** ativado
- Shadow cascades: 4, distância de 60 m
- **Adaptive Probe Volumes** para iluminação indireta em vez de light probes manuais
- Depth Texture e Opaque Texture ativados (o efeito de Sentidos de Bruxo precisa dos dois)

### Plataforma alvo
Windows x64, **Mono** durante o desenvolvimento (build muito mais rápido), **IL2CPP** só
para os builds de playtest.

## 2. Pacotes

Versões **resolvidas de fato** no projeto em 09/09/2026, não estimativas:

| Pacote | Versão | Para que |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.6.0 | Render pipeline |
| `com.unity.inputsystem` | 1.20.0 | Input com rebind |
| `com.unity.cinemachine` | 6.6.0 | Câmeras e Impulse |
| `com.unity.ai.navigation` | 2.0.14 | NavMesh |
| `com.unity.behavior` | 1.0.16 | Behavior trees dos inimigos |
| `com.unity.addressables` | 4.0.1 | Streaming de zonas |
| `com.unity.localization` | 1.5.13 | pt-BR e chaves de texto |
| `com.unity.splines` | 2.9.1 | Trajetórias e caminhos |
| `com.unity.probuilder` | 6.1.2 | Greybox |
| `com.unity.timeline` | 6.6.0 | Cinemáticas |
| `com.unity.test-framework` | 1.8.0 | Testes |
| `com.unity.nuget.newtonsoft-json` | 3.2.2 | Serialização de save |
| `com.unity.ugui` | 2.6.0 | UI e TextMeshPro |

`TextMeshPro` **não** é mais um pacote separado no Unity 6; ele vive dentro de
`com.unity.ugui`.

Removidos do template por não serem usados e custarem tempo de compilação:
`com.unity.visualscripting` e `com.unity.collab-proxy`.

A instalação é reproduzível: `Assets/Editor/ProjectSetup.cs` no projeto Unity define a
lista e resolve as versões pelo Package Manager. Para repetir em outra máquina, use o menu
**Lobo Branco → Setup**.

### Pacotes fora do registro Unity

| Pacote | Para que | Licença |
|---|---|---|
| **Ink** (inkle) + Ink Unity Integration | Diálogo e narrativa ramificada | MIT |
| **NaughtyAttributes** ou **Odin** (pago) | Melhorar Inspectors de dados | MIT / comercial |
| **DOTween** | Tweens de UI e câmera | Grátis / Pro pago |

**Recomendação sobre Ink:** use Ink. Construir um editor de grafo de diálogo é um projeto
de dois meses que não é o seu jogo. Ink é texto, versionável em Git, testável fora da Unity,
e integra com C# por variáveis externas — que é exatamente como o `WorldState` precisa
funcionar.

## 3. Estrutura de pastas do projeto Unity

```
unity/LoboBranco/
├── Assets/
│   ├── _Project/                    ← tudo nosso vive aqui, com underscore para ficar no topo
│   │   ├── Art/
│   │   │   ├── Characters/
│   │   │   ├── Environment/
│   │   │   ├── VFX/
│   │   │   └── Materials/
│   │   ├── Audio/
│   │   │   ├── SFX/ Music/ Ambience/
│   │   ├── Code/
│   │   │   ├── Core/              TW1R.Core.asmdef
│   │   │   ├── Stats/             TW1R.Stats.asmdef
│   │   │   ├── Camera/            TW1R.Camera.asmdef
│   │   │   ├── Combat/            TW1R.Combat.asmdef
│   │   │   ├── Player/            TW1R.Player.asmdef
│   │   │   ├── Inventory/         TW1R.Inventory.asmdef
│   │   │   ├── Alchemy/           TW1R.Alchemy.asmdef
│   │   │   ├── Progression/       TW1R.Progression.asmdef
│   │   │   ├── Quests/            TW1R.Quests.asmdef
│   │   │   ├── Dialogue/          TW1R.Dialogue.asmdef
│   │   │   ├── AI/                TW1R.AI.asmdef
│   │   │   ├── Investigation/     TW1R.Investigation.asmdef
│   │   │   ├── Save/              TW1R.Save.asmdef
│   │   │   ├── UI/                TW1R.UI.asmdef
│   │   │   ├── Bootstrap/         TW1R.Bootstrap.asmdef  (só aqui se conhece tudo)
│   │   │   └── Tests/             EditMode e PlayMode, com asmdef próprio cada
│   │   ├── Data/                    ← ScriptableObjects, a fonte da verdade em runtime
│   │   │   ├── Items/ Monsters/ Skills/ Recipes/ Quests/ LootTables/
│   │   ├── Ink/                     ← .ink e .json compilado
│   │   ├── Localization/
│   │   ├── Prefabs/
│   │   │   ├── Characters/ Enemies/ Props/ UI/ VFX/
│   │   ├── Scenes/
│   │   │   ├── Boot.unity           carrega tudo, nunca é jogada direto
│   │   │   ├── MainMenu.unity
│   │   │   ├── Zone_Vilarejo.unity
│   │   │   ├── Zone_Floresta.unity
│   │   │   ├── Zone_Cripta.unity
│   │   │   └── Sandbox_Combate.unity   ← cena de teste, essencial
│   │   ├── Settings/                URP assets, Input Actions, Render features
│   │   └── UI/                      UXML, USS, sprites
│   ├── Plugins/                     ← terceiros (Ink, DOTween)
│   └── StreamingAssets/
├── Packages/manifest.json
└── ProjectSettings/
```

**Regra:** nada nosso fora de `_Project`. Assets de terceiros nunca entram em `_Project`.
Isso torna possível deletar ou atualizar um pacote sem caçar arquivos.

### Assembly Definitions — por que se dar esse trabalho

Sem `.asmdef`, cada mudança em qualquer script recompila tudo. Com 12 módulos, mudar UI
recompila só UI. Em um projeto que você vai iterar por meses, isso é a diferença entre
1 segundo e 15 segundos por edição, milhares de vezes.

Grafo de dependência permitido. Cada módulo só pode referenciar os que estão **abaixo**
dele na tabela, nunca acima nem ao lado:

| Nível | Módulo | Referencia |
|---|---|---|
| 5 | `Bootstrap` | todos |
| 4 | `UI` | Core, Stats, Combat, Inventory, Alchemy, Progression, Quests, Investigation, Dialogue |
| 3 | `Player` | Core, Stats, Combat, Camera |
| 3 | `AI` | Core, Stats, Combat |
| 3 | `Alchemy` | Core, Stats, Inventory |
| 3 | `Quests` | Core, Dialogue |
| 2 | `Combat`, `Inventory`, `Progression`, `Investigation` | Core, Stats |
| 2 | `Camera` | Core |
| 1 | `Stats`, `Save`, `Dialogue` | Core |
| 0 | `Core` | nada |

`Core` não depende de nada. Se `Core` precisar de algo, o algo está no lugar errado.

Dois módulos merecem explicação, porque não são óbvios:

- **`Camera`** existe separado de `Player` porque o pivô de câmera também serve cutscene,
  câmera de diálogo e câmera livre de debug. Ele não lê input: recebe deltas. Isso é o que
  o torna reutilizável e testável em EditMode.
- **`Player`** é o dono da máquina de estados e do leitor de input. Ele conhece `Camera`,
  e não o contrário. Se a câmera precisasse conhecer o jogador, haveria ciclo.

## 4. Padrões de código

### 4.1 Dados em ScriptableObject, comportamento em MonoBehaviour

Regra absoluta. Um `MonoBehaviour` nunca contém um número de balanceamento.

```csharp
[CreateAssetMenu(menuName = "LoboBranco/Combat/Monster")]
public sealed class MonsterDef : ScriptableObject
{
    public string displayName;
    public MonsterClass monsterClass;      // Besta, Necrofago, Espectro...
    public StanceArchetype archetype;      // define a postura ideal
    public float baseVitality = 55f;
    public float baseDamage   = 16f;
    public float armor        = 2f;
    public OilClass vulnerableToOil;
    public SignType vulnerableToSign;
    public BestiaryEntry bestiaryEntry;
    public LootTable loot;
    public GameObject prefab;
}
```

Balancear o jogo passa a ser editar assets, não código. Você pode ajustar números com o
jogo rodando e ver o efeito imediatamente.

### 4.2 Sistema de stats com modificadores

Isso é a peça de infraestrutura mais importante do projeto. Poções, talentos, mutagênios,
equipamento e buffs de combate todos precisam alterar os mesmos números, e precisam poder
ser removidos individualmente.

**Implementado.** `Assets/_Project/Code/Stats/`.

```csharp
public enum ModifierOp { Flat, PercentAdd, PercentMult }

public readonly struct StatModifier
{
    public readonly StatType Stat;
    public readonly ModifierOp Op;
    public readonly float Value;
    public readonly object Source;   // quem aplicou, para remoção precisa
}

public sealed class StatSheet
{
    // valor = (base + soma dos Flat) * (1 + soma dos PercentAdd) * produto dos PercentMult
    public float Get(StatType stat);
    public void SetBase(StatType stat, float value);
    public void AddModifier(in StatModifier mod);
    public int RemoveAllFromSource(object source);    // ← a razão de existir do campo Source
    public event Action<StatType> StatChanged;        // ← a UI escuta isto, não faz polling
}
```

O campo `Source` é o que impede o bug clássico de "a poção venceu mas o bônus ficou".

Somar os `PercentAdd` antes de multiplicar é o que torna o resultado independente da ordem
de chegada. Duas poções de mais 30% dão mais 60%, não mais 69%. Beber na ordem inversa
tem que dar o mesmo número, e há teste para isso.

Valores base vêm de `StatBlockDef`, um ScriptableObject. Balancear a vida de um inimigo é
editar asset.

#### Nota de nomenclatura: Vigor

O jogo exibe **Vigor** para o recurso gasto em sinais e esquiva, e o original também
chamava um dos quatro atributos de Vigor. Em código isso colidiria, então:

| Documento | Código |
|---|---|
| Atributo Vigor | `StatType.Endurance` |
| Recurso Vigor | `StatType.MaxStamina`, `StaminaRegen` |

Os nomes exibidos vêm de localização, nunca do `enum` (`tech/adr/0005`).

### 4.3 Pipeline de dano como cadeia de estágios

O doc 03, seção 9, define 11 estágios. Implemente-os como uma lista ordenada de
`IDamageStage`, não como uma função de 200 linhas.

```csharp
public sealed class DamageContext
{
    public IDamageDealer Attacker;
    public IDamageable   Target;
    public float Amount;
    public DamageType Type;
    public Stance Stance;
    public WeaponMaterial Material;
    public int FlowChain;
    public bool IsCritical;
    public List<string> Log;    // ← ouro puro para debug: mostra cada multiplicador aplicado
}

public interface IDamageStage { void Apply(DamageContext ctx); }
```

O campo `Log` é o que torna o balanceamento depurável. Um console de debug que imprime
"12 base × 1,45 forte × 0,8 afinidade errada × 0,35 aço em monstro = 4,87" te salva
semanas de confusão.

Cada estágio é testável em isolamento com o Test Framework. Isso é a primeira coisa a
escrever testes para, porque é onde erros são invisíveis.

### 4.4 Máquina de estados do jogador

Uma FSM explícita, escrita à mão. Não use um plugin, e não use `if/else` em `Update`.

Estados: `Locomotion`, `Attack`, `Dodge`, `Roll`, `Parry`, `Riposte`, `CastSign`,
`UseItem`, `Stagger`, `Death`, `Interact`, `DialogueLocked`.

Contrato de cada estado: `OnEnter`, `OnTick(dt)`, `OnExit`, `CanTransitionTo(state)`.
A regra do doc 03 ("nada cancela animação exceto esquiva") vive em `CanTransitionTo`,
em um lugar só. Isso é o que faz a regra ser real em vez de aspiracional.

**Buffer de input:** guarde o último input por 0,2 s. Sem isso, o combate parece
irresponsivo mesmo com números perfeitos. É a causa número um de "meu combate está estranho".

### 4.5 Hitboxes por evento de animação

Não use `OnTriggerEnter` em uma colisão de espada. Use eventos de animação que ligam e
desligam uma janela de dano, e faça `Physics.OverlapCapsule` durante ela, com uma lista de
alvos já atingidos para não acertar duas vezes.

```
Animação de ataque:
  frame 8   → AnimEvent: HitboxOn(damageProfile)
  frame 14  → AnimEvent: HitboxOff()
```

Isso te dá controle de frame preciso e é como jogos de ação de verdade fazem.

### 4.6 Comunicação entre sistemas

Evite singletons espalhados. Duas ferramentas, e só duas:

1. **Canais de evento em ScriptableObject** para eventos de jogo (`OnMonsterKilled`,
   `OnQuestStageChanged`, `OnToxicityChanged`). São assets, então designers e você podem
   conectar coisas no Inspector, e nenhum módulo precisa referenciar o outro.
2. **Um `GameServices` estático** com registro explícito no `Bootstrap`, para os serviços
   de verdade (`SaveService`, `WorldState`, `TimeService`). Não é elegante, mas em um
   projeto solo um container de DI custa mais do que resolve.

### 4.7 WorldState — o coração do doc 06

```csharp
public sealed class WorldState
{
    private readonly Dictionary<string, object> _flags = new();

    public T Get<T>(string key, T fallback = default);
    public void Set<T>(string key, T value);   // registra em log; é a única escrita
    public bool Evaluate(ConditionDef condition);
    public event Action<string> OnFlagChanged;
}
```

Toda flag declarada como um `WorldFlagDef` (ScriptableObject) com chave, tipo, valores
possíveis, capítulo de plantio e capítulo de colheita. Assim a planilha de Ecos do doc 06
é gerada a partir dos assets, e não pode divergir deles.

E o mais importante: as variáveis externas do Ink se ligam direto ao `WorldState`. Um
diálogo em Ink lê e escreve as mesmas flags que o resto do jogo.

## 5. Save e load

**Formato:** JSON via Newtonsoft, em `Application.persistentDataPath`. Legível, depurável,
suficiente. Nada de `BinaryFormatter` (obsoleto e insegura).

**Padrão:** cada sistema implementa `ISaveable`.

```csharp
public interface ISaveable
{
    string SaveKey { get; }
    object CaptureState();
    void RestoreState(object state);
}
```

Um `SaveService` percorre os `ISaveable` registrados, monta um dicionário, adiciona
metadados (versão, capítulo, timestamp, screenshot) e grava.

**Duas coisas que precisam estar certas desde o dia 1:**
1. **Versionamento.** Grave um `saveVersion` e escreva migrações. Você vai mudar a estrutura
   de dados dezenas de vezes, e perder saves de teste toda vez é doloroso.
2. **Identidade estável de objeto.** Todo objeto salvável no mundo precisa de um GUID
   persistente, gerado no editor e serializado. `GetInstanceID` muda entre execuções.

Um teste automatizado de round-trip (salvar → limpar → carregar → comparar) é a coisa de
maior retorno sobre investimento em todo o projeto.

## 6. IA de inimigos

**Ferramenta:** `com.unity.behavior` (behavior trees oficiais do Unity, com editor de grafo)
mais `com.unity.ai.navigation` para NavMesh.

Estrutura de comportamento de um monstro:

```
Selector raiz
├── [Morto?]         → nada
├── [Atordoado?]     → aguardar
├── [Alvo visível?]  → Sequência de Combate
│     ├── Escolher papel (Flanqueador / Cercador / Arremessador)
│     ├── Manter distância de engajamento conforme o papel
│     ├── Aguardar o token de ataque   ← ver abaixo
│     └── Executar ataque com telegrafo
└── Patrulha / Ocioso
```

**Token de ataque (attack token):** um coordenador por encontro concede no máximo 2 tokens
de ataque simultâneos. Sem isso, 5 barghests atacam ao mesmo tempo e o combate é injusto e
ilegível. Isso é o truque mais importante de IA de combate, e quase todo jogo de ação bom
faz alguma versão dele.

## 7. Câmera

**Cinemachine 3.x.** Três câmeras virtuais, com blends:
- `CM_Exploration` — terceira pessoa, distância 4,5 m, damping alto
- `CM_Combat` — mais perto (3,2 m), damping baixo, com group framing quando há trava de alvo
- `CM_Dialogue` — over-the-shoulder, alternando entre falantes

Impulse Source em cada impacto, para o screen shake do doc 03, seção 11.

## 8. Streaming de zonas

**Addressables**, com uma cena `Boot` persistente que carrega zonas aditivamente.
Cada zona é um grupo de Addressables. Transição com fade, pré-carregamento assíncrono.

Isso resolve D12 e é muito mais fácil de fazer no começo do que retrofitar depois.

## 9. Testes

Não é sobre cobertura. É sobre as três coisas que quebram silenciosamente:

| Teste | Por que |
|---|---|
| Pipeline de dano | Um multiplicador errado é invisível e arruína o balanceamento |
| Round-trip de save | Perder progresso é o pior bug possível |
| Resolução de receitas de alquimia | Lógica de substâncias é combinatória e propensa a erro |

Modo Play tests para: transições da FSM do jogador, e avanço de estágio de quest.

## 10. Registro de Decisões de Arquitetura (ADR)

Toda decisão técnica com consequência vai para `tech/adr/NNNN-titulo.md`, no formato:

```markdown
# ADR 0001 — Usar URP em vez de HDRP
Data: 2026-09-09
Status: aceita

## Contexto
...
## Decisão
...
## Consequências
...
## Alternativas consideradas
...
```

Isso parece burocracia para um projeto solo. Não é: em três meses você não vai lembrar por
que escolheu URP, e vai reabrir a discussão consigo mesmo. Um ADR de dez linhas encerra
esse desperdício.

ADRs a escrever imediatamente: URP contra HDRP, Ink contra editor próprio, JSON contra
binário, Behavior contra FSM própria.

## 11. Git e controle de versão

Sem Git configurado, você vai perder trabalho. Configuração obrigatória antes da primeira
linha de código:

1. `.gitignore` da Unity (já criado na raiz deste repositório).
2. **Git LFS** para binários (`.gitattributes` já criado).
3. **Serialização em texto:** Project Settings → Editor → Asset Serialization →
   **Force Text**. Sem isso, cenas e prefabs são binários e todo merge é uma perda total.
4. **UnityYAMLMerge** como merge driver para `.unity` e `.prefab`.
5. Ramos: `main` estável, `dev` de trabalho, `feature/*` para features grandes.
6. Commits pequenos. Um commit que toca 40 arquivos de cena não é revisável.

Aviso prático: **a Unity gera arquivos `.meta` que precisam ser commitados.** Nunca
ignore `.meta`. Perder um `.meta` desconecta referências e é uma das piores dores da engine.

## 12. Orçamento de performance (alvo)

Alvo: **1080p, 60 fps** em uma GPU de gama média (algo como uma RTX 3060).

| Métrica | Orçamento |
|---|---|
| Tempo de frame | 16,6 ms |
| Draw calls | menos de 1.200 |
| Triângulos por frame | menos de 3 M |
| Personagem principal | 40 a 60 k triângulos |
| Inimigo | 15 a 25 k |
| Texturas | 2048² para personagens, 1024² para props |
| Luzes em tempo real com sombra | máximo 3 |
| Alocação por frame | 0 bytes em combate (sem GC spikes) |

A última linha é a mais difícil e a mais importante. Zero alocação por frame significa:
sem LINQ em `Update`, sem `foreach` sobre coleções que fazem box, sem concatenação de
string, sem `GetComponent` em loop, e uso de `NonAlloc` em todas as queries de física.
Um GC spike no meio de um riposte é um bug de jogabilidade.
