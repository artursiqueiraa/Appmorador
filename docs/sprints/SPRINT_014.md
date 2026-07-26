# Sprint 14 — Tempo Real (SignalR)

## Missão

Sprint de infraestrutura de comunicação em tempo real — não integra novos fabricantes, não altera
o domínio, não altera protocolos existentes. Reutiliza integralmente a Camada Operacional (Sprint
13, ADR 0016) para notificar automaticamente Dashboard/Mobile quando algo operacional mudar, sem
depender de atualização manual. SignalR é exclusivamente uma camada de transporte — nenhuma regra
de negócio pode existir dentro do Hub, nenhum Provider pode conhecer SignalR.

## Fase 1 — Descoberta (achados antes de qualquer código)

1. **`MonitoramentoService` não existe** com esse nome — quem cumpre esse papel é
   `SnapshotOperacionalServico` (Sprint 13). Tratado como o mesmo conceito.
2. **"Sprint 13A"/"Sprint 13B"** citadas na missão não existem nos registros — só uma "Sprint 13:
   Camada Operacional Unificada" (`docs/sprints/SPRINT_013.md`, ADR 0016).
3. **Não existe RBAC/Perfil de usuário no domínio** (dívida técnica item 6) — cada Propriedade
   tem um único dono. A missão pedia grupos "por perfil do usuário" — **decisão confirmada com o
   usuário**: implementar só grupos por Propriedade (real); grupos por Perfil ficam registrados
   como dívida técnica (item 25), dependentes de uma futura Sprint de RBAC.
4. **Único gatilho verdadeiramente assíncrono**: `AlarmEventProcessor` (JFL, evento disparado por
   uma central via TCP, fora de qualquer requisição HTTP). Todo o resto (Control iD, comandos
   JFL) já é síncrono, dentro de uma requisição HTTP autenticada.

Ver ADR 0017 para o relatório completo (fluxo atual/proposto, eventos publicados, estratégia de
grupos, estratégia de testes).

## Escopo

1. `OperacionalHub` — Hub exclusivo da Camada Operacional; nunca consulta Providers, nunca
   executa regra de negócio (única exceção: a mesma checagem de posse que todo Controller já faz,
   necessária para decidir se a conexão entra no grupo da Propriedade).
2. Eventos publicados: `OperacionalAtualizado` (Snapshot completo + motivo) e
   `NovoEventoOperacional` (evento novo da Central de Eventos) — sempre publicados pelo domínio,
   nunca diretamente pelos Providers.
3. Grupos por Propriedade (`propriedade:{id}`) — grupos por Perfil ficam de fora (achado 3).
4. Integração com `SnapshotOperacionalServico` (o "MonitoramentoService" da missão).
5. Dashboard atualiza automaticamente (Saúde, Equipamentos Online/Offline, Alarmes Ativos,
   Última Atualização) — sem substituir nenhum componente existente.
6. Mobile com cliente SignalR: conexão automática, reconexão automática
   (`withAutomaticReconnect`), atualização automática de Dashboard/Central Operacional/Saúde da
   Propriedade/Timeline. Refresh manual continua disponível como fallback.
7. Segurança: JWT (via querystring só nas rotas `/hubs`), autorização por Propriedade (ownership
   check ao entrar no grupo), rate limiting no negotiate/conexão, encerramento de conexões sem
   posse (`HubException`).
8. Performance: publicação assíncrona, broadcast por grupo (nunca global), debounce simples em
   memória (750ms por Propriedade+Motivo), sem consulta ao banco durante a publicação em si (só a
   geração do Snapshot, que já era pura agregação desde a Sprint 13).
9. Observabilidade: conexões/desconexões, entrada/saída de grupo, falhas de autorização e de
   entrega — tudo via `ILogger` já existente, sem sistema novo.

## Fora de Escopo

Push Notification, Firebase, APNs, IA, Analytics, novos fabricantes, novos comandos, WebRTC,
streaming de vídeo.

## Processo Obrigatório

Implementado em etapas pequenas: porta `IOperacionalEventoPublicador` + `RegenerarEPublicarAsync`
→ build → `OperacionalHub`/`OperacionalHubPublicador` + wiring no `Program.cs` → build →
integração nos 3 pontos de mutação (Control iD, JFL, AlarmEventProcessor) → build → validação real
via um cliente SignalR (`@microsoft/signalr`, o mesmo usado depois no mobile) contra o simulador
JFL real → mobile (`RealtimeProvider` + 4 telas) → build + validação → documentação.

## Critérios de Aceite

Backend compilando; Mobile compilando; SignalR funcional (validado com um cliente real); Dashboard
atualizando automaticamente; Timeline atualizando automaticamente; Snapshot Operacional publicado
automaticamente; Saúde da Propriedade atualizada automaticamente; reconexão automática
funcionando; grupos por Propriedade funcionando (isolamento validado); JWT funcionando; sem
regressões em Control iD/JFL/Camada Operacional; ADR 0017 criada; CHANGELOG/ROADMAP/DIVIDA_TECNICA
atualizados; Reviewer aprovando todos os 8 pilares.

## Diretriz de Engenharia

SignalR não pertence ao domínio — é exclusivamente transporte. O domínio continua funcionando
integralmente se o SignalR for removido (`IOperacionalEventoPublicador` sem implementação real
vira um no-op). Toda atualização operacional segue o fluxo: Provider → mutação já existente →
`SnapshotOperacionalServico.RegenerarEPublicarAsync` → `IOperacionalEventoPublicador` →
`OperacionalHubPublicador` → SignalR → Dashboard/Mobile. Nenhuma regra de negócio existe dentro do
Hub.
