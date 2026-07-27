# Sprint 18 — Experiência em Tempo Real (Realtime Experience)

## Missão

Aproveitar a infraestrutura já existente (SignalR, Snapshot Operacional, Timeline, Providers) para
transformar o AppMorador num aplicativo que responde imediatamente a eventos importantes — sem
refresh manual, sem recarregar a tela inteira, sem alterar arquitetura, domínio ou contratos de
API.

## Fase 0 — Auditoria (verificada por leitura direta de código)

| Tela | SignalR | Polling | Status |
|---|---|---|---|
| Dashboard (HeroCard) | ✅ (patch de campos in-place) | ❌ | OK — mantido, reforçado com pulso de atualização |
| Timeline / Central de Eventos | ⚠️ (só disparava refetch completo da página 1) | Efetivamente sim | Melhorado — inserção real, scroll preservado |
| Painel de Controle (Acessos) | ❌ | ❌ | Implementado — máquina de estados + status ao vivo |
| Ajustes/Câmeras | ❌ | ❌ | Confirmado fora de escopo |

## Escopo entregue

1. **RealtimeContext dividido em 3** (`useRealtimeConexao`/`useRealtimeSnapshot`/`useRealtimeEvento`)
   — Regra 5 (Atualização Parcial Explícita), sem biblioteca nova.
2. **Hero em Tempo Real**: pulso de atualização sutil (≤300ms), `HeroCard`/`QuickAction`/
   `ActivityCard` memoizados.
3. **Timeline Realtime**: inserção real no topo (Fade+Slide ≤300ms), selo "Novo" (5s), scroll
   preservado (Regra 2), banner "N novos eventos · Ver novos", cache máximo 50 eventos (Regra 4).
4. **Toasts Inteligentes**: `Toast.tsx` generalizado (erro/sucesso/info/alerta, fila máx. 10),
   `RealtimeToastBridge` decide com base na tela em foco (`navigationRef` + `telaAtivaStore`) —
   nunca mostra toast quando o evento já é visível (Início/Eventos).
5. **Indicador de Conexão**: `IndicadorConexaoRealtime`, silencioso quando tudo está bem, discreto
   em reconexão/sem-comunicação.
6. **Reconexão com backoff exponencial** (1s/2s/5s/10s/30s, 5 tentativas) via `IRetryPolicy`
   customizada + botão manual "Tentar novamente" que reseta o ciclo.
7. **Painel de Controle em Tempo Real**: máquina de estados (Normal/Enviando/Sucesso/Falha,
   timeout 10s), status Online/Offline por equipamento via Snapshot (patch parcial).
8. **Pull-to-Refresh**: reconciliação completa em Eventos (já existia, estendida para limpar
   badges); adicionado em Acessos (não existia antes desta Sprint).
9. **Troca de Propriedade**: cache do `RealtimeContext` e das telas (`HomeScreen`/`AccessScreen`)
   descartado imediatamente ao trocar de propriedade — achado real durante a implementação (o
   `setLoading(true)` sozinho não limpava o conteúdo já renderizado).
10. **Telemetria de desenvolvimento** (`services/telemetria.ts`, `console.info` só em `__DEV__`).

## Achado arquitetural verificado antes de desenhar a Fase 6

`JflComandoServico.ExecutarComandoAsync` é síncrono — o mesmo round-trip HTTP envia o comando e
devolve o resultado; não existe um canal separado de "confirmação assíncrona via SignalR". Os 2
estados intermediários da missão original ("Aguardando Confirmação"/"Executando") foram
conscientemente colapsados num único estado `enviando`, resolvido pela mesma resposta HTTP — ver
ADR 0022 Decisão 10 para o racional completo.

## Fora de Escopo (confirmado, nada alterado)

Push Notification, replay de vídeo, câmeras ao vivo, interfone, backend novo, alterações de
domínio, novos endpoints, analytics, novos fabricantes, novos comandos de equipamento. Confirmado
por `dotnet build` limpo sem tocar nenhum arquivo de `backend/` durante toda a Sprint.

## Processo

Executado em etapas pequenas, cada uma seguida de `npm run typecheck`/`lint`: RealtimeContext
(3 contexts + backoff + telemetria) → Indicador de Conexão → Hero (pulso + memo) → Timeline
(inserção real) → Toast genérico + bridge de tela ativa → Painel de Controle (máquina de estados)
→ Pull-to-Refresh (Acessos) → Troca de Propriedade (cache das telas) → `dotnet build` +
`expo-doctor` final → documentação.

## Critérios de Aceite

Todos atendidos — ver relatório de entrega (`docs/reviews/SPRINT_018.md`) para evidências
detalhadas e parecer do Reviewer nos 9 pilares desta Sprint.
