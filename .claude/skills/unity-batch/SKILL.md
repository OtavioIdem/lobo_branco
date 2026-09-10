---
name: unity-batch
description: Roda o editor da Unity sem interface para compilar, rodar testes, gerar build de Windows ou executar um método de editor. Use sempre que precisar saber se o projeto compila, rodar a suíte de testes, verificar se uma mudança quebrou algo, gerar um executável, ou executar qualquer coisa via -executeMethod. Use também quando alguém disser "compila", "roda os testes", "isso quebrou alguma coisa?", "gera um build", ou pedir verificação de qualquer código C# do projeto. Não tente montar a linha de comando do Unity.exe na mão: ela tem armadilhas que custam vários minutos por erro.
---

# Rodar a Unity sem interface

## Por que esta skill existe

A linha de comando da Unity é traiçoeira de três formas, e cada erro custa uma rodada
de vários minutos:

1. **`-quit` e `-runTests` são incompatíveis.** Com `-quit`, o editor fecha antes do
   runner terminar e você recebe um resultado vazio sem mensagem de erro.
2. **O código de saída mente.** A Unity sai com 0 mesmo com erros de compilação em alguns
   caminhos. Confiar no código de saída faz você declarar sucesso em cima de código quebrado.
3. **O sinal útil está enterrado.** Um log de execução tem entre 15 e 20 mil linhas, e as
   três que importam estão no meio.

O script `scripts/unity.sh` resolve os três. Use ele.

## Uso

Todos os comandos funcionam de qualquer diretório do repositório. O script acha a raiz
sozinho e lê a versão do editor de `ProjectVersion.txt`, então ele não apodrece quando
o projeto subir de versão.

```bash
.claude/skills/unity-batch/scripts/unity.sh compile
.claude/skills/unity-batch/scripts/unity.sh test all
.claude/skills/unity-batch/scripts/unity.sh test editmode
.claude/skills/unity-batch/scripts/unity.sh build
.claude/skills/unity-batch/scripts/unity.sh method LoboBranco.EditorTools.SandboxSetup.BuildSandbox
.claude/skills/unity-batch/scripts/unity.sh doctor
```

## Quanto tempo cada coisa leva

Isso determina como você chama o comando. Use `run_in_background: true` no Bash para
qualquer coisa acima de dois minutos, senão o timeout padrão mata a execução no meio e
deixa o projeto com um lockfile órfão.

| Comando | Primeira vez | Depois | Como chamar |
|---|---|---|---|
| `doctor` | instantâneo | instantâneo | foreground |
| `compile` | 2 a 4 min | 30 a 60 s | background |
| `test editmode` | 2 a 3 min | 40 s | background |
| `test playmode` | 3 a 4 min | 60 s | background |
| `build` | 6 a 8 min | 2 a 3 min | background |

Depois de disparar em background, espere a notificação de conclusão. Se precisar esperar
ativamente, use um laço até o processo sumir, nunca um `sleep` fixo:

```bash
until ! tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -qi "Unity.exe"; do sleep 10; done
```

## Interpretando o resultado

O script imprime um `RESULTADO:` no fim. Confie nele, não no código de saída bruto.

- **`compila limpo`** — nada a fazer.
- **`ERROS DE COMPILACAO`** — a lista vem deduplicada. A Unity repete o mesmo erro três
  ou quatro vezes no log; o script já filtra isso, então cada linha é um problema real.
- **`ha testes falhando`** — o script imprime o nome do método e a mensagem da asserção.
  Ignore a mensagem "One or more child tests had errors": é o nó pai do relatório, não
  um teste de verdade.
- **`ha uma instancia da Unity com este projeto aberto`** — feche o editor gráfico.
  A Unity trava o projeto e batchmode não consegue abrir.

## Antes de declarar que algo funciona

Compilar não é o mesmo que funcionar. A ordem que dá confiança de verdade:

1. `compile` — o código é válido
2. `test all` — o comportamento é o esperado
3. `build` — funciona fora do editor, que é o item 1 da definição de pronto do `docs/00`

Pular direto para "está pronto" depois de só compilar é como a maior parte dos bugs
silenciosos entra no projeto.

## Quando um teste falha

Antes de mexer no código, decida de quem é a culpa. Um teste que mede **tempo ou distância
percorrida** é suspeito, porque o framerate em batchmode sem gráficos é errático e varia
entre execuções. Um teste assim que falha por pouco geralmente está mal escrito, não
apontando um bug.

O reflexo certo é perguntar o que o teste realmente quer verificar. Se ele quer verificar
que o movimento é relativo à câmera, ele deve medir **direção**, não distância. Se quer
verificar velocidade, deve ler a propriedade de velocidade e não o deslocamento.

## Rodar um método de editor

`method` executa qualquer método `public static` sem parâmetros. Os que existem hoje:

| Método | O que faz |
|---|---|
| `LoboBranco.EditorTools.ProjectSetup.InstallPackages` | Reconcilia os pacotes com a lista declarada |
| `LoboBranco.EditorTools.ProjectSetup.ConfigureCollisionMatrix` | Reaplica os pares de colisão |
| `LoboBranco.EditorTools.ProjectSetup.CreateScenes` | Recria cenas faltantes e o Build Settings |
| `LoboBranco.EditorTools.ProjectSetup.UseVsCode` | Aponta o editor externo para o VS Code |
| `LoboBranco.EditorTools.SandboxSetup.BuildSandbox` | Remonta a hierarquia da cena de sandbox |

Ao criar um método novo de setup, prefira colocá-lo em `Assets/Editor/` com um
`[MenuItem]`, para que ele sirva tanto ao batchmode quanto ao uso manual no editor.

## Logs

Ficam em `unity/` e são ignorados pelo Git. Quando o script não bastar, leia direto:

```bash
grep -n "error CS\|Exception\|\[Build\]\|\[Setup\]" unity/batch-compile.log | head -30
```
