# 04 — Progressão e Árvores de Habilidade

Resolve o defeito **D2**: a grade de porcentagens sem sabor do original.

## 1. A regra que governa todo este documento

> **Um nó de habilidade tem que mudar uma decisão, não um número.**

Teste para aprovar qualquer nó novo: *se este nó existir, o jogador faz algo diferente em
combate ou fora dele?* Se a resposta é "faz o mesmo, só mais forte", o nó é rejeitado.

Nós de porcentagem pura são permitidos apenas como **nós de passagem** de custo 1, para
dar ritmo à árvore, e nunca mais que 1 por tier.

## 2. Moeda de progressão

| Moeda | Como se ganha | Onde se gasta |
|---|---|---|
| **Ponto de Trilha** | 1 por nível | Nós das cinco trilhas |
| **Ponto de Atributo** | 1 por nível | Força, Destreza, Vigor, Inteligência |
| **Slot de Mutagênio** | 1 a cada 4 níveis | Mesa de Mutações |
| **Mutagênio** | Loot de monstro, contratos | Encaixado nos slots |

Nós têm três graus, herdados nominalmente do original:

| Grau | Custo | Papel |
|---|---|---|
| **Bronze** | 1 ponto | Base, requisito, ajuste |
| **Prata** | 2 pontos | Muda o comportamento de um sistema |
| **Ouro** | 3 pontos | Capstone da trilha; só um por trilha pode estar ativo |

Regras estruturais:
- Cada trilha tem 3 tiers. Para abrir o tier 2 é preciso ter gasto 4 pontos no tier 1;
  para o tier 3, 8 pontos acumulados na trilha.
- **Redistribuição livre em meditação** até o nível 10, e depois só com a poção
  *Extrato de De Vries*. Isso permite experimentar sem punir, mas mantém peso na build tardia.

Com 20 níveis, o jogador termina com cerca de 20 pontos de trilha. As cinco trilhas somadas
custam bem mais de 60. **Você não consegue tudo.** Isso é intencional: cerca de duas
trilhas e meia por playthrough.

---

## 3. Trilha I — Esgrima (Aço)

Foco: combate contra humanos e humanoides. É a trilha do duelista.

### Tier 1
| Nó | Grau | Efeito |
|---|---|---|
| **Guarda Alta** | Bronze | Aparo passa a bloquear ataques de nível `Heavy`, com custo dobrado de vigor |
| **Passo de Lado** | Bronze | Esquiva ganha 0,04 s de invulnerabilidade e pode ser feita lateralmente durante ataque |
| **Fio da Lâmina** | Bronze | +8% de dano com aço *(nó de passagem)* |
| **Leitura de Postura** | Prata | Um indicador acima do inimigo mostra a postura ideal contra ele |

### Tier 2
| Nó | Grau | Efeito |
|---|---|---|
| **Riposte Cortante** | Prata | Riposte bem-sucedido causa Sangramento (3/s por 6 s) |
| **Corrente Longa** | Prata | O teto do Fluxo sobe de 5 para 8 elos, e o bônus máximo para +45% |
| **Desarme** | Prata | Aard em humanoide atordoado faz ele soltar a arma; ele passa a lutar desarmado |
| **Dança de Grupo** | Prata | Postura Grupo passa a atingir 360° em vez de 180° |

### Tier 3
| Nó | Grau | Efeito |
|---|---|---|
| **Duelista** | Ouro | Contra um único inimigo, todo riposte concede uma janela de 1,5 s em que seus ataques não têm recuperação. Correntes de Fluxo infinitas contra um alvo. |
| **Ceifador** | Ouro | Matar um inimigo com postura Grupo não interrompe a animação e transfere a corrente de Fluxo para o próximo alvo. Trilha do combate contra multidões. |

---

## 4. Trilha II — A Caça (Prata)

Foco: monstros. É a trilha do investigador e do especialista.

### Tier 1
| Nó | Grau | Efeito |
|---|---|---|
| **Olho Treinado** | Bronze | Sentidos de Bruxo mostram pistas a 20 m em vez de 10 m |
| **Anatomia** | Bronze | +8% de dano com prata *(nó de passagem)* |
| **Rastreador** | Bronze | Trilhas de rastro persistem 3x mais tempo antes de sumir |
| **Dedução** | Prata | Ao colher a terceira pista, o Bestiário sugere duas espécies em vez de te deixar sem nada |

### Tier 2
| Nó | Grau | Efeito |
|---|---|---|
| **Estudioso** | Prata | O bônus de bestiário sobe de 1,25x para 1,40x |
| **Ponto Fraco** | Prata | Sentidos de Bruxo em combate destacam por 3 s um ponto vulnerável; acertá-lo causa crítico garantido |
| **Óleo Espesso** | Prata | Óleos duram 180 golpes em vez de 60, e dois óleos podem coexistir na mesma lâmina |
| **Caçador de Matilhas** | Prata | Matar o líder de uma matilha faz os demais fugirem por 5 s |

### Tier 3
| Nó | Grau | Efeito |
|---|---|---|
| **Bruxo da Escola do Lobo** | Ouro | Contra um monstro cuja entrada do Bestiário está completa (4 de 4 seções), seus ataques ignoram armadura por completo |
| **Predador** | Ouro | Um primeiro golpe desferido contra um monstro que não te detectou causa 5x de dano e não o alerta se matar. Habilita stealth de caça. |

---

## 5. Trilha III — Sinais

Foco: controle e magia. É a trilha do mago-bruxo.

### Tier 1
| Nó | Grau | Efeito |
|---|---|---|
| **Canalizar** | Bronze | Segurar o botão de sinal por 0,6 s aumenta o efeito em 50%, ao custo de ficar imóvel |
| **Fluxo de Vigor** | Bronze | Regeneração de vigor em combate sobe de 6/s para 9/s *(nó de passagem)* |
| **Sinal Rápido** | Bronze | Cooldowns de sinais caem 20% |
| **Domínio Mental** | Prata | Axii em diálogo não consome vigor e nunca falha |

### Tier 2
| Nó | Grau | Efeito |
|---|---|---|
| **Aard Explosivo** | Prata | Aard passa a ser radial (360°, 4 m) em vez de cone. Nova opção de escape. |
| **Igni Sustentado** | Prata | Igni pode ser mantido como lança-chamas contínuo enquanto houver vigor |
| **Quen Ativo** | Prata | Enquanto Quen estiver ativo, você recupera vitalidade igual a 20% do dano causado |
| **Yrden Duplo** | Prata | Duas armadilhas Yrden simultâneas; um inimigo entre as duas leva dano elétrico |

### Tier 3
| Nó | Grau | Efeito |
|---|---|---|
| **Intensidade** | Ouro | Todo sinal ganha um efeito secundário: Aard congela, Igni causa pânico, Quen reflete, Axii afeta monstros, Yrden silencia magos |
| **O Sexto Sinal** | Ouro | Destrava **Heliotrope**, um sinal novo: bolha de 3 m que desacelera o tempo para todos, exceto você, por 4 s. Custa 60 de vigor e 1 adrenalina. |

*Heliotrope é um sinal do lore dos livros, ausente do jogo de 2007. É uma boa adição
autoral por ser canônica sem ser redundante.*

---

## 6. Trilha IV — Alquimia e Mutações

Foco: preparação e o corpo mutado. É a trilha que mais muda a forma de jogar.

### Tier 1
| Nó | Grau | Efeito |
|---|---|---|
| **Metabolismo** | Bronze | Limite de toxicidade +15 *(nó de passagem)* |
| **Herbalista** | Bronze | Colher uma erva rende 2 unidades em vez de 1 |
| **Mão Firme** | Bronze | Bombas podem ser arremessadas em arco, com trajetória visível |
| **Trago Rápido** | Prata | Uma poção pode ser bebida **em combate**, uma vez por luta. Remove a fricção mais odiada do original. |

### Tier 2
| Nó | Grau | Efeito |
|---|---|---|
| **Sangue de Bruxo** | Prata | Toxicidade acima de 50% concede +25% de dano em vez de causar dano. Vira recurso. |
| **Destilação** | Prata | Uma receita rende 3 doses em vez de 1 |
| **Substituição** | Prata | Uma substância faltante pode ser suprida por qualquer erva, com a poção saindo de qualidade inferior |
| **Sinergia** | Prata | Duas poções ativas do mesmo grupo combinam num efeito único e mais forte |

### Tier 3
| Nó | Grau | Efeito |
|---|---|---|
| **Mutante** | Ouro | +2 slots de mutagênio (total de 7 no nível 20) e mutagênios podem ser trocados fora da meditação |
| **Êxtase** | Ouro | Em toxicidade máxima você entra em Frenesi: 2x de dano, imunidade a atordoamento, sem regeneração de vitalidade, e a tela distorce. Build de alto risco. |

---

## 7. Trilha V — A Trilha (profissão, mundo, social)

Trilha inteiramente nova, sem equivalente no original. Resolve D9 e alimenta os pilares
P1, P3 e P4. É a trilha que faz o jogo ser sobre ser um bruxo, e não sobre lutar.

### Tier 1
| Nó | Grau | Efeito |
|---|---|---|
| **Negociador** | Bronze | Contratos podem ser renegociados por até +40% de pagamento |
| **Andarilho** | Bronze | Capacidade de carga +30 e itens de alquimia deixam de pesar |
| **Reputação** | Bronze | O medidor de alinhamento (Ordem / Neutro / Scoia'tael) fica visível |
| **Intimidação** | Prata | Nova opção de diálogo baseada em Força, capaz de encerrar alguns combates antes de começarem |

### Tier 2
| Nó | Grau | Efeito |
|---|---|---|
| **Conhecido da Trilha** | Prata | Mercadores compram troféus por 60% mais; novo quadro de contratos em cada zona |
| **Meditação Profunda** | Prata | Meditar restaura vitalidade por completo e reduz toxicidade a zero |
| **Bruxo Errante** | Prata | Você pode meditar em qualquer lugar seguro, sem precisar de fogueira |
| **Leitura de Pessoas** | Prata | Diálogos mostram um indicador quando um NPC está mentindo |

### Tier 3
| Nó | Grau | Efeito |
|---|---|---|
| **Neutralidade** | Ouro | Ambas as facções te tratam como neutro independentemente das suas escolhas; destrava um caminho de quest exclusivo em cada capítulo |
| **Lenda** | Ouro | Sua reputação te precede: contratos pagam o dobro, e alguns inimigos humanos fogem ao te ver. Fecha algumas opções de diálogo por medo. |

---

## 8. Sistema de Mutagênios

A camada de build reconfigurável, sobreposta às trilhas.

**Como funciona:** a **Mesa de Mutações**, acessível em meditação, tem slots. Cada slot
recebe um mutagênio extraído de monstros. Mutagênios têm cor, e slots têm cor.

| Cor | Fonte | Efeito típico |
|---|---|---|
| **Vermelho** | Bestas, insectoides | Dano de ataque, crítico |
| **Azul** | Espectros, elementais | Vigor, intensidade de sinal |
| **Verde** | Necrófagos, humanos amaldiçoados | Vitalidade, resistências |
| **Amarelo** | Bosses e monstros raros | Efeitos únicos, um por jogo |

**Regra de sinergia:** um mutagênio colocado num slot adjacente a nós ativados da mesma
cor tem o efeito **triplicado**. Isso cria a decisão de build interessante: você especializa
(sinergia alta, frágil) ou diversifica (sinergia baixa, flexível).

Mutagênios amarelos de exemplo, para o protótipo:
- **Coração da Besta** — matar um inimigo restaura 15% do vigor máximo
- **Glândula de Afogado** — imunidade a veneno e respiração ilimitada
- **Núcleo de Golem** — imunidade a atordoamento, mas −20% de velocidade de movimento

## 9. Curva de nível

| Nível | XP acumulado | Desbloqueio marcante |
|---|---|---|
| 1 | 0 | — |
| 2 | 400 | Primeiro ponto de trilha |
| 3 | 1.000 | — |
| 4 | 1.800 | Primeiro slot de mutagênio |
| 5 | 2.900 | Tier 2 acessível (fim do protótipo) |
| 8 | 7.500 | Segundo slot |
| 12 | 18.000 | Tier 3 acessível, terceiro slot |
| 16 | 34.000 | Quarto slot |
| 20 | 56.000 | Quinto slot, teto |

Fórmula: `xp_para_proximo(n) = 400 * n^1.55`, arredondado para a centena.

## 10. Builds arquetípicas viáveis (validação do design)

Se as cinco trilhas estão bem desenhadas, elas devem produzir arquétipos distintos e
igualmente viáveis. Estes são os alvos:

| Arquétipo | Trilhas | Como joga |
|---|---|---|
| **Duelista** | Esgrima 3 + Trilha 1 | Aparo e riposte, combate um contra um, evita grupos |
| **Caçador** | Caça 3 + Alquimia 2 | Prepara tudo, mata bosses rápido, sofre com humanos |
| **Mago de Guerra** | Sinais 3 + Alquimia 1 | Controle de área, mal encosta na espada |
| **Mutante** | Alquimia 3 + Caça 1 | Vive em toxicidade alta, dano absurdo, frágil |
| **Andarilho** | Trilha 3 + Esgrima 1 | Resolve por diálogo e economia, luta pouco |

**Teste de balanceamento:** cada um desses arquétipos precisa vencer a Besta. Se algum não
conseguir, o problema está no design da Besta, não na build.
