# Relatório — Fase 1: Confiabilidade do Evento

Data: 2026-07-18
Status: **implementada e compilando. Migration gerada mas NÃO aplicada ao banco — aguardando sua confirmação explícita (ver seção 5).**

Esta é a primeira fatia de código real do projeto `appmorador` (a Fase 0 só tinha produzido
documentos/ADR). Objetivo desta fase, conforme pedido: garantir que **nenhum disparo da central
seja perdido** — ACK imediato ao painel, e `Occurrence` criada logo em seguida, sem esperar
snapshot/banco/qualquer I/O que não seja estritamente necessário.

---

## 1. O que foi criado (visão geral)

Como não havia nenhum código de backend ainda, esta fase também é o scaffold inicial da solução —
mas deliberadamente mínimo: só o que sustenta o fluxo "recebe evento → ACK → cria ocorrência", nada
de snapshot, câmera, storage ou notificação (isso é de fases seguintes, ainda não aprovadas).

```
backend/
  AppMorador.sln
  src/
    AppMorador.Domain/          — entidades puras (Site, AlarmPanel, Zone, Occurrence)
    AppMorador.Jfl/              — protocolo JFL portado (framing, sessão, handshake, keep-alive, evento)
    AppMorador.Infrastructure/   — EF Core + MySQL, EventoCommandHandler real
    AppMorador.Api/               — Program.cs, hosted service do listener TCP
```

Build validado: `dotnet build` na solução inteira — **0 erros, 0 avisos**.

---

## 2. Arquivos criados, por que cada um existe

### `AppMorador.Domain` (novo)

| Arquivo | Por quê |
|---|---|
| `Entities/Site.cs` | Instalação (residência/loja) — o tenant do produto. Mínimo (`Id`, `Nome`) porque nada além disso é necessário nesta fase. |
| `Entities/AlarmPanel.cs` | Central JFL cadastrada, com `NumeroSerie` — é a chave de correlação com a sessão TCP (handshake). |
| `Entities/Zone.cs` | Zona monitorada por uma central. |
| `Entities/Occurrence.cs` | **A peça central desta fase.** Ver decisão de design abaixo. |

**Decisão de design (a mais importante desta fase): `Occurrence` sempre grava os campos brutos do
evento (`NumeroSeriePainel`, `CodigoEvento`, `ZonaOuUsuario`, `Particao`), e os vínculos resolvidos
(`AlarmPanelId`, `SiteId`, `ZoneId`) são *nullable*.** Se a central que mandou o evento ainda não
está cadastrada no banco (nenhuma tela de cadastro existe ainda — isso é intencional, não esquecido,
ver seção 4), a `Occurrence` é criada mesmo assim, só com os FKs nulos. Isso é literal ao objetivo
da fase: nenhum disparo pode ser perdido, nem por central desconhecida, nem por zona não mapeada.

### `AppMorador.Jfl` (novo — protocolo portado de `Integra-o-FL-main/SDK/CentralHub.SDK`)

Portei o protocolo (não o listener ingênuo do `Teste-portaria-main1`, que só fazia
`.Contains("1130")` em string — ver a análise já feita no plano). Arquivos, todos adaptados de
`CentralHub.SDK.Jfl.*` para o namespace `AppMorador.Jfl.*`:

| Arquivo | Origem | Mudança em relação ao original |
|---|---|---|
| `Protocol/JflProtocol.cs`, `JflPacket.cs`, `ChecksumCalculator.cs`, `PacketParser.cs`, `PacketBuilder.cs`, `JflFrameReader.cs`, `JflProtocolException.cs` | Portados sem alteração de lógica (só namespace) | — |
| `Protocol/JflCommand.cs` | Portado, **trimmed** | Mantive só os 4 comandos usados nesta fase (`Conexao`, `ConexaoModulo`, `KeepAlive`, `Evento`) — os demais (status, armar, PGM, zonas) pertencem a fases não implementadas ainda |
| `Protocol/JflModel.cs` | Portado sem alteração | Tabela de modelos de equipamento, usada só para log legível |
| `Messages/JflText.cs`, `JflVersion.cs`, `ConnectionRequest.cs`, `ConnectionResponse.cs` | Portados sem alteração de lógica | Necessários para o handshake (0x21/0x2A) — sem ele nenhuma sessão fica ativa |
| `Messages/EventoRequest.cs` | **Novo — não existe em nenhuma fonte** | Parser do payload do comando 0x24 (`CONTA/EVENTO/PART/U-Z/CONTADOR/SPART/PROB`, 19 bytes), escrito a partir da especificação documentada em `Integra-o-FL-main/Documentation/Protocol/09_EVENTS.md` (lá só existe um handler stub, sem parser) |
| `Messages/EventoResponse.cs` | **Novo** | Monta o ACK (`OK + Contador ecoado`, 5 bytes) exigido pelo protocolo |
| `Server/JflSessionState.cs`, `JflServerOptions.cs`, `ICentralAuthorizationProvider.cs` | Portados, **trimmed** | Removi `LogHexAtivado`/`HexLoggingStream` (diagnóstico de homologação, não necessário nesta fase) |
| `Server/JflSession.cs` | Portado, **trimmed** | Removi `SendAndWaitAsync`/`NextSeq` do lado "servidor pergunta, central responde" (usado só por comandos como status/armar, que não existem nesta fase) |
| `Server/SessionManager.cs` | Portado sem alteração de lógica | Correlaciona sessão TCP por `NumeroSerie`, não por IP — necessário porque o painel pode estar atrás de NAT |
| `Server/JflTcpServer.cs` | Portado, **trimmed** | Removida a instrumentação de log extra de homologação e o `HexLoggingStream`; mantida a lógica central (accept loop, um `HandleClientAsync` por conexão, dispatch por comando) |
| `Server/Handlers/IJflCommandHandler.cs`, `JflCommandDispatcher.cs`, `ConnectionCommandHandler.cs`, `KeepAliveCommandHandler.cs` | Portados sem alteração de lógica | Handshake e keep-alive — pré-requisitos de protocolo, não features novas |
| `JflServiceCollectionExtensions.cs` | Portado, **fortemente trimmed** | Registra só sessão/dispatcher/handshake/keep-alive. Não registra nenhum handler de comando de negócio fora do evento (esse é registrado à parte, ver abaixo) — nenhum stub de status/armar/PGM/zona foi trazido, porque nenhuma dessas funcionalidades existe nesta fase |

**Não portado, deliberadamente**: `Diagnostics/*` (HexLoggingStream, PacketAnalyzer, ReplayEngine —
ferramental de homologação, não necessário para rodar), todos os `Server/Handlers/Stubs/*` exceto o
de evento (que virou implementação real, não stub), `CentralStatusQueryService`,
`PgmCommandService`, `ArmCommandService`, `ZoneInhibitCommandService` (comandos de armar/desarmar/
PGM/zonas — fora de escopo, o dispatcher já loga um aviso e ignora com segurança qualquer comando
sem handler registrado, então nada quebra por não portar esses).

### `AppMorador.Infrastructure` (novo)

| Arquivo | Por quê |
|---|---|
| `Persistence/AppDbContext.cs` | `DbContext` com os 4 `DbSet`s. FKs de `Occurrence` para `AlarmPanel`/`Site`/`Zone` configurados como `SetNull` no delete (nunca `Cascade`/`Restrict`) — uma ocorrência não pode deixar de existir por causa da remoção de um cadastro depois. |
| `Jfl/EventoCommandHandler.cs` | **O coração desta fase.** Implementa `IJflCommandHandler` para o comando 0x24. Vive aqui (não em `AppMorador.Jfl`) porque precisa do `AppDbContext` — a biblioteca de protocolo continua sem nenhuma dependência de banco/domínio. |

### `AppMorador.Api`

| Arquivo | Por quê |
|---|---|
| `Program.cs` | Reescrito (removido o template de `WeatherForecast`). Registra `AppDbContext` (MySQL via Pomelo), `AddJflServer(...)`, o `EventoCommandHandler` como `IJflCommandHandler`, e o hosted service do listener TCP. |
| `Hosting/JflServerHostedService.cs` | Sobe/derruba o `JflTcpServer` junto com o ciclo de vida do ASP.NET Core — portado quase sem alteração de `Integra-o-FL-main/Backend/CentralHub.Api/Services/JflServerHostedService.cs`. |
| `appsettings.json` | Adicionado `ConnectionStrings:DefaultConnection` (placeholder, senha `CHANGE_ME` — **precisa ser trocada antes de qualquer uso real**) e `Jfl:Porta`/`Jfl:IntervaloKeepAliveMinutos`. |
| `src/AppMorador.Infrastructure/Persistence/Migrations/*` | Migration `InitialCreate` gerada (ver seção 5) — **não aplicada**. |

Removidos do template padrão: `WeatherForecast.cs`, `Controllers/WeatherForecastController.cs`,
`AppMorador.Api.http` (referenciava o endpoint removido).

---

## 3. O fluxo implementado, exatamente como pedido

```
TCP: painel conecta → handshake 0x21/0x2A (ConnectionCommandHandler, ja existente, portado)
                                    ↓
Painel envia evento 0x24 (a qualquer momento)
                                    ↓
EventoCommandHandler.HandleAsync:
  1. EventoRequest.Parse(packet.Dados)         — validação mínima (tamanho do payload)
  2. session.ReplyAsync(... EventoResponse.BuildAck ...)   — ACK, ANTES de qualquer I/O de banco
  3. (fora do try/catch do ACK) tenta criar a Occurrence:
     - busca AlarmPanel por NumeroSerie (pode não achar — não impede a criação)
     - busca Zone por (AlarmPanelId, Numero) (idem)
     - grava Occurrence (campos brutos sempre, FKs quando resolvidos)
  4. se o passo 3 falhar (banco fora do ar etc.), loga erro — o ACK já foi enviado, o
     painel não vai reenviar o evento; a perda é só de persistência, não de protocolo
```

Verifiquei linha a linha que **não há nenhuma leitura/escrita de banco entre o recebimento do
pacote e o envio do ACK** — `EventoCommandHandler.HandleAsync` chama `session.ReplyAsync` antes de
qualquer `_scopeFactory.CreateScope()`.

## 4. O que foi deliberadamente NÃO implementado nesta fase

Por instrução explícita ("não implemente nenhuma funcionalidade nova"):

- **Nenhum filtro de código Contact ID.** Todo evento 0x24 recebido (arme, desarme, disparo, o que
  for) vira uma `Occurrence`. A classificação "isto é um disparo real vs. arme/desarme/teste" fica
  para quando o snapshot entrar em cena (é lá que faz diferença não gerar ruído) — decisão
  consciente, não esquecimento, e vai na direção mais segura possível ("nenhum disparo perdido"
  também significa não correr o risco de um filtro errado descartar um disparo real).
- **Nenhuma deduplicação/debounce.** Pelo mesmo motivo — dedup é sobre suprimir eventos "repetidos",
  o que tensiona com "nunca perder um disparo"; fica para quando houver de fato um motivo prático
  (evitar handshake ao DVR repetido), que não existe nesta fase.
- **Nenhuma tela/endpoint de cadastro de Site/AlarmPanel/Zone.** Para testar esta fase agora, os
  registros precisam ser inseridos manualmente no banco (uma linha em `Sites`, uma em `AlarmPanels`
  com o `NumeroSerie` real do painel de teste). Isso é uma lacuna operacional conhecida, não um bug.
- **Nenhum endpoint HTTP de leitura** (`GET /api/occurrences/...`). A verificação desta fase é por
  consulta direta ao banco. Endpoints de leitura ficam para quando fizerem falta de fato.
- **Nenhum comando de armar/desarmar/PGM/status.** O protocolo dá suporte (documentado no SDK de
  origem), mas não portei nenhum desses handlers — não fazem parte do objetivo desta fase.

## 5. Migration EF Core — gerada, NÃO aplicada

Rodei `dotnet ef migrations add InitialCreate` (não `dotnet ef database update`). O conteúdo
completo do `Up()` está abaixo — **é inteiramente aditivo: 4 `CreateTable` + 6 `CreateIndex`, zero
`DropColumn`/`DropTable`/`Truncate` ou qualquer outra operação destrutiva.** É a primeira migration
do projeto, então não há schema anterior para comparar/quebrar.

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterDatabase().Annotation("MySql:CharSet", "utf8mb4");

    migrationBuilder.CreateTable(name: "Sites", columns: table => new {
        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
        Nome = table.Column<string>(type: "longtext", nullable: false)
    }, constraints: table => { table.PrimaryKey("PK_Sites", x => x.Id); });

    migrationBuilder.CreateTable(name: "AlarmPanels", columns: table => new {
        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
        SiteId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
        NumeroSerie = table.Column<string>(type: "varchar(255)", nullable: false),
        Nome = table.Column<string>(type: "longtext", nullable: false)
    }, constraints: table => {
        table.PrimaryKey("PK_AlarmPanels", x => x.Id);
        table.ForeignKey(name: "FK_AlarmPanels_Sites_SiteId", column: x => x.SiteId,
            principalTable: "Sites", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
    });

    migrationBuilder.CreateTable(name: "Zones", columns: table => new {
        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
        AlarmPanelId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
        Numero = table.Column<string>(type: "varchar(255)", nullable: false),
        Nome = table.Column<string>(type: "longtext", nullable: false)
    }, constraints: table => {
        table.PrimaryKey("PK_Zones", x => x.Id);
        table.ForeignKey(name: "FK_Zones_AlarmPanels_AlarmPanelId", column: x => x.AlarmPanelId,
            principalTable: "AlarmPanels", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
    });

    migrationBuilder.CreateTable(name: "Occurrences", columns: table => new {
        Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
        NumeroSeriePainel = table.Column<string>(type: "longtext", nullable: false),
        CodigoEvento = table.Column<string>(type: "longtext", nullable: false),
        ZonaOuUsuario = table.Column<string>(type: "longtext", nullable: false),
        Particao = table.Column<string>(type: "longtext", nullable: false),
        CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
        AlarmPanelId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
        SiteId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
        ZoneId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
    }, constraints: table => {
        table.PrimaryKey("PK_Occurrences", x => x.Id);
        table.ForeignKey(name: "FK_Occurrences_AlarmPanels_AlarmPanelId", column: x => x.AlarmPanelId,
            principalTable: "AlarmPanels", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        table.ForeignKey(name: "FK_Occurrences_Sites_SiteId", column: x => x.SiteId,
            principalTable: "Sites", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
        table.ForeignKey(name: "FK_Occurrences_Zones_ZoneId", column: x => x.ZoneId,
            principalTable: "Zones", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
    });

    // + 6 CreateIndex: NumeroSerie (unique), SiteId em AlarmPanels,
    //   AlarmPanelId/SiteId/ZoneId em Occurrences, (AlarmPanelId,Numero) unique em Zones.
}
```

`Down()` apenas derruba as 4 tabelas na ordem inversa — normal para uma migration de rollback,
não é a operação que vai rodar.

**Confirmo: zero operação destrutiva. Nenhum `dotnet ef database update` foi executado — o banco
real não foi tocado.** Preciso do seu OK explícito antes de aplicar, e antes disso você também vai
precisar apontar `ConnectionStrings:DefaultConnection` (`appsettings.json`) para um MySQL real (hoje
é um placeholder com senha `CHANGE_ME`).

## 6. Documentação atualizada

- `docs/mvp-snapshot-alerta-plano.md` — status atualizado apontando para este relatório.
- Este arquivo (`docs/fase1-confiabilidade-relatorio.md`) — novo.

## 7. Pendências / próximos passos (não fazer sem seu OK)

1. Aplicar a migration `InitialCreate` (após você confirmar a connection string real).
2. Cadastro (manual ou via seed) de ao menos um `Site`/`AlarmPanel` para testar contra hardware
   real ou o simulador do `Integra-o-FL-main` (`CentralHub.Simulator`, que fala o mesmo protocolo
   0x7B — dá para apontá-lo para a porta deste servidor para testar o handshake+evento fim a fim).
2. Filtro de código Contact ID + dedup, snapshot, storage, notificação — fases seguintes, cada uma
   com sua própria aprovação.

---

**Fim da Fase 1. Parando aqui, aguardando sua revisão antes de continuar.**
