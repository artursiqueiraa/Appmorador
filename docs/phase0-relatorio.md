# Fase 0 — Relatório de Inventário e Plano (Segurança Conectada)

Data: 2026-07-18

Este documento é a entrega da Fase 0 do pivot: portaria virtual condominial → app self-service de
segurança para residências/pequenos lojistas. Não há código de produto ainda — apenas leitura das
três fontes de referência e planejamento.

---

## 1. Inventário por repositório

### 1.1 `Teste-portaria-main1` (Portaria Virtual)

- **Backend**: .NET 8, EF Core 8 + Pomelo MySQL, JWT Bearer (HMAC-SHA256, sem refresh token,
  revogação via `SecurityStamp` checado a cada request), SignalR embutido, BCrypt, ImageSharp.
  ~40 entidades EF, 35 controllers.
- **Frontend admin**: `frontend-v2`, React 19 + Vite + TS + TanStack Query + SignalR client +
  Tailwind (PWA). Há um `frontend/` legado em HTML/JS puro, aparentemente abandonado.
- **App do morador**: `mobile-app`, Expo ~57 + React Native 0.86 — **já é um app self-service**,
  o candidato mais próximo do produto B2C final.
- **Serviço de faciais**: camada `IDeviceProvider`/`DeviceProviderResolver` com implementações
  `ControlIdProvider` (delega para `ControlIdService`, fala API HTTP nativa do Control iD direto no
  IP local — `login.fcgi`, `create_objects.fcgi`, `user_set_image.fcgi`, etc., sem nuvem do
  fabricante) e `IntelbrasProvider` (CGI estilo Dahua, mais heurístico/frágil). Fila assíncrona de
  sincronização (`DeviceSyncQueue` + `DeviceSyncWorker`, roda a cada 3 min) desacopla cadastro do
  envio físico ao leitor.
- **Auth/roles**: papéis atuais Admin/Tecnico/Sindico/Morador + sub-role (Master/Operacional) e
  flag `Responsavel`. Existe uma **camada de capability** (`TenantContext`) que computa permissões a
  partir de claims em vez de checar strings de role espalhadas pelos controllers — é o padrão mais
  reaproveitável de toda a autorização.
- **SignalR**: um hub (`MonitoramentoHub`), grupos por tenant (`Condominio_{id}`,
  `GlobalMonitoramento`), evento principal `EventBusNovoEvento` publicado por um
  `IOperationalEventBus` maduro (classificação de criticidade configurável, dead-letter queue,
  auditoria, geração automática de Ocorrência). **Achado crítico**: o listener de alarme JFL
  (`JflTcpServerService`, porta 2036, Contact ID) já existe neste repo e já persiste
  Ocorrência/SystemAlert, mas **não está plugado no barramento de eventos** — hoje um disparo de
  zona não chega em tempo real via SignalR, só por polling. Esse é exatamente o gap que vira o
  "killer feature" do novo produto.
- **Modelo de domínio**: `Condominio › Apartamento › Resident`, `SystemUser` (conta de login,
  separada de `Resident`), `Device`, `AccessRule` (N:N Resident↔Device), `Credential`,
  `Ocorrencia`, `AuditLog` (automático, genérico via reflection, imutável), `ISoftDelete`. Já existe
  um modelo `JflCentral/JflZona/JflPgm/JflEvento` — é o esqueleto mais próximo do
  "Cliente › Zonas › Ocorrências" pedido.
- **Fluxo de captura facial (mobile)**: tela de instruções → câmera/galeria via
  `expo-image-picker` → preview/confirmação → upload → backend decide entre moderação humana
  (`FacialApprovalRequest`) ou liberação direta + fila de sync. É o fluxo mais próximo do produto
  final.
- **Risco observado**: `Program.cs` tem centenas de `ExecuteSqlRaw` idempotentes no boot em vez de
  migrations limpas, e há vários arquivos `.bak` ao lado dos originais — sinal de schema
  patchado, não de histórico de migration confiável. **Não replicar esse padrão no novo projeto.**

### 1.2 `monitoramento` (VMS Orchestrator)

- **Stack real**: Python + FastAPI + SQLAlchemy + SQLite (`vms.db`), OpenCV para streaming RTSP.
  Estrutura: `app/main.py`, `app/models.py`, `app/routers/{auth,devices,events,condominios,
  rondas,ocorrencias,logs,health,ws}.py`.
- **Modelo de dispositivo**: dois conceitos não unificados — `Dispositivo` (câmera IP via RTSP
  direto) e `Dvr` (o gravador, falado via HTTP/CGI ou ISAPI). **Não há FK entre eles** — um canal
  de câmera não referencia o DVR pai. Lacuna a resolver no modelo novo.
- **Conector Dahua/Intelbras (CGI)**: HTTP Digest (com detecção dinâmica Basic/Digest via header
  `WWW-Authenticate` em `dvr_monitor.py`, mas não em `health.py` — duplicado e inconsistente).
  Busca de gravação via `mediaFileFind.cgi` (`factory.create` → `findFile` → `findNextFile` →
  `close`/`destroy`), mas **só busca o primeiro resultado** (para estimar retenção), nunca lista
  ou baixa um clipe específico.
- **Conector Hikvision (ISAPI)**: HTTP Digest. Em produção usa `dailyDistribution` (varre mês a
  mês, só para saber quais dias têm gravação). Existe um teste manual (não integrado) do endpoint
  correto para busca fina, `PUT /ISAPI/ContentMgmt/search` (`CMSearchDescription`), mas sem
  parsing nem uso downstream.
- **Achado crítico**: **não existe, em nenhum dos dois conectores, uma etapa de download/playback
  de um trecho gravado.** O único vídeo que o sistema gera hoje vem de um buffer RTSP em memória ao
  vivo (`video_manager.py`, `CameraThread`, deque de 900 frames = 60s fixos, sem pós-roll, sem
  intervalo configurável). **O motor de "clipe por `[T-pré, T+pós]` puxado do DVR" pedido no pivot
  não tem implementação de referência — precisa ser desenhado do zero**, usando os endpoints de
  busca já mapeados (`mediaFileFind` Dahua, `ContentMgmt/search` Hikvision) como ponto de partida
  e adicionando a etapa de download que hoje não existe.
- **Storage/retenção**: clipes salvos em `static/videos/pre_{alarme_id}.mp4`, nomenclatura simples.
  **Sem nenhuma lógica de expiração/limpeza automática** (sem cron, sem TTL, sem worker de purge) —
  "grava e esquece". Precisa ser desenhado do zero.
- **Código reutilizável como padrão (não como classe pronta)**: nenhuma abstração
  `IDvrConnector`/factory por fabricante existe — é tudo `if/elif marca == ...` inline dentro de
  funções de rota. Ao portar para C#, a lógica de protocolo (parsing key=value do Dahua, XML do
  ISAPI, orquestração de sessão de busca) precisa ser extraída para uma camada nova
  (`IDvrConnector` + `DahuaCgiConnector`/`HikvisionIsapiConnector`), não há o que copiar 1:1.

### 1.3 `Integra-o-FL-main` (Central de alarme JFL)

- **Stack**: .NET 9, EF Core 9 + SQLite (ainda com `EnsureCreated()`, sem migrations formais).
  Solução com 9 projetos: `CentralHub.Api` (backend + hospeda o servidor TCP), `CentralHub.SDK`
  (protocolo JFL puro, sem dependência web), `CentralHub.Simulator` (simula o painel físico do
  lado cliente TCP, usado em testes de integração ponta a ponta), `CentralHub.StressTest`.
- **Protocolo**: binário proprietário JFL (cabeçalho `0x7B`), **não é Contact ID nem SIA no
  transporte** — Contact ID é reaproveitado só dentro do campo `EVENTO` do comando de evento
  (0x24). Framing: `CAB+QDE+SEQ+CMD+DADOS+checksum XOR`. Parsing/montagem isolados no SDK
  (`PacketParser`, `PacketBuilder`, `ChecksumCalculator`, `JflFrameReader`), roteamento por
  `JflCommandDispatcher`, correlação pergunta/resposta por byte de sequência
  (`JflSession.SendAndWaitAsync`).
- **Transporte — achado mais importante para a arquitetura geral**: **é o painel que abre a
  conexão TCP de saída para o servidor (push/report)** — confirmado no manual oficial da JFL, no
  código (`JflTcpServer` é um `TcpListener` que só aceita conexões, nunca disca para fora) e no
  simulador (que representa o painel como cliente TCP). Sessões são correlacionadas por número de
  série do equipamento (não por IP, porque o painel pode estar atrás de NAT/celular). **Isso
  significa que, para a central de alarme, não há necessidade de porta aberta no cliente nem de
  MikroTik** — o único requisito é o painel apontar (via configuração ActiveNet) para o
  IP:porta público do nosso servidor, e nosso servidor ter essa porta liberada de entrada.
  Existe vestígio de uma arquitetura antiga invertida (adapters em `SDK/Adapters/`, marcados
  `[Obsolete]`) que foi corrigida após auditoria — **não usar como referência**.
- **Parsing de evento de disparo de zona**: **especificado e documentado, mas não implementado —
  há só um stub** (`EventoCommandHandlerStub`). Formato do comando 0x24: conta, evento (Contact ID
  ASCII), partição, campo único usuário-ou-zona (o protocolo base não separa os dois — só a
  variante 0x7A, não suportada pelo painel testado, separaria), contador sequencial (não é
  timestamp — a hora vem separadamente do comando de status 0x4D), flag de problema.
- **Comandos suportados** (todos já implementados, sobre a mesma sessão TCP persistente):
  consultar status completo (0x4D), armar/desarmar (0x4E/0x4F), armar Stay/Away (0x53/0x54),
  acionar/desacionar PGM (0x50/0x51, mais um "pulso" composto), inibir zonas via bitmap completo
  (0x52 — substitui o conjunto, não soma). Dois comandos ainda são stub (`0x93` status leve,
  `0x37` comandos com senha).
- **Mapeamento zona → câmera**: **não existe** — nem no código nem no modelo de dados. Só há
  entidades `Building`, `Central`, `CentralSession`, `History`; não há `Zone` persistida (o estado
  de zona só existe efemeramente na resposta 0x4D). Precisa ser modelado do zero.

---

## 2. Mapa de reuso por módulo

| Módulo novo | De onde vem | Como reaproveitar |
|---|---|---|
| `Auth` | `Teste-portaria-main1` (JWT/BCrypt/rate-limit/lockout) + `TenantContext` (capability layer) | Migra quase direto; simplificar papéis para dono/membro/técnico; manter o padrão de capability em vez de checar role crua |
| `Sites` (Cliente) | Modelo `Condominio`/`Apartamento` simplificado para instância única | Adaptar: remove hierarquia Bloco/Apartamento, mantém 1 registro "Cliente" por instalação |
| `People` | `Resident`/`SystemUser` | Adaptar: remove campos de apartamento/onboarding condominial, mantém TipoMorador→papel simples |
| `FacialEnrollment` | `ControlIdService` + `FacialRegistrationScreen.tsx` + `MobileFacialController` + `FacialApprovalRequest` | Migra quase direto — é o módulo mais maduro e menos acoplado a condomínio de todo o código-fonte |
| `Devices` | `IDeviceProvider`/`DeviceProviderResolver` (facial) + `DeviceSyncQueue`/Worker + `DeviceMonitoringService` (saúde) | Migra quase direto para facial; para DVR, nova camada `IDvrConnector` (nada a portar 1:1 de `monitoramento`, é reescrita orientada pelos endpoints já mapeados) |
| `AlarmEvents` | `JflTcpServerService` (transporte) do `Teste-portaria-main1` **e** protocolo completo do `Integra-o-FL-main` (SDK) | O SDK do `Integra-o-FL-main` é mais maduro/testado que o listener do `Teste-portaria-main1` — adotar o SDK como base, implementar o handler de evento (0x24) que hoje é só stub, plugar no `IOperationalEventBus` (que já existe e é bom) |
| `Clips` | Nenhuma fonte tem o motor pronto | Novo, do zero: usar endpoints de busca já mapeados (`mediaFileFind` Dahua, `ContentMgmt/search` Hikvision) + adicionar download, atrás de `IDvrConnectivity` (ADR 0001) |
| `Storage` | Nenhuma fonte tem lifecycle/expiração | Novo, do zero (object store abstraction + signed URL + lifecycle) |
| `Notifications` | `INotificationService` (hoje mockado, só loga) + `PushToken` em SystemUser | Estrutura existe, implementação real (FCM/APNs) é nova |
| Barramento de eventos / real-time | `IOperationalEventBus` + `OperationalEvent` + `AlertRule` + `EventDlq` + `MonitoramentoHub` (SignalR) | Migra quase direto — é a peça de infraestrutura mais madura de todo o levantamento |
| Auditoria/Soft delete | `AppDbContext.OnBeforeSaveChanges` + `ISoftDelete` | Migra 1:1 (genérico via reflection, sem acoplamento a condomínio) |

---

## 3. Estrutura de pastas proposta (monorepo)

```
appmorador/
  backend/          # ASP.NET Core 8 (scaffold na Fase 1)
  mobile/           # React Native + Expo (app do cliente final)
  admin/            # Next.js (backoffice de instalação/config — fase posterior)
  docs/
    adr/            # Architecture Decision Records
    phase0-relatorio.md
```

Já criada nesta entrega (só a árvore, sem código): `backend/`, `mobile/`, `admin/`, `docs/adr/`.

---

## 4. Plano de fases

- **Fase 1**: Scaffold do backend (.NET 8 + EF Core + MySQL), modelo de site único
  (Cliente/Pessoa/Dispositivo/Zona/Ocorrencia), primeira migration (**mostrada e aprovada antes de
  aplicar**, por regra de gate). Porta de `Auth` + capability layer.
- **Fase 2**: `FacialEnrollment` — portar `ControlIdService`/`IDeviceProvider` + fila de sync;
  scaffold do app mobile (Expo) com a tela de enrollment adaptada.
- **Fase 3**: `AlarmEvents` — portar SDK do protocolo JFL (`Integra-o-FL-main`), implementar o
  handler de evento 0x24 (hoje stub), modelar `Zone`/`ZoneCameraMapping` (não existe em nenhuma
  fonte), plugar no barramento de eventos (`IOperationalEventBus`), expor comandos
  (armar/desarmar/bypass/status) como endpoints.
- **Fase 4**: `Devices`/live-view — abstração `IDvrConnectivity` (ADR 0001) com stub, conectores
  `IDvrConnector` CGI (Dahua/Intelbras) e ISAPI (Hikvision), live-view sob demanda.
- **Fase 5**: `Clips` — pipeline assíncrono completo (disparo → live-view → espera pós-roll →
  busca+download `[T-pré, T+pós]` → transcodifica → sobe no object store → notifica → expira),
  `Storage` com signed URL + lifecycle rule.
- **Fase 6**: `Notifications` (FCM/APNs real), polimento do app mobile, dashboard do cliente.
- **Fase posterior**: `admin` (Next.js) para instalação/configuração pela equipe técnica.

Cada fase termina com apresentação e aprovação antes de avançar, por regra de gate combinada.

---

## 5. ADRs iniciais

- [`0002-conectividade-dvr.md`](adr/0002-conectividade-dvr.md) — decisão em aberto, interface
  `IDvrConnectivity` com stub, sem hardcode da opção final (renumerado de `0001` para `0002` na
  consolidação de ADRs pré-Sprint-4, ver `docs/adr/README.md`).
