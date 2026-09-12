# Brief — PlayerWolf Proxy

## Controle

- ID: `CHAR-PLAYER-WOLF-PROXY-001`
- Nome agnóstico de IP: `PlayerWolf_Proxy`
- Categoria: personagem jogável
- Estágio: prova de pipeline / proxy
- Status: aprovado
- Responsável: `blender_artist`
- Aprovador visual: `art_director`
- Aprovador técnico: `unity_technical_artist`
- Data de aprovação: 2026-09-12

## Função e leitura

- Necessidade: substituir temporariamente a cápsula por um fan model do Geralt produzido do zero e validar escala, silhueta e handoff Blender → Unity.
- Pilar: prova técnica limitada que habilita o personagem já aprovado; não adiciona uma feature nova.
- Contexto: câmera de terceira pessoa, leitura principal entre 2 m e 20 m, coop para 2–4 jogadores.
- Escala: altura total de 1,85 m, pés no plano Z=0 do Blender.
- Referência Unity: `CharacterController` atual com 1,85 m.

## Visual

- Geralt fan-made, aproximadamente 45 anos, atlético e magro, postura firme e experiente.
- Cabelo branco penteado para trás, barba curta grisalha, sobrancelhas marcadas e cicatriz facial discreta conforme a referência enviada.
- Armadura escura em camadas: couro negro, cota de malha, peitoral reforçado, tiras cruzadas, ombreiras, braçadeiras e luvas.
- Medalhão e duas espadas nas costas entram como formas simplificadas no proxy para validar a silhueta; detalhes ficam para fases posteriores.
- Paleta: preto carvão, aço escurecido, couro marrom-avermelhado e cabelo branco frio.
- A-pose relaxada, braços aproximadamente 35 graus afastados do tronco.
- Referência externa enviada pelo usuário: `C:\Users\Otávio\Downloads\Witcher_PNG22.webp` (não versionar como asset).
- Preview do blockout: `art/concept/characters/PlayerWolf_Geralt_Blockout_v01.png`.
- Turnaround ortográfico fiel: pendente; a geração automática foi bloqueada, então as vistas lateral e traseira do proxy são aproximações baseadas na referência frontal.

## Contrato técnico

- Blender: 5.2.1 LTS em `E:\Blender\blender.exe`.
- Fonte: `art/source/blender/characters/player-wolf/SM_PlayerWolf_Proxy_v01.blend`.
- FBX: `unity/LoboBranco/Assets/_Project/Art/Characters/PlayerWolf/SM_PlayerWolf_Proxy_v01.fbx`.
- Texturas: não aplicável; materiais planos de proxy.
- Destino Unity: `Assets/_Project/Art/Characters/PlayerWolf/`.
- Collection exportada: `EXPORT_PlayerWolf_Proxy`.
- Objeto raiz: `SM_PlayerWolf_Proxy_v01`.
- Orçamento: máximo de 15.000 triângulos para esta prova; não é orçamento de arte final.
- Materiais: até 5 slots planos (`M_ProxySkin`, `M_ProxyHair`, `M_ProxyCloth`, `M_ProxyLeather`, `M_ProxyMetal`).
- UV: não aplicável nesta prova; sem texturas.
- LOD: não aplicável; proxy descartável.
- Colisão: não aplicável; o `CharacterController` continua sendo a autoridade física.
- Rig e animação: não aplicável nesta prova; a A-pose prepara a próxima etapa.
- Exportação: FBX, `Y up`, `-Z forward`, transforms aplicadas, somente a collection autorizada.

## Critérios mensuráveis

- Altura esperada: 1,85 m, tolerância máxima de 1%.
- Transform após importação: escala `(1,1,1)` e rotação sem correção manual permanente.
- Origem: no chão, centralizada entre os pés.
- A silhueta deve permanecer legível ao lado da referência de jogador de 1,85 m.
- Reimportar o FBX não pode alterar dimensão, materiais ou orientação.
- Nenhum erro relacionado ao modelo no Console.
- Visual e materiais devem ser identificáveis na cena sem depender de textura.
- Build Windows: não exigido nesta primeira etapa; necessário antes de substituir o placeholder jogável.

## Entrega e pendências

- Entregas: concept turnaround, `.blend`, FBX e relatório curto de validação.
- `art/CREDITS.md`: não aplicável nesta etapa; a imagem oficial é referência externa e não é incorporada. Concept de trabalho e geometria são gerados para o projeto.
- Pendências: rig, retopologia final, UV, texturas, cabelo final, armas, sockets, LOD e prefab jogável.
- Resultado: proxy aprovado para teste de escala e importação em 2026-09-12; ver o relatório ao lado do `.blend`.
