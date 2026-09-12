# ADR 0009 — Percepcao e ataque em componente, arvore no grafo

Data: 2026-09-12
Status: aceita

## Contexto
A [ADR 0004](0004-behavior-trees-oficiais.md) decidiu que a IA de inimigos usa
`com.unity.behavior`. Ela nao disse **quanto** do comportamento mora dentro do grafo, e
essa e a pergunta que a tarefa 1.21 obrigou a responder.

Tres fatos apareceram ao implementar:

1. **Um no so executa enquanto o galho dele esta ativo.** Se a percepcao morasse em um no,
   uma criatura perceberia o bruxo apenas enquanto estivesse no ramo de patrulha, e ficaria
   cega justamente enquanto persegue ou golpeia.
2. **O asset de grafo nao e construivel por script.** `BehaviorAuthoringGraph` e interno ao
   pacote, e `BehaviorGraphModule` tambem. Todo o resto do projeto e montado por codigo
   (`SandboxSetup`, `CombatDataSetup`), justamente para ser reproduzivel e aparecer em
   diff. O grafo e a unica coisa que nao pode ser.
3. **O grafo nao e testavel em EditMode.** Ele e um asset com referencias serializadas, e
   verificar um cone de visao atraves dele exigiria cena, fisica e framerate.

## Decisao
O grafo carrega **apenas a estrutura de decisao**: a ordem dos ramos, as condicoes de
troca e as repeticoes. Tudo que tem numero, geometria ou tempo mora em componente e em
ScriptableObject.

Na pratica, a divisao e esta:

| Onde | O que |
|---|---|
| `MonsterDef` | alcance de visao, cone, ouvido, memoria, velocidade, golpe, pausa |
| `EnemySenses` | a matematica do cone, do circulo e da memoria. Classe pura, testada |
| `EnemyAgent` | busca de alvo, linha de visao, giro, velocidade do NavMeshAgent |
| `EnemyMeleeAttacker` | linha do tempo do golpe, hitbox, pipeline de dano |
| `AttackTokenPool` | quem tem a vez de golpear. Classe pura, testada |
| Nos customizados | verbos finos que so repassam: pegar alvo, esta cacando, ao alcance, golpear, rondar |
| Grafo | a arvore: patrulhar, ou perseguir, ou golpear, ou rondar esperando a vez |

A percepcao roda no `Update` do `EnemyAgent`, e nao dentro de um no, pelo fato 1.

## Consequencias
- O cone de visao, a memoria e a janela de dano tem teste em EditMode, sem cena.
- Balancear IA e editar asset, como manda a regra 1 do `CLAUDE.md`.
- Trocar a arvore de um monstro nao recompila nada.
- **O grafo continua sendo o unico passo manual do projeto.** Todo setup novo de inimigo
  termina com uma visita ao editor grafico. Isso esta registrado como risco.
- Os nos customizados ficam quase vazios, e isso e proposital: um no gordo seria logica
  fora de teste.

## Apêndice: como montar o grafo do barghest

Isto está aqui, e não numa skill, porque é o único passo do projeto que não pode ser
automatizado, e ele precisa estar escrito em algum lugar antes de ser esquecido.

1. `Assets/_Project/Data/AI/` → botão direito → **Create > Behavior > Behavior Graph**.
   Nomeie `BT_Barghest`.
2. No quadro negro do grafo, crie uma variável `Target` do tipo `GameObject`. A variável
   `Self` já existe e aponta para quem carrega o grafo.
3. Monte a árvore:

```
Repeat Forever
└── Selector
    ├── Sequence                                  ← o ramo de combate
    │   ├── [Self] esta cacando alguem            ← condição, categoria Lobo Branco
    │   ├── [Self] pega o alvo em [Target]        ← ação, categoria Lobo Branco
    │   └── Selector
    │       ├── Sequence                          ← já está perto o bastante
    │       │   ├── [Self] esta ao alcance do golpe
    │       │   └── Selector
    │       │       ├── [Self] golpeia            ← falha quando não tem a vez
    │       │       └── [Self] ronda o alvo       ← espera a vez rondando
    │       └── Navigate To Target                 ← nó do pacote; ainda longe
    └── Wait                                       ← ocioso; vira patrulha quando houver rota
```

A ordem dos dois últimos filhos do Selector interno é o que faz o token funcionar: `golpeia`
pede a vez ao coordenador e devolve falha quando o alvo já tem dois atacantes, e é essa falha
que manda a criatura rondar em vez de bater.

No `Navigate To Target`, ponha `Distance Threshold` em torno de 1,6, que é a distância de
engajamento do barghest. Maior que o alcance do golpe faz a criatura parar longe demais para
acertar.

4. Abra `Assets/_Project/Prefabs/Characters/Enemy_Barghest.prefab` e arraste `BT_Barghest`
   para o campo `Behavior Graph` do componente `Behavior Agent`. Não mexa em
   `Netcode Run Only On Owner`: ele tem que continuar ligado, senão a árvore roda também
   no cliente e os dois lados decidem coisas diferentes.
5. Rode a `Sandbox_Combate`, ande até 18 m de um caçador e confira três coisas: ele vira,
   vem, e para a 0,65 s antes de acertar.
6. Duplique os caçadores até ter quatro em cima de você e confira a quarta: no máximo dois
   golpeiam de cada vez, e os outros dois rondam.

## Alternativas consideradas
- **Tudo dentro do grafo**, com nos que fazem a conta de percepcao. Descartada pelo fato 1:
  a criatura ficaria cega fora do ramo de patrulha, e o sintoma apareceria como "as vezes
  ele nao me ve" sem nada no Console.
- **FSM propria para inimigos**, como a do jogador. Descartada porque contraria a ADR 0004
  e porque nao escala com o numero de arquetipos. A ADR 0004 ja tinha recusado isso.
- **Gerar o asset de grafo por reflexao** sobre os tipos internos do pacote. Descartada: o
  projeto ja usa reflexao uma vez (`NetworkObject.OnValidate`, em `SandboxSetup`), e ali e
  uma chamada de um metodo. Montar um grafo inteiro por reflexao seria depender da forma
  interna de um pacote novo, que muda entre versoes menores.
