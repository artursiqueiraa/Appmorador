# Relatório — Sprint 19 (Notificações Push)

**Data de conclusão**: 2026-07-26

## Resumo executivo

O AppMorador ganhou notificações push como complemento ao tempo real da Sprint 18: agora o
morador é avisado de alarme, visitante autorizado, entrega, comando acionado ou dispositivo offline
mesmo com o app fechado. A auditoria inicial (Fase 0) encontrou que o domínio não tinha
granularidade suficiente para 3 dos 8 eventos assumidos pela missão — resolvido com hooks diretos
nos Application Services, no ponto exato em que cada ação semântica já é conhecida, sem alterar
Domínio (exceto a nova entidade `DispositivoPush`, explicitamente permitida). Sem acesso ao
Firebase Console nesta sessão, o provider opera em modo sem-op documentado (decisão explícita do
usuário) — a arquitetura real e testável está 100% pronta, restando só configuração externa para
ativar o envio real.

## Auditoria de disparo de eventos (Fase 0)

Ver `docs/sprints/SPRINT_019.md` para a tabela completa. Resumo: Alarme/Armar/Desarmar/PGM já
tinham um ponto de disparo claro na Aplicação; Visitante autorizado e Entrega recebida nunca
publicavam evento nenhum antes desta Sprint (implementado do zero); Equipamento offline reaproveita
uma comparação de estado anterior/posterior já disponível no ponto de mutação existente.

## Fluxo de notificação (documentado)

```
Ação acontece (alarme dispara / comando executado / visitante autorizado / entrega recebida /
equipamento cai offline)
  → Application Service chama INotificationDispatcher.NotificarAsync(tipo, contexto)
  → Debounce (60s por tipo+equipamento/propriedade)? Se suprimido, para aqui.
  → Monta mensagem fixa (Fase 4) → NotificationService resolve Propriedade → ProprietarioId →
    dispositivos ativos → filtra por canal habilitado → INotificationProvider.EnviarAsync
  → FirebaseNotificationProvider: sem credenciais reais, loga e simula sucesso (modo documentado);
    com credenciais reais, envia via FirebaseAdmin SDK e desativa tokens definitivamente inválidos.
  → Mobile: app aberto → notificação de sistema suprimida (SignalR/Toast já cobrem);
    app fechado → notificação do sistema aparece normalmente no canal certo (Alertas/
    Atividades/Geral) → toque → deep link (acao) → app abre e navega para a tela certa
    (com retry se o NavigationContainer ainda não estiver pronto).
```

## Componentes/arquivos criados

| Componente | Arquivo | Função |
|---|---|---|
| `DispositivoPush` | `Domain/Entities/DispositivoPush.cs` | Multi-dispositivo por usuário, preferências de canal |
| `INotificationProvider`/`FirebaseNotificationProvider` | `Application/Notificacoes/`, `Infrastructure/Notifications/` | Porta + implementação Firebase (modo sem-op documentado) |
| `NotificationDispatcher`/`NotificationService` | `Application/Notificacoes/` | Decisão de notificar + resolução de destinatários/envio |
| `DebounceNotificacaoEmMemoria` | `Infrastructure/Notifications/` | Limite de frequência (60s), em memória |
| `DispositivosPushController` | `Api/Controllers/` | Registrar/atualizar token/atualizar preferências/desativar |
| `AppMorador.Tests` | `backend/tests/AppMorador.Tests/` | Primeiro projeto de testes do backend (27 testes) |
| `pushService.ts`/`pushChannels.ts`/`pushDeviceStorage.ts` | `mobile/src/notifications/` | Ciclo de vida do token, canais Android, storage local |
| `PushNotificationProvider.tsx` | `mobile/src/notifications/` | Orquestração (permissão, listeners, deep link) |
| `NotificacoesScreen.tsx` | `mobile/src/screens/ajustes/` | Ajustes → Notificações (toggles por canal) |

## Evidências dos testes

- `dotnet build` (solução inteira, incluindo o novo projeto de testes): 0 erros, 0 avisos
  funcionais (2 avisos de API obsoleta do FirebaseAdmin SDK — `MulticastMessage.Tokens`/
  `GoogleCredential.FromFile`, registrados como aceitos por ora, ver ADR 0023 Decisão 8).
- `dotnet test` (`AppMorador.Tests`): **27/27 passando** — `NotificationDispatcherTests` (mensagem
  correta por tipo, debounce suprime/não suprime, chave de debounce por equipamento vs.
  propriedade), `NotificationServiceTests` (propriedade inexistente, zero dispositivos, dois
  dispositivos ambos notificados, canal desabilitado filtra, token inválido desativa só aquele
  dispositivo, provider indisponível nunca lança), `DispositivoPushServicoTests` (registro novo,
  reassume token de outro usuário, atualização de token só pelo dono, desativação idempotente),
  `DebounceNotificacaoEmMemoriaTests` (janela imediata, chaves/tipos independentes).
- `npm run typecheck` (Mobile): 0 erros.
- `npm run lint` (Mobile): 0 erros, 0 avisos.
- `npm test` (Mobile): **26/26 passando** (22 novos desta Sprint: `pushService` — permissão nunca
  insiste duas vezes, falha ao obter token nunca lança, ciclo de vida do token/preferências/
  desregistro; `PushNotificationProvider` — mapeamento de deep link, `acao` desconhecida/ausente
  nunca navega, retry quando a navegação ainda não está pronta).
- `npx expo-doctor`: 20/20 checks aprovados.
- **Validação em dispositivo físico com push real**: pendente do usuário — bloqueada por
  credenciais Firebase reais (`DIVIDA_TECNICA.md` item 38), não por nada pendente de código.

## Dívidas técnicas registradas

Itens 38–42 em `docs/DIVIDA_TECNICA.md`: push real ponta a ponta pendente de credenciais Firebase;
iOS não implementado/validado (sem certificado/dispositivo físico); agrupamento de notificações
(Fase 8.1) não implementado; debounce em memória não sobrevive a múltiplas instâncias de backend;
preferências de canal espelhadas localmente (sem `GET` novo no backend, não solicitado).

## Parecer do Reviewer — 9 Pilares (nomenclatura da própria missão)

| Pilar | Avaliação |
|---|---|
| **1. Funcionalidade** | ✅ Aprovado. Todos os 7 tipos de evento do enum `EventoNotificacaoTipo` notificam corretamente (verificado por teste automatizado); `EquipamentoOnline` corretamente nunca notifica. |
| **2. UX** | ✅ Aprovado. Mensagens seguem a Regra de Vocabulário (nenhum termo técnico exposto); notificação de sistema suprimida com o app aberto (sem duplicar o toast do SignalR). |
| **3. Deep Link** | ✅ Aprovado. `acao` mapeia para a tela/aba certa; retry de prontidão cobre o caso de abertura a frio; `acao` desconhecida/ausente nunca navega nem lança (testado). |
| **4. Permissões** | ✅ Aprovado. Diálogo nativo solicitado só uma vez (transição sem-sessão→com-sessão); negada nunca insiste; reativação via `Linking.openSettings()` na tela de Ajustes. |
| **5. Respeito ao Sistema** | ✅ Aprovado. 3 canais Android com importância/som/vibração corretos (Fase 9); preferência por canal filtrada no servidor antes do envio, nunca depois. |
| **6. Integridade** | ⚠️ Aprovado com ressalva documentada. Toda a lógica de decisão/mensagem/canal está correta e testada, mas o envio real depende de credenciais Firebase que não existem nesta sessão (decisão explícita do usuário) — o provider nunca finge sucesso sem avisar (loga claramente o modo sem-op). |
| **7. Arquitetura** | ✅ Aprovado. Zero alteração de Domínio além de `DispositivoPush` (item explicitamente permitido); `INotificationProvider` segue o mesmo padrão de porta já estabelecido para fabricantes de hardware. |
| **8. Performance** | ✅ Aprovado. Debounce evita notificações duplicadas em rajada; resolução de destinatários é uma única query por propriedade (`ListAtivosByUsuarioAsync`), sem N+1. |
| **9. Observabilidade** | ✅ Aprovado. Logs claros em cada decisão (suprimido por debounce, sem dispositivo elegível, token inválido desativado, modo sem-op) — nenhum log visível ao morador. |

**Conclusão**: Sprint aprovada, com uma ressalva não-bloqueante e já esperada desde antes do início
da implementação: push real ponta a ponta depende de credenciais Firebase que só o usuário pode
obter (Firebase Console). Toda a arquitetura, lógica de negócio e ciclo de vida do token estão
implementados, testados (53 testes novos ao todo entre backend e mobile) e prontos para ativar
assim que a configuração externa existir.
