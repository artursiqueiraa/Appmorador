# Relatório — Fase 2: Captura de Snapshot

Data: 2026-07-18
Status: **implementada e compilando. Migration regerada (não aplicada) — aguardando OK.**

## Arquivos criados

**Domain** (`AppMorador.Domain`):
- `Entities/Dvr.cs` — entidade `Dvr` + enum `DvrFabricante` (Intelbras/Dahua/Hikvision)
- `Entities/Camera.cs`
- `Entities/ZoneCameraLink.cs` — entidade de vínculo Zona↔Câmera (não FK cravada em Zone)
- `Snapshots/SnapshotRequest.cs` — Ip/Porta/Username/Password/Canal
- `Snapshots/SnapshotResult.cs` — só `Success`/`ImagePath`/`Error`
- `Snapshots/ISnapshotProvider.cs`
- `Snapshots/ISnapshotStorage.cs`

**Infrastructure** (`AppMorador.Infrastructure`):
- `Snapshots/CgiSnapshotProviderBase.cs` — lógica HTTP compartilhada (Digest+Basic, `cgi-bin/snapshot.cgi?channel=N`) para Dahua e Intelbras, que usam o mesmo endpoint
- `Snapshots/DahuaCgiSnapshotProvider.cs`
- `Snapshots/IntelbrasCgiSnapshotProvider.cs` (compatível — confirmado na Fase 0 que o Intelbras usa o mesmo CGI do Dahua)
- `Snapshots/HikvisionIsapiSnapshotProvider.cs` — `ISAPI/Streaming/channels/{canal}01/picture`, Digest sem fallback Basic
- `Snapshots/SnapshotStorageOptions.cs`
- `Snapshots/SnapshotStorage.cs` — disco local, `{BasePath}/{siteId}/{yyyy}/{MM}/{dd}/{guid}.jpg`, cria diretórios automaticamente
- `Snapshots/SnapshotCaptureService.cs` — orquestra Zona→ZoneCameraLink→Camera→Dvr→provider→storage
- `Snapshots/SnapshotServiceCollectionExtensions.cs` — `AddSnapshotCapture(...)`

## Arquivos alterados

- `Entities/Occurrence.cs` — campo novo `ImagePath` (nullable)
- `Infrastructure/Persistence/AppDbContext.cs` — `DbSet<Dvr>`, `DbSet<Camera>`, `DbSet<ZoneCameraLink>`; conversão de `Dvr.Fabricante` para string
- `Infrastructure/Jfl/AlarmEventProcessor.cs` — injeta `SnapshotCaptureService`; depois de criar e salvar a `Occurrence`, se `ResolutionStatus == Resolved`, chama a captura e atualiza `ImagePath` (com try/catch próprio, para uma falha aqui nunca reclassificar o resultado do evento nem afetar a Occurrence já salva)
- `Api/Program.cs` — registra `AddSnapshotCapture(...)`
- `Api/appsettings.json` — `Snapshots:BasePath`
- `Infrastructure/Persistence/Migrations/*` — regenerada do zero (a anterior nunca tinha sido aplicada)

## Fluxo implementado

```
Occurrence criada e salva (AlarmEventProcessor, inalterado)
  ↓ (só se ResolutionStatus == Resolved)
SnapshotCaptureService.CapturarAsync(siteId, zoneId, ...)
  → resolve ZoneCameraLink → Camera → Dvr
  → seleciona ISnapshotProvider por Dvr.Fabricante
  → provider.CaptureAsync (uma tentativa, timeout 5s, sem retry)
  → sucesso: SnapshotStorage.SaveAsync (disco local) → devolve path
  → Occurrence.ImagePath atualizado, salvo
  → falha (sem câmera, sem provider, timeout, HTTP não-2xx): log, ImagePath permanece null
Fim
```

Sem câmera vinculada ou zona não resolvida → nenhuma tentativa de captura, `ImagePath` fica `null`, a `Occurrence` não é afetada.

## Migration

Regerada do zero (a anterior, da Fase 1.2, nunca tinha sido aplicada). Conteúdo do `Up()`: **8 `CreateTable`** (`AlarmEventLogs`, `Sites`, `AlarmPanels`, `Dvrs`, `Zones`, `Cameras`, `Occurrences`, `ZoneCameraLinks`) **+ 11 `CreateIndex`**, zero operação destrutiva. `Occurrences` ganhou a coluna `ImagePath` (nullable). **Não aplicada** — aguardando OK e connection string real.

## Build

`dotnet build` da solução inteira: **0 erros, 0 avisos.**

## Confirmação: nenhuma funcionalidade extra

Implementado exatamente o pedido — `ISnapshotProvider`, `SnapshotRequest`, `SnapshotResult`, `SnapshotStorage` em disco, providers Dahua/Hikvision/Intelbras, fluxo Occurrence→câmera→JPEG→disco→`ImagePath`. Nada de vídeo, playback, clips, filas, `BackgroundService`, Polly/retry, notificações, SignalR ou Firebase. `AlarmEventProcessor` só ganhou a chamada mínima ao snapshot, sem alterar o fluxo de filtro/log/criação de ocorrência já existente.
