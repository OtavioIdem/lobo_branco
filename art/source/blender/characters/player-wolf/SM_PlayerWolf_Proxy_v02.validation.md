# Revisão de modelagem — PlayerWolf v02

Data: 2026-09-12. Blender 5.2.1 LTS.

Revisão solicitada pelo usuário a partir da referência frontal externa. A fonte v01
foi preservada. Esta versão continua sendo um proxy de modelagem; a semelhança fina
e o realismo da referência ainda não estão concluídos.

## Alterações

- Cabeça com superfície facial contínua, nariz projetado, órbitas, olhos âmbar,
  pálpebras, boca, sobrancelhas, orelhas, mandíbula e cicatriz facial.
- Barba curta pintada na própria malha por cores de vértice, com fios curtos.
  O volume oval branco da v01 foi substituído.
- Cabelo branco orientado para trás, cobertura das laterais e cabelo preso na nuca.
- Tronco curvo, cintura mais estreita, coxas e panturrilhas com variação anatômica.
- Luvas com quatro dedos separados, articulações e polegar; revisão de ombros,
  cotovelos, joelhos e botas. Punhos e transições cobrem as junções de volumes.
- Peitoral curvo, faldões separados, cota frontal, cintos, fivelas e bainhas.
- Componentes identificáveis por grupos de vértices no objeto de exportação.

## Entregas

- `SM_PlayerWolf_Proxy_v02.blend`: fonte editável, coleção `EXPORT_PlayerWolf_Proxy`.
- `SM_PlayerWolf_Proxy_v02.fbx`: exportação isolada, sem cenário de apresentação.
- `previews/PlayerWolf_v02_*.png`: renders frontal em perspectiva, rosto, três quartos,
  perfil e costas. São renders reais da malha no Blender.
- `scripts/refine_player_wolf.py`: construção reproduzível a partir da v01.
- `scripts/validate_revision.py`: verificações da fonte e reimportação real do FBX.
- `validation_roundtrip.json`: resultados medidos da validação.

## Validação e critérios

A validação automatizada verifica coordenadas finitas, altura de 1,85 m, pés em Z=0,
pivô na origem, escala e rotação aplicadas, limite de 15.000 triângulos, cinco slots
de material, cores de vértice presentes e ausência de triângulos degenerados.
O FBX é reimportado em uma cena limpa; altura, triângulos e materiais são comparados
com a fonte. Contagens exatas e resultado estão em `validation_roundtrip.json`.

Os renders foram inspecionados durante a revisão para verificar leitura do rosto,
barba, silhueta, mãos e junções. A inspeção não equivale a aprovação de arte final.

## Limitações

- Malha de proxy composta por componentes, sem retopologia para deformação, rig,
  UV, mapas de textura, animação ou LOD. Não está pronta para animar em produção.
- Materiais usam cores de vértice para barba, olhos e variações de roupa. O FBX
  preserva essas cores, mas a Unity precisará de um shader que as leia.
- A revisão não foi instalada no prefab jogável e não foi validada na Unity.
- A referência é frontal: perfil, nuca, costas e parte inferior das pernas são
  aproximações. A aparência segue estilizada e ainda requer escultura fina.
- O controle visual da sessão interativa falhou ao inicializar. A revisão foi
  criada com o Blender em modo de processamento, a partir da v01 salva em disco.
  Alterações eventualmente não salvas na sessão original não foram acessadas.
- A referência oficial permanece externa e não foi copiada nem empacotada.
