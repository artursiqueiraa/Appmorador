# ADR 0022 — Experiência em Tempo Real (Sprint 18)

**Data**: 2026-07-26

## Contexto

O backend já tinha SignalR funcional desde a Sprint 14 (ADR 0017) e o Mobile já consumia
`ultimoSnapshot`/`ultimoEvento` desde então, mas de forma limitada: o HeroCard atualizava campos
in-place (bom), mas a Timeline só usava o sinal do SignalR como gatilho para um **refetch completo
da página 1** (efetivamente um polling disparado por evento, não uma inserção real), e o Painel de
Controle (Sprint 17) não tinha nenhuma relação com SignalR — era um POST síncrono simples. Esta
Sprint não adiciona nenhuma capacidade nova de backend — só aproveita melhor o que já existe
(SignalR, Snapshot Operacional, Timeline paginada) para o app "parecer vivo": toda atualização
relevante chega automaticamente, sem refresh manual, sem recarregar a tela inteira.

## Fase 0 — Auditoria (verificada por leitura direta de código)

| Tela | SignalR | Comportamento real | Ação desta Sprint |
|---|---|---|---|
| Dashboard (HeroCard) | ✅ | Já fazia patch de campos in-place a partir de `ultimoSnapshot` | Mantido + pulso de atualização (Fase 1) |
| Timeline / Central de Eventos | ⚠️ | SignalR só disparava um refetch completo da página 1 (substituía o array inteiro) | Reescrito para inserção real no topo, com badge/scroll preservado (Fase 2) |
| Painel de Controle (Acessos) | ❌ | POST síncrono puro, nenhuma relação com SignalR | Máquina de estados + status de equipamento ao vivo via Snapshot (Fase 6) |
| Ajustes/Câmeras | ❌ | Confirmado: fora de escopo (nenhuma funcionalidade em tempo real faz sentido aqui hoje) | Mantido, sem mudança |

## Decisão 1 — Contexto do SignalR dividido em 3 (Regra 5 — Atualização Parcial Explícita)

`RealtimeContext` (um único Context, um único `value` memoizado com os 3 campos) fazia **todo**
consumidor re-renderizar em **qualquer** mudança (conexão, snapshot ou evento) — violava
diretamente a Regra 5 ("HeroCard: HeroCard apenas / NÃO: Timeline, Painel"). Dividido em 3 contexts
próprios (`useRealtimeConexao`, `useRealtimeSnapshot`, `useRealtimeEvento`), cada um com seu próprio
`value` memoizado — um componente que só lê o estado da conexão (`IndicadorConexaoRealtime`) nunca
re-renderiza por causa de um snapshot novo, e vice-versa. Decisão consciente: **sem biblioteca
nova** (Redux/Zustand) — dividir por contexto já resolve o problema real dentro do que o React
oferece, e trocar de arquitetura de estado global não é o escopo desta Sprint (que não altera
arquitetura).

## Decisão 2 — Padrão de atualização parcial no React Native (Regra 5)

Componentes memoizados (`React.memo`) nos pontos que realmente importam: `HeroCard`, `QuickAction`,
`ActivityCard`, `ItemEvento`, `CommandCard`. Callbacks estabilizados via `useCallback` onde o
memo dependeria disso (`HomeScreen.handleArme`/`armarTotal`/`armarNoturno`/`desarmar`). `<App />`
nunca re-renderiza por causa de um evento SignalR — confirmado por inspeção: `RealtimeProvider`
fica abaixo de `AuthProvider` na árvore, `App.tsx` não guarda nenhum estado próprio.

## Decisão 3 — Backoff exponencial customizado + estado "sem comunicação"

`withAutomaticReconnect()` (padrão do SignalR) usa um array fixo `[0, 2000, 10000, 30000]` e depois
continua tentando a cada 30s indefinidamente, em silêncio. Substituído por uma `IRetryPolicy`
customizada (`nextRetryDelayInMilliseconds`) implementando exatamente a tabela da missão — 1s, 2s,
5s, 10s, 30s (5 tentativas) — que devolve `null` na 6ª chamada, fazendo o SignalR desistir e disparar
`onclose`. Nesse momento o estado vira `sem-comunicacao` (novo, distinto de `desconectado`) com um
botão "Tentar novamente" que reconstrói a conexão do zero (via um contador `tentativaManual` no
array de dependências do `useEffect` de conexão) — reseta o contador de tentativas naturalmente,
por ser uma conexão nova.

## Decisão 4 — Regra 3 (Cache Offline) já existia, mantida e reforçada

`ultimoSnapshot`/`ultimoEvento` já ficavam em memória mesmo com a conexão caída (desde a Sprint 14)
— Hero/Timeline/Painel continuam mostrando o último dado conhecido. Reforçado nesta Sprint: nunca
há tela de loading bloqueante nem modal de erro por reconexão — só o indicador discreto (Decisão
6).

## Decisão 5 — Regra 4 (Política de Cache) aplicada por componente

| Componente | Cache máximo | Onde |
|---|---|---|
| HeroCard | 1 snapshot | `RealtimeContext` (`ultimoSnapshot`, sempre substituído) |
| Timeline | 50 eventos | `EventosScreen` (`.slice(0, 50)` a cada inserção, FIFO) |
| Painel de Controle | Estado atual | `AccessScreen` (`comandos` sempre substituído/patchado, nunca acumulado) |
| Toast | 10 mensagens pendentes | `Toast.tsx` (fila com corte automático do mais antigo) |

## Decisão 6 — Indicador de conexão (Fase 4) consolidado, sem estado "Atualizando" redundante

A missão original pedia 4 estados visuais (Conectado/Atualizando/Reconectando/Sem comunicação).
"Atualizando" foi absorvido pelo pulso de atualização do próprio HeroCard (Fase 1, Decisão 7) — um
indicador global adicional só para "processando snapshot" seria redundante com o feedback que já
acontece no próprio card afetado, e contradiz o princípio "não poluir quando tudo está bem".
`IndicadorConexaoRealtime` (novo componente reutilizável) só renderiza algo em 2 estados:
`reconectando` (ícone piscando suavemente, texto "Reconectando...") e `sem-comunicacao` (mensagem
amigável + "Tentar novamente") — `conectado`/`conectando`/`desconectado` não renderizam nada.

## Decisão 7 — HeroCard: pulso sutil de atualização (Fase 1)

Além do patch de campos já existente (Sprint 14), o `HeroCard` agora dispara uma animação de escala
curta (1 → 1.02 → 1, ~270ms totais, dentro do limite de 300ms) toda vez que `titulo`/`subtitulo`/
`conectividade.label` mudam — sinaliza "isto acabou de atualizar" sem precisar de um indicador
externo. Ignorado na primeira renderização (não pulsa ao simplesmente montar a tela).

## Decisão 8 — Timeline: inserção real, scroll preservado (Fase 2, Regra 2)

`EventosScreen` reescrito: um evento novo (SignalR) só é inserido diretamente no array `itens`
quando (a) a página atual é a 1ª, (b) não há busca de texto ativa, e (c) o usuário está no topo da
lista (rastreado via `onScroll` com limiar de 24px, guardado em ref para não causar re-render a
cada pixel de scroll). Se qualquer uma dessas condições falhar, o evento fica em `pendentes` — um
banner "N novos eventos · Ver novos" aparece (Fade+Slide, ≤220ms) e, ao tocar, mescla `pendentes`
no topo de `itens` e rola para o topo (`scrollToOffset`). O evento recém-inserido ganha um selo
"Novo" por 5s (ou até o usuário rolar de volta ao topo, o que vier primeiro) via
`ItemEvento`'s prop `destaqueNovo`, entrando com `FadeInUp` (280ms).

**Decisão consciente de escopo**: a inserção ao vivo só acontece com busca de texto vazia — o DTO
`EventoResponse` não expõe nenhuma informação que permita saber, no cliente, se um evento bateria
num filtro de texto livre sem reconsultar o servidor. Fingir que ele passaria seria arriscado
(mostraria um evento que desapareceria no próximo refresh); a escolha mais honesta é não inserir
ao vivo nesse caso — o pull-to-refresh continua reconciliando tudo normalmente.

## Decisão 9 — Toasts Inteligentes (Fase 3, Regra 1)

`Toast.tsx` generalizado (`mostrarToast`, tipos `erro`/`sucesso`/`info`/`alerta`, cada um com ícone
e cor próprios) — `mostrarErro` (Sprint 17) vira um atalho para `tipo: 'erro'`, mantendo
compatibilidade com todo código existente. Fila com no máximo 10 mensagens pendentes (Regra 4),
uma exibida por vez.

**Decisão de arquitetura para "a tela em foco já mostra o evento"**: como não há um jeito limpo de
usar hooks de navegação (`useNavigationState`) fora de uma tela, foi criado o padrão oficial do
próprio React Navigation para isto — `navigationRef` (`createNavigationContainerRef`) + `onStateChange`
no `NavigationContainer`, alimentando um armazém mínimo (`telaAtivaStore.ts`, `useSyncExternalStore`,
sem biblioteca nova) exposto via `useTelaAtiva()`. `RealtimeToastBridge` (montado uma vez, dentro do
`NavigationContainer` mas fora do `Stack.Navigator`) decide: se a tela em foco é `Inicio` (Atividade
recente agora recebe o evento ao vivo, Fase 1 estendida) ou `Eventos` (Timeline ao vivo, Decisão 8),
não mostra toast — o evento já é visível por si só. Em qualquer outra tela, mostra um toast
discreto (`alerta` se `destaque`, `info` caso contrário).

## Decisão 10 — Painel de Controle: máquina de estados grounded na arquitetura real (Fase 6)

Antes de desenhar a máquina de estados, o código de `JflComandoServico.ExecutarComandoAsync` foi
lido diretamente: o comando (PGM/arme/desarme) é **síncrono** — o mesmo round-trip HTTP que envia o
comando já devolve o resultado (`await`s a chamada ao provider **e** a publicação do snapshot antes
de retornar). Não existe um canal separado de "confirmação assíncrona via SignalR" para comandos.

Por isso, os 2 estados intermediários da missão original ("Aguardando Confirmação" e "Executando")
foram conscientemente colapsados num único estado `enviando` (spinner), resolvido pela mesma
resposta HTTP — nunca um estado fictício sem sinal real por trás. Estados finais: `sucesso`
(check verde, "Concluído ✓", volta a `normal` após 2s) e `falha` (ícone de alerta, mensagem
amigável, botão vira "Tentar novamente"). Timeout de 10s implementado via `Promise.race` contra a
própria chamada HTTP (não contra um canal SignalR que não existe) — se estourar, transiciona para
`falha` com a mensagem "Não foi possível confirmar a execução. Verifique se o dispositivo está
online.", nunca deixando o spinner girando para sempre. Estado "Desabilitado" já existia (Sprint
17, `conectado=false`), preservado.

**Status Online/Offline em tempo real**: `AccessScreen` agora assina `useRealtimeSnapshot()` e
faz patch parcial (Regra 5) só dos campos `conectado`/`descricaoEstado` dos comandos cujo
`equipamentoId` aparece no snapshot (`SnapshotOperacionalResponse.equipamentos[].estado`) — nunca
refaz a lista inteira de moradores/visitantes/comandos por causa disso.

## Decisão 11 — Troca de Propriedade (Fase 8)

`RealtimeContext` já saía/entrava do grupo SignalR correto ao trocar de propriedade (Sprint 14).
Adicionado nesta Sprint: descarte explícito de `ultimoSnapshot`/`ultimoEvento` no próprio contexto
quando `selectedProperty.id` muda (nunca vaza cache de uma propriedade para outra) — e, como
achado real durante a implementação, **as telas também precisavam limpar seu próprio estado
local**: `HomeScreen` (`dashboard`/`atividades`) e `AccessScreen` (`moradores`/`visitantes`/
`comandos`) mostravam brevemente o dado da propriedade anterior até o novo GET resolver, porque
`setLoading(true)` sozinho não limpa o conteúdo já renderizado. Corrigido com um `useEffect`
dedicado em cada tela, disparado só quando o id da propriedade realmente muda (nunca no primeiro
carregamento, que já é coberto pelo fluxo normal).

## Decisão 12 — Pull-to-Refresh (Fase 7)

Já reconciliava corretamente em `EventosScreen` (GET completo da página 1) — estendido nesta
Sprint para também limpar `pendentes`/o selo "Novo". `AccessScreen` **não tinha nenhum
pull-to-refresh** antes desta Sprint — adicionado (`RefreshControl` chamando `carregar()`, mesmo
padrão já usado em `HomeScreen`/`EventosScreen` desde as Sprints 13/16).

## Decisão 13 — Telemetria (Fase 9)

`services/telemetria.ts` — `console.info` só em `__DEV__`, nunca visível ao morador. Cobre:
conectado/desconectado/reconectando/reconectado/sem-comunicação, snapshot recebido, evento
processado, comando enviado/resultado. **Simplificação consciente**: "cache hit/miss" (citado na
missão como recomendação, não critério de aceite obrigatório) não foi instrumentado — o ganho de
observabilidade seria marginal frente ao esforço de instrumentar cada ponto de leitura de cache
sem alterar comportamento nenhum; registrado aqui para não ser um corte silencioso.

## Fora de escopo (confirmado, nada alterado)

Push Notification, replay de vídeo, câmeras ao vivo, interfone, backend novo, alterações de
domínio, novos endpoints, analytics, novos fabricantes, novos comandos de equipamento. Confirmado
por `dotnet build` limpo sem tocar nenhum arquivo de `backend/` durante toda a Sprint.

## Lições aprendidas

1. **Nunca desenhar uma máquina de estados sem antes ler o código real do outro lado** — a missão
   assumia um canal de confirmação assíncrona que não existe; ler `JflComandoServico` antes de
   escrever qualquer linha de UI evitou construir uma interface que prometeria algo o backend não
   entrega.
2. **Um Context "único" é, na prática, o oposto de "atualização parcial"** — qualquer novo campo
   adicionado a um Context existente amplia silenciosamente o raio de re-renderização de todo
   mundo que o consome; dividir por padrão de leitura (não por conveniência de escrita) é a forma
   mais simples de manter a Regra 5 sem biblioteca nova.
3. **"Descartar o cache ao trocar de propriedade" não é só sobre onde os dados vêm** — mesmo com
   o `RealtimeContext` corretamente limpo, cada tela que guarda seu próprio estado local precisa do
   mesmo cuidado, ou o vazamento acontece de qualquer forma por um instante.
