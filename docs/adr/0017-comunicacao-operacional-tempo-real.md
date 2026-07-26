# ADR 0017 — Comunicação Operacional em Tempo Real

**Data**: 2026-07-25

## Contexto

Depois da Sprint 13 (Camada Operacional Unificada, ADR 0016), toda informação operacional já
existia consolidada num único lugar (`SnapshotOperacional`) — mas só era acessível por *pull*: o
Dashboard e o mobile precisavam de um GET explícito ou do botão "Atualizar" para enxergar qualquer
mudança. A Sprint 14 pediu uma camada de transporte em tempo real, reutilizando integralmente o
domínio existente, sem alterar nenhuma regra de negócio já homologada e sem integrar nenhum
fabricante novo.

A Fase 1 (Descoberta obrigatória) encontrou duas divergências entre a missão e o estado real do
projeto, resolvidas antes de qualquer código:

1. **`MonitoramentoService` não existe** com esse nome — o serviço que já cumpre esse papel
   conceitual (orquestrar a regeneração do estado operacional) é `SnapshotOperacionalServico`
   (Sprint 13). Tratado como o mesmo conceito, sem criar um serviço paralelo.
2. **Não existe nenhum sistema de Perfil/Papel de usuário** no domínio (Administrador/Operador/
   Morador/Técnico) — cada Propriedade tem um único dono (`Propriedade.ProprietarioId`), sem RBAC
   (dívida técnica item 6, desde a Sprint 3.1). A missão pedia grupos SignalR "por perfil do
   usuário" — decisão confirmada com o usuário (Fase 1): implementar só grupos por Propriedade
   (real, existente), registrar grupos por perfil como dívida técnica (item 25), nunca simular um
   RBAC que não existe.

## Decisão 1 — SignalR é transporte puro; o domínio nunca o conhece

```
Provider (Control iD/JFL)
        │
        ▼
Mutação já existente (Equipamento.Status / StatusCentralJfl / Ocorrencia)
        │
        ▼
SnapshotOperacionalServico.RegenerarEPublicarAsync — mesmo Estado Bruto → Classificador
Operacional já existente (ADR 0016), nunca duplicado
        │
        ▼
IOperacionalEventoPublicador (porta, Application/Operacional) — nunca conhece SignalR
        │
        ▼
OperacionalHubPublicador (Api/Realtime) — única implementação real
        │
        ▼
OperacionalHub.Clients.Group("propriedade:{id}") — SignalR
        │
        ▼
Dashboard / Mobile (assinantes do grupo daquela Propriedade)
```

`IOperacionalEventoPublicador` vive em `Application/Operacional` (mesmo padrão de `IFonteEventos`/
`I<Fabricante>Provider` — uma porta para uma capacidade externa plugável). Se nenhuma
implementação real for registrada, `SnapshotOperacionalServico` e todo o resto continuam
funcionando exatamente como antes — só sem notificação automática. Nenhum tipo de
`Microsoft.AspNetCore.SignalR` aparece em `Domain` ou `Application`.

## Decisão 2 — Hub e Publicador vivem em Api, não em Infrastructure

`OperacionalHub` e `OperacionalHubPublicador` (implementação real da porta) vivem em
`Api/Realtime/`, não em `Infrastructure/` — mesmo motivo dos Controllers: SignalR exige o modelo
de hospedagem Web (`Microsoft.NET.Sdk.Web`), que só o projeto `Api` referencia. Isso não quebra a
direção de dependência da Clean Architecture (Api é a camada mais externa) — só reconhece que
"transporte HTTP/WebSocket" é responsabilidade do host web, igual a toda rota REST já existente.

## Decisão 3 — O Hub nunca consulta dado operacional; a única exceção é autorização, igual a todo Controller

`OperacionalHub.EntrarNaPropriedade(propriedadeId)` confere que `Propriedade.ProprietarioId`
corresponde ao usuário autenticado (`Context.User`) antes de adicionar a conexão ao grupo — a
mesma checagem de posse que **todo** Controller já faz (`IPropriedadeRepositorio.GetByIdAsync` +
comparação de `ProprietarioId`) antes de expor qualquer dado. Fora dessa checagem de autorização,
o Hub nunca lê `Equipamento`/`SnapshotOperacional`/`Ocorrencia`, nunca classifica nada, nunca
conhece um Provider — toda publicação de fato é *empurrada* pelo domínio via
`IOperacionalEventoPublicador`, nunca puxada pelo Hub.

## Decisão 4 — Grupos só por Propriedade; grupos por Perfil ficam como dívida técnica

`OperacionalHub.GrupoPropriedade(propriedadeId)` (`"propriedade:{id}"`) é o único agrupamento
implementado. Decisão confirmada com o usuário na Fase 1: como o domínio não tem RBAC (item 6),
simular grupos por perfil (Administrador/Operador/Morador/Técnico) exigiria inventar um conceito
que nenhuma Sprint de produto decidiu ainda — o que violaria a proibição explícita desta Sprint de
"não alterar o domínio". Registrado como dívida técnica item 25, dependente de uma futura Sprint
de RBAC.

## Decisão 5 — Autenticação JWT via querystring só nas rotas do Hub

O handshake de transporte do SignalR (WebSocket) não permite cabeçalhos customizados como
`Authorization` — o cliente (`@microsoft/signalr`) envia o token via `?access_token=` na conexão.
`JwtBearerEvents.OnMessageReceived` (Program.cs) intercepta isso **apenas** quando
`HttpContext.Request.Path` começa com `/hubs` — toda outra rota (Controllers) continua exigindo o
header `Authorization: Bearer` normal, sem essa exceção.

## Decisão 6 — Reconexão automática no cliente; sem fila/replay no servidor

Mobile usa `withAutomaticReconnect()` do próprio `@microsoft/signalr` (backoff padrão) — nenhuma
lógica de reconexão própria foi escrita. SignalR não faz replay de mensagens perdidas durante uma
queda; como toda publicação já carrega o Snapshot Operacional completo (nunca um delta), isso é
seguro — ao reconectar, o cliente reentra no grupo e o próximo evento real já traz o estado
completo atual. O botão "Atualizar" (Sprint 13) continua funcionando de forma totalmente
independente da conexão em tempo real — nunca foi removido, é o fallback permanente.

## Decisão 7 — Debounce simples em memória, nunca uma fila/worker

`OperacionalHubPublicador` mantém um `ConcurrentDictionary` (chave: Propriedade + Motivo) para
descartar uma publicação idêntica em motivo que saiu há menos de 750ms para a mesma Propriedade —
protege contra o caso concreto que existe hoje (ex.: inibir zona faz consulta+comando em sequência
rápida). Nenhuma fila/worker/background service foi introduzido — mesma régua de simplicidade em
MVMs já aplicada desde a Fase 0 ([[feedback_mvp_simplicity]]).

## Decisão 8 — Enums de negócio também serializam como texto no protocolo do Hub

`AddControllers().AddJsonOptions(...)` (ADR 0005) só afeta Controllers — o protocolo JSON do
SignalR tem sua própria configuração de serialização. Sem `AddSignalR().AddJsonProtocol(options
=> options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))`, os enums
(`EstadoOperacional`, `FabricanteEquipamento`) chegariam ao cliente como número cru — quebrando a
consistência do contrato já estabelecida pela ADR 0005. Bug real encontrado e corrigido durante a
validação desta Sprint (confirmado via um cliente SignalR real antes/depois da correção).

## Decisão 9 — Três pontos de publicação, um deles verdadeiramente assíncrono

`RegenerarEPublicarAsync` é chamado depois de toda mutação real de `Equipamento.Status`/
`StatusCentralJfl`:
- `EquipamentoIntegracaoServico` (Control iD): testar conexão, consultar informações,
  sincronizar, importar eventos — todos síncronos, dentro de uma requisição HTTP.
- `JflComandoServico`: testar conexão e qualquer comando de superusuário — também síncronos,
  dentro de uma requisição HTTP.
- `AlarmEventProcessor` (Infrastructure/Jfl): **o único gatilho verdadeiramente assíncrono** —
  uma central pode discar um evento a qualquer momento, fora de qualquer requisição HTTP. Depois
  de persistir a `Ocorrencia` (nunca antes — a garantia de confiabilidade da Fase 1 não muda),
  publica tanto o evento novo (`NovoEventoOperacional`) quanto o Snapshot recalculado
  (`OperacionalAtualizado`, motivo `AlarmeDisparado`). Título/destaque reaplicam o mesmo
  mapeamento já estabelecido em `JflFonteEventos`/ADR 0016 (catálogo Contact ID + StatusResolucao)
  — não é uma regra nova.

Toda publicação é *best-effort*: uma falha de entrega em tempo real (rede, cliente desconectado)
nunca pode reclassificar como erro uma operação de domínio que já teve sucesso — sempre capturada
e só logada.

## Impactos

`Application/Operacional/IOperacionalEventoPublicador.cs` (porta + enum
`MotivoAtualizacaoOperacional`); `Application/Operacional/{ISnapshotOperacionalServico,
SnapshotOperacionalServico}.cs` (`RegenerarEPublicarAsync`); `Application/Equipamentos/
EquipamentoIntegracaoServico.cs`; `Application/Jfl/JflComandoServico.cs`; `Infrastructure/Jfl/
AlarmEventProcessor.cs`; `Api/Realtime/{OperacionalHub,OperacionalHubPublicador}.cs` (novos);
`Api/RateLimiterPolicies.cs` (política `Realtime`); `Api/Program.cs` (AddSignalR, JWT via
querystring para `/hubs`, rate limiter, `MapHub`); mobile `src/realtime/RealtimeContext.tsx`
(novo); `App.tsx`; `screens/dashboard/DashboardScreen.tsx`; `screens/operacional/
{CentralOperacionalScreen,SaudePropriedadeScreen}.tsx`; `screens/eventos/EventosScreen.tsx`.

## Como revisar futuramente

Ao integrar um fabricante novo (Intelbras, Hikvision, Dahua) que siga a ADR 0014/0015, nenhuma
mudança é necessária nesta camada — o Provider novo só precisa continuar mutando
`Equipamento.Status` (ou um rollup próprio, como `StatusCentralJfl`) através do serviço de
integração/comando que já chama `RegenerarEPublicarAsync`. Se um RBAC real for implementado no
futuro (ver dívida técnica item 25), os grupos por Perfil se somam aos grupos por Propriedade sem
exigir mudança no fluxo Provider→Snapshot→Publicador — só uma nova dimensão de agrupamento no
Hub.
