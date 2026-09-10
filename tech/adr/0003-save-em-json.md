# ADR 0003 — Save em JSON com Newtonsoft e interface ISaveable

Data: 2026-09-09
Status: aceita

## Contexto
O jogo precisa de save e load desde M2. `BinaryFormatter` e obsoleto e insegura.
A estrutura de dados vai mudar dezenas de vezes durante o desenvolvimento.

## Decisao
Serializar em JSON via `com.unity.nuget.newtonsoft-json`, com cada sistema implementando
`ISaveable`, e um campo `saveVersion` com migracoes explicitas.

## Consequencias
- Saves legiveis e editaveis a mao, o que acelera muito o debug.
- Arquivos maiores que binario. Irrelevante nesta escala.
- Migracoes precisam ser escritas quando o formato muda; o custo e real mas menor do que
  perder saves de teste.
- Objetos de mundo precisam de GUID persistente, gerado no editor.

## Alternativas consideradas
- **BinaryFormatter**: descartado, obsoleto e insegura.
- **MessagePack ou binario custom**: descartado, otimizacao prematura.
- **PlayerPrefs**: inadequado para estado estruturado.
