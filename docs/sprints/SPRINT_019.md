# Sprint 19 — Notificações Push (Push Notifications)

## Missão

Complementar a experiência em tempo real da Sprint 18 (SignalR, app aberto) com notificações push
(app fechado/em segundo plano) — sem acoplar nenhuma regra de negócio a um provedor específico
(Firebase hoje, outro amanhã).

## Fase 0 — Auditoria (verificada por leitura direta de código, não pela tabela assumida pela missão)

| Evento | SignalR (app aberto) | Push (app fechado) antes desta Sprint | Achado real |
|---|---|---|---|
| Alarme disparado | ✅ | ❌ | Ponto de disparo único e claro (`AlarmEventProcessor`) |
| Portão aberto (PGM) | ✅ | ❌ | Backend só sabe "PGM N acionado" — rótulo "portão" é preferência só do Mobile |
| Sistema armado | ✅ | ❌ | Ponto de disparo claro (`JflComandoServico.ArmarAsync`) |
| Sistema desarmado | ✅ | ❌ | Ponto de disparo claro (`JflComandoServico.DesarmarAsync`) |
| Visitante autorizado | ❌ | ❌ | `AutorizacaoServico` nunca publicava evento nenhum |
| Entrega recebida | ❌ | ❌ | `EntregaServico` nunca publicava evento nenhum |
| Equipamento offline | ✅ (via Snapshot) | ❌ | Transição de status sem registro auditável (dívida técnica já existente, Sprint 13 item 24) |
| Equipamento online | ✅ (via Snapshot) | N/A | Nunca deve notificar (Regra de Ouro da missão, confirmada) |

## Escopo entregue

1. **`DispositivoPush`** (Domínio) — multi-dispositivo por usuário, preferências de canal por
   dispositivo (Alertas/Atividades/Geral), token em texto puro (precisa ser reenviado ao FCM).
2. **`INotificationProvider`/`FirebaseNotificationProvider`** — porta abstrata (mesmo padrão de
   `IJflProvider`), implementação Firebase com modo "sem-op documentado" quando não há credenciais
   reais configuradas (decisão do usuário, sem acesso ao Firebase Console nesta sessão).
3. **`NotificationDispatcher`/`NotificationService`** — decide/monta mensagem (tabela fixa da
   missão) e resolve destinatários via ownership (`Propriedade → ProprietarioId → dispositivos`),
   com debounce de 60s em memória por `(tipo, equipamento ou propriedade)`.
4. **Hooks reais de disparo** — `AlarmEventProcessor` (alarme), `JflComandoServico` (armar/
   desarmar/PGM + transição para offline), `AutorizacaoServico` (visitante autorizado, novo),
   `EntregaServico` (entrega recebida, novo).
5. **`DispositivoPushServico`/`DispositivosPushController`** — registrar (upsert por token, nunca
   duplica), atualizar token, atualizar preferências, desativar (idempotente, rota por `id`).
6. **Mobile**: `expo-notifications`, token nativo FCM (`getDevicePushTokenAsync`, não o proxy do
   Expo — precisa bater com o provider do backend), 3 canais Android (Fase 9), supressão da
   notificação de sistema com app em primeiro plano, deep link (`acao` → tela/aba) com retry de
   prontidão do `NavigationContainer`, tela Ajustes → Notificações.
7. **Ciclo de vida do token**: registro no login (permissão só solicitada uma vez), refresh via
   `addPushTokenListener`, desregistro no logout via `registerBeforeLogoutHook` (roda antes da
   sessão ser limpa — mesmo padrão de `registerSessionExpiredHandler`).
8. **Testes automatizados** — primeiro projeto de testes do backend (`AppMorador.Tests`, xUnit +
   Moq, 27 testes); 22 novos testes no Mobile (`pushService`, deep link).

## Achado arquitetural verificado antes de desenhar os hooks

`MotivoAtualizacaoOperacional` (pipeline genérico de eventos operacionais) só tem 3 valores amplos
— insuficiente para distinguir a maioria dos 8 eventos da missão. Em vez de forçar esse pipeline a
carregar detalhe que ele nunca teve, `INotificationDispatcher.NotificarAsync` é chamado
diretamente pelos Application Services, no ponto exato em que cada ação semântica específica já é
conhecida — ver ADR 0023 para o racional completo.

## Fora de escopo confirmado

Rich notifications (imagem/botão de ação), notificação por e-mail/SMS, notificação para síndico/
administrador, notificações agendadas/marketing, web push, agrupamento (Fase 8.1), iOS (sem
dispositivo físico/certificado APNs nesta sessão) — todos registrados em `DIVIDA_TECNICA.md`.
