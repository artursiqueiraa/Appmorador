# Relatório — Fase 2.2: extração do ICameraResolver

Data: 2026-07-18
Status: **implementado e compilando. Nenhuma entidade/migration alterada.**

## Arquivos alterados

| Arquivo | Motivo |
|---|---|
| `AppMorador.Domain/Snapshots/ICameraResolver.cs` | **Novo.** Contrato `Task<Camera?> ResolveAsync(Guid zoneId, CancellationToken)` — única responsabilidade: Zona → ZoneCameraLink → Camera → Dvr (canal incluso via `Camera.Canal`). |
| `AppMorador.Infrastructure/Snapshots/CameraResolver.cs` | **Novo.** Implementação: a mesma consulta EF Core (`ZoneCameraLinks.Include(Camera).ThenInclude(Dvr).FirstOrDefault(ZoneId)`) que antes vivia dentro de `SnapshotCaptureService`, movida para cá sem nenhuma alteração de lógica. |
| `AppMorador.Infrastructure/Snapshots/SnapshotCaptureService.cs` | Não consulta mais o banco diretamente — perdeu a dependência de `AppDbContext`/`Microsoft.EntityFrameworkCore`, ganhou `ICameraResolver` injetado. `CapturarAsync` agora só: chama `_cameraResolver.ResolveAsync(zoneId, ct)`, monta o `SnapshotRequest` com o resultado, escolhe o provider por fabricante, captura, salva. Mensagens de erro e ordem de validação idênticas às anteriores. |
| `AppMorador.Infrastructure/Snapshots/SnapshotServiceCollectionExtensions.cs` | Registra `services.AddScoped<ICameraResolver, CameraResolver>()`. |

## Motivo

Separar a responsabilidade de "achar qual câmera/DVR atende a uma zona" (agora só em `ICameraResolver`/`CameraResolver`) da responsabilidade de "orquestrar a captura em si" (`SnapshotCaptureService`, que passa a só consumir o resultado já resolvido). Nenhuma consulta, mensagem de erro ou ordem de execução mudou — é puramente uma extração de classe.

## Build

`dotnet build` da solução inteira: **0 erros, 0 avisos.**
