---
name: avaliar-design-jogo
description: Avaliar ideias, mecanicas e mudancas de escopo do Projeto Lobo Branco antes de implementacao. Use para decidir se uma proposta entra no slice, quais pilares serve, quais riscos cria e como testa-la; nao use para modelagem ou implementacao C#.
---

# Avaliar design do jogo

Comece por `AGENTS.md`, `CLAUDE.md` e pelo documento canônico relacionado. Leia `docs/13_COOP_E_REDE.md` quando houver cooperação, escola, autoridade ou contradição com documentos anteriores.

## Avaliação

1. Expresse o problema do jogador que a proposta tenta resolver.
2. Identifique evidência no projeto; não transforme preferência em requisito.
3. Verifique se a feature serve a P1 investigação ou P2 preparação. P3 e P4 estão adiados. Uma prova técnica pode não servir diretamente a um pilar, mas precisa habilitar uma feature aprovada, ter hipótese mensurável e ser limitada em tempo e escopo.
4. Confirme que cabe no Capítulo I, para 2–4 jogadores e escolas Lobo/Grifo.
5. Relacione a um defeito D1–D13 de `docs/01` quando aplicável.
6. Descreva custo, dependências, comportamento em coop, casos de borda e risco de regressão.
7. Defina hipótese e teste observável antes de recomendar produção.

Se a ideia estiver fora do escopo, diga onde o limite está registrado e proponha a menor variante que preserve a intenção. Não altere código ou assets durante uma avaliação.

Quando houver números, mantenha-os nos CSVs correspondentes em `design/` e sincronize a documentação. Diferencie claramente valor atual, valor proposto e resultado de playtest.

## Saída

Entregue: decisão (`aprovar experimento`, `adiar`, `rejeitar` ou `precisa de evidência`), justificativa canônica, impacto no coop, custo, riscos, critérios de aceite e próximos responsáveis.
