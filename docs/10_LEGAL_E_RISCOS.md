# 10 — Propriedade Intelectual e Riscos

Não sou advogado e isto não é orientação jurídica. É um levantamento do que as políticas
públicas dizem e do que isso implica em decisões de projeto.

## 1. Quem é dono do quê

| Camada | Titular |
|---|---|
| Os livros, os personagens originais, o universo | **Andrzej Sapkowski** |
| Os jogos, seu design, arte, código, música, roteiro | **CD PROJEKT** |
| A marca "The Witcher" nos jogos | **CD PROJEKT** |
| O nome "Geralt de Rívia", "Vizima", "Kaer Morhen" | Sapkowski, licenciado à CDPR |

Você não tem direito sobre nenhuma dessas camadas. Um projeto derivado existe por
tolerância do titular, não por direito.

## 2. O que as Diretrizes de Conteúdo de Fãs da CDPR permitem

A CD PROJEKT RED publica **Fan Content Guidelines** em cdprojektred.com/en/fan-content.
Os pontos que importam aqui:

**Permitido:**
- Praticamente qualquer conteúdo de fã "dentro do razoável" — vídeos, streams, fanart,
  e por extensão projetos de fã não-comerciais.
- Aceitar **doações razoáveis** ligadas ao conteúdo de fã.

**Proibido:**
- **Qualquer uso comercial** sem permissão explícita. Isso inclui cobrar pelo jogo, colocá-lo
  atrás de paywall, ou monetizá-lo.
- Mods feitos com REDkit só podem funcionar dentro do The Witcher 3, e só para fins
  não-comerciais. Isso não se aplica a você (você não está usando REDkit), mas mostra a
  postura do estúdio.
- Criar confusão com produtos oficiais.

Contato para dúvidas: `legal@cdprojektred.com`. Se um dia você quiser distribuir
publicamente, escrever para esse endereço antes é mais inteligente do que descobrir a
resposta por notificação de remoção.

## 3. Regras operacionais deste projeto

Derivadas do acima, e são vinculantes para o repositório:

| # | Regra | Motivo |
|---|---|---|
| R1 | Nenhum asset extraído do jogo original entra no repositório | Violação direta do EULA |
| R2 | Nenhuma música ou áudio do original | Direito autoral separado, do compositor |
| R3 | Nenhum texto de diálogo copiado literalmente | Direito autoral sobre o roteiro |
| R4 | Zero monetização: sem venda, doação, Patreon, anúncio, cripto | Cláusula comercial |
| R5 | Se for distribuído, deixar explícito que é projeto de fã não oficial | Evitar confusão |
| R6 | Não usar os logos da CDPR, da série, nem a arte oficial | Marca |
| R7 | Não importar modelos, texturas ou outros assets oficiais/REDkit; personagens fan-made devem ser produzidos do zero | Evitar redistribuição de assets proprietários |

**Interpretação operacional de R7:** referências visuais oficiais podem orientar um modelo
fan-made deste projeto não comercial, mas os arquivos de referência não entram como assets
versionados e nenhuma geometria, textura, rig, animação ou material da CDPR é reutilizado.
Malha, UV, bake, materiais, texturas e animações são produzidos do zero no Blender e nas
ferramentas do projeto. Nomes da IP continuam fora do código, conforme a seção 4.

## 4. A decisão estratégica que vale a pena tomar agora

Existe uma escolha de arquitetura com consequência legal, e o custo de fazê-la certo no
dia 1 é quase zero, enquanto o custo de fazê-la depois é enorme.

**Construa todos os sistemas como agnósticos de IP.**

Em termos concretos: nenhum nome do universo The Witcher aparece **em código ou em nome de
classe**. Eles aparecem apenas em **assets de dados** e em **texto localizável**.

```
✗ ERRADO                              ✓ CERTO
class GeraltController                class PlayerCharacter
class AardSign                         class KnockbackSign  (nome "Aard" no SO)
enum MonsterType { Barghest }          MonsterDef asset "Barghest"
const int VIZIMA_SCENE = 3;            ZoneDef asset "Vizima"
"Bruxo" hardcoded na UI                chave de localização "class.witcher.name"
```

Por que isso importa muito:

1. **Se a CDPR pedir para você parar**, você troca uma pasta de assets e um arquivo de
   localização, e tem um jogo próprio. Não perde os 216 horas de engenharia.
2. **Se o projeto ficar bom** e você quiser comercializá-lo, o caminho existe: mesmo design,
   mesmo código, universo próprio. Muitos jogos nasceram assim.
3. É simplesmente melhor engenharia. Dados fora do código é o princípio central do doc 07.

Registre isso como **ADR 0005 — Sistemas agnósticos de IP**.

## 5. Registro de riscos

Probabilidade e impacto em escala de 1 a 5.

| # | Risco | Prob. | Imp. | Mitigação |
|---|---|---|---|---|
| X1 | **Escopo cresce e o projeto morre** | 5 | 5 | Pilares do doc 00; a métrica de tarefas adicionadas do doc 09; a lista do "não fazer" |
| X2 | Combate não fica divertido | 3 | 5 | O portão de M1: não avançar sem passar |
| X3 | Curva de aprendizado da Unity trava o progresso | 4 | 3 | M0 e M1 são deliberadamente pequenos; multiplicador de aprendizado no cronograma |
| X4 | Volume de escrita subestimado (doc 06, seção 9) | 4 | 3 | Slice tem apenas 15 mil palavras; escrever em Ink desde o começo |
| X5 | Notificação de remoção da CDPR | 1 | 4 | R1 a R7; sistemas agnósticos de IP (seção 4) |
| X6 | Perda de trabalho por falta de versionamento | 3 | 5 | Git e LFS em M0, antes de qualquer código |
| X7 | Arte consome todo o tempo | 4 | 4 | Greybox até M4; pacotes de placeholder pagos |
| X8 | Motivação cai no vale de 3 meses | 4 | 4 | Milestones curtos e jogáveis; nada de 6 semanas sem algo jogável |
| X9 | Refatoração infinita em vez de progresso | 3 | 3 | ADRs; proibição de refatorar sem problema medido |
| X10 | Save quebra e perde playtests | 3 | 2 | Versionamento de save e teste de round-trip em M2 |

**X1 e X8 são os riscos que realmente matam projetos solo.** Nenhum dos dois é técnico.
As mitigações contra eles são as partes chatas destes documentos: pilares, escopo fechado,
milestones jogáveis. Elas existem porque a alternativa é abandonar em cinco meses.

## 6. Se você quiser transformar isso em um jogo comercial

Caminho possível, se um dia fizer sentido:

1. Mantenha os sistemas agnósticos (seção 4).
2. Troque o cenário: outra geografia, outra política, outros nomes. O design de "caçador
   de monstros profissional que investiga, prepara e escolhe entre males" **não é protegido**
   — mecânicas de jogo não são objeto de direito autoral.
3. Não use nada que evoque diretamente a marca: nem "bruxo", nem medalhão de lobo, nem
   cabelo branco com cicatriz no olho, nem os nomes das poções.
4. O que você pode levar: todo o código, toda a arquitetura, todo o balanceamento,
   as cinco trilhas de habilidade, o sistema de Ecos, o loop de contrato, o pipeline de dano.

Isso é a maior parte do valor real do trabalho. É por isso que a seção 4 vale a disciplina.
