# 06 — Narrativa, Quests e Consequência

Resolve os defeitos **D4** (Capítulo IV arrasta), **D5** (Capítulo V truncado),
**D7** (diário confuso) e **D10** (cartas de sexo).

## 1. Postura de adaptação

Este projeto **não reescreve a história**. A trama da Salamandra funciona: um mistério de
detetive dentro de uma guerra civil, com um antagonista que tem razão sobre o problema e
está monstruosamente errado sobre a solução.

O que muda é **ritmo, agência e clareza**. Três operações:

| Operação | Onde se aplica |
|---|---|
| **Comprimir** | Capítulo IV: de 5 zonas para 2, corta a metade do vai-e-vem |
| **Expandir** | Capítulo V: recebe 2 horas e uma estrutura de cerco real |
| **Costurar** | Escolhas de todos os capítulos passam a ter eco explícito no final |

## 2. Reestruturação por capítulo

### Prólogo — Kaer Morhen (1h)
Sem mudança estrutural. Ganha função nova: é o **tutorial de investigação**, não só de
combate. O jogador investiga como a Salamandra entrou na fortaleza, e essa dedução
determina qual bruxo sobrevive ao ataque. Primeira demonstração do pilar P3.

### Capítulo I — Arredores de Vizima (5h)
Estruturalmente intacto. É o capítulo do protótipo. Mudanças:
- A investigação da Besta passa a ser o ciclo completo de contrato (doc 02, seção 2).
- A turba da Abigail ganha uma **terceira opção**: se você colheu 5 de 5 pistas, pode
  provar quem convocou a Besta e desviar a fúria da turba. Recompensa por trabalho de
  investigação bem-feito, e reforça P1 sem remover o dilema para quem não investigou.

### Capítulo II — Bairro do Templo e o Pântano (7h)
- O pântano perde 40% da sua área e ganha densidade. Os obeliscos passam a ser um puzzle
  de uma sessão, não uma coleta longa.
- A revelação do doppler ganha **pistas plantáveis**: se o jogador estiver atento, três
  detalhes na fala de "Raymond" o denunciam antes da revelação. Recompensa: uma cena extra.

### Capítulo III — Bairro do Comércio (6h)
Mudanças mínimas. É o melhor capítulo do original. Ganha só um quadro de contratos.

### Capítulo IV — Águas Turvas (5h, era ~8h) — a compressão
Este é o maior trabalho de adaptação.
- **Corta:** metade das quests de aldeão, o culto vodyanoi como linha separada, a
  ida-e-volta ao lago repetida.
- **Funde:** o arco de Alvin e o arco da Dama do Lago passam a ser a mesma linha. A Dama
  te dá o teste, e o resultado do teste determina o que acontece com Alvin.
- **Adiciona pressão:** um relógio narrativo. A Salamandra está a caminho de Águas Turvas.
  Você tem um número finito de dias in-game antes do ataque. Quests não feitas são
  perdidas. Isso conserta o problema de ritmo na raiz: o capítulo arrastava porque nada
  te apressava.

### Capítulo V — Vizima Antiga (5h, era ~3h) — a expansão
- Estrutura nova: **cerco em três frentes**. A cidade tem três distritos, e cada um pode
  cair para a Ordem, para os Scoia'tael, ou ficar neutro, dependendo de quem você ajuda.
- Cada distrito tem uma quest de 40 minutos. Você tem tempo para duas das três. A terceira
  cai sem você.
- Os **Ecos** (seção 4) se manifestam aqui: se você salvou Abigail, ela aparece curando
  feridos e abre uma rota; se ela morreu, uma praga se espalha e fecha uma rota.
- Azar Javed e Jacques de Aldersberg mantêm-se como bosses, mas o confronto ideológico com
  Aldersberg ganha uma opção de diálogo: você pode concordar com o diagnóstico dele e
  ainda assim matá-lo. Sem redenção, sem vilania simples.

### Epílogo (1h)
Mantido, com um relatório de Ecos: uma sequência que mostra as consequências de longo prazo
de cada escolha grande, no estilo de Fallout, antes do gancho para o TW2.

## 3. Estrutura de quest

Três categorias, com regras diferentes:

| Categoria | Quantidade | Regra |
|---|---|---|
| **Missão principal** | 1 por capítulo | Sempre disponível, nunca falha por tempo |
| **Contrato** | 2 a 5 por capítulo | Ciclo investigativo completo, escrito à mão |
| **Assunto local** | 3 a 8 por capítulo | Curtas, sem combate obrigatório, revelam a zona |

**Nenhum "assunto local" é uma fetch quest sem contexto.** A regra: toda quest secundária
precisa revelar algo sobre o mundo, uma facção ou um personagem. Se não revela, é cortada.

### Anatomia de um contrato (modelo, usar sempre)

Usando *A Besta dos Arredores* como referência:

| Etapa | Conteúdo | Falha possível |
|---|---|---|
| **Gancho** | O Reverendo pede ajuda; Odo culpa a Abigail | — |
| **Negociação** | 200 orens; renegociável até 280 com *Negociador* | Aceitar barato |
| **Cena 1** | Corpo no campo. Pistas: mordidas, pegadas, cheiro de enxofre | — |
| **Cena 2** | Cemitério à noite. Pistas: círculo ritual, ossos de cão | Não ir à noite |
| **Cena 3** | Interrogar Abigail, Odo, o coveiro. Pista: quem estava no cemitério | Perguntas erradas |
| **Dedução** | 3 de 5 pistas identificam "besta"; 5 de 5 revelam quem a invocou | Deduzir espécie errada |
| **Preparo** | Óleo de Besta, Trovão, Igni. Pistas dizem que ela regenera | Ir sem Igni |
| **Caça** | Cripta. Matilha, então a Besta, três fases | — |
| **Resolução** | Troféu, pagamento, **e a turba** | — |
| **Escolha** | Defender Abigail / entregá-la / expor o culpado (só com 5 pistas) | — |
| **Eco** | Semeado: `abigail_fate` | — |

Todo contrato do jogo completo segue esta tabela de dez linhas. Ela é o template.

## 4. Sistema de Ecos — consequência atrasada formalizada

O traço mais valioso do original, transformado em sistema explícito e testável.

**Mecânica:** o mundo tem um dicionário global de flags, o `WorldState`. Escolhas escrevem
nele. Conteúdo lê dele, muitas horas depois.

```
WorldState (chave → valor)
  abigail_fate         : "salva" | "linchada" | "exonerada"
  chapter1_villagers   : int    (quantos aldeões você matou)
  raymond_suspected    : bool   (você desconfiou do doppler antes da revelação)
  adda_fate            : "curada" | "morta"
  alvin_fate           : "triss" | "lago" | "abandonado"
  alignment            : float  (-1 Scoia'tael  →  +1 Ordem)
  faction_favors       : dict   (favores devidos por facção)
```

Regras de disciplina, e elas são o que impede o sistema de virar caos:
1. Uma flag só é escrita **em um lugar**. Grep por ela deve retornar uma escrita e N leituras.
2. Todo Eco tem um **capítulo de plantio** e um **capítulo de colheita** documentados numa
   planilha em `design/narrativa/ecos.csv`.
3. Todo Eco tem que ser **perceptível sem tutorial**: uma cena, um NPC, uma rota aberta ou
   fechada. Um Eco que só muda uma linha de diálogo não conta.
4. Nenhuma escolha é marcada como boa ou má na interface. Nunca.

### Tabela de Ecos (jogo completo)

| Eco | Plantado | Colhido | Manifestação |
|---|---|---|---|
| `abigail_fate` | I | V | Abigail cura feridos, ou uma praga fecha um distrito |
| `chapter1_villagers` | I | II | Sobreviventes te reconhecem no Bairro do Templo, com hostilidade |
| `raymond_suspected` | II | III | Cena extra que revela mais sobre Azar Javed |
| `adda_fate` | III | V, Epílogo | Foltest como aliado ou como inimigo político |
| `alvin_fate` | IV | Epílogo | Determina qual profecia se realiza |
| `alignment` | II, III, V | V, Epílogo | Qual facção controla Vizima no final |

## 5. Reputação e a cidade viva (pilar P4)

`alignment` é um float de −1 a +1. Ele não é escondido (o nó *Reputação* o revela), mas
também não é uma barra na HUD por padrão.

Manifestações sistêmicas em Vizima, todas legíveis sem texto:

| alignment | Patrulhas | Preços | Comportamento de rua |
|---|---|---|---|
| > +0,5 | Ordem te saúda | Humanos: −15% | Não-humanos evitam você |
| +0,2 a +0,5 | Ordem indiferente | Normais | Normal |
| −0,2 a +0,2 | Ambos desconfiam | Normais | Ninguém se aproxima |
| −0,5 a −0,2 | Ordem te para para revista | Anões: −15% | Humanos cospem no chão |
| < −0,5 | Ordem te ataca à vista | Humanos: +30% | Scoia'tael te dão informação |

## 6. Diálogo

**Ferramenta:** grafo de nós, com condições sobre `WorldState` e atributos.
A recomendação técnica (ver doc 07) é usar **Ink** em vez de construir um editor de grafo.

**Regras de escrita:**
1. Nenhuma opção de diálogo revela seu resultado. Sem "[Mentir]" ou "[Isso vai deixá-lo furioso]".
   Exceção: marcadores de **mecânica**, não de moral: `[Axii]`, `[Intimidar]`, `[Pagar 50 orens]`.
2. Opções de investigação ficam agrupadas e são **reentráveis** — você pode voltar e
   perguntar de novo. Não perder uma pista por clicar rápido é acessibilidade básica.
3. Cada NPC importante tem uma opinião sobre o conflito Ordem/Scoia'tael, e ela aparece.
4. Máximo de 4 opções por nó. Acima disso, submenu.

## 7. Diário e direcionamento (resolve D7)

O diário do original era uma pilha de texto. O novo tem três abas:

- **Quests** — objetivo atual em uma frase, mais o histórico do que você já descobriu.
- **Bestiário** — a ferramenta mecânica (doc 02, seção 7).
- **Pessoas** — quem você conheceu, o que quer, de que lado está. Alimentado
  automaticamente, e é onde o jogador reconstrói o mistério.

Direcionamento: um objetivo por vez, com **área** marcada no mapa em vez de um ponto exato.
No *Caminho do Bruxo* nem a área aparece; só a descrição textual. Isso é uma escolha de
design, não de dificuldade: ler "no cemitério, ao norte da capela" ativa o cérebro de um
modo que seguir uma seta não ativa.

## 8. Vínculos — o que substitui as cartas de sexo (resolve D10)

Sistema de relacionamento com estado, sem colecionáveis.

**Como funciona:** cada personagem relacionável tem um valor de **Confiança** (0 a 100) e um
conjunto de **Fatos Conhecidos** — o que ele sabe sobre você e sobre suas escolhas.

Confiança sobe por: cumprir o que prometeu, escolhas alinhadas com os valores dele, e
diálogos de vulnerabilidade. Cai por: mentiras descobertas, escolhas contrárias, ausência.

Consequências reais, não recompensas colecionáveis:
- Alta Confiança abre ajuda concreta em momentos de crise (Triss te cura no Capítulo V).
- Baixa Confiança fecha rotas e às vezes cria antagonismo.
- Romances existem, são exclusivos entre si, e não são o objetivo do sistema.
- **Nenhum item, carta, arte ou colecionável é concedido.** A recompensa é narrativa.

Relacionáveis no jogo completo: Triss, Shani, Siegfried, Yaevinn, Kalkstein, Zoltan.
No protótipo: apenas Abigail, como prova de conceito.

## 9. Volume de texto (estimativa para planejamento)

| Escopo | Palavras | Notas |
|---|---|---|
| Protótipo (Cap. I) | 12.000 a 18.000 | Diálogo, diário, bestiário, itens |
| Jogo completo | 180.000 a 250.000 | Comparável a um romance longo |

Isso é a maior tarefa oculta do projeto. Um dev solo escreve cerca de 1.500 palavras
utilizáveis de diálogo de jogo por dia. O protótipo custa cerca de **10 dias só de escrita**,
e o jogo completo mais de um ano. Está no roadmap (doc 09) por isso.
