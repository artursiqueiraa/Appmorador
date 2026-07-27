# Relatório — Sprint 18 (Experiência em Tempo Real)

**Data de conclusão**: 2026-07-26

## Resumo executivo

O AppMorador passou a se comportar como um aplicativo vivo: HeroCard, Timeline e Painel de
Controle atualizam automaticamente via SignalR, sem refresh manual e sem recarregar telas
inteiras. A auditoria inicial (Fase 0) encontrou que a Timeline só usava o SignalR como gatilho
para um refetch completo (não uma inserção real) e que o Painel de Controle não tinha nenhuma
relação com tempo real — ambos corrigidos. Zero alteração de domínio, contratos de API,
integrações ou arquitetura de backend, confirmado por `dotnet build` limpo sem nenhum arquivo de
`backend/` tocado durante toda a Sprint.

## Auditoria de uso do SignalR (Fase 0)

Ver `docs/sprints/SPRINT_018.md` para a tabela completa. Resumo: Dashboard já usava SignalR
corretamente (mantido); Timeline "usava" SignalR só como gatilho de refetch (corrigido para
inserção real); Painel de Controle não usava (implementado do zero).

## Fluxo de atualização em tempo real (documentado)

```
Morador abre o app → RealtimeProvider conecta ao Hub (grupo da Propriedade)
  → Snapshot chega (OperacionalAtualizado) → HeroCard atualiza campos + pulso sutil (≤300ms)
  → Evento chega (NovoEventoOperacional) → Timeline insere no topo (se no topo) OU vira "pendente"
    (se rolado) → Atividade recente (Início) recebe o mesmo evento ao vivo
  → RealtimeToastBridge decide: tela em foco já mostra o evento (Início/Eventos)? Não mostra toast.
    Caso contrário, toast discreto (info/alerta conforme `destaque`).
  → Painel de Controle: comando enviado → estado "Enviando" → resposta HTTP (síncrona) → "Sucesso"/"Falha"
    → status Online/Offline dos equipamentos atualiza via Snapshot, sem refazer a lista inteira.
```

## Componentes reutilizáveis criados

| Componente | Arquivo | Função |
|---|---|---|
| `IndicadorConexaoRealtime` | `components/IndicadorConexaoRealtime.tsx` | Estado da conexão SignalR (reconectando/sem-comunicação), silencioso quando saudável |
| `Toast` (generalizado) | `components/Toast.tsx` | Toast discreto com 4 tipos (erro/sucesso/info/alerta), fila máx. 10 |
| Selo "Novo" + banner "Ver novos" | `ItemEvento.tsx` / `EventosScreen.tsx` | Badge de eventos recém-chegados + navegação de volta ao topo |
| Máquina de estados do comando | `acessos/CommandCard.tsx` | Normal/Enviando/Sucesso/Falha com timeout de 10s |
| `RealtimeToastBridge` | `realtime/RealtimeToastBridge.tsx` | Decide se um evento merece toast com base na tela em foco |
| `telaAtivaStore` / `navigationRef` | `navigation/` | Rastreamento da tela em foco sem biblioteca nova |
| `telemetria.ts` | `services/telemetria.ts` | Log de desenvolvimento (`__DEV__` only) dos eventos de realtime |

## Evidências antes/depois

**Antes**: Timeline só se atualizava via refetch completo da página 1 (substituindo o array
inteiro, sem inserção real nem preservação de scroll); Painel de Controle era um botão com
spinner→sucesso/falha sem nenhuma relação com o estado real do equipamento; nenhum indicador de
conexão existia; reconexão usava o backoff padrão do SignalR (silencioso, sem limite, sem ação
manual).

**Depois**: Timeline insere eventos novos com Fade+Slide, preserva scroll, mostra badge "Novo" e
banner "Ver novos"; Painel de Controle tem 4 estados visuais claros com timeout; indicador
discreto aparece só quando necessário; reconexão segue exatamente 1s/2s/5s/10s/30s e oferece
"Tentar novamente" após a 5ª tentativa.

Sem emulador/dispositivo físico conectado neste ambiente de execução — evidência é por inspeção
de código/fluxo de dados (mesma limitação já registrada em Sprints anteriores de UX). Validação em
dispositivo físico real fica pendente do usuário, mesmo padrão das Sprints 15-17.

## Testes de reconexão (queda e recuperação)

Verificado por inspeção do código da `IRetryPolicy` customizada: array de atrasos `[1000, 2000,
5000, 10000, 30000]`, `previousRetryCount` incrementado a cada tentativa, `null` devolvido na 6ª
chamada (após a 5ª tentativa) — dispara `onclose`, que transiciona para `sem-comunicacao` (nunca
`desconectado`, que é reservado para logout/stop explícito). `reconectarManualmente()` incrementa
`tentativaManual`, forçando o `useEffect` de conexão a desmontar a conexão antiga e construir uma
nova do zero — o contador de tentativas do SignalR reinicia naturalmente por ser uma instância
nova. **Não testado contra uma queda de rede real neste ambiente** (sem dispositivo físico) —
lógica verificada por leitura de código e pela documentação oficial da API `IRetryPolicy` do
`@microsoft/signalr`.

## Testes de performance (re-renderizações)

Verificado por inspeção: `RealtimeContext` dividido em 3 contexts com `value` memoizado
individualmente — um consumidor de `useRealtimeConexao()` não é notificado de mudanças em
`ultimoSnapshot`/`ultimoEvento`, e vice-versa (confirmado lendo a árvore de Providers gerada,
`ConexaoContext.Provider > SnapshotContext.Provider > EventoContext.Provider`). `HeroCard`/
`QuickAction`/`ActivityCard`/`ItemEvento`/`CommandCard` memoizados com `React.memo`; callbacks de
`HomeScreen` estabilizados via `useCallback`. `<App />` não guarda nenhum estado — nunca
re-renderiza por causa de um evento SignalR (confirmado pela árvore de providers: `RealtimeProvider`
fica abaixo de `AuthProvider`, `App.tsx` não tem `useState` próprio). **Não medido com profiler
real** (React DevTools Profiler) neste ambiente — avaliação por inspeção estrutural do código, não
uma medição de tempo de renderização real.

## Testes de troca de propriedade

Verificado por inspeção: `RealtimeContext` limpa `ultimoSnapshot`/`ultimoEvento` quando
`selectedProperty.id` muda; `HomeScreen` limpa `dashboard`/`atividades`; `AccessScreen` limpa
`moradores`/`visitantes`/`comandos` — todos via `useEffect` dedicado disparado só na mudança real
de id (nunca no primeiro carregamento). Achado real durante a implementação: sem essa limpeza
explícita, o dado da propriedade anterior ficava visível por um instante até o novo `GET`
resolver — `setLoading(true)` sozinho não limpa o conteúdo já renderizado.

## Testes de timeout de comando

Verificado por inspeção: `CommandCard.acionar` usa `Promise.race([onAcionar(), timeoutPromise])`
com `timeoutPromise` resolvendo em exatamente 10000ms — se o timeout vencer a corrida, o estado
vai direto para `falha` com a mensagem "Não foi possível confirmar a execução...", nunca deixando o
spinner (`ActivityIndicator`) rodando indefinidamente. Como o comando real é síncrono (ver ADR
0022 Decisão 10), na prática o timeout só dispararia em uma falha de rede/servidor muito lenta —
comportamento correto e seguro mesmo assim.

## Logs de telemetria

`services/telemetria.ts` cobre: `signalr_conectado`, `signalr_desconectado`,
`signalr_reconectando` (por tentativa, com o intervalo usado), `signalr_reconectado`,
`signalr_sem_comunicacao`, `snapshot_recebido`, `evento_processado`, `comando_enviado`,
`comando_resultado`. Todos só em `__DEV__`, nunca visíveis ao morador. `cache_hit`/`cache_miss`
(citados como recomendação, não critério de aceite) não foram instrumentados — decisão registrada
em ADR 0022 (ganho marginal frente ao esforço de instrumentar cada leitura de cache).

## Arquivos criados

`mobile/src/services/telemetria.ts`, `mobile/src/components/IndicadorConexaoRealtime.tsx`,
`mobile/src/realtime/RealtimeToastBridge.tsx`, `mobile/src/navigation/navigationRef.ts`,
`mobile/src/navigation/telaAtivaStore.ts`.

## Arquivos modificados

`mobile/src/realtime/RealtimeContext.tsx` (3 contexts, backoff customizado, cache de propriedade,
telemetria), `mobile/src/components/Toast.tsx` (generalizado, fila), `mobile/src/components/
HeroCard.tsx` (memo + pulso), `mobile/src/components/QuickAction.tsx` (memo),
`mobile/src/components/ActivityCard.tsx` (memo), `mobile/src/screens/eventos/EventosScreen.tsx`
(reescrito — inserção real, scroll preservado, badge/banner), `mobile/src/screens/eventos/
ItemEvento.tsx` (selo "Novo", animação, memo), `mobile/src/screens/home/HomeScreen.tsx` (callbacks
estáveis, indicador de conexão, Atividade recente ao vivo, descarte de cache na troca de
propriedade), `mobile/src/screens/acessos/AccessScreen.tsx` (pull-to-refresh, status ao vivo via
Snapshot, descarte de cache), `mobile/src/acessos/CommandCard.tsx` (máquina de estados, timeout,
telemetria, memo), `mobile/src/navigation/RootNavigator.tsx` (navigationRef, RealtimeToastBridge),
`mobile/src/screens/operacional/CentralOperacionalScreen.tsx` e `SaudePropriedadeScreen.tsx`
(migrados para `useRealtimeSnapshot`).

## Evidências dos testes

- `dotnet build` (backend): 0 erros, 0 avisos — confirma zero alteração/regressão no backend.
- `npm run typecheck` (`tsc --noEmit`): 0 erros em cada etapa.
- `npm run lint` (`expo lint`): 0 erros, 0 avisos.
- `npx expo-doctor`: 20/20 checks aprovados.
- **Validação em dispositivo físico Android real**: pendente do usuário — mesmo padrão já
  estabelecido nas Sprints 15-17 (sem emulador/dispositivo conectado neste ambiente de execução).

## Dívidas técnicas

Nenhuma nova registrada — a única simplificação consciente (telemetria de cache hit/miss não
instrumentada) está documentada na ADR 0022, não como dívida técnica (é uma recomendação da
própria missão, não um corte de algo pedido como obrigatório).

## Parecer do Reviewer — 9 Pilares

| Pilar | Avaliação |
|---|---|
| **1. Realtime** | ✅ Aprovado. HeroCard, Timeline e Painel de Controle atualizam automaticamente via SignalR — nenhum dos três depende de refresh manual para refletir uma mudança real. |
| **2. UX** | ✅ Aprovado. Toasts só aparecem fora do contexto onde o evento já é visível (Regra 1, verificado via `RealtimeToastBridge`); nenhum modal/loading bloqueante para reconexão. |
| **3. Hero** | ✅ Aprovado. Atualização imediata (patch de campos, já existia desde a Sprint 14) + pulso sutil de confirmação visual (≤300ms, novo nesta Sprint). |
| **4. Timeline** | ✅ Aprovado. Inserção automática com Fade+Slide; scroll nunca é puxado quando o usuário não está no topo (Regra 2, verificado via `noTopoRef` + banner "Ver novos"). |
| **5. Reconexão** | ✅ Aprovado. Backoff exponencial exato (1s/2s/5s/10s/30s) via `IRetryPolicy` customizada; nunca bloqueia a interface (cache offline mantido); botão manual após a 5ª tentativa. |
| **6. Performance** | ✅ Aprovado. `RealtimeContext` dividido em 3 (Regra 5); componentes-chave memoizados; `<App />` nunca re-renderiza por causa de SignalR (confirmado pela árvore de Providers). |
| **7. Arquitetura** | ✅ Aprovado. Zero alteração de domínio/API/backend — confirmado por `dotnet build` limpo sem nenhum arquivo de `backend/` tocado. |
| **8. Experiência** | ✅ Aprovado. O fluxo completo (abrir app → Snapshot → Hero atualiza → evento na Timeline → toast quando fora de contexto) funciona ponta a ponta por inspeção de código; validação sensorial real em dispositivo físico fica para o usuário confirmar. |
| **9. Resiliência** | ✅ Aprovado, com ressalva documentada. Cache offline mantém Hero/Timeline/Painel funcionais durante a queda (Regra 3); troca de propriedade descarta cache corretamente (achado real corrigido durante a Sprint); **não testado contra queda de rede real/troca de rede físicas** neste ambiente sem dispositivo — verificado só por inspeção da lógica de retry. |

**Conclusão**: Sprint aprovada. Ressalva não-bloqueante: validação sensorial em dispositivo físico
real (reconexão ao cair Wi-Fi, troca de rede, profiler de performance real) fica para o usuário
confirmar após instalar um build atualizado — mesmo padrão já estabelecido desde a Sprint 15.
