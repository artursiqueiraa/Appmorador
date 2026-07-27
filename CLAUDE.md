# CLAUDE.md — AppMorador

Este arquivo é o **roteador oficial** do projeto — não um manual. Ele diz *onde* buscar
contexto (`.agents/`, `.rules/`, `docs/adr/`), não *o que* fazer em cada domínio. Regras
específicas de backend, mobile, segurança, design system, etc. vivem nos agentes e regras
correspondentes, nunca aqui.

## Visão do AppMorador

Segurança Conectada: um app de segurança self-service (B2C/B2B) para residências e pequenos
comércios/condomínios — cadastro, propriedades, central de alarme (protocolo JFL), câmeras e um
dashboard que traduz tudo isso em linguagem simples e tranquilizadora para quem não é técnico.

## Arquitetura geral

Backend em Clean Architecture: `Domain` (entidades/regras puras) → `Application` (casos de uso,
DTOs, portas) → `Infrastructure` (EF Core, JWT, protocolo JFL, integração de câmeras) → `Api`
(controllers finos). Mobile é um app React Native/Expo consumindo a API via REST/JSON, sem lógica
de negócio duplicada — só apresentação e estado de sessão.

## Tecnologias

- **Backend**: .NET 8, ASP.NET Core, EF Core (Pomelo/MySQL), JWT + refresh token, BCrypt.
- **Mobile**: React Native, Expo, TypeScript, React Navigation, Reanimated.
- **Protocolo**: JFL (TCP proprietário de centrais de alarme) — tratado como infraestrutura,
  nunca como domínio de negócio.

## Filosofia Product First

O projeto deixou de ser "só um sistema" — é um produto. Toda entrega precisa parecer
profissional, transmitir confiança e ser simples de usar, não só "funcionar". Isso é avaliado a
cada Sprint pelos pilares Arquitetura / Segurança / Produto / UX-UI / Performance /
Acessibilidade / Manutenção / Documentação. O detalhamento de como cada domínio aplica isso vive
nos agentes e regras — aqui fica só o princípio.

## Component Driven Development

Nenhuma tela cresce como arquivo único. Composição sempre em camadas — Screen → Section → Card →
Component → Primitive — e um Design System único (tokens) como fonte de verdade visual. Antes de
criar algo novo, perguntar se é reutilizável; se for, vira componente compartilhado.

## Como selecionar agentes

Os agentes vivem em `.agents/`. Comece por `.agents/README.md` — a tabela de uma linha por
agente é suficiente para decidir quais carregar sem abrir os 15 arquivos completos. Carregar
**só** os agentes cujos domínios são tocados pela tarefa (ex.: uma mudança no Dashboard mobile
carrega `mobile.md` + `design-system.md` + `ux.md`, não o time inteiro). Nunca carregar todos os
agentes de uma vez — isso infla contexto sem melhorar qualidade.

## Como selecionar regras

As regras vivem em `.rules/`. Comece por `.rules/README.md` pelo mesmo motivo — lookup rápido
antes de abrir o arquivo completo. Mesmo critério dos agentes: carregar só as regras dos domínios
afetados pela tarefa atual, combinando com os agentes já selecionados. Regras não substituem
agentes nem vice-versa — agentes definem *quem decide*, regras definem *o que não pode ser
violado*.

## Regra de Validação em Dispositivo (Obrigatória, desde a Sprint 20)

Toda Sprint que alterar o aplicativo mobile (interface, navegação, autenticação, integrações,
tempo real, notificações, formulários ou qualquer fluxo do usuário) só é considerada entregue após
5 etapas: **Implementação → Testes Automatizados (`dotnet test`/`jest`/`typecheck`/`lint`/
`expo-doctor`) → APK Preview (EAS, `--profile preview`) → Homologação Manual em dispositivo físico
Android (link do build + Build ID + checklist específico da Sprint + regressão obrigatória:
login/logout/troca de propriedade/Dashboard/Acessos/Push/SignalR) → Reviewer (todos os pilares)**.
Resultado da homologação (Aprovada/Reprovada, com severidade dos bugs) e o histórico ficam em
`docs/testing/SprintXX.md`. Bug **Blocker** impede encerrar a Sprint; **High** só encerra com aceite
explícito do usuário; **Medium**/**Low** viram backlog e não bloqueiam. Exceção: dispensável só em
Sprints exclusivamente de backend/documentação/infraestrutura que não alterem o comportamento do
app mobile.

## Fluxo oficial de desenvolvimento

1. Identificar a tarefa.
2. Identificar os domínios afetados por ela.
3. Carregar apenas os agentes necessários (`.agents/`).
4. Carregar apenas as regras necessárias (`.rules/`).
5. Consultar decisões relacionadas (`docs/adr/`).
6. Executar.
7. Solicitar revisão ao agente Reviewer (`.agents/reviewer.md`).

Pular etapas ou carregar contexto além do necessário é o principal risco a evitar — o objetivo é
**reduzir contexto e aumentar qualidade**, não o contrário.
