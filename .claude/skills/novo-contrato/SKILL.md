---
name: novo-contrato
description: Monta um contrato de bruxo completo nas dez etapas do projeto, do gancho até o Eco, com pistas, dedução falível, diálogo em Ink e flag de WorldState. Use sempre que for criar um contrato, uma caçada, uma quest principal ou secundária, ou quando pedirem "cria a quest de X", "monta o contrato do monstro Y", "escreve uma missão". Use também ao desenhar qualquer escolha moral com consequência. O ciclo investigativo é o que diferencia este jogo, então improvisar a estrutura desmonta o pilar central.
---

# Montar um contrato

## O que é um contrato aqui

Não é "vá até o marcador e mate a coisa". É o loop médio do jogo, de 20 a 45 minutos,
e é onde os quatro pilares acontecem ao mesmo tempo. A estrutura está no `docs/06`
seção 3, com "A Besta dos Arredores" preenchida como referência viva.

## As dez etapas

Preencha esta tabela inteira antes de escrever uma linha de diálogo. Uma etapa vazia é
uma etapa que vai virar improviso e quebrar o ciclo.

| # | Etapa | O que definir | Falha possível do jogador |
|---|---|---|---|
| 1 | Gancho | Quem pede, e por quê agora | — |
| 2 | Negociação | Pagamento base e teto renegociável | aceitar barato |
| 3 | Cena 1 | Local, 1 a 2 pistas | — |
| 4 | Cena 2 | Local, condição (noite? chuva?), pistas | não cumprir a condição |
| 5 | Cena 3 | Interrogatório, pistas por pergunta | perguntar errado |
| 6 | Dedução | 3 pistas identificam, 5 revelam algo a mais | **deduzir espécie errada** |
| 7 | Preparo | Óleo, poção e sinal que as pistas indicam | ir sem o que importa |
| 8 | Caça | Local do covil, composições, boss | — |
| 9 | Resolução | Troféu, pagamento, e o que dá errado | — |
| 10 | Eco | Nome da flag e capítulo de colheita | — |

## As três etapas que fazem o jogo ser este jogo

**Etapa 6, dedução falível.** O jogador pode errar a espécie, comprar o óleo errado e
descobrir isso no meio da luta. Não há confirmação automática. Escreva as pistas para que
o erro seja **plausível, não burro**: uma pista aponta para duas espécies e a terceira
desempata. Um jogador que errou deve conseguir reconstruir por que errou.

**Recompensa por investigar bem.** Cinco pistas de cinco destravam algo que três não
destravam. No contrato da Besta, cinco pistas revelam quem invocou a criatura e abrem uma
terceira saída para a turba. É assim que o pilar 1 vira recompensa em vez de tarefa.

**Etapa 10, o Eco.** Uma escolha que se resolve na mesma cena é um botão, não uma escolha.
Ver a seção abaixo.

## Escolha moral

O molde é a escolha da Abigail, que é o melhor momento de design do jogo original:

1. Duas opções ruins, nenhuma limpa.
2. Informação incompleta no momento de decidir.
3. Ambos os lados com razão parcial.
4. **Nenhuma marcação na interface** de opção boa ou má. Sem `[Mentir]`, sem ícone.
   A única marcação permitida é mecânica: `[Axii]`, `[Intimidar]`, `[Pagar 50 orens]`.
5. Consequência muito depois.

Se você consegue dizer qual é a opção certa, a escolha não está pronta.

## Registrar o Eco

1. Nome de flag em `snake_case`, registrado em `design/narrativa/ecos.csv` com tipo,
   valores possíveis, capítulo de plantio e capítulo de colheita.
2. A flag é escrita em **um único lugar**. Um `grep` por ela deve achar uma escrita e
   N leituras. Duas escritas produzem estado impossível de depurar.
3. A manifestação precisa ser **perceptível sem tutorial**: uma cena, um NPC que aparece
   ou some, uma rota aberta ou fechada. Um Eco que só muda uma fala não conta.
4. Declare a flag como `WorldFlagDef` em `Assets/_Project/Data/Flags/`, para que a
   planilha de Ecos seja gerada dos assets e não possa divergir deles.

## Ink

Diálogo em `Assets/_Project/Ink/`, um arquivo por NPC ou por contrato.
As variáveis externas do Ink se ligam ao mesmo `WorldState` que o resto do jogo lê
(`tech/adr/0002`).

Regras de escrita de opção:
- Máximo de quatro por nó, acima disso submenu.
- Opções de investigação agrupadas e **reentráveis**: o jogador pode voltar e perguntar
  de novo. Perder uma pista por clicar rápido é frustração gratuita.
- Cada NPC importante tem uma opinião sobre o conflito Ordem contra Scoia'tael, e ela aparece.

## Escala

Um contrato consome de 1.500 a 3.000 palavras. O slice inteiro tem orçamento de 12 a 18 mil
(`docs/06` seção 9). Ao propor um contrato novo, diga quanto ele custa em palavras: é a
maior tarefa oculta do projeto.

O Capítulo I tem três contratos e nada mais. Um quarto contrato é escopo crescendo.

## Checklist antes de fechar

```
[ ] As dez etapas preenchidas
[ ] Pelo menos uma falha possível do jogador em cada etapa investigativa
[ ] Dedução errada plausível, com desempate na terceira pista
[ ] Recompensa para quem colheu tudo
[ ] Nenhuma opção de diálogo revela seu resultado
[ ] Flag registrada em ecos.csv, com escrita única
[ ] Manifestação do Eco perceptível sem tutorial
[ ] Quest secundária revela algo do mundo, de uma facção ou de um personagem
[ ] Linha no CHANGELOG.md
```
