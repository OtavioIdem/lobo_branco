# Validação — SM_PlayerWolf_Proxy_v01

Data: 2026-09-12

## Blender

- Blender: 5.2.1 LTS, hash `9e2066aef7ef`.
- Fonte: `SM_PlayerWolf_Proxy_v01.blend`.
- Exportação: FBX 7.4, `Y up`, `-Z forward`, somente a malha do personagem.
- Altura da fonte: 1,850000 m.
- Triângulos: 6.500 de um máximo de 15.000.
- Materiais: `M_ProxyCloth`, `M_ProxyHair`, `M_ProxyLeather`, `M_ProxyMetal` e `M_ProxySkin`.

## Round-trip FBX

- Malhas importadas em cena limpa: 1.
- Altura reimportada: 1,850000 m.
- Base: Z = 0,000000 m.
- Triângulos reimportados: 6.500.
- Materiais reimportados: 5.
- Resultado: aprovado.

## Unity

- Unity: 6000.6.0f1, URP 17.6.0.
- `ModelImporter.globalScale`: 1.
- Colliders automáticos: desligados.
- UV secundária automática: desligada, conforme o brief do proxy.
- Compilação/importação batch: `RESULTADO: compila limpo.`
- Build Windows e substituição da cápsula: não executados nesta etapa.

## Limitações visuais

- Blockout por volumes, sem anatomia final, UV, texturas, rig ou animação.
- Vistas lateral e traseira são aproximações porque a referência fornecida é frontal.
- O turnaround fiel gerado por imagem não ficou disponível; não foi usada API/CLI alternativa.

