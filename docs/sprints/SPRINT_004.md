# Sprint 4 — Dashboard Operacional Inteligente

## Missão

Execute a Sprint 4 do AppMorador seguindo integralmente o processo definido em CLAUDE.md.

Antes de implementar:

- Leia CLAUDE.md.
- Revise ROADMAP.md.
- Revise as ADRs.
- Revise a Sprint 3.1 e a documentação da versão v0.3.0-alpha.
- Carregue apenas os agentes necessários.

Não implemente nada fora do escopo desta Sprint.

## Objetivo

Transformar o Dashboard na principal central operacional do AppMorador para residências,
condomínios e pequenos comércios.

Ao abrir o aplicativo, o usuário deve visualizar rapidamente o estado geral da propriedade.

O foco é entregar valor imediato ao usuário, preservando toda a arquitetura consolidada nas
Sprints anteriores.

## Escopo

Implementar:

- Dashboard operacional.
- Cards de status da propriedade.
- Timeline de eventos recentes.
- Atalhos rápidos.
- Estado geral da propriedade.
- Layout responsivo reutilizando o Design System existente.

Sempre reutilizar serviços e componentes existentes.

### Cards

O Dashboard deverá suportar indicadores como:

- 📹 Câmeras
- 🚪 Portões
- 🔐 Controle de acesso
- 🚨 Alarmes
- 📦 Entregas
- 👥 Visitantes
- 🔔 Eventos recentes

Os indicadores devem utilizar dados reais quando disponíveis e apresentar estados vazios quando
ainda não existirem integrações.

## Arquitetura

Respeitar integralmente a Clean Architecture.

- Não duplicar regras de negócio.
- Não alterar contratos existentes.
- Não antecipar funcionalidades de outras Sprints.
- Quando necessário, preparar extensões futuras sem implementá-las.

## Qualidade

Após cada etapa:

- Build Backend.
- Build Mobile.
- Validar ausência de regressões.
- Atualizar documentação apenas se houver mudanças relevantes.

## Fora de Escopo

Não implementar:

- Push Notifications
- SignalR
- WebSockets
- MQTT
- Integrações reais com hardware
- Analytics
- Controle de acesso
- Visitantes
- Entregas

Registrar qualquer necessidade como backlog.

## Entrega

Ao finalizar apresentar:

- Resumo executivo.
- Arquivos modificados.
- Fluxos homologados.
- Evidências dos testes.
- Pendências.
- Atualizações de documentação.
- Parecer do Reviewer.

Somente considere a Sprint concluída após aprovação do Reviewer e validação completa dos
critérios de aceite.

## Processo de execução (definido pelo usuário nesta mesma conversa)

A Sprint 4 segue duas fases obrigatórias:

1. **Fase 1 — Planejamento**: ler CLAUDE.md, analisar a arquitetura existente, identificar
   módulos impactados, listar arquivos a criar/modificar, identificar riscos de regressão,
   verificar necessidade de ADR, definir plano de execução em etapas pequenas. **Não escrever
   código nesta fase.** Entregar: resumo da Sprint, estratégia de implementação, arquitetura
   proposta, diagrama de componentes, lista de arquivos novos, lista de arquivos modificados,
   ordem das implementações, riscos identificados, estratégia de rollback, estratégia de testes,
   critérios de aceite revisados. Aguardar aprovação explícita do usuário.
2. **Fase 2 — Implementação**: só começa após o usuário responder "Aprovado. Inicie a Fase 2 —
   Implementação." Executar uma etapa por vez, com build + testes + validação antes de avançar
   para a próxima etapa — nunca implementar a Sprint inteira de uma vez.
