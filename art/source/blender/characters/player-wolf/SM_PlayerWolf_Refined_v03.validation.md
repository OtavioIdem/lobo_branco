# PlayerWolf v03 — acabamento no Blender

Revisão concluída em 2026-09-14. Fonte: v02 aprovada pelo usuário.

O pedido de mais acabamento foi aplicado como estudo de modelagem em maior
resolução. A v02 leve foi preservada. Esta v03 não substitui o asset do jogador
na Unity nem altera os critérios de desempenho do proxy já integrado.

## Alterações

- Rosto reconstruído com mais resolução: órbitas, pálpebras, ponte nasal,
  mandíbula, sulcos de expressão, lábios, sobrancelhas e cicatriz.
- Barba curta com fios individuais e transição de cor sobre a própria pele.
- Cabelo com mechas e fios acompanhando a superfície, laterais e cabelo preso.
- Superfícies do corpo e das peças de roupa suavizadas; componentes continuam
  separados e nomeados para edição na coleção `AUTHORING_PlayerWolf_v03`.
- Cota de malha com anéis volumétricos, costuras e materiais procedurais que
  diferenciam pele, cabelo, couro, tecido e aço.

## Arquivos

- `SM_PlayerWolf_Refined_v03.blend`: fonte editável com materiais completos.
- `SM_PlayerWolf_Refined_v03.fbx`: geometria e cores de vértice para intercâmbio.
- `previews/PlayerWolf_v03_full.png` e `previews/PlayerWolf_v03_face.png`:
  renders da revisão final, inspecionados durante a entrega.
- `scripts/finish_player_wolf_v03.py`: construção sobre a v02 salva.
- `scripts/validate_player_wolf_v03.py`: validação da fonte e reimportação FBX.
- `validation_v03_roundtrip.json`: contagens medidas e resultado das verificações.

## Validação

Blender 5.2.1 LTS. Altura total de 1,85 m incluindo as espadas, pés em Z=0,
raiz na origem, dez materiais. A fonte possui aproximadamente 460 mil triângulos:
é uma malha de trabalho de alta resolução, acima do orçamento de 15 mil da v02.
As contagens exatas constam no JSON de validação.

O teste reabre o `.blend`, verifica coordenadas finitas, faces degeneradas, cores,
organização dos componentes e integridade da v02. Depois importa o FBX em uma
cena limpa e compara altura, triângulos, componentes e materiais com a fonte.

## Limitações

A aparência continua estilizada e ainda não reproduz a referência em nível
fotorrealista. Não há rig, animação, retopologia de produção, LOD ou integração
validada na Unity. Os materiais procedurais funcionam no Blender; a exportação
FBX não leva toda a rede de shaders. Para o jogo serão necessários otimização,
UVs, bake e materiais compatíveis. Não foi copiado nem empacotado nenhum asset
oficial; a imagem de referência permanece externa.
