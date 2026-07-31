# ADR-0033 — Plataforma de Execução de Comandos de Hardware

| Campo | Valor |
|---|---|
| ID | ADR-0033 |
| Título | Plataforma de Execução de Comandos de Hardware |
| Status | Aprovado |
| Data | 2026-07-29 |
| Revisão | Rev. 3 — incorpora revisão de prontidão de execução da Sprint 22C |
| Autor | Equipe AppMorador |
| Decisores | Architecture Review Board |
| Depende de | ADR-0001 (referenciado, não bloqueante) |
| Pré-requisito para | Sprint 22C, ADR-0034 |

> **Nota de proveniência**: este documento nasceu fora do fluxo usual de ADRs do AppMorador
> (`docs/adr/0001-template.md`) — foi escrito e revisado externamente para a Sprint 22C, ainda não
> iniciada, e trazido para o repositório já em Rev. 2. Mantido com sua estrutura original
> (mais detalhada que o template padrão do projeto) por ser genuinamente mais rica para uma
> decisão desta magnitude. Nenhum código desta ADR foi implementado ainda — é puramente
> especificação, base para quando a Sprint 22C começar.

## 1. Contexto

O AppMorador precisa executar comandos em equipamentos de diferentes naturezas — painéis de
alarme, DVRs, controladores de acesso, catracas, leitores faciais — sem que a lógica de cada
fabricante se espalhe pelos controllers, serviços e frontend da aplicação.

A Sprint 22B introduziu o módulo Diagnóstico com ações simuladas (mock). A evolução natural é
substituir esses mocks por uma plataforma real de execução de comandos, mas essa substituição não
pode ser um "botão liga/desliga" global. Ela precisa ser gradual, segura e extensível.

O problema central não é "como falar com Intelbras, Hikvision ou JFL". O problema é: como
construir uma plataforma que consiga representar qualquer equipamento, de qualquer fabricante, sem
que a plataforma precise ser reescrita a cada nova integração?

Esta ADR define a plataforma. Os fabricantes são apenas implementações.

## 2. Problema Arquitetural

### 2.1 O que estava errado antes

- **Interface gorda (`IHardwareProvider`)**: obrigava todo provider a implementar todos os
  comandos, gerando `throw new NotSupportedException()` em metade das implementações — cheiro
  claro de abstração errada.
- **Retry cego**: comandos destrutivos (reiniciar equipamento) eram repetidos automaticamente em
  caso de timeout, sem considerar que o comando pode já ter sido executado e o ACK se perdido.
- **Acoplamento fabricante/funcionalidade**: o controller conhecia o protocolo e o fabricante.
- **Sem idempotência**: não havia mecanismo para evitar execução duplicada de comandos.
- **Sem capacidade declarativa**: a UI não sabia quais ações um equipamento suportava antes de
  tentar executar.
- **Strings como identificadores**: `Provider = "JflProvider"` e `Tipo = "Restart"` envelhecem
  mal, geram problemas de casing e versionamento.

### 2.2 O que esta ADR resolve

- Separa a plataforma (infraestrutura de comandos) dos providers (implementações por fabricante).
- Introduz um modelo de capacidades que permite à plataforma representar qualquer equipamento sem
  conhecê-lo antecipadamente.
- Define políticas de retry e idempotência por tipo de comando, não globalmente.
- Garante auditoria e observabilidade em todos os comandos, desde o primeiro dia.
- Permite que a substituição de mocks por comunicação real seja por equipamento, não global.
- Elimina strings soltas do contrato público, usando identificadores tipados e registrados.

## 3. Objetivos

- Criar uma plataforma agnóstica de fabricante para execução de comandos em equipamentos.
- Permitir que cada equipamento declare suas capacidades, e a UI habilite ações apenas sobre
  capacidades suportadas.
- Garantir que comandos sejam executados de forma segura, com retry inteligente e idempotência
  onde necessário.
- Registrar auditoria completa de todo comando executado (quem, quando, em qual equipamento, qual
  resultado).
- Fornecer feedback em tempo real ao usuário sobre o estado da execução.
- Permitir que novos fabricantes sejam adicionados sem alterar a plataforma.
- Estabelecer identificadores estáveis que não quebrem com refatoração de nomes de classe.

## 4. Princípios

| Princípio | Descrição |
|---|---|
| Agnosticismo | A plataforma não conhece fabricantes. Conhece capacidades, comandos e providers. |
| Capacidade sobre Interface | Um provider declara o que faz, não implementa uma interface que obriga o que não faz. |
| Idempotência Explícita | Cada comando declara se é idempotente. Retry só ocorre sobre comandos seguros. |
| Auditoria por Design | Todo comando gera registro. Não é opt-in. |
| Assincronia Nativa | Comandos são operações potencialmente longas. A plataforma assume assincronia desde a raiz. |
| Extensibilidade | Novo fabricante = novo provider + novas capacidades + novos payloads. Nenhuma alteração na plataforma. |
| Identificadores Estáveis | Provider e Tipo de comando são identificadores persistentes, não nomes de classe ou strings soltas. |
| Mock como Cidadão | O `MockProvider` não é um hack. É uma implementação válida que declara capacidades ricas para validar a plataforma contra equipamentos futuros. |
| Pipeline Extensível | A execução de comandos passa por um pipeline onde interceptores podem ser adicionados (rate limit, circuit breaker, cache) sem alterar o dispatcher. |

## 5. Decisões

### 5.1 Identificadores Estáveis — `ProviderType` e `HardwareCommandType`

#### 5.1.1 Por que não `string`?

Strings envelhecem mal. `Provider = "JflProvider"` acopla o contrato ao nome da classe.
`Tipo = "Restart"` permite variações de casing (`restart`, `ReStart`) e não versiona.

#### 5.1.2 Regra de governança (Rev. 2 — incorporada do feedback ARB)

> **O `Codigo` (`"JFL"`, `"SYNC_CLOCK"`, etc.) é um contrato público e imutável, distinto do nome
> da constante em C#.**
>
> - O nome da constante (`ProviderType.JFL`, `HardwareCommandType.RESTART`) pode ser renomeado
>   livremente numa refatoração — é só um identificador de código.
> - O `Codigo` que ele carrega (a string persistida no banco, trafegada na API, gravada em
>   auditoria) **nunca muda** sem uma migração de dados explícita e um plano de compatibilidade
>   com dados históricos já persistidos.
> - Renomear a constante `ProviderType.JFL` para `ProviderType.JFL_ACTIVE100BUS` sem tocar no
>   `Codigo = "JFL"` é uma refatoração segura. Mudar o `Codigo` de `"JFL"` para `"JFL_V2"` é uma
>   mudança de contrato e exige o mesmo cuidado de uma migration de schema.
>
> Esta regra existe precisamente para que ninguém, no futuro, confunda "refatorar o nome da
> constante" com "mudar o valor persistido" — são operações de risco completamente diferentes.

#### 5.1.3 `ProviderType`

```csharp
public sealed class ProviderType : IEquatable<ProviderType>
{
    public static readonly ProviderType JFL = new("JFL");
    public static readonly ProviderType HIKVISION = new("HIKVISION");
    public static readonly ProviderType INTELBRAS = new("INTELBRAS");
    public static readonly ProviderType CONTROLID = new("CONTROLID");
    public static readonly ProviderType MOCK = new("MOCK");

    public string Codigo { get; }
    private ProviderType(string codigo) => Codigo = codigo;

    public bool Equals(ProviderType other) => Codigo == other?.Codigo;
    public override bool Equals(object obj) => obj is ProviderType other && Equals(other);
    public override int GetHashCode() => Codigo.GetHashCode();
    public override string ToString() => Codigo;

    public static ProviderType Parse(string codigo) => codigo?.ToUpperInvariant() switch
    {
        "JFL" => JFL,
        "HIKVISION" => HIKVISION,
        "INTELBRAS" => INTELBRAS,
        "CONTROLID" => CONTROLID,
        "MOCK" => MOCK,
        _ => throw new ArgumentException($"Provider desconhecido: {codigo}")
    };
}
```

#### 5.1.4 `HardwareCommandType`

```csharp
public sealed class HardwareCommandType : IEquatable<HardwareCommandType>
{
    public static readonly HardwareCommandType RESTART = new("RESTART");
    public static readonly HardwareCommandType SYNC_CLOCK = new("SYNC_CLOCK");
    public static readonly HardwareCommandType READ_STATUS = new("READ_STATUS");
    public static readonly HardwareCommandType OPEN_DOOR = new("OPEN_DOOR");
    public static readonly HardwareCommandType ENROLL_FACE = new("ENROLL_FACE");
    public static readonly HardwareCommandType READ_EVENTS = new("READ_EVENTS");
    public static readonly HardwareCommandType ARM_PARTITION = new("ARM_PARTITION");
    public static readonly HardwareCommandType DISARM_PARTITION = new("DISARM_PARTITION");
    public static readonly HardwareCommandType FIRMWARE_VERSION = new("FIRMWARE_VERSION");

    public string Codigo { get; }
    private HardwareCommandType(string codigo) => Codigo = codigo;

    public bool Equals(HardwareCommandType other) => Codigo == other?.Codigo;
    public override bool Equals(object obj) => obj is HardwareCommandType other && Equals(other);
    public override int GetHashCode() => Codigo.GetHashCode();
    public override string ToString() => Codigo;

    public static HardwareCommandType Parse(string codigo) => codigo?.ToUpperInvariant().Replace("-", "_") switch
    {
        "RESTART" => RESTART,
        "SYNC_CLOCK" => SYNC_CLOCK,
        "SYNCLOCK" => SYNC_CLOCK,
        "READ_STATUS" => READ_STATUS,
        "OPEN_DOOR" => OPEN_DOOR,
        "OPENDOOR" => OPEN_DOOR,
        "ENROLL_FACE" => ENROLL_FACE,
        "READ_EVENTS" => READ_EVENTS,
        "ARM_PARTITION" => ARM_PARTITION,
        "DISARM_PARTITION" => DISARM_PARTITION,
        "FIRMWARE_VERSION" => FIRMWARE_VERSION,
        _ => throw new ArgumentException($"Tipo de comando desconhecido: {codigo}")
    };
}
```

O banco armazena o `Codigo` (ex: `"JFL"`, `"RESTART"`). A aplicação trabalha com os objetos
tipados.

### 5.2 `HardwareCommand` — Envelope Genérico + Payload Tipado

#### 5.2.1 Modelo híbrido

O `HardwareCommand` é um envelope comum (sempre igual) que carrega um payload tipado (específico
por tipo de comando).

```csharp
public sealed class HardwareCommand
{
    // Identificação
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public string IdempotencyKey { get; set; }
    public DateTime? IdempotencyExpiresAt { get; set; } // janela explícita, não depende de DATE()

    // Contexto
    public Guid TenantId { get; set; }
    public Guid EquipamentoId { get; set; }
    public ProviderType Provider { get; set; }

    // Comando
    public HardwareCommandType Tipo { get; set; }
    public string PayloadJson { get; set; }

    // Estado
    public CommandStatus Status { get; set; }
    public string ResultadoJson { get; set; }
    public string Erro { get; set; }

    // Auditoria
    public DateTime CriadoEm { get; set; }
    public string CriadoPor { get; set; }
    public string IpOrigem { get; set; }
    public DateTime? IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
}

public enum CommandStatus
{
    Pendente = 0,
    EmExecucao = 1,
    Concluido = 2,
    Erro = 3,
    Timeout = 4,
    Cancelado = 5
}
```

#### 5.2.2 Payloads Tipados

Cada tipo de comando tem seu próprio DTO. Exemplos:

```csharp
public sealed class SyncClockPayload
{
    public DateTime DataHora { get; set; }
}

public sealed class OpenDoorPayload
{
    public int DoorId { get; set; }
}

public sealed class RestartPayload
{
    // Sem parâmetros
}
```

O provider recebe o `HardwareCommand`, deserializa o `PayloadJson` para o tipo correto (via
Registry) e executa.

### 5.3 Command Registry — Fonte Única da Verdade

O Registry resolve a cadeia completa:

```
Tipo do Comando → Capability → PayloadType → PayloadConverter → Handler
```

> **Rev. 2 — `PayloadConverter` incorporado ao Registry (feedback ARB)**: a cadeia original (Rev.
> 1) ia direto de `PayloadType` para `Handler`, assumindo implicitamente que a serialização é
> sempre JSON via o serializador padrão da aplicação. Isso funciona hoje (todos os providers
> conhecidos falam JSON), mas é uma suposição implícita, não uma decisão documentada. O Registry
> agora carrega explicitamente um `IPayloadConverter` por tipo de comando — com um
> `JsonPayloadConverter` como implementação padrão registrada para todos os comandos existentes.
> Se um provider futuro precisar de um formato diferente (XML de um protocolo legado, um binário
> proprietário, um schema Protobuf), a extensão é registrar um `IPayloadConverter` novo para
> aquele tipo de comando — **nenhuma mudança na plataforma, no Dispatcher ou nos outros
> providers**. Sem esse metadado explícito, essa evolução exigiria uma mudança estrutural no
> Registry no dia em que o primeiro provider não-JSON aparecesse.

```csharp
public interface IPayloadConverter
{
    string Serializar(object payload);
    object Desserializar(string payloadSerializado, Type payloadType);
}

public sealed class JsonPayloadConverter : IPayloadConverter
{
    public string Serializar(object payload) => JsonSerializer.Serialize(payload);
    public object Desserializar(string payloadSerializado, Type payloadType) =>
        JsonSerializer.Deserialize(payloadSerializado, payloadType);
}

public interface ICommandRegistry
{
    HardwareCapability GetCapability(HardwareCommandType type);
    Type GetPayloadType(HardwareCommandType type);
    IPayloadConverter GetPayloadConverter(HardwareCommandType type);
    ICommandHandler GetHandler(HardwareCommandType type);
    bool IsRegistered(HardwareCommandType type);
}
```

Registro no startup:

```csharp
registry.Register(
    type: HardwareCommandType.RESTART,
    capability: new HardwareCapability { /* ... */ },
    payloadType: typeof(RestartPayload),
    payloadConverter: new JsonPayloadConverter(), // padrão; só muda para providers com formato próprio
    handler: new RestartHandler()
);
```

Isso centraliza toda a metadata do comando em um único ponto. Nenhum switch espalhado.

### 5.4 `HardwareCapability` — Modelo Rico

Não é um enum. É um objeto declarativo que descreve o que um equipamento pode fazer e como a
plataforma deve tratá-lo.

```csharp
public sealed class HardwareCapability
{
    public HardwareCommandType Tipo { get; set; }
    public string Nome { get; set; }
    public string Categoria { get; set; }

    // Execução
    public bool SuportaRetry { get; set; }
    public bool RequerIdempotencia { get; set; }
    public bool SuportaAsync { get; set; } // true = resposta não é imediata, usa fila + evento
    public bool RequerConectividade { get; set; }

    // Timeout
    public int? TimeoutPadraoSegundos { get; set; }
    public TimeoutStrategy TimeoutStrategy { get; set; }

    // Segurança
    public string Permissao { get; set; }
    public bool Experimental { get; set; }
}

public enum TimeoutStrategy
{
    Hard,           // Timeout absoluto, cancela imediatamente
    Soft,           // Timeout avisa, mas permite continuação se ACK recebido
    ProviderManaged // O provider gerencia seu próprio timeout interno
}
```

#### 5.4.1 `SuportaAsync`

Alguns equipamentos respondem imediatamente. Outros:

```
↓ Enfileira
↓ ACK imediato
↓ Executa depois
↓ Evento de conclusão
```

A UI precisa saber a diferença. Se `SuportaAsync = true`, o frontend mostra "Em processamento" em
vez de aguardar resposta síncrona.

#### 5.4.2 `TimeoutStrategy`

| Estratégia | Comportamento |
|---|---|
| Hard | Timeout atingido = cancela, status = `Timeout` |
| Soft | Timeout atingido = avisa, mas se ACK chegar depois, aceita e finaliza |
| ProviderManaged | O provider tem seu próprio timeout interno (ex: sessão TCP com keepalive) |

#### 5.4.3 Exemplos de Capacidades

| Tipo | Categoria | Retry | Idempotência | Async | Timeout | Permissão |
|---|---|---|---|---|---|---|
| RESTART | Diagnostico | ❌ | ✅ | ❌ | Hard | Diagnostico.Executar |
| READ_STATUS | Diagnostico | ✅ | ❌ | ❌ | Hard | Diagnostico.Visualizar |
| SYNC_CLOCK | Diagnostico | ❌ | ✅ | ❌ | Soft | Diagnostico.Executar |
| OPEN_DOOR | ControleAcesso | ❌ | ✅ | ❌ | Hard | Acesso.Executar |
| ENROLL_FACE | ControleAcesso | ❌ | ✅ | ✅ | ProviderManaged | Acesso.Administrar |
| READ_EVENTS | Diagnostico | ✅ | ❌ | ✅ | Hard | Diagnostico.Visualizar |
| ARM_PARTITION | Alarme | ❌ | ✅ | ❌ | Hard | Alarme.Executar |
| FIRMWARE_VERSION | Diagnostico | ✅ | ❌ | ❌ | Hard | Diagnostico.Visualizar |

### 5.5 Política de Retry

#### 5.5.1 Regra Fundamental

Retry automático só ocorre sobre comandos idempotentes.

Comandos destrutivos (reiniciar, abrir porta, armar partição) nunca fazem retry automático. Em
caso de timeout, o status é `Timeout` e a decisão é do usuário.

#### 5.5.2 Tabela de Decisão

| Tipo de Comando | Retry Automático | Idempotência | Comportamento em Timeout |
|---|---|---|---|
| Consulta (status, versão, eventos) | ✅ Sim | ❌ Não necessário | Retry com backoff |
| Leitura (dados do equipamento) | ✅ Sim | ❌ Não necessário | Retry com backoff |
| Ping / Conectividade | ✅ Sim | ❌ Não necessário | Retry com backoff |
| Sincronizar Relógio | ❌ Não | ✅ Sim | Soft timeout → decisão humana |
| Reiniciar Equipamento | ❌ Não | ✅ Sim | Hard timeout → decisão humana |
| Abrir Porta | ❌ Não | ✅ Sim | Hard timeout → decisão humana |
| Atualizar Firmware | ❌ Nunca | ✅ Sim | Erro imediato, nunca retry |
| Cadastrar Biometria | ❌ Não | ✅ Sim | ProviderManaged → decisão humana |

#### 5.5.3 Backoff

```csharp
public sealed class RetryPolicy
{
    public int MaximoTentativas { get; set; } = 3;
    public int DelayInicialMs { get; set; } = 1000;
    public double BackoffMultiplicador { get; set; } = 2.0;
    public int DelayMaximoMs { get; set; } = 30000;

    public int CalcularDelay(int tentativa)
    {
        var delay = DelayInicialMs * Math.Pow(BackoffMultiplicador, tentativa - 1);
        return (int)Math.Min(delay, DelayMaximoMs);
    }
}
```

### 5.6 Idempotência

#### 5.6.1 `IdempotencyKey` + `ExpiresAt`

Todo comando que altera estado deve carregar uma `IdempotencyKey` e um `IdempotencyExpiresAt`. A
plataforma garante que, dentro da janela de tempo explícita, um comando com a mesma chave para o
mesmo `EquipamentoId` não será executado duas vezes.

```sql
-- Constraint no banco
ALTER TABLE ComandosHardware
ADD CONSTRAINT UQ_ComandosHardware_Idempotency
UNIQUE (EquipamentoId, IdempotencyKey);
```

A janela é controlada em aplicação, não no banco:

```csharp
public class IdempotencyService
{
    public async Task<bool> JaExecutadoAsync(Guid equipamentoId, string key, DateTime expiresAt)
    {
        if (DateTime.UtcNow > expiresAt) return false; // chave expirou, pode reexecutar

        var existente = await _repository.BuscarAsync(equipamentoId, key);
        return existente != null && existente.Status != CommandStatus.Cancelado;
    }
}
```

#### 5.6.2 Por que não `DATE(CriadoEm)`?

Usar `DATE(CriadoEm)` como parte da constraint cria uma fronteira no meia-noite:

```
23:59 → executa
00:01 → mesma chave → executa novamente (porque mudou o dia)
```

Com `ExpiresAt`, a janela é explícita e contínua:

```
Chave gerada às 23:59, ExpiresAt = 24h depois
→ Só reexecuta após 23:59 do dia seguinte
```

#### 5.6.3 Geração da Chave

```csharp
// Opção 1: Cliente gera
IdempotencyKey = $"{equipamentoId}-{tipo}-{payloadHash}"

// Opção 2: Guid único para comandos únicos
IdempotencyKey = Guid.NewGuid().ToString("N")
```

#### 5.6.4 Comportamento de Duplicata

Se uma requisição com `IdempotencyKey` duplicada chegar:

1. Verifica se ainda está dentro de `ExpiresAt`.
2. Se sim, retorna o comando existente (seja qual for o status).
3. Se não, cria novo comando.
4. Nunca executa o comando novamente.

### 5.7 State Machine — Estados do Comando

```
[Pendente] → (Dispatcher pega) → [EmExecucao] → (Provider executa)
                                    ↓
                    ┌───────────────┼───────────────┐
                    ↓               ↓               ↓
              [Concluido]      [Erro]          [Timeout]
                                    ↓
                              (Cancelado pelo usuário)
```

#### 5.7.1 Estados

| Estado | Descrição |
|---|---|
| Pendente | Comando criado, aguardando dispatcher |
| EmExecucao | Dispatcher enviou ao provider, aguardando resposta |
| Concluido | Provider retornou sucesso |
| Erro | Provider retornou erro explícito |
| Timeout | Tempo máximo excedido, sem resposta do equipamento |
| Cancelado | Usuário cancelou antes da conclusão |

#### 5.7.2 Transições Permitidas

```csharp
public static class TransicoesPermitidas
{
    public static readonly Dictionary<CommandStatus, CommandStatus[]> Mapa = new()
    {
        [CommandStatus.Pendente] = new[] { CommandStatus.EmExecucao, CommandStatus.Cancelado },
        [CommandStatus.EmExecucao] = new[] { CommandStatus.Concluido, CommandStatus.Erro, CommandStatus.Timeout, CommandStatus.Cancelado },
        [CommandStatus.Concluido] = Array.Empty<CommandStatus>(), // terminal
        [CommandStatus.Erro] = new[] { CommandStatus.EmExecucao }, // retry manual permitido
        [CommandStatus.Timeout] = new[] { CommandStatus.EmExecucao }, // retry manual (se idempotente)
        [CommandStatus.Cancelado] = Array.Empty<CommandStatus>() // terminal
    };

    public static bool PodeTransicionar(CommandStatus atual, CommandStatus novo)
    {
        return Mapa.TryGetValue(atual, out var permitidos) && permitidos.Contains(novo);
    }
}
```

### 5.8 Execution Pipeline

O pipeline fica entre o Dispatcher e a fila, permitindo interceptores sem alterar o dispatcher.

```csharp
public interface IExecutionPipeline
{
    Task<HardwareCommand> ExecutarAsync(
        HardwareCommand command,
        Func<HardwareCommand, Task<HardwareCommand>> next,
        CancellationToken ct = default);
}
```

#### 5.8.1 Contrato formal do pipeline (Rev. 2 — incorporado do feedback ARB)

A Rev. 1 desenhava o pipeline mas não formalizava seu contrato de execução. Três regras passam a
valer, obrigatoriamente, para qualquer interceptor adicionado (presente ou futuro):

1. **Cada interceptor decide, explicitamente, continuar ou interromper.** Continuar significa
   chamar `next(command)` e propagar o resultado. Interromper significa **não** chamar `next` e
   retornar (ou lançar) uma resposta terminal própria — por exemplo, o `RateLimitInterceptor`
   lança `RateLimitExceededException` em vez de chamar `next`, o que impede qualquer interceptor
   posterior (incluindo o `CircuitBreaker`) de sequer ser invocado. Isso é comportamento
   pretendido, não um bug: um comando barrado por rate limit nunca deve contar como uma tentativa
   para fins de circuit breaker.
2. **A ordem dos interceptores é determinística e definida em um único lugar** (a composição do
   pipeline no startup/DI, nunca descoberta por reflexão ou por ordem de registro implícita). A
   ordem padrão desta ADR é: Autorização Extra → Rate Limiter → Circuit Breaker → Feature Flag →
   Cache de Status → `CommandQueue`. Qualquer novo interceptor deve declarar explicitamente onde
   entra nessa cadeia e por quê.
3. **Interceptores nunca executam lógica de provider.** Um interceptor pode inspecionar, negar,
   atrasar ou enriquecer um `HardwareCommand`, mas nunca chama `IHardwareProvider.ExecuteAsync`
   diretamente nem conhece protocolo de fabricante — isso mantém o pipeline agnóstico do mesmo
   jeito que o Dispatcher é, e evita que um interceptor vire, na prática, um segundo dispatcher
   paralelo e não auditado.

#### 5.8.2 Pipeline Padrão

```
Dispatcher
    ↓
[Autorização Extra] → verifica permissão granular
    ↓
[Rate Limiter] → limita comandos por equipamento/minuto
    ↓
[Circuit Breaker] → abre se provider falhar consecutivamente
    ↓
[Feature Flag] → permite desabilitar comandos em runtime
    ↓
[Cache de Status] → evita comando duplicado se status recente
    ↓
CommandQueue
    ↓
Provider
```

#### 5.8.3 Exemplo de Interceptor

```csharp
public class RateLimitInterceptor : IExecutionPipeline
{
    public async Task<HardwareCommand> ExecutarAsync(
        HardwareCommand command,
        Func<HardwareCommand, Task<HardwareCommand>> next,
        CancellationToken ct)
    {
        var chave = $"ratelimit:{command.EquipamentoId}:{command.Tipo}";
        var permitido = await _rateLimiter.AcquireAsync(chave, max: 5, window: TimeSpan.FromMinutes(1));

        if (!permitido)
            throw new RateLimitExceededException($"Limite de comandos excedido para {command.Tipo}");
        // Interrompe: `next` nunca é chamado. Circuit Breaker/Feature Flag/Cache não executam.

        return await next(command);
    }
}
```

### 5.9 Dispatcher

O `HardwareCommandDispatcher` é o orquestrador. Ele coordena:

1. Recebe o comando.
2. Valida RBAC (via `capability.Permissao`).
3. Resolve Provider via Factory.
4. Valida capacidades (`provider.GetCapability(tipo)`).
5. Verifica idempotência (`IdempotencyService`).
6. Persiste no banco como `Pendente`.
7. Envia pelo Execution Pipeline → Fila.
8. Aguarda execução pelo worker.
9. Atualiza estado via SignalR.
10. Registra auditoria.

```csharp
public interface IHardwareCommandDispatcher
{
    Task<HardwareCommand> EnviarAsync(HardwareCommand comando, CancellationToken ct = default);
    Task CancelarAsync(Guid comandoId, CancellationToken ct = default);
}
```

#### 5.9.1 Fluxo de Execução

```
Controller / Frontend
        ↓
POST /api/hardware/comandos
        ↓
HardwareCommandDispatcher.EnviarAsync()
        ↓
┌─────────────────────────────────────────┐
│  1. Valida RBAC                         │
│  2. Busca Provider via Factory          │
│  3. provider.GetCapability(tipo)        │
│  4. Verifica IdempotencyKey + ExpiresAt │
│  5. Persiste como Pendente              │
│  6. Execution Pipeline                  │
│  7. Publica na fila (CommandQueue)      │
└─────────────────────────────────────────┘
        ↓
CommandQueue (Background Worker)
        ↓
┌─────────────────────────────────────────┐
│  1. Dequeue próximo comando             │
│  2. Atualiza para EmExecucao            │
│  3. SignalR: CommandStarted             │
│  4. Provider.ExecuteAsync()             │
│  5. Aguarda resultado ou timeout        │
│  6. Atualiza estado final               │
│  7. SignalR: CommandFinished            │
│  8. Persiste auditoria                  │
└─────────────────────────────────────────┘
```

### 5.10 Provider

#### 5.10.1 Interface

```csharp
public interface IHardwareProvider
{
    ProviderType Tipo { get; }
    IReadOnlyCollection<HardwareCapability> Capabilities { get; }

    HardwareCapability GetCapability(HardwareCommandType commandType);
    bool Supports(HardwareCommandType commandType);

    Task<HardwareCommandResult> ExecuteAsync(
        HardwareCommand command,
        CancellationToken ct = default);
}
```

#### 5.10.2 Responsabilidades

- Declarar quais comandos suporta (via `Capabilities`).
- Responder `GetCapability(tipo)` sem busca linear na coleção.
- Deserializar o payload para o tipo correto.
- Traduzir o comando para o protocolo específico do fabricante.
- Executar a comunicação.
- Traduzir a resposta de volta para `HardwareCommandResult`.
- Nunca implementar retry — isso é da plataforma.
- Nunca implementar idempotência — isso é da plataforma.

#### 5.10.3 Implementação Base

```csharp
public abstract class HardwareProviderBase : IHardwareProvider
{
    private readonly Dictionary<HardwareCommandType, HardwareCapability> _capabilityMap;

    protected HardwareProviderBase(IEnumerable<HardwareCapability> capabilities)
    {
        _capabilityMap = capabilities.ToDictionary(c => c.Tipo);
    }

    public abstract ProviderType Tipo { get; }
    public IReadOnlyCollection<HardwareCapability> Capabilities => _capabilityMap.Values.ToList();

    public HardwareCapability GetCapability(HardwareCommandType commandType)
    {
        return _capabilityMap.TryGetValue(commandType, out var cap)
            ? cap
            : null;
    }

    public bool Supports(HardwareCommandType commandType) =>
        _capabilityMap.ContainsKey(commandType);
}
```

#### 5.10.4 `MockProvider` Evoluído

```csharp
public class MockProvider : HardwareProviderBase
{
    public override ProviderType Tipo => ProviderType.MOCK;

    public MockProvider() : base(new[]
    {
        new HardwareCapability
        {
            Tipo = HardwareCommandType.RESTART,
            Nome = "Reiniciar Equipamento",
            Categoria = "Diagnostico",
            SuportaRetry = false,
            RequerIdempotencia = true,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 30,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Diagnostico.Executar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.READ_STATUS,
            Nome = "Ler Status",
            Categoria = "Diagnostico",
            SuportaRetry = true,
            RequerIdempotencia = false,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 10,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Diagnostico.Visualizar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.SYNC_CLOCK,
            Nome = "Sincronizar Relógio",
            Categoria = "Diagnostico",
            SuportaRetry = false,
            RequerIdempotencia = true,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 15,
            TimeoutStrategy = TimeoutStrategy.Soft,
            Permissao = "Diagnostico.Executar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.OPEN_DOOR,
            Nome = "Abrir Porta",
            Categoria = "ControleAcesso",
            SuportaRetry = false,
            RequerIdempotencia = true,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 5,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Acesso.Executar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.ENROLL_FACE,
            Nome = "Cadastrar Face",
            Categoria = "ControleAcesso",
            SuportaRetry = false,
            RequerIdempotencia = true,
            SuportaAsync = true,
            TimeoutPadraoSegundos = 60,
            TimeoutStrategy = TimeoutStrategy.ProviderManaged,
            Permissao = "Acesso.Administrar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.READ_EVENTS,
            Nome = "Ler Eventos",
            Categoria = "Diagnostico",
            SuportaRetry = true,
            RequerIdempotencia = false,
            SuportaAsync = true,
            TimeoutPadraoSegundos = 30,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Diagnostico.Visualizar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.ARM_PARTITION,
            Nome = "Armar Partição",
            Categoria = "Alarme",
            SuportaRetry = false,
            RequerIdempotencia = true,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 10,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Alarme.Executar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.DISARM_PARTITION,
            Nome = "Desarmar Partição",
            Categoria = "Alarme",
            SuportaRetry = false,
            RequerIdempotencia = true,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 10,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Alarme.Executar"
        },
        new HardwareCapability
        {
            Tipo = HardwareCommandType.FIRMWARE_VERSION,
            Nome = "Versão de Firmware",
            Categoria = "Diagnostico",
            SuportaRetry = true,
            RequerIdempotencia = false,
            SuportaAsync = false,
            TimeoutPadraoSegundos = 10,
            TimeoutStrategy = TimeoutStrategy.Hard,
            Permissao = "Diagnostico.Visualizar"
        },
    })
    { }

    // Implementação simulada com delay e resposta controlada
}
```

Isso permite validar a plataforma contra equipamentos que ainda não existem no código.

### 5.11 Factory e Provider Resolver

A Factory não resolve provider diretamente do banco. Ela delega para um `IProviderResolver`.

```csharp
public interface IProviderResolver
{
    ProviderType Resolver(Guid equipamentoId);
}

public class BancoProviderResolver : IProviderResolver
{
    private readonly IEquipamentoRepository _repository;
    public ProviderType Resolver(Guid equipamentoId)
    {
        var equipamento = _repository.Obter(equipamentoId);
        return ProviderType.Parse(equipamento.ProviderCodigo);
    }
}

public class CacheProviderResolver : IProviderResolver
{
    private readonly IProviderResolver _inner;
    private readonly IMemoryCache _cache;

    public ProviderType Resolver(Guid equipamentoId)
    {
        return _cache.GetOrCreate($"provider:{equipamentoId}", entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            return _inner.Resolver(equipamentoId);
        });
    }
}
```

#### 5.11.2 Factory

```csharp
public interface IHardwareProviderFactory
{
    IHardwareProvider Criar(ProviderType providerType);
    IHardwareProvider CriarParaEquipamento(Guid equipamentoId);
}

public class HardwareProviderFactory : IHardwareProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProviderResolver _resolver;

    public IHardwareProvider Criar(ProviderType providerType)
    {
        return providerType switch
        {
            var t when t == ProviderType.JFL => _serviceProvider.GetRequiredService<JflProvider>(),
            var t when t == ProviderType.MOCK => _serviceProvider.GetRequiredService<MockProvider>(),
            _ => throw new ProviderNaoSuportadoException(providerType.Codigo)
        };
    }

    public IHardwareProvider CriarParaEquipamento(Guid equipamentoId)
    {
        var providerType = _resolver.Resolver(equipamentoId);
        return Criar(providerType);
    }
}
```

**Importante**: nenhum controller conhece o provider. O controller conhece apenas o
`IHardwareCommandDispatcher`.

### 5.12 Auditoria

#### 5.12.1 O que é registrado

Todo comando gera um registro de auditoria imutável:

```csharp
public sealed class AuditoriaComando
{
    public Guid Id { get; set; }
    public Guid ComandoId { get; set; }
    public Guid TenantId { get; set; }
    public Guid EquipamentoId { get; set; }
    public string Usuario { get; set; }
    public string IpOrigem { get; set; }
    public HardwareCommandType TipoComando { get; set; }
    public ProviderType Provider { get; set; }
    public CommandStatus StatusInicial { get; set; }
    public CommandStatus StatusFinal { get; set; }
    public string ResultadoJson { get; set; }
    public string Erro { get; set; }
    public long DuracaoMs { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public Guid CorrelationId { get; set; }
}
```

#### 5.12.2 Quando é registrado

- **Criação**: quando o comando é enfileirado.
- **Início**: quando o worker pega o comando.
- **Conclusão**: quando o provider retorna (sucesso ou erro).
- **Timeout**: quando o tempo máximo é excedido.

#### 5.12.3 Retenção

Auditoria é imutável e retida por 2 anos (configurável). Após isso, pode ser arquivada para cold
storage.

### 5.13 Observabilidade

#### 5.13.1 Métricas

| Métrica | Tipo | Descrição |
|---|---|---|
| hardware_comandos_total | Counter | Total de comandos por tipo e status |
| hardware_comandos_duracao | Histogram | Duração da execução por provider |
| hardware_comandos_fila | Gauge | Comandos pendentes na fila |
| hardware_provider_erros | Counter | Erros por provider e tipo de erro |
| hardware_pipeline_interceptors | Counter | Interceptores acionados no pipeline |
| hardware_comandos_recuperados | Counter | Comandos recuperados pelo `StalledCommandRecoveryHostedService` (§16.1, item 4) — todo incremento aqui é, por definição, um crash ou restart que deixou algo preso; alarme deve disparar em qualquer valor > 0, não só acima de um limiar |
| hardware_fila_tempo_permanencia | Histogram | Tempo entre `CriadoEm` e o worker efetivamente pegar o comando (`EmExecucao`) — detecta fila engargalada antes que vire timeout do usuário |

> Os dois últimos itens foram adicionados na Rev. 3 (ver seção 16.2) — sem eles, o
> `StalledCommandRecoveryHostedService` (obrigatório, §16.1 item 4) roda "às cegas": ele resolve o
> problema quando ele já aconteceu, mas ninguém é avisado de que aconteceu, nem consegue ver a
> fila degradando *antes* de um comando ficar preso.

#### 5.13.2 Logs Estruturados

Todo log de comando inclui: `CorrelationId`, `CommandId`, `EquipamentoId`, `Provider`,
`TipoComando`, `Status`, `DuracaoMs`.

> Nota de continuidade com a Sprint 22B: o padrão de log estruturado exigido aqui é o mesmo já
> validado no `CorrelationIdMiddleware`/`UsuarioLogadoEnrichmentMiddleware` (ADR 0031) — campos
> como parâmetros estruturados da mensagem, nunca dependendo apenas de `BeginScope` com um
> `Dictionary` (cujo estado não é impresso de forma legível pelo formatter de console simples do
> .NET). Qualquer log emitido pela plataforma de comandos deve seguir essa mesma disciplina.

#### 5.13.3 SignalR — Eventos

| Evento | Quando | Payload |
|---|---|---|
| CommandStarted | Worker inicia execução | `{ commandId, equipamentoId, tipo }` |
| CommandFinished | Execução concluída | `{ commandId, status, resultado, duracaoMs }` |
| CommandError | Erro na execução | `{ commandId, erro, podeRetry }` |
| CommandTimeout | Timeout atingido | `{ commandId, tentativas }` |

Fora do escopo desta ADR: `EquipmentOnline` / `EquipmentOffline` — isso pertence ao módulo de
Health Monitoring (Sprint 22F).

## 6. Schema do Banco

### 6.1 Tabela: `ComandosHardware`

```sql
CREATE TABLE ComandosHardware (
    Id UUID PRIMARY KEY,
    CorrelationId UUID NOT NULL,
    IdempotencyKey VARCHAR(255) NOT NULL,
    IdempotencyExpiresAt TIMESTAMP,
    TenantId UUID NOT NULL,
    EquipamentoId UUID NOT NULL,
    Provider VARCHAR(50) NOT NULL, -- "JFL", "MOCK", "HIKVISION"
    Tipo VARCHAR(100) NOT NULL,    -- "RESTART", "SYNC_CLOCK", "OPEN_DOOR"
    PayloadJson JSONB,
    Status VARCHAR(50) NOT NULL,   -- "Pendente", "EmExecucao", "Concluido", "Erro", "Timeout", "Cancelado"
    ResultadoJson JSONB,
    Erro TEXT,
    CriadoEm TIMESTAMP NOT NULL DEFAULT NOW(),
    CriadoPor VARCHAR(255) NOT NULL,
    IpOrigem VARCHAR(45) NOT NULL,
    IniciadoEm TIMESTAMP,
    FinalizadoEm TIMESTAMP,

    CONSTRAINT UQ_ComandosHardware_Idempotency
        UNIQUE (EquipamentoId, IdempotencyKey)
);

CREATE INDEX IDX_ComandosHardware_Status ON ComandosHardware(Status);
CREATE INDEX IDX_ComandosHardware_Equipamento ON ComandosHardware(EquipamentoId);
CREATE INDEX IDX_ComandosHardware_Correlation ON ComandosHardware(CorrelationId);
CREATE INDEX IDX_ComandosHardware_CriadoEm ON ComandosHardware(CriadoEm DESC);
CREATE INDEX IDX_ComandosHardware_ExpiresAt ON ComandosHardware(IdempotencyExpiresAt);
```

> **Nota de portabilidade**: o schema acima usa tipos PostgreSQL (`UUID`, `JSONB`). O AppMorador
> roda hoje sobre MySQL/Pomelo (ver ADR 0007/ADR 0008) — ao iniciar a Sprint 22C, este schema
> precisa ser traduzido para `CHAR(36)`/`char(36)` (padrão já usado em todas as entidades do
> projeto, ver `AppDbContextModelSnapshot.cs`) e `JSON` (tipo nativo do MySQL 8, sem o índice
> GIN que o `JSONB` do Postgres ofereceria) antes de virar uma migration real. Mantido como está
> nesta ADR por ser a forma como foi especificado e revisado pela ARB; a tradução de dialeto é
> tarefa de implementação da Sprint 22C, não uma decisão arquitetural nova.

### 6.2 Tabela: `AuditoriaComandos`

```sql
CREATE TABLE AuditoriaComandos (
    Id UUID PRIMARY KEY,
    ComandoId UUID NOT NULL REFERENCES ComandosHardware(Id),
    TenantId UUID NOT NULL,
    EquipamentoId UUID NOT NULL,
    Usuario VARCHAR(255) NOT NULL,
    IpOrigem VARCHAR(45) NOT NULL,
    TipoComando VARCHAR(100) NOT NULL,
    Provider VARCHAR(50) NOT NULL,
    StatusInicial VARCHAR(50) NOT NULL,
    StatusFinal VARCHAR(50) NOT NULL,
    ResultadoJson JSONB,
    Erro TEXT,
    DuracaoMs BIGINT,
    CriadoEm TIMESTAMP NOT NULL DEFAULT NOW(),
    FinalizadoEm TIMESTAMP,
    CorrelationId UUID NOT NULL
);

CREATE INDEX IDX_Auditoria_Tenant ON AuditoriaComandos(TenantId, CriadoEm DESC);
CREATE INDEX IDX_Auditoria_Equipamento ON AuditoriaComandos(EquipamentoId, CriadoEm DESC);
CREATE INDEX IDX_Auditoria_Correlation ON AuditoriaComandos(CorrelationId);
```

> Mesma nota de portabilidade da seção 6.1 se aplica aqui. Além disso: o AppMorador já tem uma
> trilha de auditoria genérica (`AuditoriaMaster`, ADR 0021) e uma trilha de negócio por domínio
> (padrão `HistoricoX`, ver ADR 0009). `AuditoriaComandos` é uma **terceira** trilha, específica
> de execução de comando de hardware — a Sprint 22C precisa decidir explicitamente se ela
   substitui, complementa ou se integra com `AuditoriaMaster` (ex.: um evento espelhado também
   ali) para não fragmentar "onde eu vejo o que aconteceu" em três lugares sem nenhuma relação
   documentada entre eles. Registrado aqui como pergunta em aberto para o início da Sprint 22C,
   não uma decisão desta ADR.

## 7. API

### 7.1 Endpoints

```
POST   /api/hardware/comandos              → Enviar comando
GET    /api/hardware/comandos/{id}         → Consultar comando por ID
GET    /api/hardware/comandos              → Listar comandos (paginado, filtros)
POST   /api/hardware/comandos/{id}/cancelar → Cancelar comando pendente
GET    /api/hardware/capacidades/{equipamentoId} → Listar capacidades do equipamento
```

### 7.2 Exemplo de Requisição

```json
POST /api/hardware/comandos
{
  "equipamentoId": "550e8400-e29b-41d4-a716-446655440000",
  "tipo": "SYNC_CLOCK",
  "payload": {
    "dataHora": "2026-07-29T22:00:00Z"
  },
  "idempotencyKey": "sync-550e8400-20260729",
  "idempotencyExpiresAt": "2026-07-30T22:50:00Z"
}
```

### 7.3 Exemplo de Resposta

```json
{
  "id": "660e8400-e29b-41d4-a716-446655440001",
  "correlationId": "770e8400-e29b-41d4-a716-446655440002",
  "status": "Pendente",
  "equipamentoId": "550e8400-e29b-41d4-a716-446655440000",
  "tipo": "SYNC_CLOCK",
  "provider": "JFL",
  "criadoEm": "2026-07-29T22:50:00Z",
  "criadoPor": "fernanda.oliveira@appmorador.local",
  "idempotencyExpiresAt": "2026-07-30T22:50:00Z"
}
```

## 8. Segurança (RBAC)

### 8.1 Permissões

| Permissão | Descrição |
|---|---|
| Diagnostico.Visualizar | Ver painel de diagnóstico e histórico |
| Diagnostico.Executar | Executar comandos de diagnóstico |
| Diagnostico.Administrar | Configurar políticas e ver logs completos |
| Acesso.Executar | Enviar comandos de abertura de porta |
| Acesso.Administrar | Cadastrar biometria, face, credenciais |
| Alarme.Executar | Armar/desarmar partições |

> Nota de integração com o RBAC já existente: o AppMorador modela permissões hoje via
> `PermissaoFuncionalidade` (12 valores, ADR 0025) + `RoleSistema`/`Policies` para papéis internos
> (ADR 0021). As permissões acima (`Diagnostico.Executar`, `Acesso.Administrar`, etc.) precisam,
> no início da Sprint 22C, ser mapeadas para esse vocabulário existente (novos valores de
> `PermissaoFuncionalidade`, ou novas `Policies`) em vez de introduzir um segundo sistema de
> permissões paralelo. Não é uma decisão desta ADR, é um ponto de integração a resolver na
> implementação.

### 8.2 Isolamento por Tenant

Todo comando é automaticamente filtrado por `TenantId`. Um usuário nunca vê ou executa comandos de
outro tenant.

> O AppMorador não usa o termo "Tenant" no domínio hoje (ver ADR 0031) — o equivalente mais
> próximo é `PropriedadeId` (isolamento por propriedade) combinado com `ProprietarioId` (isolamento
> por cliente). Esta ADR usa "Tenant" no sentido genérico de "fronteira de isolamento"; a
> implementação real na Sprint 22C deve mapear para `PropriedadeId`, não introduzir um conceito de
> Tenant novo e paralelo ao que já existe.

## 9. Relação com Outras ADRs

| ADR | Relação |
|---|---|
| ADR-0001 | Referenciada como "resolvida no caminho de transporte" para providers que necessitem. A plataforma de comando não depende dela para funcionar. |
| ADR-0031 | Estabelece o padrão de observabilidade (CorrelationId/RequestId/log estruturado) e o precedente arquitetural (Painel Web, Equipamentos/Provisionamentos) sobre o qual esta plataforma constrói. |
| ADR-0034 | Pré-requisito para providers que necessitem de conectividade remota (DVRs). A plataforma está pronta para recebê-los quando a conectividade estiver implementada. |

## 10. Consequências

### 10.1 Positivas

- ✅ Extensível: novo fabricante = novo provider, sem alterar a plataforma.
- ✅ Segura: retry inteligente, idempotência com janela explícita, auditoria completa.
- ✅ Testável: `MockProvider` evoluído valida a plataforma contra equipamentos futuros.
- ✅ Observável: métricas, logs estruturados, SignalR, pipeline interceptável.
- ✅ Desacoplada: controllers não conhecem protocolos, fabricantes, nem nomes de classes.
- ✅ Identificadores estáveis: `ProviderType` e `HardwareCommandType` sobrevivem a refatorações,
  com a imutabilidade do `Codigo` agora formalizada como regra de governança.
- ✅ Pipeline extensível: rate limit, circuit breaker, feature flags sem alterar dispatcher, com
  contrato de curto-circuito e ordem determinística agora explícitos.
- ✅ Serialização extensível: `IPayloadConverter` permite formatos não-JSON no futuro sem mudança
  estrutural no Registry.

### 10.2 Negativas / Trade-offs

- ⚠️ Complexidade inicial maior que um switch por fabricante.
- ⚠️ `PayloadJson` como string no banco perde queryabilidade nativa (mitigado por índices JSONB no
  PostgreSQL — a tradução para MySQL/JSON perde parte dessa mitigação, ver nota na seção 6.1).
- ⚠️ Idempotência requer disciplina do cliente para gerar chaves e `ExpiresAt` consistentes.
- ⚠️ SignalR adiciona infraestrutura de WebSockets (já presente no projeto desde a Sprint 14,
  ADR 0017 — reuso, não uma dependência nova).
- ⚠️ `ExecutionPipeline` adiciona uma camada de indireção que pode dificultar debugging inicial.

## 11. Alternativas Descartadas

| Alternativa | Por que descartada |
|---|---|
| Classe por comando | Explosão de classes, banco não uniforme, difícil manutenção |
| JSON totalmente livre | Sem validação, sem IntelliSense, parsing manual em cada provider |
| Retry uniforme para todos os comandos | Perigoso para comandos destrutivos (reiniciar, abrir porta) |
| `IHardwareProvider` com métodos fixos | Violação de Liskov, `NotSupportedException` em metade dos providers |
| Remover `MockProvider` | Perderíamos a capacidade de validar a plataforma antes de ter hardware real |
| `EquipmentOnline`/`Offline` nesta ADR | Mistura monitoramento contínuo com execução sob demanda. Health Monitoring é Sprint 22F |
| Strings como identificadores de provider/tipo | Envelhece mal, acopla a nomes de classe, problemas de casing e versionamento |
| Idempotência com `DATE(CriadoEm)` | Fronteira no meia-noite permite duplicação entre 23:59 e 00:01 |
| Factory resolver provider direto do banco | Acopla Factory a infraestrutura de persistência. `IProviderResolver` permite cache, config, feature flags |
| Assumir JSON implicitamente, sem metadado no Registry | Funciona hoje, mas exigiria mudança estrutural no Registry no dia em que o primeiro provider não-JSON aparecesse |
| Pipeline sem contrato formal de curto-circuito/ordem | Comportamento divergente e não-determinístico à medida que novos interceptores fossem adicionados por pessoas diferentes |

## 12. Validação do Contrato

Antes de aprovar esta ADR, o contrato de capacidades deve ser validado no papel contra três
naturezas de equipamento:

| Equipamento | Capacidades a Validar |
|---|---|
| Painel JFL | RESTART, SYNC_CLOCK, READ_STATUS, ARM_PARTITION, DISARM_PARTITION, READ_EVENTS |
| DVR Intelbras/Hikvision | RESTART, SYNC_CLOCK, READ_STATUS, FIRMWARE_VERSION |
| Control iD (futuro) | OPEN_DOOR, ENROLL_FACE, READ_EVENTS |

Se o `HardwareCapability` + `HardwareCommand` aguentam os três de mentira, aguentam de verdade.

## 13. Checklist de Aprovação

- [x] Identificadores estáveis (`ProviderType`, `HardwareCommandType`) em vez de strings
- [x] Regra de governança formal: `Codigo` é contrato persistente imutável, nome da constante não é (Rev. 2)
- [x] Schema do banco revisado (incluindo constraint de idempotência sem `DATE()`)
- [x] `IdempotencyExpiresAt` em vez de janela baseada em calendário
- [x] Contrato de capacidades validado contra 3 naturezas de equipamento
- [x] `SupportsAsync` e `TimeoutStrategy` nas capacidades
- [x] `ExecutionPipeline` documentado entre Dispatcher e Fila
- [x] Contrato formal do `ExecutionPipeline`: curto-circuito explícito, ordem determinística, interceptores nunca executam lógica de provider (Rev. 2)
- [x] `IProviderResolver` desacoplando Factory do banco
- [x] Registry como fonte única da verdade (Tipo → Capability → Payload → `PayloadConverter` → Handler) (Rev. 2)
- [x] `GetCapability(tipo)` no provider sem busca linear
- [x] Política de retry revisada por segurança
- [x] RBAC mapeado para todas as capacidades iniciais (com nota de integração ao vocabulário existente, ver seção 8.1)
- [x] `MockProvider` declara capacidades que o JFL ainda não usa
- [x] SignalR eventos definidos (sem `EquipmentOnline`/`Offline`)
- [x] Relação com ADR-0001, ADR-0031 e ADR-0034 documentada
- [x] Testes de aceitação definidos para Sprint 22C
- [x] Nota de arquitetura futura sobre `HardwareEvent` registrada, sem virar decisão desta ADR (Rev. 2, ver seção 15)

## 14. Glossário

| Termo | Significado |
|---|---|
| Provider | Implementação que traduz comandos genéricos para protocolo de um fabricante |
| Capability | Capacidade declarativa que descreve o que um equipamento pode fazer |
| Payload | Dados específicos de um tipo de comando (ex: `DoorId` para `OpenDoor`) |
| Envelope | Estrutura comum a todos os comandos (Id, Status, Auditoria, etc.) |
| Dispatcher | Orquestrador que recebe, valida, enfileira e acompanha comandos |
| Pipeline | Camada de interceptores entre Dispatcher e Fila (rate limit, circuit breaker, etc.) |
| Resolver | Abstração que resolve qual provider serve um equipamento (banco, cache, config) |
| Idempotência | Propriedade de um comando que garante que executar N vezes tem o mesmo efeito de executar 1 vez |
| CorrelationId | Identificador que liga todos os eventos de uma mesma operação |
| ProviderType | Identificador estável de um fabricante (`"JFL"`, `"MOCK"`, não nome de classe) |
| HardwareCommandType | Identificador estável de um tipo de comando (`"RESTART"`, `"SYNC_CLOCK"`) |
| PayloadConverter | Estratégia de serialização/desserialização de um payload, registrada por tipo de comando (Rev. 2) |
| HardwareEvent | Conceito **não implementado nesta ADR** — modelo paralelo a `HardwareCommand` para eventos assíncronos espontâneos (heartbeat, evento de alarme, notificação não solicitada). Ver seção 15 |

## 15. Nota de Arquitetura Futura — `HardwareEvent` (Rev. 2, não normativa)

Registrado aqui como observação, não como decisão: esta ADR modela o fluxo
**Comando → Fila → Provider → Resultado**, que é exatamente o que a Sprint 22C precisa.

Olhando para as Sprints seguintes (22D, 22E, 22F), é esperado que apareçam fluxos de natureza
diferente — eventos que **chegam** do equipamento sem que a plataforma tenha pedido nada:

- Evento espontâneo recebido de uma central JFL (o padrão já existente hoje, ver
  `JflFonteEventos`/ADR 0006/ADR 0015 — a central disca para o AppMorador, nunca o contrário).
- Notificação espontânea de um Control iD.
- Heartbeat/keepalive de conectividade.
- Alarme disparado sem nenhum comando ter sido enviado.

Forçar esses casos dentro de `HardwareCommand` (que representa uma operação **solicitada pela
plataforma**, com dispatcher, RBAC de execução, idempotência e retry) provavelmente exigiria
gambiarras — um "comando" que ninguém enviou, sem `CriadoPor` real, sem sentido de retry.

Quando esse momento chegar, a recomendação é introduzir um conceito paralelo,
`HardwareEvent` (nome provisório), com seu próprio ciclo de vida (recebido → classificado →
processado), reaproveitando o `ProviderType` e o vocabulário de auditoria/observabilidade já
estabelecidos aqui, mas **sem** reusar o `HardwareCommand`/`Dispatcher`/`ExecutionPipeline` para
um fluxo que é, por natureza, o oposto (entrada não solicitada, não saída solicitada).

Esta nota não bloqueia a Sprint 22C nem exige nenhuma mudança nesta ADR — é só o registro de uma
decisão que precisará ser tomada, formalmente, em uma ADR própria quando a primeira dessas
Sprints começar.

## 16. Escopo de Execução — Sprint 22C (Rev. 3)

Esta seção registra o resultado de uma revisão de prontidão de execução feita sobre uma
especificação de Sprint 22C (documento externo a este ADR, não versionado no repositório —
mission briefs de Sprint neste projeto vivem na conversa de planejamento, não como arquivo
próprio, ver Sprints 21/22A/22B). A revisão encontrou pontos onde aquela especificação divergia
do que esta própria ADR já define, e pontos onde ela precisava de detalhamento adicional para ser
executável sem ambiguidade. Nenhum item aqui muda a arquitetura decidida nas seções 1–15 — são
critérios de prontidão para o *início* da Sprint 22C, e requisitos que a especificação de
implementação da Sprint deve satisfazer.

**Veredito**: Aprovada com ajustes obrigatórios. Os itens da seção 16.1 são bloqueadores de
início; os da seção 16.2 devem estar na especificação de implementação, mas não impedem o
primeiro dia de trabalho; a seção 16.3 fica explicitamente fora do escopo da Sprint 22C.

### 16.1 Obrigatórios antes de iniciar a Sprint

1. **Campos do envelope não são opcionais.** `TenantId`, `IpOrigem` e `CorrelationId` já são
   parte de `HardwareCommand` (§5.2.1) e `AuditoriaComando` (§5.12.1) desde a Rev. 1 desta ADR —
   nunca deixaram de ser. Qualquer especificação de Sprint que os omita do schema real está
   divergindo da ADR, não propondo uma alternativa; a correção é alinhar a especificação à ADR,
   não decidir de novo se esses campos são necessários.

2. **`MockProvider` e os payloads tipados devem cobrir o mesmo conjunto de comandos.** O
   `MockProvider` (§5.10.4) declara 9 capacidades (`RESTART`, `READ_STATUS`, `SYNC_CLOCK`,
   `OPEN_DOOR`, `ENROLL_FACE`, `READ_EVENTS`, `ARM_PARTITION`, `DISARM_PARTITION`,
   `FIRMWARE_VERSION`). A Sprint 22C deve implementar os 9 payloads correspondentes
   (`RestartPayload`, `ReadStatusPayload`, `SyncClockPayload`, `OpenDoorPayload`,
   `EnrollFacePayload`, `ReadEventsPayload`, `ArmPartitionPayload`, `DisarmPartitionPayload`,
   `FirmwareVersionPayload`) — os três últimos não podem ficar de fora. Reduzir o `MockProvider`
   para bater com uma lista menor de payloads não é uma alternativa aceitável: a validação contra
   as 3 naturezas de equipamento (§12) depende de `ARM_PARTITION`/`DISARM_PARTITION` (painel JFL) e
   `FIRMWARE_VERSION` (DVR).

3. **Resolução de Provider reaproveita `Equipamento.Fabricante`, não uma coluna nova.** A
   especificação revisada propunha adicionar `ProviderCodigo VARCHAR(50)` à tabela `Equipamentos`
   com migration + seed dedicados. Isso é desnecessário e cria uma segunda fonte de verdade: o
   campo `Equipamento.Fabricante` (`FabricanteEquipamento` — `ControlId`/`Jfl`/`Intelbras`/
   `Hikvision`/`Dahua`/`Outro`, ver `backend/src/AppMorador.Domain/Entities/Equipamento.cs`) já
   existe desde a Sprint 11/12 (ADR 0014/0015) e já serve exatamente esse propósito — decidir qual
   integração trata o equipamento. O `BancoProviderResolver` (§5.11.1) deve mapear
   `FabricanteEquipamento → ProviderType` (`Jfl→JFL`, `ControlId→CONTROLID`,
   `Intelbras→INTELBRAS`; `Hikvision`/`Dahua`/`Outro` ainda sem provider real — resolvem para
   `MOCK` até esses fabricantes ganharem uma integração própria), em vez de ler uma coluna nova.
   **Nenhuma migration é necessária para este item.** Fica em aberto, para o início da
   implementação, apenas como um equipamento *já* provisionado com Fabricante real (não-Mock)
   entra ou não em modo mock durante o rollout gradual — sugestão: uma flag própria
   (`Equipamento.ExecutarComandosViaMock: bool`, default `true`) é mais honesta que forçar essa
   decisão dentro do campo `Fabricante`, que já tem um significado estabelecido (qual fabricante é
   o equipamento, não como ele deve ser operado nesta fase de rollout).

4. **Recovery de comandos presos em `EmExecucao`.** Obrigatório desde o primeiro deploy: um
   `StalledCommandRecoveryHostedService` (rodando a cada 5 minutos, configurável) que detecta
   comandos em `EmExecucao` há mais tempo que o `TimeoutPadraoSegundos` da capability correspondente
   e os transiciona para `Erro` (nunca para `Pendente` diretamente — reenfileirar automaticamente
   um comando que pode já ter sido executado no equipamento é o mesmo erro de retry cego que a
   seção 5.5 já proíbe; a decisão de tentar de novo é humana, exatamente como em qualquer outro
   timeout). Complementado pelo `StopAsync` cooperativo do item 16.2.1 — o recovery cobre crash
   (`SIGKILL`, queda de energia); o shutdown cooperativo cobre desligamento ordenado (deploy,
   `SIGTERM`). São mecanismos complementares, não alternativos.

5. **A fila é `Channel<T>` bounded, com capacidade e `FullMode` explícitos.**

   ```csharp
   Channel.CreateBounded<HardwareCommand>(new BoundedChannelOptions(1000)
   {
       FullMode = BoundedChannelFullMode.Wait,
       SingleReader = true,
       SingleWriter = false
   });
   ```

   Unbounded não é uma opção válida para a Sprint 22C: nada impede hoje que um `MockProvider`
   configurado para simular lentidão, combinado com um cliente que reenvia comandos, acumule
   milhares de itens em memória. `FullMode.Wait` aplica backpressure no `Dispatcher` (o `POST
   /api/hardware/comandos` bloqueia — dentro de um timeout HTTP razoável — em vez de aceitar
   trabalho ilimitado); a capacidade (1000, sugerida) é configurável por ambiente, não fixa no
   código.

### 16.2 Recomendados — devem estar na especificação de implementação

Estes itens não bloqueiam o início da Sprint, mas devem estar descritos na especificação de
implementação antes que o time comece a escrever código, para não virarem decisões *ad hoc*
tomadas isoladamente por quem implementar cada parte:

1. **Graceful shutdown cooperativo.** O `BackgroundService` do worker deve sobrescrever
   `StopAsync` para completar o `Channel` (`_channel.Writer.Complete()`) e aguardar o
   processamento em curso terminar antes de permitir que o host finalize — evita que um `SIGTERM`
   de deploy deixe um comando "executado no Mock mas nunca gravado no banco".

2. **`CriadoPor` resolvido por um `IUserContextService` único**, nunca inline em cada Controller:
   usuário autenticado → identidade do JWT (mesmo padrão de `User.GetUsuarioId()` já usado em todo
   o projeto, ver `ClaimsPrincipalExtensions`); chamada de sistema/agendamento →
   `"system"` literal, nunca um Guid vazio ou nulo (a auditoria precisa de um valor sempre
   presente e sempre distinguível de um usuário real).

3. **RBAC verificado uma única vez, dentro do `Dispatcher`, antes de persistir o comando** — nunca
   espalhado entre Controller e Pipeline. O Dispatcher resolve `capability.Permissao` (via
   `ICommandRegistry.GetCapability`) e chama `IAuthorizationService.AuthorizeAsync` do próprio
   ASP.NET Core (mesmo mecanismo já usado pelas `Policies` existentes, ver
   `AppMorador.Api/Auth/Policies.cs`) — nunca uma checagem de claim manual dentro de um
   Controller, mesma regra já estabelecida desde a Sprint 21 (ADR 0021). Falha de autorização aqui
   é `403 Forbidden`, nunca `401` (o usuário está autenticado; só não tem a permissão específica
   daquela capability) — e deve passar pelo mesmo handler central de auditoria de falha de
   autorização já existente (`AuditoriaAuthorizationMiddlewareResultHandler`), não um caminho
   próprio.

4. **Migração do módulo Diagnóstico (Sprint 22B) é uma tarefa explícita da Sprint 22C**, não um
   efeito colateral: o controller de Diagnóstico deixa de chamar o mock síncrono diretamente e
   passa a chamar `IHardwareCommandDispatcher.EnviarAsync`; qualquer endpoint de ação (hoje
   desabilitado/mock visual, ver ADR 0031 §10 do Sprint 22B) passa a devolver `202 Accepted` com o
   `commandId` (nunca `200 OK` com um resultado síncrono — a própria natureza assíncrona da
   plataforma exige isso); os testes de integração do Diagnóstico precisam validar as transições
   de estado do comando, não só o retorno imediato do endpoint.

5. **`HardwareCommandResult` faz parte do escopo de domínio da Sprint**, não é um detalhe de
   implementação de cada Provider — é o tipo de retorno do contrato `IHardwareProvider.ExecuteAsync`
   (§5.10.1), então precisa existir desde o primeiro Provider implementado (`MockProvider`):

   ```csharp
   public sealed class HardwareCommandResult
   {
       public bool Sucesso { get; set; }
       public string ResultadoJson { get; set; }
       public string Erro { get; set; }
       public long DuracaoMs { get; set; }
   }
   ```

6. **Cenários de teste de integração — lista fechada, não "fluxo completo".** A especificação de
   implementação deve enumerar, no mínimo, estes 10 cenários (cada um mapeado a um teste
   automatizado, não a verificação manual):

   | # | Cenário | Resultado esperado |
   |---|---|---|
   | 1 | Comando bem-sucedido | `Pendente → EmExecucao → Concluido` + evento SignalR + registro de auditoria |
   | 2 | Comando com erro do provider | `Pendente → EmExecucao → Erro` + evento SignalR + registro de auditoria |
   | 3 | Comando com timeout | `Pendente → EmExecucao → Timeout`, sem retry automático |
   | 4 | Idempotência — chave repetida dentro da janela | Retorna o comando existente, nunca executa de novo |
   | 5 | Idempotência — chave repetida após `IdempotencyExpiresAt` | Cria um novo comando |
   | 6 | Cancelamento | `Pendente → Cancelado`, nunca chega a `EmExecucao` |
   | 7 | Rate limit | 6º comando no mesmo minuto para o mesmo equipamento é rejeitado antes de chegar à fila |
   | 8 | RBAC negado | `403`, nunca `401`/`404`/`500` |
   | 9 | Capability não suportada pelo equipamento (ex.: `OPEN_DOOR` num painel JFL) | `400`, nunca `500` |
   | 10 | Worker crash + recovery | Comando em `EmExecucao` há mais que o timeout da capability é recuperado para `Erro` pelo `StalledCommandRecoveryHostedService` |

7. **Métricas de fila** — ver a tabela atualizada em §5.13.1 (`hardware_comandos_recuperados`,
   `hardware_fila_tempo_permanencia`), adicionadas nesta revisão.

### 16.3 Explicitamente fora do escopo da Sprint 22C

Frontend (tela de comandos em execução, histórico visual, integração SignalR no cliente) fica
inteiramente fora — vira uma Sprint própria (`22C.1`) depois que a plataforma estiver validada em
produção com o `MockProvider`. Misturar infraestrutura de backend com UI na mesma Sprint dificulta
isolar regressão e revisão; o precedente do projeto (ver Sprint 22A vs. 22B, ambas puramente
backend+frontend de um domínio já modelado, nunca backend novo + UI nova simultâneos num escopo
deste tamanho) já favorece essa separação.

## Histórico de Revisões

| Revisão | Data | Mudanças |
|---|---|---|
| Rev. 1 | 2026-07-29 | Versão inicial submetida à ARB |
| Rev. 2 | 2026-07-29 | Incorporado feedback ARB: (1) regra de governança formal para `ProviderType.Codigo`/`HardwareCommandType.Codigo` como contratos persistentes imutáveis (5.1.2); (2) `PayloadConverter` adicionado como metadado do Command Registry, com `JsonPayloadConverter` como implementação padrão (5.3); (3) contrato formal do `ExecutionPipeline` — curto-circuito explícito, ordem determinística, interceptores nunca executam lógica de provider (5.8.1); (4) nota de arquitetura futura sobre `HardwareEvent` como modelo separado para eventos assíncronos espontâneos, não normativa (seção 15). Adicionadas também notas de integração com o estado real do projeto: portabilidade PostgreSQL→MySQL do schema (seção 6), relação com trilhas de auditoria já existentes — `AuditoriaMaster`/`HistoricoX` (seção 6.2), mapeamento de RBAC para `PermissaoFuncionalidade`/`Policies` já existentes em vez de sistema paralelo (seção 8.1), e uso de "Tenant" como sinônimo de `PropriedadeId` (seção 8.2), já que o domínio do AppMorador não usa esse termo (ADR 0031). |
| Rev. 3 | 2026-07-29 | Nova seção 16 — critérios de prontidão de execução da Sprint 22C, resultado de uma revisão de prontidão: 5 itens obrigatórios antes de iniciar (campos do envelope já definidos na Rev. 1, alinhamento MockProvider↔payloads em 9 comandos, resolução de Provider via `Equipamento.Fabricante` já existente em vez de coluna nova, recovery de comandos presos, fila `Channel<T>` bounded), 7 itens recomendados para a especificação de implementação (graceful shutdown, `IUserContextService`, RBAC via `IAuthorizationService` dentro do Dispatcher, migração do módulo Diagnóstico da Sprint 22B, `HardwareCommandResult` como parte do domínio, 10 cenários de teste de integração fechados, métricas de fila), e frontend explicitamente fora de escopo (Sprint 22C.1). Métricas `hardware_comandos_recuperados`/`hardware_fila_tempo_permanencia` adicionadas à seção 5.13.1. |

---

Documento revisado em 29/07/2026. **Aprovado** pela ARB, sem bloqueadores arquiteturais. A Sprint
22C tem 5 ajustes obrigatórios de prontidão de execução antes de iniciar (seção 16.1) — nenhum
deles reabre a arquitetura decidida nas seções 1–15.
