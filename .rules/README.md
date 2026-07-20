# Regras — AppMorador

Cada arquivo é um conjunto de regras permanentes de um domínio, independente dos demais.
Regras nunca substituem agentes nem vice-versa: agentes definem *quem decide*, regras definem
*o que não pode ser violado* (ver `CLAUDE.md`).

| Regra | Cobre |
|---|---|
| `arquitetura` | Camadas, direção de dependência, fronteira de idioma (negócio vs. infraestrutura) |
| `backend` | Casos de uso, DTOs, mensagens de erro, serialização |
| `mobile` | Componentização, tokens, sessão segura, estados de tela |
| `produto` | Escopo, Product First, corte explícito de funcionalidade |
| `seguranca` | Segredos, OWASP, autenticação/autorização |
| `design-system` | Fonte única de tokens visuais, semântica de cor/animação |
| `performance` | Medir antes de otimizar, consultas, thread de animação |
| `documentacao` | ADRs, changelog, dívida técnica |

## Como escolher

Carregue só as regras dos domínios afetados pela tarefa atual, combinando com os agentes já
selecionados (ver `CLAUDE.md` → "Como selecionar regras"). Nunca carregar as 8 de uma vez.
