---
name: novo-sistema
description: Cria um sistema de gameplay novo na Unity seguindo a arquitetura do projeto - módulo com asmdef no nível certo do grafo, dados em ScriptableObject, lógica testável e testes. Use sempre que for implementar um sistema do zero (stats, inventário, alquimia, quests, save, IA, progressão) ou criar um módulo novo. Use também quando alguém disser "implementa o sistema de X", "cria o módulo de Y", "começa o pipeline de dano" ou qualquer coisa que vá gerar código C# novo de gameplay. Seguir isso desde o primeiro arquivo é muito mais barato que reorganizar depois.
---

# Criar um sistema novo

## Ordem de trabalho

A ordem importa. Escrever `MonoBehaviour` primeiro e tentar extrair os dados depois é o
caminho que produz sistemas impossíveis de balancear.

### 1. Ler o documento antes de decidir qualquer coisa

Os números já estão escritos. Combate no `docs/03`, progressão no `docs/04`, alquimia
e itens no `docs/05`, quests e WorldState no `docs/06`, padrões no `docs/07`.

Se você está inventando um número, ou o documento não cobre o caso (e você deve dizer
isso), ou você não leu o documento.

### 2. Decidir onde o módulo entra no grafo

A tabela de níveis está no `docs/07` seção 3. As referências só apontam para baixo.

Antes de criar um módulo, pergunte se ele cabe em um existente. Módulo novo custa um
asmdef, uma linha na tabela e um ADR. Criar `TW1R.Player` e `TW1R.Camera` valeu porque o
pivô de câmera serve cutscene e diálogo, não só o jogador. Criar um módulo por classe não vale.

Se o módulo é novo, o asmdef segue este formato:

```json
{
    "name": "TW1R.NomeDoModulo",
    "rootNamespace": "LoboBranco.NomeDoModulo",
    "references": ["TW1R.Core", "TW1R.Stats"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

Cuidado com `Camera` como nome de namespace: ele colide com `UnityEngine.Camera`. O
módulo da câmera usa a pasta `Camera` e o namespace `LoboBranco.CameraSystem` por isso.

Depois: atualizar a tabela do `docs/07`, escrever o ADR, e adicionar o módulo às
referências de `TW1R.Bootstrap` e das assemblies de teste que precisarem dele.

### 3. Modelar os dados como ScriptableObject

Todo número de balanceamento vira asset. O teste é direto: dá para ajustar esse valor com
o jogo rodando, sem recompilar? Se não dá, balancear virou tarefa de programador e o
sistema está errado.

```csharp
[CreateAssetMenu(menuName = "LoboBranco/Combat/Monster")]
public sealed class MonsterDef : ScriptableObject
{
    public string displayName;
    public MonsterClass monsterClass;
    public float baseVitality = 55f;
    // ...
}
```

Assets em `Assets/_Project/Data/<Categoria>/`. Nomes da IP moram aqui e em localização,
nunca em `enum` ou nome de tipo (`tech/adr/0005`).

### 4. Escrever a lógica sem input e sem câmera

A parte que contém as regras não deve conhecer `UnityEngine.InputSystem` nem
`Camera.main`. Ela recebe valores por propriedade, e um componente fino de ligação faz a
leitura do mundo e escreve neles.

Isso não é purismo. É a diferença entre poder testar e não poder: `PlayerLocomotion` tem
oito testes de PlayMode justamente porque o teste escreve em `MoveInput` diretamente.

Para pipeline com etapas, como o cálculo de dano de onze estágios do `docs/03` seção 9,
faça cada etapa um objeto com interface, numa lista ordenada. Carregue um campo de log no
contexto que registre cada multiplicador aplicado. Sem esse log, um erro de balanceamento
é invisível.

### 5. Escrever teste para o que quebra em silêncio

Não busque cobertura. Busque as três categorias que causam dano invisível:

| Categoria | Exemplo | Onde |
|---|---|---|
| Matemática que ninguém confere | pipeline de dano, toxicidade | EditMode |
| Combinatória | resolução de receitas de alquimia | EditMode |
| Perda de dados | round-trip de save | EditMode |

EditMode quando a lógica é pura, porque roda em menos de um segundo. PlayMode só quando
precisar do loop de jogo ou da física.

Evite teste que mede tempo, distância percorrida ou contagem de frames: o framerate em
batchmode é errático e o teste fica intermitente. Meça a grandeza que o teste quer de
fato verificar. Para movimento relativo à câmera, meça direção.

Nomes de teste em português e descritivos, porque o nome é o que aparece quando falha:
`Yaw_de_referencia_de_90_graus_move_em_X_positivo`.

### 6. Verificar

```bash
.claude/skills/unity-batch/scripts/unity.sh compile
.claude/skills/unity-batch/scripts/unity.sh test all
```

Detalhes e tempos na skill `unity-batch`.

### 7. Fechar

- Linha no `CHANGELOG.md`
- Tarefa marcada no `docs/12_BACKLOG_VERTICAL_SLICE.md`
- ADR se houve decisão de arquitetura
- Documento atualizado se o código divergiu do que estava escrito

## Cena montada por script, não à mão

Hierarquia de cena montada no editor não é revisável em diff e não é reproduzível. Quando
um sistema precisar de objetos na cena, escreva um método de editor que monta tudo, com
`[MenuItem]`, como em `Assets/Editor/SandboxSetup.cs`. Rodar de novo reconstrói do zero.

## Erro de API de pacote

Não adivinhe nome de tipo duas vezes. A fonte está em
`unity/LoboBranco/Library/PackageCache/`, e um `grep` resolve em segundos o que tentativa
e erro resolve em três rodadas de cinco minutos:

```bash
grep -rn "enum BindingMode" unity/LoboBranco/Library/PackageCache/com.unity.cinemachine*/Runtime
```
