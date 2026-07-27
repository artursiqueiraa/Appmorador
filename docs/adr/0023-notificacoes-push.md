# ADR 0023 — Notificações Push (Sprint 19)

**Data**: 2026-07-26

## Contexto

A Sprint 18 (ADR 0022) cobriu "app aberto": SignalR atualiza tudo em tempo real. Esta Sprint
complementa com "app fechado/em segundo plano" — o morador precisa ser avisado de alarme,
visitante autorizado, entrega, dispositivo offline etc. mesmo sem o app aberto.

## Fase 0 — Auditoria (achado que redesenhou a estratégia de disparo)

A missão original assumia 8 tipos de evento já distinguíveis no domínio, prontos para virar
notificação. Leitura direta do código (`CategoriaEvento`, `MotivoAtualizacaoOperacional`,
`AutorizacaoServico`, `EntregaServico`) mostrou o oposto:

| Evento assumido pela missão | Realidade encontrada |
|---|---|
| Alarme disparado | ✅ Já existe um ponto único e claro (`AlarmEventProcessor.CriarOcorrenciaAsync`) |
| Sistema armado/desarmado | ✅ `JflComandoServico.ArmarAsync`/`DesarmarAsync` — ação já conhecida no momento certo |
| Portão aberto (PGM) | ⚠️ Backend só sabe "PGM N foi acionado com sucesso" — o nome amigável "portão" é preferência **só do Mobile** (Sprint 17, `pgmLabels.ts`), nunca chega ao backend |
| Visitante autorizado | ⚠️ `AutorizacaoServico` existia mas **nunca publicava evento nenhum** — sem hook nenhum antes desta Sprint |
| Entrega recebida | ⚠️ `EntregaServico` também nunca publicava evento — sem hook nenhum antes desta Sprint |
| Equipamento offline | ⚠️ Confirmado como dívida técnica já registrada (Sprint 13, item 24): a transição é só uma sobrescrita de `Equipamento.Status`, sem registro auditável de transição |

**Decisão de resolução**: em vez de tentar extrair 8 sinais distintos de um pipeline de eventos
genérico que não tem detalhe suficiente (`MotivoAtualizacaoOperacional` só tem 3 valores amplos),
`INotificationDispatcher.NotificarAsync` é chamado diretamente pelos Application Services, no
ponto exato em que cada ação semântica específica já é conhecida — nunca a partir do sinal
genérico do SignalR. Isso mantém "nenhuma alteração de domínio, exceto `DispositivoPush`" (a
mudança fica inteiramente na camada de Aplicação, nos pontos de orquestração já existentes).

**Decisão de honestidade de copy**: como o backend não conhece o rótulo "portão" (é preferência
local do Mobile), o evento de PGM notifica com "🔓 Comando acionado" / "Um comando foi executado em
{Propriedade}" em vez de fingir "🚪 Portão aberto" — mentir sobre o que o backend sabe seria pior
do que uma mensagem levemente mais genérica, porém sempre verdadeira.

## Decisão 1 — `DispositivoPush` como entidade própria, não um campo em `Usuario`

Um morador pode ter múltiplos dispositivos (celular pessoal, tablet, celular do cônjuge) — cada um
com seu próprio token, plataforma e preferências de canal. Campos: `Id`, `UsuarioId`,
`PropriedadeId?` (hint opcional, não é a chave real de envio — ver Decisão 4),
`Plataforma` (enum `Android`/`Ios`), `Token`, `Modelo?`, `VersaoApp?`, `Ativo`,
`NotificarAlertas`/`NotificarAtividades`/`NotificarGeral` (Decisão 6), `UltimoUsoUtc`,
`CreatedAtUtc`.

**`Token` em texto puro, não hash** (ao contrário de `RefreshToken.TokenHash`): o valor precisa ser
reenviado ao FCM em todo envio — não há como hashear e ainda usar. Um token vazado sozinho não
autentica nada nem permite login/ações como o usuário; só permite tentar enviar um push para
aquele único aparelho, algo que o próprio FCM valida e escopa.

## Decisão 2 — `INotificationProvider` como porta, mesmo padrão de `IJflProvider`/`IControlIdProvider`

Nenhum código de Domínio/Aplicação referencia Firebase diretamente — sempre via
`INotificationProvider.EnviarAsync`/`ValidarTokenAsync`. `FirebaseNotificationProvider` é a
implementação de hoje; `OneSignalNotificationProvider`/`AzureNotificationHubProvider`/
`HuaweiNotificationProvider` no futuro implementam a mesma porta, sem tocar
`NotificationDispatcher`/`NotificationService`.

## Decisão 2.1 — Provider "sem-op documentado" por falta de credenciais Firebase reais

Push de verdade exige um projeto Firebase real (credencial de conta de serviço + `google-services.json`
no app) — não há como criar isso sem acesso ao Firebase Console, que não está disponível nesta
sessão. Decisão explícita do usuário (perguntado antes de qualquer código ser escrito): construir a
arquitetura real completa, com `FirebaseNotificationProvider` operando em modo documentado quando
`FirebaseOptions.Configurado` é `false` (sem `CredenciaisPath` válido) — loga
`"[PUSH] (sem Firebase configurado) enviaria..."` e retorna sucesso simulado, nunca lança, nunca
bloqueia o fluxo. O mesmo caminho de código passa a enviar de verdade assim que credenciais reais
forem configuradas — sem nenhuma mudança de código, só configuração (`Firebase:CredenciaisPath` em
`appsettings.json`).

## Decisão 3 — `NotificationDispatcher` decide, `NotificationService` resolve destinatários e envia

`NotificationDispatcher` (Aplicação): aplica debounce (Decisão 5), monta a mensagem fixa por tipo
de evento (tabela da missão, Fase 4 — texto não é configurável, é decisão de produto) e delega a
`INotificationService.EnviarParaPropriedadeAsync`. `NotificationService`: resolve
Propriedade → `ProprietarioId` (mesma cadeia de ownership de Dashboard/Eventos/Equipamentos) →
dispositivos ativos desse usuário → filtra por canal habilitado (Decisão 6) → chama o Provider →
desativa dispositivos cujo token o Provider devolveu como definitivamente inválido.

## Decisão 4 — Alvo de envio é sempre `Propriedade.ProprietarioId`, nunca `DispositivoPush.PropriedadeId`

`PropriedadeId` no dispositivo é só um hint (para telemetria/depuração futura) — o destinatário
real de toda notificação é resolvido por ownership (`Propriedade → ProprietarioId → dispositivos
desse usuário`), o mesmo padrão já usado em todo o resto do domínio. Por isso
`IDispositivoPushRepositorio` só tem `ListAtivosByUsuarioAsync`, sem um `ListAtivosByPropriedadeAsync`.

## Decisão 5 — Debounce em memória (`ConcurrentDictionary`), não Redis/fila

Continuação da filosofia de simplicidade do projeto (sem filas/workers/cloud storage antecipados,
ver Sprint 12/17.5): `DebounceNotificacaoEmMemoria` (Infrastructure, registrado `Singleton` — precisa
sobreviver entre requests) guarda `(EventoNotificacaoTipo, Chave) → DateTimeUtc`, janela fixa de
60s. Chave é `EquipamentoId` quando existe (evita ping-pong de "offline"/"online" no mesmo
equipamento), senão `PropriedadeId`. **Limitação documentada**: só funciona corretamente com um
único processo de backend — se o backend rodar multi-instância no futuro, precisa virar um cache
distribuído (Redis) para o debounce continuar funcionando entre instâncias.

## Decisão 6 — Preferência de canal por dispositivo, filtrada no servidor antes do envio

3 booleanos na própria entidade `DispositivoPush` (`NotificarAlertas`/`NotificarAtividades`/
`NotificarGeral`, default `true`) — não em uma entidade nova (permanece dentro do escopo "só
`DispositivoPush` como mudança de domínio"). Filtrado **antes** do envio, nunca depois: uma
notificação já entregue não pode ser "desfeita". Mapeamento fixo: Alertas → Alarme/Offline;
Atividades → Comando/Visitante/Entrega; Geral → Armar/Desarmar.

## Decisão 7 — Canais Android (Fase 9) fixos, criados uma vez por dispositivo

`alertas` (Importância Alta, som, vibração), `atividades` (Normal, som, vibração), `geral` (Baixa,
som, sem vibração) — nomes visíveis ao morador são os mesmos usados na tela Ajustes → Notificações
("Alarmes e alertas"/"Atividades em casa"/"Mudanças de status"), nunca "canal" ou o id técnico.
Depois de criado, o Android só permite alterar nome/descrição de um canal (limitação do próprio
SO) — os valores de importância/som/vibração ficam fixos desde a primeira execução em cada
aparelho.

## Decisão 8 — Token do dispositivo é o FCM nativo (`getDevicePushTokenAsync`), não o token-proxy do Expo

O backend fala com o FCM diretamente (`FirebaseAdmin` SDK) — por isso o Mobile precisa do token
nativo do Firebase (`Notifications.getDevicePushTokenAsync()`), não do
`getExpoPushTokenAsync()` (que passaria pelo serviço de relay da própria Expo, um provider
diferente do que o backend implementa). Consequência aceita: sem `google-services.json` real
configurado no app (mesma limitação de credenciais da Decisão 2.1), o token obtido não corresponde
a nenhum projeto Firebase de verdade — o fluxo ponta a ponta só valida de verdade quando ambos os
lados (app + backend) tiverem credenciais reais do mesmo projeto Firebase. Registrado como dívida
técnica (`DIVIDA_TECNICA.md` item 38).

## Decisão 9 — Registro de token: upsert por token, nunca duplica

`DispositivoPushServico.RegistrarAsync` busca por `Token` primeiro; se já existir (reinstalação do
app, ou login com outra conta no mesmo aparelho), **reassume** o registro para o usuário atual em
vez de criar um novo — evita um dispositivo "fantasma" continuar ativo para a conta anterior após
um logout/login com outra conta no mesmo aparelho.

## Decisão 10 — Desregistro roda ANTES da sessão ser limpa, via hook (mesmo padrão de `registerSessionExpiredHandler`)

`DELETE /api/dispositivos-push/{id}` exige token de acesso válido — se o Mobile esperasse `user`
virar `null` para reagir (mesmo padrão reativo do `RealtimeContext`), a chamada aconteceria DEPOIS
da sessão já ter sido limpa por `AuthContext.logout()`, falhando com 401. Resolvido com
`registerBeforeLogoutHook`/`onBeforeLogout` (mesmo padrão já existente de
`registerSessionExpiredHandler` em `api/client.ts`): `PushNotificationProvider` registra o hook,
`AuthContext.logout()` chama `await onBeforeLogout?.()` antes de revogar o refresh token — enquanto
a sessão ainda é válida.

## Decisão 11 — Rota `DELETE .../{id}`, não `.../{token}` como a missão sugeriu

Um token pode conter caracteres não seguros para segmento de URL; o Mobile já recebe o `id` na
resposta do registro. Rota nova (sem contrato existente para quebrar) — deviação deliberada,
documentada aqui.

## Decisão 12 — Suprimir notificação de sistema quando o app está em primeiro plano (Fase 5)

`Notifications.setNotificationHandler` sempre retorna `shouldShowBanner/List/PlaySound/SetBadge:
false` — esse handler só roda quando o app está em primeiro plano (é a própria definição da API);
nesse estado, o SignalR (Sprint 18) já atualiza a tela e o `RealtimeToastBridge` já mostra um toast
discreto quando o evento acontece fora da tela em foco. Mostrar TAMBÉM a notificação do sistema
duplicaria o aviso. Com o app fechado/em segundo plano, quem decide exibir é o próprio SO — este
código nunca roda nesse caso.

## Decisão 13 — Deep link com retry de prontidão, nunca falha silenciosa nem crash

`acao` no payload (`ABRIR_APP_HISTORICO`/`ABRIR_APP_INICIO`/`ABRIR_APP_ACESSOS`) mapeia para uma
tela/aba fixa. Ao abrir o app a frio por um toque na notificação, o `NavigationContainer` pode
ainda não existir no primeiro instante — `navegarParaAcao` tenta por até 3s (10 tentativas de
300ms) antes de desistir silenciosamente (o app já abriu normalmente; no pior caso, na tela
inicial). `acao` desconhecida ou ausente nunca navega e nunca lança.

**Resiliência offline (app aberto sem conexão com o backend)**: nenhuma tela nova de resiliência foi
criada — cada tela de destino (Eventos, Início, Acessos) já tem seu próprio estado de
carregamento/erro/"Tentar novamente" estabelecido desde Sprints anteriores; duplicar essa lógica
aqui seria uma abstração nova sem necessidade real.

## Decisão 14 — Preferências de canal espelhadas localmente (sem `GET` novo no backend)

A tela Ajustes → Notificações precisa mostrar o estado atual dos 3 toggles, mas o backend não
expõe (nem a missão pedia) um `GET /api/dispositivos-push/{id}` — só `POST`/`PUT`/`DELETE`. Em vez
de inventar um endpoint não solicitado, o Mobile espelha as preferências localmente
(`pushDeviceStorage.salvarPreferenciasLocais`, default `true`/`true`/`true`, igual ao backend) a
cada alteração bem-sucedida — a fonte de verdade para o ENVIO continua sendo sempre o servidor
(Decisão 6); o espelho local é só para renderizar o estado inicial da tela.

## Fase 8.1 (Agrupamento) — não implementado nesta Sprint

Só a Fase 8.2 (limitação de frequência/debounce) foi implementada. Agrupar múltiplas notificações
do mesmo tipo numa única mensagem ("3 novas atividades em Casa Serra") exigiria estado adicional
por usuário/tipo de evento com janela de agregação — decisão consciente de não implementar
especulativamente sem um caso de uso real pressionando por isso ainda (debounce já evita a maior
parte do ruído prático: eventos repetidos do mesmo tipo/equipamento em menos de 60s). Registrado em
`DIVIDA_TECNICA.md` item 40.

## iOS — dívida técnica confirmada (Fase 3)

Sem dispositivo iOS físico disponível neste ambiente — só Android foi implementado/validado nesta
Sprint, exatamente como a missão previu ("documentar como dívida técnica e validar apenas Android").
A arquitetura (`INotificationProvider`, `DispositivoPush.Plataforma`) já suporta `Ios` sem mudança
estrutural quando um certificado APNs real e um dispositivo físico existirem.

## Testes de resiliência

Cobertos por teste automatizado (não apenas leitura de código, diferente da Sprint 18): Firebase
indisponível/exceção nunca lança para quem chama (`NotificationServiceTests`); token inválido
desativa só o dispositivo específico, nunca os demais; usuário sem dispositivo ativo não gera
nenhuma chamada ao provider; dois dispositivos do mesmo usuário recebem ambos; canal desabilitado
filtra corretamente antes do envio. Do lado do Mobile: permissão negada nunca insiste numa segunda
chamada ao diálogo nativo; falha ao obter o token nunca impede o app de funcionar; deep link com
`acao` desconhecida/ausente nunca navega nem lança.

## Lições aprendidas

1. **Uma missão pode assumir uma granularidade de evento que o domínio real não tem** — a mesma
   disciplina da Sprint 17 se repetiu aqui: ler o código antes de desenhar qualquer coisa evitou
   inventar campos/eventos "de mentirinha" no `MotivoAtualizacaoOperacional` só para caber na
   tabela da missão. A solução real (hooks diretos na Aplicação) era mais simples que forçar o
   pipeline genérico existente a carregar detalhe que ele nunca teve.
2. **"Sem credenciais reais" não é motivo para não construir a arquitetura real** — um provider
   com modo sem-op explícito e documentado (nunca fingido como sucesso real sem avisar) permite
   entregar 100% do desenho testável, com a troca para produção sendo pura configuração.
3. **Um hook de "antes de" (`registerBeforeLogoutHook`) resolve um problema de ordem que um
   `useEffect` reativo não resolve** — quando uma limpeza de estado precisa acontecer enquanto uma
   pré-condição (sessão válida) ainda é verdadeira, reagir à mudança de estado já é tarde demais.
