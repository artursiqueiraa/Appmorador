# Sprint 8 — Visitantes e Autorizações

## Missão

Execute esta Sprint seguindo integralmente o fluxo definido em `CLAUDE.md`. Antes de iniciar:
ler `CLAUDE.md`, revisar `ROADMAP.md`, revisar as ADRs, revisar a Sprint 7, carregar apenas os
agentes necessários. Não implementar funcionalidades fora deste escopo.

## Objetivo

Construir o domínio completo de Visitantes e Autorizações. Esta Sprint NÃO realizará integração
com equipamentos físicos. O objetivo é preparar toda a estrutura de negócio responsável pelo
gerenciamento de visitantes. Ao final desta Sprint o AppMorador deverá permitir controlar quem
pode visitar uma propriedade, quando poderá entrar e quem autorizou o acesso.

## Escopo

1. **Visitantes** — Nome, Documento, Telefone, Foto (estrutura preparada), Observações. Preparar
   o domínio para reconhecimento facial futuro. Não implementar captura facial nesta Sprint.
2. **Autorizações** — Morador responsável, Unidade, Visitante, Data inicial, Data final, Horário
   inicial, Horário final, Status. Status: Pendente, Ativa, Expirada, Cancelada, Utilizada.
3. **Tipos de Visita** — Visitante, Prestador de Serviço, Entregador, Evento, Temporário. Preparar
   para expansão futura.
4. **Histórico** — registrar automaticamente: Autorização criada/alterada/cancelada/expirada,
   Visitante removido. Preparar apenas o domínio, sem auditoria visual.
5. **Dashboard** — indicadores com dados reais: Visitantes ativos, Autorizações pendentes,
   Autorizações expiradas. Reutilizar componentes existentes.
6. **Mobile** — telas: Visitantes, Autorizações. Seguir integralmente o Design System existente.
   Nenhum redesign.
7. **UX** — Loading, Empty State, Validação, Feedback de sucesso, Feedback de erro, Confirmação
   antes de cancelar autorizações.
8. **Arquitetura** — Clean Architecture, ADRs existentes, Design System, estrutura atual. Não
   criar soluções paralelas, não duplicar regras de negócio.

## Exclusão

Seguir integralmente o padrão da Sprint 6. Soft Delete. Nunca excluir fisicamente: Visitantes,
Autorizações. Registrar: Excluido, DataExclusaoUtc, ExcluidoPorUsuarioId.

## Fora de Escopo

Não implementar: QR Code funcional, Reconhecimento facial, Liberação automática, Integração
Control iD, Integração Intelbras, Integração Hikvision, Integração JFL, Push Notification,
WebSocket, Analytics, IA. Registrar qualquer necessidade como backlog.

## Processo Obrigatório

Executar a Sprint por etapas. Após cada etapa: Build Backend, Build Mobile, Validar regressões,
Atualizar documentação quando necessário. Somente avançar quando a etapa anterior estiver
completamente validada.

## Critérios de Aceite

Backend compilando; Mobile compilando; CRUD de Visitantes funcional; CRUD de Autorizações
funcional; Dashboard utilizando dados reais; Soft Delete funcionando; Histórico funcionando; Sem
regressões; Documentação atualizada; Reviewer aprovando todos os pilares.

## Diretriz de Produto

Esta Sprint deverá preparar completamente o domínio de Visitantes. Ao final deverá ser possível
executar o seguinte fluxo: Criar uma propriedade → Selecionar uma unidade → Selecionar um morador
responsável → Cadastrar um visitante → Criar uma autorização → Definir período de validade →
Visualizar visitantes autorizados no Dashboard → Consultar o histórico da autorização. Sem
qualquer comunicação com equipamentos físicos. Toda a arquitetura deverá permanecer preparada
para futuras integrações com Control iD, Intelbras, Hikvision e JFL. O objetivo desta Sprint é
construir o domínio completo de Visitantes e Autorizações, permitindo que futuras integrações
apenas consumam essas regras já consolidadas.

## Decisões tomadas na Fase 1 (aprovação do usuário antes da implementação)

1. **Visitante pertence direto à Propriedade** (não à Unidade) — reaproveitável entre
   autorizações de unidades diferentes, mesmo padrão de `PontoAcesso` (ADR 0010). A `Autorizacao`
   já amarra Unidade + Morador responsável, então o vínculo específico por visita não se perde.
   Ver ADR 0011.
2. **Status da Autorização é híbrido** — Pendente/Ativa/Expirada são computados a partir de
   `DataInicial`/`DataFinal`/`HorarioInicial`/`HorarioFinal` em tempo de leitura, nunca exigindo
   job/scheduler (regra permanente de simplicidade em MVPs); Cancelada/Utilizada são ações
   manuais explícitas do usuário, que sempre vencem o cálculo por data. Ver ADR 0011.
