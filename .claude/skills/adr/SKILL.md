---
name: adr
description: Registra uma decisão de arquitetura em tech/adr/ com contexto, decisão, consequências e alternativas descartadas. Use sempre que escolher entre duas abordagens técnicas com consequência duradoura - biblioteca, formato de dados, engine, padrão de código, criação de módulo novo, ou qualquer coisa que seja cara de reverter depois. Use também quando alguém perguntar "por que escolhemos X?" e a resposta não estiver escrita em lugar nenhum. Num projeto solo, o ADR é o que impede você de rediscutir a mesma decisão consigo mesmo daqui a três meses.
---

# Registrar uma decisão de arquitetura

## Quando escrever

Escreva quando a decisão for **cara de reverter** e o motivo não for óbvio a partir do
resultado. O teste: daqui a três meses, olhando só para o código, dá para reconstruir por
que foi assim? Se não dá, é ADR.

Casos típicos: escolher biblioteca ou pacote, formato de serialização, render pipeline,
criar um módulo novo, adotar ou recusar um padrão, decidir que algo fica fora de escopo
por razão técnica.

**Não escreva** para escolha de nome, formatação, ou qualquer coisa que um `git revert`
desfaz em um minuto. ADR barato demais dilui os que importam.

## Formato

Copie `tech/adr/0000-template.md`. Numere em sequência, quatro dígitos, com título em
`kebab-case`: `0007-formato-de-localizacao.md`.

```markdown
# ADR NNNN — Título

Data: AAAA-MM-DD
Status: proposta | aceita | substituida por ADR NNNN

## Contexto
Qual é o problema e quais restrições existem.

## Decisão
O que foi decidido, em uma frase.

## Consequências
O que fica mais fácil, o que fica mais difícil, e o que se torna irreversível.

## Alternativas consideradas
O que foi descartado, e por quê.
```

## A seção que a maioria escreve mal

**Consequências** não é uma lista de benefícios. Se ela só tem coisa boa, você escreveu
uma justificativa, não um registro. Toda decisão de arquitetura cobra alguma coisa, e a
parte que cobra é justamente a que você vai querer reler daqui a seis meses.

Compare:

> Consequências: iteração rápida, builds curtos, roda em hardware modesto.

com o que está no `tech/adr/0001`:

> - Iteração rápida, builds curtos, roda em hardware modesto.
> - Teto de qualidade visual mais baixo, mas suficiente para o alvo do protótipo.
> - Migrar para HDRP depois é possível mas doloroso: materiais, luzes e volumes teriam
>   que ser refeitos. Reavaliar somente após o vertical slice.

A segunda diz o que a decisão custa e quando reabri-la. A primeira é propaganda.

## Alternativas descartadas

Registre também o que você **não** escolheu, com o motivo. Metade do valor do ADR está
aí: sem isso, a alternativa descartada volta como sugestão nova daqui a três meses, e a
discussão inteira se repete.

## Depois de escrever

- Referencie o ADR de onde a decisão aparece: no `docs/07`, no `CLAUDE.md`, ou em um
  comentário no código quando ele parecer arbitrário sem o contexto.
- Linha no `CHANGELOG.md`.
- Se um ADR novo substitui um antigo, mude o status do antigo para
  `substituida por ADR NNNN` em vez de apagá-lo. O histórico do raciocínio é o ativo.

## Os que já existem

| ADR | Assunto |
|---|---|
| 0001 | URP em vez de HDRP |
| 0002 | Ink para diálogo |
| 0003 | Save em JSON com Newtonsoft |
| 0004 | Behavior trees oficiais para IA |
| 0005 | Sistemas agnósticos de propriedade intelectual |
| 0006 | Módulos Player e Camera separados |

Leia os relacionados antes de escrever um novo. Uma decisão que contradiz um ADR existente
precisa dizer isso explicitamente e mudar o status do anterior.
