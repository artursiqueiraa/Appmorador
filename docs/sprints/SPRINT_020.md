# Sprint 20 — Visualização de Câmeras

## Missão

Tornar a aba "Câmeras" (Empty State desde a Sprint 16) funcional: lista de câmeras com status/
miniatura, detalhe com imagem ampliada, atualização sob demanda, tempo real via SignalR.
Streaming ao vivo fica para Sprint 21+.

## Fase 0 — Auditoria (verificada por leitura direta de código)

| Item | Existe? | Realidade |
|---|---|---|
| Entidade `Camera` | ✅ | `Id, PropriedadeId, GravadorId, Canal, Nome` — sem `Status`, sem timestamp, sem soft delete |
| `Gravador` (DVR/NVR) | ✅ | `Fabricante` (Intelbras/Dahua/Hikvision), `Ip/Porta/NomeAcesso/Senha` |
| Captura de snapshot real | ✅ | `SnapshotCaptureService` + providers CGI/ISAPI — chamadas HTTP reais |
| Captura sob demanda | ⚠️ | Só como efeito colateral de alarme numa Zona vinculada — sem resolução direta por CameraId |
| Servir imagem por HTTP | ❌ | Nenhum endpoint devolvia bytes de imagem |
| `GET /api/cameras`/`/snapshot`/`/status` | ❌ | Nenhum endpoint de câmera existia |
| Cadastro de Câmera | ❌ | Sem CRUD nem seed — impossível ter `Camera` sem inserir na mão |
| Detecção de movimento no gravador | ❌ | Providers só capturam snapshot; sem sinal de movimento |
| Aba Câmeras no mobile | ✅ | `CamerasScreen.tsx` 100% estática (Empty State genérico) |
| Biblioteca de imagem/cache no mobile | ❌ | Sem `expo-image` |

## Escopo entregue

1. **`Camera` evoluída** — `StatusCamera` (Desconhecido/Online/Offline), `UltimoSnapshotPath`
   (relativo), `UltimaTentativaCapturaUtc`/`UltimoSucessoCapturaUtc` (dois timestamps — sem o
   primeiro não dá para saber se a câmera falha há dias ou nunca foi tentada). Migration aditiva.
2. **Endpoints** — `GET /api/properties/{id}/cameras` (lista), `GET`/`POST /api/cameras/{id}/snapshot`
   (metadados vs. captura sob demanda, timeout de 15s), `GET /api/cameras/{id}/imagem` (bytes
   autenticados, content-type sniffado pela assinatura do arquivo), `GET /api/cameras/{id}/status`.
3. **`ISnapshotCaptureService`** (porta extraída em Domain) — `CapturarPorCameraIdAsync` novo,
   reaproveitando providers/storage já existentes, resolvendo a Câmera diretamente por Id
   (`ICameraResolver.ResolveByIdAsync`, novo) sem depender de Zona/alarme.
4. **Evento SignalR leve `CameraStatusAlterado`** — separado do Snapshot Operacional (câmera é
   exibição, não faz parte do cálculo de saúde), mesmo hub/grupo já existente.
5. **Seed** — 1 gravador + 3 câmeras de exemplo (Entrada/Sala/Fundos), 2 com imagem real
   (`PlaceholderImageGenerator`, PNG gerado em memória, sem dependência nova), idempotente por
   conta própria — backfilla mesmo num banco onde a conta Morador já existia (confirmado contra o
   banco de desenvolvimento real desta sessão).
6. **Mobile** — `expo-image`; `CamerasScreen` reescrita (grid 2 colunas, skeleton, pull-to-refresh,
   Empty State honesto); `DetalheCameraScreen` (imagem ampliada, "Atualizar imagem" com timeout/
   loading/aviso amigável); 4º context em `RealtimeContext` (`useRealtimeCamera`).
7. **Testes** — 17 novos no backend (`CameraServico`, seed), 15 novos no mobile (`cameraLabels`,
   `aplicarAtualizacaoCamera`, `useAuthHeader`).

## Achado arquitetural verificado antes de desenhar os endpoints

A entidade `Camera` e a captura de snapshot real já existiam desde antes da Sprint 1 (Fases 1-2
do projeto), mas serviam exclusivamente ao fluxo de "snapshot no disparo de alarme" — não havia
nenhum endpoint, nenhuma forma de servir a imagem de volta por HTTP, e nenhum status. A solução
não foi criar uma entidade nova — foi estender a existente com o mínimo necessário (4 campos) e
extrair uma porta (`ISnapshotCaptureService`) para reaproveitar a infraestrutura de captura já
validada, sem duplicar lógica.

## Fora de escopo confirmado

Streaming ao vivo (RTSP/HLS/WebRTC), PTZ, gravação de vídeo no mobile, cadastro/configuração de
câmera no mobile, detecção de movimento (nenhum gravador emite esse sinal — ver
`DIVIDA_TECNICA.md` item 43), novos fabricantes, analytics de visualização.
