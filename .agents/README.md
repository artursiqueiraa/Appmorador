# Agentes — AppMorador AI Engineering Team

Cada arquivo desta pasta é um especialista permanente com responsabilidade única, sem
sobreposição com os demais. `CLAUDE.md` explica o fluxo de seleção; esta tabela existe para que
esse passo seja rápido — decidir quais agentes carregar sem precisar abrir os 15 arquivos.

| Agente | Domínio (uma linha) |
|---|---|
| `arquiteto` | Limites de camada (Domain/Application/Infrastructure/Api), direção de dependência, ADRs |
| `produto` | Escopo de Sprint, Product First, decisões de classificação de negócio |
| `backend` | Entidades, casos de uso, DTOs, controllers (.NET) |
| `mobile` | Telas, navegação, estado de sessão (React Native/Expo) |
| `banco` | Schema EF Core, migrations, protocolo de revisão de migration |
| `seguranca` | JWT, OWASP, segredos, rate limit/lockout |
| `design-system` | Tokens visuais (`theme/tokens.ts`), fonte única de verdade visual |
| `ux` | Copy, fluxo de experiência, estados vazios/erro, semântica de feedback tátil |
| `performance` | Re-renders, queries, tempo de abertura — audita e recomenda, não implementa |
| `testes` | Roteiro de verificação (build, type-check, fluxo `curl`), casos de erro |
| `documentacao` | `docs/*.md`, organização de `docs/adr/` |
| `jfl` | Protocolo 0x7B, `AlarmEventProcessor`, catálogo Contact ID |
| `integracao` | Captura de snapshot: CGI/ISAPI, Digest Auth, DVRs/NVRs |
| `devops` | Ambiente local, secrets, `Program.cs`, setup reproduzível |
| `reviewer` | Portão final — audita os outros 14 contra os 8 pilares, nunca implementa |

## Como escolher

Carregue só os agentes cujo domínio a tarefa toca — nunca o time inteiro (ver `CLAUDE.md` →
"Como selecionar agentes"). Uma mudança visual no Dashboard mobile, por exemplo, tipicamente
carrega `mobile` + `design-system` + `ux`, não os 15.

## Regra de não sobreposição

Cada agente tem uma seção "O que nunca pode alterar" apontando explicitamente para quem é dono
daquilo. Se dois agentes parecerem reivindicar a mesma responsabilidade, é um bug de
documentação — reporte para correção (ver `docs/adr/` para o histórico de arbitragens já
feitas pelo `arquiteto`).
