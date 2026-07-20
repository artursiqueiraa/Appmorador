# Relatório — Fase 1.1: Revisão da Fase 1

Data: 2026-07-18
Status: **implementada e compilando. Migration regerada (ainda não aplicada) — aguardando seu OK.**

Esta fase corrigiu 6 pontos apontados na revisão da Fase 1. Nenhuma funcionalidade nova foi
adicionada além do que foi explicitamente pedido nos 6 itens — nada de snapshot, câmera, storage
ou notificação.

---

## 1. Arquivos modificados/criados, e por quê

### Novos (Domain)

| Arquivo | Motivo |
|---|---|
| `AppMorador.Domain/Entities/AlarmEventLog.cs` | Item 1. Entidade técnica de auditoria: grava **todo** evento 0x24 recebido, independente do resultado. Campos exatamente os pedidos: `Payload` (hex do campo Dados), `NumeroSerie`, `CodigoEvento`, `Zona`, `Timestamp`, `ResultadoProcessamento`. Nenhum código de negócio lê esta tabela — é só escrita, para diagnóstico. |
| `AppMorador.Domain/Entities/EventProcessingResult.cs` | Enum usado no campo `ResultadoProcessamento` de `AlarmEventLog` (`OcorrenciaCriada` / `IgnoradoPorFiltro` / `ErroAoProcessar`). Guardado como string no banco (ver seção 3) para ficar legível em consulta SQL direta — é uma tabela de suporte, não de negócio. |
| `AppMorador.Domain/Entities/ResolutionStatus.cs` | Item 3. Enum `Resolved`/`Unresolved`. |
| `AppMorador.Domain/ContactId/ContactIdClassifier.cs` | Item 2. O filtro Contact ID. Ver decisão de escopo abaixo — é intencionalmente conservador. |

### Modificados (Domain)

| Arquivo | Motivo |
|---|---|
| `AppMorador.Domain/Entities/Occurrence.cs` | Item 3: adicionado o campo `ResolutionStatus` (`required`, não nullable — toda ocorrência sabe dizer se resolveu painel/zona ou não). Nenhum outro campo mudou. |

### Novos/Modificados (Infrastructure)

| Arquivo | Motivo |
|---|---|
| `AppMorador.Infrastructure/Jfl/AlarmEventProcessor.cs` | **Novo.** Item 4 (indiretamente — ver explicação abaixo). Concentra toda a lógica de negócio que antes estava dentro do `EventoCommandHandler`: grava `AlarmEventLog` (sempre, no `finally`), aplica `ContactIdClassifier`, resolve `AlarmPanel`/`Zone`, cria `Occurrence` só quando aplicável, calcula `ResolutionStatus`. Registrado como `Scoped` (depende do `AppDbContext`). |
| `AppMorador.Infrastructure/Jfl/EventoCommandHandler.cs` | **Reescrito, bem mais enxuto.** Agora só faz: parse do payload, ACK imediato, e delega para `AlarmEventProcessor` (resolvido via `IServiceScopeFactory`, porque o handler é `Singleton` e o processor é `Scoped`). Zero regra de negócio própria. |
| `AppMorador.Infrastructure/Persistence/AppDbContext.cs` | `DbSet<AlarmEventLog>` adicionado; índices novos (`CreatedAtUtc`, `ZoneId+CreatedAtUtc` — item 5); conversão para string dos dois enums novos. |
| `AppMorador.Infrastructure/Persistence/Migrations/*` | Migration `InitialCreate` **regenerada do zero** (a anterior nunca tinha sido aplicada — ver seção 4). |

### `AppMorador.Api`

| Arquivo | Motivo |
|---|---|
| `Program.cs` | Registra `AlarmEventProcessor` como `Scoped`. |
| `Hosting/JflServerHostedService.cs` | **Não alterado.** Revisei linha a linha (item 4) — já continha só `Start()`/`StopAsync()` do `JflTcpServer` mais logging, nenhuma regra de negócio. Nada a corrigir aqui; a correção de fato necessária estava um nível abaixo (ver próxima seção). |

---

## 2. Item 4 em detalhe — onde a regra de negócio realmente estava

O `JflServerHostedService` (o `IHostedService`) **já estava correto** desde a Fase 1: ele só chama
`_server.Start()`/`_server.StopAsync()`. Ele nunca teve lógica de evento — quem processava o evento
era o `EventoCommandHandler`, que é um `IJflCommandHandler` (peça do protocolo, não do hosting).

O problema real — e o que interpreto como o espírito do item 4 ("todo processamento deve ocorrer em
serviços específicos") — era que o **próprio `EventoCommandHandler`** acumulava parse de protocolo
**e** lógica de negócio (consultar `AlarmPanel`/`Zone`, decidir criar `Occurrence`) na mesma classe.
Extraí toda a parte de negócio para `AlarmEventProcessor`. Agora:

- `EventoCommandHandler` = adaptador de protocolo (parse + ACK + delega).
- `AlarmEventProcessor` = serviço de negócio (filtro, log, resolução, criação de ocorrência).
- `JflServerHostedService` = só liga/desliga o servidor TCP.

Três responsabilidades, três classes.

---

## 3. Item 2 em detalhe — o filtro Contact ID, e por que é conservador

`ContactIdClassifier.EhDisparoDeZona(codigo)` hoje reconhece **só o código `"1130"`** como disparo
de zona. É o único código com confirmação documental própria no protocolo (`Integra-o-FL-main`,
`Documentation/Protocol/09_EVENTS.md`). Qualquer outro código — incluindo `1401`/`3401`
(desarme/arme) e os que o `Teste-portaria-main1` trata como bateria/falha de energia sem
confirmação contra hardware real — **não** passa no filtro: fica registrado em `AlarmEventLog` com
`ResultadoProcessamento = IgnoradoPorFiltro`, mas não vira `Occurrence`.

Isto é uma mudança de postura em relação ao que eu tinha decidido (e documentado) na Fase 1
original, onde propositalmente não filtrava nada ("fail-open") para não arriscar perder um disparo
por um filtro errado. A revisão pede explicitamente "Occurrence apenas quando aplicável", então
troquei para este filtro — mas mantive ele mínimo e estritamente baseado em evidência (só o único
código confirmado), em vez de inventar uma tabela Contact ID completa sem fonte. **Isto é uma
decisão meta assumida por mim, não uma instrução explícita sua sobre quais códigos usar — sinalizo
para você revisar.** O `HashSet` é o único lugar a editar quando mais códigos forem confirmados
(contra hardware real ou contra a tabela padrão Contact ID/Ademco, se você preferir adotá-la).

---

## 4. Item 5 — migration

A migration `InitialCreate` da Fase 1 **nunca tinha sido aplicada a nenhum banco** (confirmei isso
no relatório anterior). Como o modelo mudou bastante nesta revisão (nova tabela, novo campo, novos
índices), apaguei os arquivos da migration antiga e gerei uma `InitialCreate` nova do zero — mais
limpo do que empilhar uma segunda migration em cima de uma que nunca existiu de fato em produção.

Índices adicionados, exatamente os pedidos, nada além:
- `IX_Occurrences_CreatedAtUtc` (coluna única)
- `IX_Occurrences_ZoneId_CreatedAtUtc` (composto)

Conteúdo completo do `Up()` (a parte que importa para confirmar ausência de operação destrutiva):

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AlterDatabase().Annotation("MySql:CharSet", "utf8mb4");

    migrationBuilder.CreateTable(name: "AlarmEventLogs", columns: table => new {
        Id = table.Column<Guid>(...), Payload = table.Column<string>("longtext", ...),
        NumeroSerie = table.Column<string>(...), CodigoEvento = table.Column<string>(...),
        Zona = table.Column<string>(...), Timestamp = table.Column<DateTime>(...),
        ResultadoProcessamento = table.Column<string>(...)
    }, constraints: table => table.PrimaryKey("PK_AlarmEventLogs", x => x.Id));

    migrationBuilder.CreateTable(name: "Sites", ...);       // Id, Nome
    migrationBuilder.CreateTable(name: "AlarmPanels", ...); // Id, SiteId, NumeroSerie, Nome + FK->Sites (Cascade)
    migrationBuilder.CreateTable(name: "Zones", ...);       // Id, AlarmPanelId, Numero, Nome + FK->AlarmPanels (Cascade)

    migrationBuilder.CreateTable(name: "Occurrences", columns: table => new {
        Id, NumeroSeriePainel, CodigoEvento, ZonaOuUsuario, Particao, CreatedAtUtc,
        AlarmPanelId (nullable), SiteId (nullable), ZoneId (nullable),
        ResolutionStatus = table.Column<string>("longtext", ...)   // <- novo campo
    }, constraints: table => {
        // FKs para AlarmPanel/Site/Zone, todas com onDelete: SetNull (sem alteração da Fase 1)
    });

    // Indices:
    migrationBuilder.CreateIndex("IX_AlarmPanels_NumeroSerie", "AlarmPanels", "NumeroSerie", unique: true);
    migrationBuilder.CreateIndex("IX_AlarmPanels_SiteId", "AlarmPanels", "SiteId");
    migrationBuilder.CreateIndex("IX_Occurrences_AlarmPanelId", "Occurrences", "AlarmPanelId");
    migrationBuilder.CreateIndex("IX_Occurrences_CreatedAtUtc", "Occurrences", "CreatedAtUtc");           // <- novo (item 5)
    migrationBuilder.CreateIndex("IX_Occurrences_SiteId", "Occurrences", "SiteId");
    migrationBuilder.CreateIndex("IX_Occurrences_ZoneId_CreatedAtUtc", "Occurrences", new[]{"ZoneId","CreatedAtUtc"}); // <- novo (item 5)
    migrationBuilder.CreateIndex("IX_Zones_AlarmPanelId_Numero", "Zones", new[]{"AlarmPanelId","Numero"}, unique: true);
}
```

**Confirmo: 5 `CreateTable` + 7 `CreateIndex`, zero `DropColumn`/`DropTable`/`Truncate` ou qualquer
outra operação destrutiva.** `Down()` só derruba as 5 tabelas (rollback normal). **Nenhum
`dotnet ef database update` foi executado — o banco real continua intocado.** Aguardo seu OK
explícito antes de aplicar (e, como já registrado, a connection string ainda é um placeholder
`CHANGE_ME`).

`dotnet build` da solução inteira: **0 erros, 0 avisos.**

---

## 5. Impacto arquitetural

- **Separação de responsabilidades mais estrita**: protocolo (`EventoCommandHandler`) vs. negócio
  (`AlarmEventProcessor`) vs. hosting (`JflServerHostedService`) — três classes, cada uma com um só
  motivo para mudar.
- **`Occurrence` deixou de ser criada para todo evento** — agora depende do filtro Contact ID. O
  princípio "nenhum disparo pode ser perdido" da Fase 1 continua valendo, só que agora restrito ao
  universo de disparos que o filtro reconhece como reais; eventos filtrados não desaparecem, ficam
  em `AlarmEventLog`.
- **`AlarmEventLog` é uma tabela write-only do ponto de vista de regra de negócio** — só existe para
  quem for investigar depois (suporte, diagnóstico, auditoria). Nenhum serviço consulta essa tabela
  para decidir nada.
- **`ResolutionStatus`** deixa explícito, direto na `Occurrence`, quando um evento passou no filtro
  mas não achou painel/zona cadastrados — antes isso só dava para inferir olhando se `AlarmPanelId`
  era nulo.

## 6. Confirmação: nenhuma funcionalidade nova foi adicionada

Os 6 itens da revisão foram tratados estritamente como correções ao que já existia:
1. Nova tabela de auditoria — não influencia nenhum fluxo existente.
2. Filtro Contact ID — restringe o que a Fase 1 já fazia (criar Occurrence), não adiciona um recurso novo.
3. Campo novo em uma entidade já existente.
4. Refatoração de responsabilidade (nenhum comportamento observável novo).
5. Índices (não mudam comportamento, só desempenho de consulta).

Nada de snapshot, câmera, storage, notificação, endpoints HTTP ou telas foi tocado.

---

**Fim da Fase 1.1. Parando aqui, não avancei para Snapshot.**

---

## Adendo — Fase 1.2 (revisão pontual: catálogo Contact ID + código desconhecido)

Data: 2026-07-18. Duas correções pontuais, sem mexer em mais nada (migrations, entidades além do
necessário, ou fluxo geral).

### Arquivos alterados

| Arquivo | Motivo |
|---|---|
| `AppMorador.Domain/ContactId/ContactIdClassifier.cs` | **Removido.** Dependia de um `HashSet` fixo; substituído pelos dois arquivos abaixo. |
| `AppMorador.Domain/ContactId/ContactIdDefinition.cs` | **Novo.** `record ContactIdDefinition(Code, Description, GeneratesOccurrence)` — exatamente a forma pedida. |
| `AppMorador.Domain/ContactId/ContactIdCatalog.cs` | **Novo.** Substitui o `HashSet` por um dicionário de `ContactIdDefinition`, hoje só com `"1130"`. Adicionar um código novo é acrescentar uma entrada aqui — nenhuma outra classe muda. |
| `AppMorador.Domain/Entities/EventProcessingResult.cs` | Adicionado o valor `UnknownContactId` ao enum (entre `IgnoradoPorFiltro` e `ErroAoProcessar`). Como a coluna já é gravada como string (`HasConversion<string>`), isso **não muda o schema/migration**. |
| `AppMorador.Infrastructure/Jfl/AlarmEventProcessor.cs` | `ProcessarAsync` agora consulta `ContactIdCatalog.TryGet(...)`: código fora do catálogo → `EventProcessingResult.UnknownContactId` + `LogWarning` + nenhuma Occurrence; código catalogado com `GeneratesOccurrence=false` → `IgnoradoPorFiltro` (mesmo comportamento de antes, só reclassificado); código catalogado com `GeneratesOccurrence=true` → cria Occurrence, `OcorrenciaCriada`. `AlarmEventLog` continua sendo gravado sempre, em todos os casos. |

### Confirmação de build

`dotnet build` na solução inteira: **0 erros, 0 avisos.** Nenhuma migration foi gerada, alterada ou
aplicada nesta revisão — o campo `ResultadoProcessamento` já era `longtext`/string no banco, então o
novo valor de enum não exige mudança de schema.
