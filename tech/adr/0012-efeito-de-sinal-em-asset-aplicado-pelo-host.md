# ADR 0012 — Efeito de sinal: subclasse de asset, aplicado pelo host, com variante somada pela escola

Data: 2026-09-15
Status: aceita

## Contexto
A tarefa 1.18b cria o efeito de sinal como dado. É a base das partes 1.18c a 1.18f (abridor,
fogo, escudo, armadilha) e da variante de escola do `docs/13` §5, em que cada escola é
especializada num sinal e ganha um efeito só dela. Quatro perguntas tinham que ser respondidas
antes do primeiro efeito:

1. **Como um efeito vira dado**, se cada sinal faz uma coisa diferente.
2. **Quem aplica**, e como o resultado chega nas outras máquinas.
3. **Onde mora a variante da escola**, sem `if` de escola em código de combate (regra 7).
4. **O que a intensidade escala**, se o `docs/03` §8 fala em "escalado por Inteligência" sem
   fórmula e o `docs/02` §5 põe a intensidade na Inteligência.

## Decisão
**Um efeito é uma subclasse de `SignEffectDef`, um asset. A habilidade tem uma área e uma lista
de efeitos. Só o host resolve: acha quem está na área e aplica os efeitos da habilidade e depois
os da variante da escola. A intensidade escala potência e nunca forma.**

| Pergunta | Resposta |
|---|---|
| Formato do efeito | Subclasse abstrata de `ScriptableObject`, com `Apply(in SignCast, CharacterVitals)` |
| Área | `SignArea` no `AbilityDef`: cone ou raio, alcance e abertura. Uma consulta `OverlapSphereNonAlloc` e filtro de ângulo |
| Alvo | `CharacterVitals`, e não `IDamageable`: nem todo sinal fere, e controle, vida e perfil moram ao lado dele |
| Ordem dos alvos | Do mais perto para o mais longe, com teto de memória. Quem está abatido e quem conjurou ficam de fora |
| Quem aplica | O host, no `CastTriggered` do `PlayerAbilityCaster`, que só dispara em quem resolve. Posição e frente são a cópia do host |
| Como chega aos outros | Pelo estado replicado do alvo (`ControlStatus`, `CharacterVitals`). Efeito não manda mensagem própria |
| Variante | `SchoolDef.specializedAbility` mais `variantEffects`. Os efeitos da variante são **somados** depois dos do sinal, nunca trocados |
| Intensidade | `CombatTuningDef.SignIntensity`: `SignIntensity × Inteligência ÷ 10`. Com o Lobo, 1,0 |
| O que escala | Potência (duração, dano, força), pelo `SignCast.Scale`. Área, custo e recarga nunca |

## Consequências
- **Criar um sinal é criar assets, e um efeito novo é uma classe pequena.** O abridor da 1.18c é
  um `SignEffectDef` de controle, e a variante do Grifo na 1.33 é um asset arrastado para a escola.
- **O efeito sai na posição que o host vê.** Com ping, o cone parte de onde o bruxo estava há meia
  ida e volta. É o mesmo atraso da janela de dano do golpe, e pelo mesmo motivo: a alternativa é o
  cliente declarar onde o sinal pegou.
- **Nenhum byte a mais na rede por conjuração.** O pedido de efeito já existia desde a 1.32, e o
  resultado viaja no estado que cada alvo já replica.
- **A Inteligência passa a fazer alguma coisa.** O viés de atributo do Grifo no `docs/13` §5 é
  Inteligência, e com ela na fórmula ele fortalece os sinais sem campo novo. O multiplicador
  `SignIntensity` fica neutro em 1,0 para poção, talento e o sinal reforçado da adrenalina.
- **Um bloco sem Inteligência ou sem multiplicador zera todo sinal sem erro.** Há teste sobre as
  escolas do projeto e um aviso único em jogo.
- **Variante somada não sabe tirar.** Se a 1.33 desenhar um Igni do Grifo que *não* queima, a
  regra precisa mudar para troca, e essa troca vira decisão nova.
- **O escudo (1.18e) não tem área**, e a regra "efeito sem área é problema" vai precisar de uma
  forma para quem conjura. Ela entra com o escudo, e não antes.
- **A altura não entra na área.** Uma criatura em cima de um muro, dentro do alcance, é atingida.
  O capítulo I é quase todo chão; quando não for, a regra entra no `SignArea`.

## Alternativas consideradas
- **Lista polimórfica com `[SerializeReference]` dentro da habilidade.** Guarda tudo num asset só,
  mas precisa de editor próprio para escolher o tipo de cada item, e o mesmo efeito não pode ser
  reaproveitado na variante de outra escola sem cópia.
- **Um enum de tipo de efeito com um `switch`.** Todo efeito novo mexe no mesmo `switch`, e os
  campos de todos os efeitos convivem no mesmo asset, a maioria sem sentido para cada sinal.
- **Variante como outra `AbilityDef` que a escola põe na vaga.** Duplica custo, recarga e área do
  sinal base em dois assets, e o primeiro ajuste de balanceamento esquece um dos dois.
- **Intensidade só pelo atributo `SignIntensity`, sem Inteligência.** Deixaria o viés de atributo
  do Grifo sem efeito, e a escola teria que subir dois números para dizer a mesma coisa.
- **Intensidade escalando o custo, lendo "custo escalado por Inteligência" ao pé da letra.** Uma
  escola de sinais conjuraria mais barato, e os 30 de vigor do `docs/03` §8 deixariam de ser o
  número do documento para quem mais usa sinal. O dilema do vigor do §7 é medido contra eles.
- **Intensidade escalando a área.** O cone é o que o jogador aprende a mirar e o companheiro
  aprende a ler. Um alcance que muda com a escola quebra essa leitura em coop.
- **O dono consulta a área e manda a lista de alvos.** Seria o cliente declarando quem o sinal
  pegou, que é declarar efeito, e a ADR 0008 não permite.
