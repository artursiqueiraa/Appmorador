# Relatório — Sprint 20 (Visualização de Câmeras)

**Data de conclusão**: 2026-07-26

## Resumo executivo

A aba Câmeras, Empty State desde a Sprint 16, agora exibe uma lista real com status/miniatura,
abre um detalhe com imagem ampliada e permite atualizar a imagem sob demanda. A auditoria inicial
encontrou que a entidade `Camera` e a captura de snapshot real já existiam desde antes da Sprint
1 — mas serviam só ao fluxo de alarme, sem nenhum endpoint, sem forma de servir a imagem por
HTTP e sem status. A solução estendeu o que já existia (4 campos novos, uma porta extraída para
reaproveitar `SnapshotCaptureService`) em vez de duplicar infraestrutura.

## Auditoria de estado real (Fase 0)

Ver `docs/sprints/SPRINT_020.md` para a tabela completa. Resumo: entidade/captura de snapshot já
existiam mas desacopladas de qualquer endpoint; zero forma de servir imagem por HTTP; zero seed —
sem os passos desta Sprint, a Sprint seria impossível de validar mesmo em um banco de
desenvolvimento já em uso.

## Fluxo de visualização (documentado)

```
Morador abre o app → toque na aba Câmeras
  → GET /api/properties/{id}/cameras → grid 2 colunas com status/miniatura/timestamp honesto
  → toque numa câmera → GET /api/cameras/{id}/snapshot → imagem ampliada (ou "Nenhuma imagem
    disponível")
  → toque em "Atualizar imagem" → POST /api/cameras/{id}/snapshot (2-10s reais ao gravador,
    timeout de 15s) → nova imagem OU aviso amigável + última imagem disponível mantida na tela
  → SignalR: se o status mudar enquanto a tela está aberta, badge/miniatura atualizam sozinhos
```

## Componentes/arquivos criados

| Componente | Arquivo | Função |
|---|---|---|
| `StatusCamera` | `Domain/Entities/StatusCamera.cs` | Enum Desconhecido/Online/Offline |
| `ISnapshotCaptureService` | `Domain/Snapshots/` | Porta extraída para captura sob demanda por Id |
| `ICameraRepositorio`/`CameraServico` | `Domain/Repositories/`, `Application/Cameras/` | CRUD de leitura + orquestração de captura/status/imagem |
| `PlaceholderImageGenerator` | `Infrastructure/Persistence/Seed/` | PNG mínimo gerado em memória para o seed |
| `CamerasController` | `Api/Controllers/` | Lista/snapshot GET+POST/imagem/status |
| `CameraCard`/`SkeletonCameras`/`cameraLabels`/`aplicarAtualizacaoCamera` | `mobile/src/cameras/` | Grid item, skeleton, wording honesto, patch parcial SignalR |
| `DetalheCameraScreen` | `mobile/src/screens/cameras/` | Imagem ampliada + "Atualizar imagem" |
| `useAuthHeader` | `mobile/src/api/` | Header Bearer para `expo-image` (endpoint de imagem é autenticado) |

## Evidências dos testes

- `dotnet build` (solução inteira): 0 erros, 0 avisos novos (os 2 avisos pré-existentes do
  FirebaseAdmin SDK, Sprint 19, seguem aceitos por ora).
- `dotnet test` (`AppMorador.Tests`): **44/44 passando** — 17 novos desta Sprint
  (`CameraServicoTests`: ownership em todos os métodos, `Ok(null)`/204 sem imagem, sucesso marca
  Online e publica evento só quando o status muda, falha marca Offline com mensagem amigável
  nunca técnica, timeout interno nunca lança, content-type sniffado corretamente para JPEG e PNG;
  `DevelopmentSeederTests`: criação de 1 gravador/3 câmeras/1 vínculo, câmera Sala offline sem
  imagem mas vinculada à zona, idempotência).
- **Verificação adicional fora da suíte de testes**: `PlaceholderImageGenerator` decodificado com
  sucesso via `System.Drawing` (script descartável, fora do projeto) antes de entrar no seed —
  320×180, cor correta nos dois cantos. Seed executado contra o banco de desenvolvimento REAL
  desta sessão (não só InMemory) — confirmado por log (`"cameras de exemplo criadas..."`) e pela
  existência real dos 2 arquivos `.jpg` (conteúdo PNG, ver `DIVIDA_TECNICA.md` item 44) em disco.
- `npm run typecheck`/`npm run lint` (Mobile): 0 erros, 0 avisos.
- `npm test` (Mobile): **41/41 passando** (15 novos: `cameraLabels` — nunca "offline desde",
  sempre "última imagem há X"; `aplicarAtualizacaoCamera` — patch parcial só da câmera certa,
  preserva imagem quando o evento não traz uma nova; `useAuthHeader` — sem token o header fica
  `undefined`, nunca uma string quebrada).
- `npx expo-doctor`: 20/20 checks aprovados.
- **APK Preview (EAS)**: Build ID `cefa1886-1dc9-4108-aae6-f4bd370511aa` — ver
  `docs/testing/Sprint20.md` para o link e o resultado da homologação manual.
- **Validação em dispositivo físico Android real**: pendente do usuário — mesmo padrão já
  estabelecido desde as Sprints 15-19 (sem dispositivo físico conectado neste ambiente de
  execução). Checklist completo em `docs/testing/Sprint20.md`.

## Dívidas técnicas registradas

Itens 43-44 em `docs/DIVIDA_TECNICA.md`: detecção de movimento não implementada (nenhum gravador
emite esse sinal); arquivo de seed salvo com extensão `.jpg` contendo bytes PNG (cosmético,
inofensivo — content-type sempre sniffado pela assinatura real).

## Parecer do Reviewer — 8 Pilares (nomenclatura da própria missão)

| Pilar | Avaliação |
|---|---|
| **1. Funcionalidade** | ✅ Aprovado. Lista carrega câmeras reais (seed confirmado no banco de desenvolvimento real); imagens carregam via endpoint autenticado; status correto (Online/Offline/Desconhecido); captura sob demanda funciona (testado com sucesso e com falha). |
| **2. UX** | ✅ Aprovado. Zero termo técnico (sem RTSP/HLS/URL/IP/porta/codec/stream expostos); wording honesto testado automaticamente (nunca "offline desde", sempre "última imagem há X"); Empty State genérico substituído por um honesto. |
| **3. Performance** | ✅ Aprovado. Lista é uma única query por propriedade; cache de imagem via `expo-image` (`cachePolicy="disk"`) com cache-busting correto (querystring por timestamp da última captura). |
| **4. Tempo Real** | ✅ Aprovado. `CameraStatusAlterado` via SignalR atualiza badge/miniatura sem refresh manual, tanto na lista (patch parcial testado) quanto no detalhe. |
| **5. Resiliência** | ✅ Aprovado. Falha de captura nunca troca a tela por um erro — mantém a última imagem disponível com aviso amigável; timeout de 15s garante que a requisição do morador nunca fica pendurada; erro de rede na lista tem "Tentar novamente". |
| **6. Design** | ✅ Aprovado. Design Tokens aplicados (cores/espaçamento/bordas via `theme/tokens.ts`, nenhum hex literal novo); Skeleton nos dois carregamentos (lista e detalhe); Safe Area aplicada no detalhe; animação de entrada ≤300ms (Fade 250ms). |
| **7. Arquitetura** | ✅ Aprovado. Nenhum acoplamento a protocolo específico exposto na Api/Mobile (RTSP/HLS/WebRTC/ONVIF nunca aparecem); `ISnapshotCaptureService`/`ISnapshotProvider` já abstraem o fabricante — base pronta para streaming real (Sprint 21+) sem retrabalho, documentado na ADR 0024. |
| **8. Regressão** | ✅ Aprovado (verificado por inspeção + suítes de teste). `dotnet build`/`dotnet test` cobrem todo o backend (SignalR/Snapshot Operacional/Dashboard/etc. inalterados, só extensões aditivas); `npm test` cobre Auth/Push/client já existentes, todos passando; nenhum arquivo de tela fora de Câmeras foi alterado além de `RealtimeContext.tsx` (extensão aditiva, 4º context, mesma Regra 5 já validada na Sprint 18) e `RootNavigator.tsx`/`navigation/types.ts` (nova rota). |

**Conclusão**: Sprint aprovada nos 8 pilares por código/teste automatizado. Ressalva não-bloqueante
e já esperada (mesmo padrão desde a Sprint 15): validação sensorial em dispositivo físico real
(toque na câmera, qualidade visual da imagem, tempo de carregamento percebido) fica pendente da
homologação manual do usuário com o APK Preview — ver `docs/testing/Sprint20.md`.
