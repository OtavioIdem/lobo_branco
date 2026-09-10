# 09 — Roadmap e Milestones

## 1. Premissas do cronograma

| Premissa | Valor |
|---|---|
| Time | 1 pessoa |
| Horas por semana | 12 (estimativa conservadora para projeto paralelo) |
| Experiência prévia em Unity | Baixa (é o propósito declarado do projeto) |
| Escopo | Vertical slice do Capítulo I |

**Multiplicador de aprendizado:** enquanto você está aprendendo a engine, cada tarefa custa
de 2 a 3 vezes o que custaria depois. O cronograma abaixo já embute isso. Não é pessimismo,
é a razão pela qual a maioria dos projetos solo morre: a estimativa foi feita como se o dev
já soubesse tudo.

## 2. Milestones

### M0 — Fundação (2 semanas, 24 h)
**Entregável:** projeto Unity que abre, com Git funcionando e um cubo que anda.

- Criar projeto URP, configurar render pipeline, serialização em texto
- Git, LFS, YAMLMerge, primeiro commit
- Estrutura de pastas e os 12 `.asmdef`
- Input System com Action Asset e rebind
- Cinemachine com câmera de terceira pessoa
- Controlador de movimento: andar, correr, gravidade, slopes
- Cena `Sandbox_Combate` com chão e três cápsulas

**Critério de saída:** você move um personagem com teclado e gamepad, a câmera acompanha
sem tremer, e o build de Windows abre.

### M1 — Combate cru (4 semanas, 48 h) — **o milestone mais importante do projeto**
**Entregável:** combate divertido contra cápsulas cinzas.

- `StatSheet` com modificadores (doc 07, seção 4.2)
- Pipeline de dano com os 11 estágios e o `Log` de debug
- FSM do jogador com todos os estados
- Ataque leve e forte, com hitbox por evento de animação
- Esquiva e rolamento com frames de invulnerabilidade
- Aparo e riposte com a janela de 0,18 s
- Sistema de Fluxo
- Três posturas com afinidade de arquétipo
- Aço e prata, com a troca de 0,7 s
- Vigor e adrenalina
- Um inimigo com behavior tree, tell de ataque e attack token
- Hitstop, screen shake, camera punch (doc 03, seção 11)
- Console de debug mostrando o log do cálculo de dano

**Critério de saída — e este é o portão do projeto:** você luta contra 4 cápsulas por
10 minutos seguidos e quer continuar. Se não quiser, **pare e reprojete o combate antes de
seguir**. Todo o resto do jogo é conteúdo pendurado neste sistema. Um combate ruim com
300 horas de conteúdo em cima é o pior resultado possível.

### M2 — Sistemas de RPG (4 semanas, 48 h)
**Entregável:** progressão, inventário e alquimia funcionando.

- Inventário com abas, peso e stacking
- `ItemDef` e a hierarquia completa do doc 05, seção 11
- Equipamento com durabilidade e slots
- Alquimia: substâncias, receitas, a tela de três painéis, produção
- Toxicidade com as quatro zonas e os efeitos de tela
- Óleos e bombas
- Trilha de Esgrima completa (13 nós) com a tela de talentos
- Mutagênios com dois slots
- XP, níveis 1 a 5, meditação
- Save e load com round-trip testado

**Critério de saída:** você sobe do nível 1 ao 5, produz três poções, aplica óleo, gasta
oito pontos de trilha, salva, fecha o jogo, abre e tudo está exatamente como estava.

### M3 — Mundo e conteúdo (5 semanas, 60 h)
**Entregável:** o Capítulo I jogável em greybox.

- Vilarejo blocado em ProBuilder, com NavMesh
- Floresta e cemitério blocados
- Cripta blocada
- Addressables e transição entre zonas
- Cinco NPCs com diálogo em Ink, ligado ao `WorldState`
- Sistema de quests com estágios e condições
- Sentidos de Bruxo com o Renderer Feature
- Pistas colhíveis e a tela de dedução
- Bestiário com as quatro seções e o bônus de dano
- Três monstros: barghest, ghoul, afogado
- A Besta com as três fases
- Mercadores e economia
- Cinco composições de encontro
- A escolha da Abigail, com as três resoluções

**Critério de saída:** um jogador externo joga do começo ao fim sem sua intervenção.

### M4 — Vestir e validar (3 semanas, 36 h)
**Entregável:** o protótipo mostrável.

- Assets de placeholder aplicados sobre o greybox
- Animações de Mixamo integradas
- VFX dos três sinais
- Áudio nas três camadas de impacto, ambiente, duas faixas de música
- HUD, menus, tela de morte, opções
- Playtest com três pessoas, e correção do que elas travarem
- Build de release com IL2CPP

**Critério de saída:** o critério de sucesso do doc 00, seção 6, é atingido — 25 a 40
minutos, sem instrução externa, e falha perceptível para quem não se prepara.

## 3. Cronograma consolidado

| Milestone | Semanas | Horas | Acumulado |
|---|---|---|---|
| M0 Fundação | 2 | 24 | 24 h |
| M1 Combate | 4 | 48 | 72 h |
| M2 Sistemas RPG | 4 | 48 | 120 h |
| M3 Mundo e conteúdo | 5 | 60 | 180 h |
| M4 Vestir e validar | 3 | 36 | 216 h |
| **Total** | **18** | **216** | — |

**Protótipo pronto em cerca de 4 a 5 meses**, a 12 horas por semana.

Adicione 30% de folga para imprevistos, que sempre existem: **5 a 6 meses** é a estimativa
honesta. Se você tiver 20 horas por semana, são 3 meses.

## 4. O que NÃO fazer durante os 5 meses

Lista explícita, porque cada item abaixo já matou milhares de projetos solo:

- Não trocar de engine
- Não migrar para HDRP
- Não refatorar arquitetura sem um problema concreto medido
- Não modelar personagens
- Não escrever os capítulos II a V
- Não implementar minijogos
- Não fazer trailer, site, Discord ou página de Steam
- Não adicionar multiplayer, mesmo "só cooperativo, seria fácil"
- Não anunciar publicamente antes de M4

## 5. Roadmap de longo prazo (só se M4 for bem)

Registrado para dar horizonte, não para planejar agora.

| Fase | Escopo | Ordem de grandeza |
|---|---|---|
| F1 | Slice do Capítulo I (M0 a M4) | 5 meses |
| F2 | Prólogo + Capítulo I completos, arte própria | 8 meses |
| F3 | Capítulos II e III | 14 meses |
| F4 | Capítulos IV, V e epílogo | 12 meses |
| F5 | Polimento, localização, otimização | 6 meses |

Total teórico: cerca de **4 anos** em tempo parcial. Este número existe no documento por um
motivo: para você saber, com clareza, que o jogo completo não é o objetivo deste ano. O
objetivo deste ano é o slice, e aprender Unity de verdade fazendo ele.

## 6. Métricas de acompanhamento

Uma planilha simples em `design/tracking.csv`, atualizada semanalmente:

| Coluna | Por que |
|---|---|
| Semana | — |
| Horas trabalhadas | Detecta se a premissa de 12 h é real |
| Tarefas concluídas | Velocidade |
| Tarefas adicionadas | **Detecta escopo crescendo** — a métrica mais importante |
| Bugs abertos | Saúde do código |
| Milestone atual | Progresso |

Se "tarefas adicionadas" superar "tarefas concluídas" por três semanas seguidas, você não
está desenvolvendo, está projetando. Corte escopo.
