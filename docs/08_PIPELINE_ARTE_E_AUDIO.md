# 08 — Pipeline de Arte e Áudio

## 1. A restrição que define tudo

**Nenhum asset do The Witcher (2007) entra neste projeto.** Nem malha, nem textura, nem
som, nem música, nem animação, nem texto de diálogo copiado. Detalhes em `10_LEGAL_E_RISCOS.md`.

Isso não é só uma questão legal: extrair assets de um jogo de 2007 te daria arte de 2007,
o que derrota o propósito de fazer um remaster.

## 2. Estratégia de arte para dev solo

A armadilha clássica é tentar produzir arte final antes de o jogo ser divertido. A ordem
correta é a inversa, em quatro fases:

### Fase A — Greybox (semanas 1 a 6)
Tudo é cubo cinza. **ProBuilder** para blocar o vilarejo, a floresta e a cripta.
Personagem é uma cápsula com um cone na frente. Inimigo é uma cápsula vermelha.

Objetivo: validar escala, distâncias, tempo de travessia e legibilidade de combate.
Se o combate não é divertido em greybox, arte não vai salvar.

Escalas de referência a fixar agora e nunca mudar:
| Medida | Valor |
|---|---|
| Altura do jogador | 1,85 m |
| Velocidade de caminhada | 2,0 m/s |
| Velocidade de corrida | 5,5 m/s |
| Largura de porta | 1,2 m |
| Altura de degrau máxima | 0,4 m |
| Travessia do vilarejo | 45 segundos |
| Vilarejo até a cripta | 2 minutos |

### Fase B — Placeholder funcional (semanas 6 a 14)
Assets de terceiros, gratuitos ou baratos, apenas para dar leitura. Não polir nada.

Fontes recomendadas:

| Fonte | O que | Custo |
|---|---|---|
| **Unity Asset Store** — pacotes de medieval/fantasy | Ambiente, props | 0 a 60 USD |
| **Quaternius** | Modelos low-poly CC0 | Grátis |
| **Kenney.nl** | Props, UI, áudio CC0 | Grátis |
| **Mixamo** (Adobe) | Animações humanoides, rig automático | Grátis |
| **Polyhaven** | HDRIs, texturas, modelos CC0 | Grátis |
| **Sketchfab** (filtro CC) | Modelos diversos | Grátis / pago |
| **Synty Studios** | Pacotes estilizados coerentes entre si | 20 a 80 USD |

**Recomendação prática:** um único pacote Synty de fantasia medieval te dá um vilarejo
inteiro coerente por menos de 50 dólares, e economiza mais de 200 horas. Para um protótipo
de aprendizado, isso é o melhor dinheiro que você vai gastar.

**Sobre animação:** Mixamo cobre locomoção e reações. Ele **não** cobre combate de espada
bom. Para o slice, use Mixamo e aceite que o combate vai parecer genérico; a decisão de
investir em animação de combate (Asset Store, Cascadeur, ou animar à mão em Blender) fica
para depois de o combate estar validado.

### Fase C — Arte de direção (depois do slice)
Só aqui se define uma direção de arte própria. Não antes.

### Fase D — Polimento
Nunca no protótipo.

## 3. Direção de arte pretendida (para quando chegar a fase C)

Registrada agora para que as escolhas de placeholder não contradigam o alvo.

- **Paleta:** dessaturada, terrosa. Marrom, verde-musgo, cinza-pedra. O único vermelho
  saturado do jogo é sangue e o brasão da Salamandra.
- **Iluminação:** naturalista, contrastada. Interiores escuros de verdade — a poção Gato
  precisa ter função.
- **Referência visual:** pintura holandesa do século XVII para interiores, e cinema de
  fantasia suja (não high fantasy). Nada brilhante, nada limpo.
- **Legibilidade acima de realismo:** monstros precisam ter silhueta reconhecível a 20 m,
  e cada ataque precisa ter uma pose de anticipação distinta.

## 4. Pipeline técnico de assets

### Modelagem
**Blender** (grátis) para tudo. Fluxo: modelar → UV → bake → exportar FBX.

Convenções de exportação para Unity, e elas evitam horas de retrabalho:
- Escala: 1 unidade Blender = 1 metro = 1 unidade Unity
- Eixo para cima: Z no Blender, Y na Unity. Exportar FBX com `Y up`, `-Z forward`
- Aplicar todas as transformações antes de exportar
- Origem do objeto nos pés, no chão, no centro
- Nomear: `SM_` para malha estática, `SK_` para skinned, `T_` para textura, `M_` para material

### Texturas
- **Materialize** ou **ArmorPaint** (grátis) se não quiser assinar Substance
- PBR metallic-roughness, que é o que URP espera
- Canal packing: Metallic em R, Occlusion em G, nada em B, Smoothness em A (`_MaskMap`)
- Formato: importar como PNG/TGA, deixar a Unity comprimir para BC7

### Rig e animação
- Rig humanoide compatível com Mecanim, para reaproveitar animações entre personagens
- Avatar humanoide na importação; retargeting resolve o resto
- Root motion **desligado** para o jogador (o movimento é dirigido por código, o que dá
  controle preciso das distâncias de esquiva do doc 03)
- Root motion **ligado** para inimigos grandes e ataques com investida

### Efeitos visuais
- **VFX Graph** para sinais (Igni, Aard, Yrden) e sangue. É o que justifica o custo de
  aprendizado do sistema.
- **Shader Graph** para: Sentidos de Bruxo (efeito de tela cheia), aplicação de óleo na
  lâmina, dissolução de morte, distorção de toxicidade alta.
- Sentidos de Bruxo especificamente: um Renderer Feature de URP com um passe de contorno
  por Depth Normals, mais dessaturação da tela e realce de objetos numa layer específica.

## 5. Áudio

### Fontes
| Fonte | O que | Licença |
|---|---|---|
| **Freesound.org** | SFX diversos | CC0 e CC-BY (checar cada arquivo) |
| **Kenney Audio** | Pacotes de SFX | CC0 |
| **Sonniss GDC Bundle** | Bibliotecas profissionais, liberadas anualmente | Uso livre |
| **Zapsplat** | SFX amplo | Grátis com atribuição |
| **Incompetech / Kevin MacLeod** | Música | CC-BY |

**Música:** não use a trilha de Adam Skorupa do original, nem no protótipo. Para
prototipagem, use faixas CC-BY de fantasia. Se o projeto crescer, contratar um compositor
para 4 faixas é mais viável do que parece.

### Arquitetura de áudio
Unity Audio nativo é suficiente para o protótipo. **FMOD** ou **Wwise** só se o projeto
virar sério (ambos têm licença gratuita para uso não-comercial).

Mixer com quatro grupos: `Master → { Music, SFX, Ambience, UI }`, com snapshots para
combate (abafa ambiente, realça impactos) e para toxicidade crítica (filtro low-pass).

### As 3 camadas de som de impacto (doc 03, seção 11)
1. **Swing** — whoosh da lâmina, varia por postura
2. **Impacto** — por material do alvo: carne, couro, metal, pedra, osso
3. **Reação** — vocalização do alvo, com variação para não repetir

Isso exige uma matriz de material por arma. Implementar como um `ScriptableObject` de
tabela de impacto, indexado por `(WeaponType, SurfaceType)`.

## 6. Fontes e UI

- Fonte de corpo: algo legível com suporte a acentos portugueses. **EB Garamond** ou
  **Cardo** (SIL Open Font License) dão o tom de manuscrito sem sacrificar leitura.
- Fonte de títulos: uma serifa com peso, não uma fonte gótica ilegível.
- UI construída em **UI Toolkit** (UXML/USS) e não em uGUI. É mais próximo de CSS, escala
  melhor e é o caminho que a Unity está seguindo. A curva de aprendizado vale.

## 7. Rastreamento de licenças

`art/CREDITS.md` e `audio/CREDITS.md`, atualizados **no mesmo commit** em que o asset entra.
Cada linha com: arquivo, fonte, autor, licença, URL.

Deixar isso para o fim é como você acaba com 300 assets de procedência desconhecida e um
projeto que não pode ser mostrado a ninguém.
