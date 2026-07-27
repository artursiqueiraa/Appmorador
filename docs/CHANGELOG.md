# Changelog

Registro cronológico das mudanças relevantes do projeto. Cada Sprint/Fase adiciona uma entrada.

## [Sprint 21 — RBAC Master (Base de Permissões da Plataforma)] — 2026-07-26

Objetivo: base de autorização para AppMorador + futuro Painel Web — papéis internos (Master/
Técnico/Suporte), Permissões Funcionais, Feature Flags por Propriedade, Capacidades Dinâmicas por
Equipamento, Provisionamento e Auditoria. Cliente (Administrador) mantém `ProprietarioId` como
fonte de verdade nesta Sprint — `UsuarioPropriedade` nasce como abstração preparatória para o
Multiusuário (Sprint futura dedicada), não como substituição. Ver `docs/audits/AUDIT_RBAC_021.md`
e ADRs 0021/0025/0026/0027/0028.

### Adicionado (backend)
- Domínio: `RoleSistema` (Master/Tecnico/Suporte, exclusivo de internos), `UsuarioPropriedade` +
  `PerfilPropriedade`, `PermissaoFuncionalidade` + `UsuarioPropriedadePermissao`, `FeatureFlag` +
  `PropriedadeFeatureFlag`, `EquipamentoCapacidade` + `ModeloEquipamento` (entidade própria,
  substitui `Equipamento.Modelo` texto) + `ModeloEquipamentoCapacidade`, `Provisionamento`
  (metadados — árvore de equipamentos vinculados fica para Sprint futura), `AuditoriaMaster` (sem
  FK para `Usuario`, por design — snapshot desnormalizado).
- Autorização: 7 Policies (`RequerMaster/Tecnico/Suporte/Interno/Cliente/Administrador/Morador`)
  via `RequireAssertion` centralizado em `Program.cs`; `IPermissaoService` (permissão funcional,
  feature flag, capacidade de equipamento); handler de auditoria automática de falha de
  autorização (`AuditoriaAuthorizationMiddlewareResultHandler`, decora o handler padrão do
  ASP.NET Core — nenhum Controller chama isso manualmente).
- Impersonation: `POST api/auth/impersonar`/`impersonar/encerrar` (Master/Suporte — Técnico
  deliberadamente sem essa capacidade), token de 15min sem refresh, 100% auditado.
- Endpoints novos: `usuarios-internos`, `modelos-equipamento` (+ `equipamentos/{id}/capacidades`
  client-facing, ownership-checked), `properties/{id}/provisionamentos`,
  `properties/{id}/features`, `properties/{id}/usuarios/{id}/permissoes`, `auditoria` — todos
  protegidos por Policy.
- Seed: conta Master padrão (`master@appmorador.local`); backfill de `UsuarioPropriedade` +
  Permissões do Plano Básico para propriedades pré-existentes.
- Migration `RbacMaster`: backfill de dados (`Equipamento.Modelo` texto → `ModeloEquipamento` FK)
  escrito manualmente antes do `DropColumn`, e correção do default de `Usuarios.Ativo` — ver
  `docs/ALTERACOES_BANCO.md`. Aplicada e verificada: 0 contas desativadas, 0 dado de Modelo
  perdido, 44/44 testes de backend existentes continuam passando.

### Adicionado (backend, testes)
- 43 novos testes automatizados (`Rbac/`, `Auditoria/`, `Equipamentos/`, `Provisionamentos/`,
  `Propriedades/`) — 87/87 testes de backend passando (44 pré-existentes + 43 novos), zero
  regressão.
- `GET /api/properties` enriquecido com `perfil`/`permissoes`/`features` por propriedade
  (`PropriedadeServico.ToDtoEnriquecidoAsync`) — único ponto pelo qual o cliente lê seu próprio
  RBAC, sempre ownership-checked (nunca expõe as rotas internas de Feature Flag/Permissão
  diretamente ao app).

### Adicionado (mobile)
- `usePermissao` (novo hook): única fonte de verdade no app para "o que este usuário pode fazer"/
  "o que esta propriedade contratou" — lê de `selectedProperty` (nunca do `perfil` local de
  `profilePreference.ts`, que é preferência de UI sem relação com este modelo).
- UI condicional em 5 telas: `MoradoresScreen`/`VisitantesScreen` (esconde "Adicionar" sem
  `CadastrarMorador`/`CriarVisitante`), `AccessScreen` (esconde Painel de Controle sem
  `AbrirPortao`), `CamerasScreen` (estado honesto "não contratadas" sem `FeatureFlag.Cameras` —
  aba continua sempre visível, preservando ADR 0019/Navegação Previsível), `CredenciaisScreen` +
  `TipoCredencialSelector` (esconde chips "Facial"/"Tag RFID" sem `CadastrarFacial`/`CadastrarTag`).
- 16 novos testes automatizados (`usePermissao.test.tsx`) — 57/57 testes de mobile passando (41
  pré-existentes + 16 novos), zero regressão. `typecheck`/`lint`/`expo-doctor` limpos.

### Pendente nesta Sprint
EAS Build Preview + homologação manual em dispositivo físico + parecer do Reviewer (9 pilares).
Painel Web fica fora de escopo (Sprint 22), por decisão explícita do usuário.

## [Sprint 20 — Visualização de Câmeras] — 2026-07-26

Objetivo: tornar a aba Câmeras funcional (existia desde a Sprint 16 só como Empty State). A
entidade `Camera`/captura de snapshot já existiam (Fases 1-2, pré-Sprint 1), mas só serviam ao
fluxo de snapshot-no-disparo-de-alarme — não havia endpoint, não havia como servir a imagem por
HTTP, e não havia seed para popular a propriedade de exemplo. Ver ADR 0024 e
`docs/reviews/SPRINT_020.md`.

### Adicionado
- **Backend**: `Camera` evoluída com `StatusCamera` (Desconhecido/Online/Offline) +
  `UltimoSnapshotPath`/`UltimaTentativaCapturaUtc`/`UltimoSucessoCapturaUtc`; endpoints
  `GET /api/properties/{id}/cameras`, `GET`/`POST /api/cameras/{id}/snapshot` (metadados vs.
  captura sob demanda), `GET /api/cameras/{id}/imagem` (bytes autenticados, content-type
  sniffado), `GET /api/cameras/{id}/status`.
- **Backend**: `ISnapshotCaptureService` extraída (porta em Domain) para permitir captura sob
  demanda por Id, sem depender de uma Zona/alarme; `ICameraResolver.ResolveByIdAsync` novo.
- **Backend**: evento SignalR leve `CameraStatusAlterado`, separado do Snapshot Operacional
  (câmera é exibição, não faz parte do cálculo de saúde operacional).
- **Backend**: seed de desenvolvimento — 1 gravador + 3 câmeras de exemplo (Entrada/Sala/Fundos),
  2 com imagem real gerada em memória (`PlaceholderImageGenerator`, PNG sem dependência nova),
  idempotente por conta própria (backfilla mesmo num banco onde a conta Morador já existia).
- **Backend**: 17 novos testes automatizados (`CameraServico`, seed).
- **Mobile**: `expo-image` instalado; aba Câmeras real (grid 2 colunas, skeleton, pull-to-refresh,
  Empty State honesto); `DetalheCameraScreen` (imagem ampliada, botão "Atualizar imagem" com
  timeout/loading/aviso amigável); 4º context em `RealtimeContext` (`useRealtimeCamera`) — status
  atualiza sozinho via SignalR.
- **Mobile**: 15 novos testes automatizados (`cameraLabels`, `aplicarAtualizacaoCamera`,
  `useAuthHeader`).
- **CLAUDE.md**: Regra de Validação em Dispositivo tornada permanente (5 etapas obrigatórias —
  Implementação → Testes Automatizados → APK Preview → Homologação Manual → Reviewer — para toda
  Sprint que altere o app mobile).

### Decisões conscientes de escopo
- Wording honesto: nunca "Offline desde X" (sem monitoramento contínuo, não sabemos o instante
  exato) — sempre "última imagem há X".
- Detecção de movimento não implementada — nenhum gravador emite esse sinal hoje.
- Imagem servida via endpoint autenticado (Bearer + checagem de posse), nunca static files
  públicos.
- `POST /snapshot` sempre 200 (nunca 202 "processando") — a captura é síncrona, sem canal
  assíncrono real por trás (mesmo racional do ADR 0022 Decisão 10 para comandos JFL).

## [Sprint 19 — Notificações Push] — 2026-07-26

Objetivo: complementar o tempo real da Sprint 18 (app aberto) com notificações push (app
fechado/em segundo plano). Auditoria inicial encontrou que o domínio não tinha granularidade
suficiente para 3 dos 8 eventos assumidos pela missão (visitante autorizado, entrega recebida,
equipamento offline nunca publicavam evento algum) — resolvido com hooks diretos nos Application
Services, sem alterar Domínio (exceto a nova entidade `DispositivoPush`). Ver ADR 0023 e
`docs/reviews/SPRINT_019.md`.

### Adicionado
- **Backend**: entidade `DispositivoPush` (multi-dispositivo por usuário, preferências de canal por
  dispositivo), `INotificationProvider`/`FirebaseNotificationProvider` (modo sem-op documentado sem
  credenciais Firebase reais), `NotificationDispatcher`/`NotificationService` (debounce de 60s em
  memória, mensagens fixas por tipo de evento), `DispositivoPushServico` + `DispositivosPushController`
  (registrar/atualizar token/atualizar preferências/desativar).
- **Backend**: hooks reais de disparo — alarme (`AlarmEventProcessor`), armar/desarmar/PGM
  (`JflComandoServico`), visitante autorizado (`AutorizacaoServico`, nunca publicava evento antes),
  entrega recebida (`EntregaServico`, idem), transição para offline de equipamento (nunca notifica
  o retorno online, por decisão explícita da missão).
- **Backend**: primeiro projeto de testes automatizados do repositório (`AppMorador.Tests`, xUnit +
  Moq) — 27 testes cobrindo dispatcher, service, ciclo de vida do token e debounce.
- **Mobile**: `expo-notifications` integrado — permissão solicitada só na transição sem-sessão →
  com-sessão (nunca insiste se negada), registro/atualização/remoção de token (token nativo FCM,
  não o proxy do Expo), 3 canais Android (Alertas/Atividades/Geral), supressão da notificação de
  sistema quando o app está em primeiro plano (SignalR/Toast já cobrem), deep link por `acao` com
  retry de prontidão do `NavigationContainer`, tela Ajustes → Notificações (toggles por canal,
  reativação via `Linking.openSettings()` quando negada).
- **Mobile**: `registerBeforeLogoutHook` em `AuthContext` (mesmo padrão de
  `registerSessionExpiredHandler`) — desregistra o dispositivo antes da sessão ser limpa no logout.
- **Mobile**: 22 novos testes automatizados (`pushService`, mapeamento de deep link).

### Decisões conscientes de escopo
- PGM notifica como "🔓 Comando acionado" (não "Portão aberto") — o backend genuinamente não
  conhece o rótulo amigável do PGM (preferência só local do Mobile, Sprint 17).
- Agrupamento de notificações (Fase 8.1) não implementado — só o limite de frequência (debounce).
- iOS não implementado/validado — sem certificado APNs/dispositivo físico neste ambiente.
- Preferências de canal exibidas na tela Ajustes são espelhadas localmente (sem `GET` novo no
  backend, não solicitado pela missão).

## [Sprint 18.1 — Hotfix: Correções Críticas de UX e Estabilidade] — 2026-07-26

Objetivo: corrigir bugs críticos encontrados na validação em dispositivo físico da Sprint 18.
Nenhuma funcionalidade nova, nenhuma alteração de backend. Ver `docs/reviews/BUG_DEBT_018_1.md`.

### Corrigido
- **Timeout de 15s em todas as requisições HTTP** (`api/client.ts`) — causa raiz confirmada de
  "propriedades não carregam" e "não sai da conta": `fetch` nunca tinha limite de tempo, então um
  backend inalcançável deixava a requisição pendurada para sempre.
- **Logout com limpeza local garantida** — `setUser(null)`/`setSelectedProperty(null)` agora
  rodam num `finally`, nunca dependendo da revogação no servidor ter sucesso; feedback visual
  (spinner) adicionado durante o processo.
- **Máquina de estados de carregamento em "Suas propriedades"** (`SelecionarPropriedadeScreen`) —
  Skeleton na primeira carga, erro com "Tentar novamente", nunca mais uma tela em branco.
- **`SafeAreaProvider` adicionado globalmente** (`App.tsx`) — antes só `BottomNavigation.tsx`
  respeitava a área segura do dispositivo; "Suas propriedades" (fora da Bottom Tab Bar) tinha um
  botão cortado pela barra de navegação do Android, agora corrigido com `useSafeAreaInsets()`.
- Cor hardcoded (`#062015`) removida de `PrimaryButton.tsx`, substituída pelo token `colors.bg`;
  opacidades de estado (`disabled`/`pressed`) trocadas pelos tokens já existentes.
- Espaçamento do formulário em "Suas propriedades" (colava no topo com a lista vazia); placeholder
  do endereço melhorado ("Rua, número, bairro, cidade").

### Adicionado
- **Infraestrutura de testes automatizados** (Jest + `jest-expo` + `@testing-library/react-native`)
  — não existia nenhuma antes desta Sprint. 4 testes cobrindo as duas causas-raiz corrigidas.

### Não reproduzido (investigado, sem correção especulativa aplicada)
- Overlay "DSW": nenhum código no repositório produz isso (busca extensiva por string, avatar/
  iniciais, cores hex, dependências de debug) — hipótese mais forte é um overlay de sistema/
  dispositivo, não um bug do app. Ver `docs/reviews/BUG_DEBT_018_1.md`.
- Texto "Tetd": sem lógica de truncamento no código; banco de dados não tem essa propriedade —
  provavelmente dado real digitado durante teste manual, não um bug de renderização.

## [Sprint 18 — Experiência em Tempo Real] — 2026-07-26

Objetivo: aproveitar a infraestrutura já existente (SignalR, Snapshot, Timeline) para o app
responder imediatamente a eventos importantes, sem refresh manual — zero alteração de domínio/API/
integrações/arquitetura de backend. Ver ADR 0022 e `docs/reviews/SPRINT_018.md`.

### Adicionado
- **RealtimeContext dividido em 3** (`useRealtimeConexao`/`useRealtimeSnapshot`/`useRealtimeEvento`)
  — cada consumidor só re-renderiza pelo que realmente usa.
- **Backoff exponencial customizado** de reconexão (1s/2s/5s/10s/30s, 5 tentativas) + estado
  `sem-comunicacao` com botão "Tentar novamente" manual.
- **`IndicadorConexaoRealtime`**: componente reutilizável de status de conexão, silencioso quando
  saudável.
- **Timeline com inserção real**: eventos novos entram no topo com Fade+Slide, selo "Novo" (5s),
  scroll nunca é puxado quando o usuário rolou para baixo (banner "N novos eventos · Ver novos"),
  cache máximo de 50 eventos.
- **Toast generalizado** (`erro`/`sucesso`/`info`/`alerta`, fila máx. 10) + `RealtimeToastBridge`:
  toast só aparece quando a tela em foco (rastreada via `navigationRef`/`telaAtivaStore`, sem
  biblioteca nova) ainda não torna o evento visível por si só.
- **Painel de Controle com máquina de estados** (Normal/Enviando/Sucesso/Falha, timeout de 10s) e
  status Online/Offline por equipamento atualizado ao vivo via Snapshot.
- **Pull-to-Refresh na aba Acessos** (não existia antes desta Sprint).
- **Telemetria de desenvolvimento** (`services/telemetria.ts`, `__DEV__` only).
- HeroCard/QuickAction/ActivityCard/ItemEvento/CommandCard memoizados (`React.memo`).

### Corrigido
- Troca de propriedade deixava o dado da propriedade anterior visível por um instante em
  `HomeScreen`/`AccessScreen` — corrigido com descarte explícito de cache no momento da troca.
- Timeline só fazia refetch completo da página 1 ao receber um evento (não uma inserção real) —
  corrigido.

### Achado arquitetural (documentado, não uma dívida técnica)
- `JflComandoServico.ExecutarComandoAsync` é síncrono — não existe canal de confirmação assíncrona
  via SignalR para comandos. A máquina de estados do Painel de Controle foi desenhada para refletir
  essa realidade (ver ADR 0022 Decisão 10).

## [Sprint 17.5 — Release 0.9.0, Backup, Portabilidade e Disaster Recovery] — 2026-07-25

Objetivo: transformar o AppMorador num projeto reproduzível, documentado, versionado e
recuperável — zero alteração funcional (domínio/API/integrações/UX intactos). Ver
`docs/reviews/SPRINT_017_5.md`.

### Adicionado
- **Auditoria de Ambiente** (`docs/AUDITORIA_AMBIENTE.md`), **Storage** (`docs/STORAGE.md`),
  **Environment** (`docs/ENVIRONMENT.md` + `backend/.env.example`), **Setup** (`docs/SETUP.md`),
  **Architecture** (`docs/ARCHITECTURE.md`), **Release 0.9.0** (`docs/RELEASE_0.9.0.md`),
  **Disaster Recovery** (`docs/DISASTER_RECOVERY.md`, com RTO/RPO e checklist pós-restauração).
- **Backup de banco** (`database/`: `schema.sql`, `seed_data.sql` versionados; dump completo
  anexado à Release).
- **7 scripts PowerShell** (`scripts/`): backup/restore de banco, setup/clean do projeto,
  start de backend/mobile/frontend (este último documenta a ausência de frontend web).
- Tag `v0.9.0` e Release do GitHub (commit único consolidando as Sprints 4-17, nunca commitadas
  desde `v0.3.0-alpha`).

### Corrigido
- **Bug crítico de versionamento**: `**/snapshots/` no `.gitignore` colidia (Windows,
  case-insensitive) com os namespaces de código `Snapshots/` de `AppMorador.Domain`/
  `AppMorador.Infrastructure` — 16 arquivos de integração de câmera nunca haviam sido
  versionados. Encontrado pela verificação de portabilidade desta própria Sprint (clone limpo
  falhava o build); corrigido e validado com um segundo clone limpo.
- `git config core.longpaths` ausente quebrava o checkout de migrations do EF Core em caminhos
  aninhados no Windows — corrigido em `scripts/setup_project.ps1`.
- Branch padrão do repositório no GitHub (`release/v0.3.0-alpha` → `main`).

### Pendências registradas
- Release do GitHub preparada (notas + asset) mas não criada automaticamente — bloqueio do
  classificador de segurança do ambiente de execução; comando entregue ao usuário.
- Restauração de banco ponta a ponta contra instância nova não validada nesta sessão (falta
  usuário MySQL privilegiado) — validação estrutural feita.

## [Sprint 17 — Refinamento da Experiência do Morador] — 2026-07-25

Objetivo: corrigir 7 fricções encontradas numa validação em dispositivo real sobre a Sprint 16 —
sem alterar backend, domínio, contratos de API, integrações, SignalR ou ADRs 0014-0019. Ver ADR
0020.

### Adicionado
- **Tratamento de erros centralizado**: `api/client.ts` sempre produz uma mensagem já amigável
  (`utils/errorMapper.ts` classifica sem internet/servidor indisponível/dispositivo não
  responde/não encontrado/vazamento técnico/mensagem de domínio) — toda tela existente ganha o
  benefício automaticamente, sem edição própria.
- **Toast** (`components/Toast.tsx`): feedback de falha de ação pontual (ex.: acionar um comando),
  com "Tentar novamente" quando a ação permitir.
- **Cadastro Facial completo**: anti-duplicação (modal Atualizar foto/Remover/Cancelar), captura
  real via `expo-image-picker` (câmera → galeria) com pré-visualização, credencial criada com
  aviso explícito de que a foto ainda não é persistida no backend.
- **Painel de Controle** (aba Acessos): comandos reais de PGM `permitida=true` de centrais JFL,
  com rótulo amigável configurável por equipamento, Empty State honesto quando vazio.
- **RBAC de UI** (`auth/profilePreference.ts`): preferência local `perfil` (morador/técnico,
  padrão morador) — `DetalhesEquipamentoScreen` e a seção "Proteção" de `MinhaPropriedadeScreen`
  mostram versão simplificada para morador, completa para técnico. Nunca uma fronteira de
  segurança real.
- **HeroCard**: indicador de conectividade ("Conectado"/"Última comunicação há X min"/"Sem
  comunicação desde HH:MM"), derivado de campos já reais via SignalR (Sprint 14).
- **Empty States com CTA** em 13 telas que ainda não tinham (Entregas, Permissões, Visitantes,
  Pontos de Acesso, Unidades, Veículos, Centrais JFL/Intelbras, Moradores, Credenciais,
  Equipamentos, Vagas, Autorizações, Saúde da Propriedade).
- **Ajustes reorganizado**: seções Propriedade/Conta/Legal + toggle discreto "Modo técnico".

### Corrigido
- Mensagens técnicas (`ex.Message` cru, "partição", "(offline)") não aparecem mais em nenhuma
  tela — confirmado por leitura direta das 3 fontes reais de mensagem (`ArmCommandService`,
  `ControlIdProvider`, `IntelbrasProvider`) antes de escrever o mapeamento.
- Cadastro de credencial facial duplicada — agora bloqueado no cliente antes de chamar a API.

### Dívida técnica registrada
- Foto de credencial facial não persistida no backend (item 32); `expo-camera` deliberadamente
  não instalado (item 33); perfil sem RBAC real (item 34); rótulos de PGM só locais (item 35);
  Painel de Controle só JFL (item 36). Ver `docs/DIVIDA_TECNICA.md`.

## [Sprint 16 — Implementação do Design System Mobile Oficial UX001] — 2026-07-25

Objetivo: transformar a experiência mobile de "sistema tecnicamente robusto" para "produto
centrado no usuário" (morador comum, 18-70 anos, baixo conhecimento técnico) — sem alterar
backend, domínio, contratos de API, integrações, SignalR ou ADRs 0014-0018. Ver ADR 0019.

### Adicionado
- **Bottom Navigation** de 4 abas (Início/Câmeras/Acessos/Ajustes), sempre visível — substitui a
  navegação anterior centrada no Dashboard.
- **HeroCard**: status único (Protegido/Atenção/Desarmado) derivado de campos já existentes do
  Dashboard (Sprint 13/14), com anel pulsante quando protegido.
- **11 componentes novos**: `HeroCard`, `QuickAction`, `SectionHeader`, `CameraCard`,
  `ActivityCard`, `StatusChip`, `PropertyCard`, `ProfileHeader`, `BottomNavigation`,
  `NotificationButton`, `DemoButton`. `EstadoVazio` (EmptyState) ganhou CTA acionável em toda
  lista vazia — nunca mais só "0"/"Nenhum item".
- **Design Tokens oficiais** (`colors`/`spacing`/`typography`/`borderRadius`/`animation`/`shadow`,
  arquivos dedicados) — mesma paleta já usada desde a Sprint 2 (confirmada idêntica ao protótipo
  UX001 fornecido), agora com os tipos oficiais lado a lado com os nomes de conveniência
  existentes, sem valor mágico inline em nenhum componente novo.
- **Onboarding Wizard persistente** (7 etapas, opcionais a partir da 4ª) — progresso salvo por
  Propriedade via `expo-secure-store`, recuperável a qualquer momento por 3 caminhos: card
  "Complete sua configuração" no Início, "Prefiro ser guiado" na seleção de propriedade, e
  "Continuar configuração guiada" em Ajustes → Minha Propriedade.
- **Minha Propriedade** (Ajustes): caminho permanente para configuração — nunca depende de
  logout/login.
- **AccessScreen** (Acessos): Moradores (agregados de todas as Unidades) + Visitantes, com
  atalhos para Portões/Vagas/Entregas.
- **AlertaDisparo**: alerta de disparo em tela cheia, com vocabulário traduzido para o morador
  comum.
- **Vocabulário do morador**: nenhum termo técnico (Intelbras/JFL/Snapshot/SignalR/Provider/
  Equipamentos/Eventos Operacionais/PGM/Zona/Partição) aparece em Início, Câmeras, Acessos,
  alertas ou notificações — exceção documentada em Ajustes → Minha Propriedade (ver ADR 0019
  Decisão 4).

### Corrigido
- **Bug real de navegação**: todas as telas de detalhe (Unidades, Credenciais, Centrais JFL/
  Intelbras, Equipamentos, Veículos, Vagas, Entregas...) só existiam no branch de navegação
  anterior à seleção de propriedade — depois de entrar no Dashboard, essas telas eram
  literalmente inalcançáveis. Corrigido movendo-as para o mesmo branch de `MainTabs`.
- **Onboarding que desaparecia sem forma de retomar** — agora persistente e recuperável (ver
  Onboarding Wizard acima).

### Removido
- `DashboardScreen.tsx` e 14 componentes exclusivos dele (`HeaderDashboard`, `AcoesRapidas`,
  `AtalhoEventos`, `RecursosFuturos`, `CardResumoInstalacao`, `CardControleAcesso`,
  `CardVisitantes`, `CardVeiculos`, `CardEntregas`, `CardEquipamentos`, `CardCentraisJfl`,
  `CardUltimaAtividade`, `CardSaude`, `CardSnapshotOperacional`) — substituídos por `HomeScreen`
  e os componentes novos.
- 9 ícones de atalho por propriedade em `SelecionarPropriedadeScreen` (Unidades/Pontos de Acesso/
  Visitantes/Vagas/Entregas/Equipamentos/Centrais JFL/Centrais Intelbras) — realocados para
  Ajustes → Minha Propriedade e Acessos.

### Dívida técnica
- Cartão de auto-cadastro facial (câmera + upload) do protótipo não implementado — sem
  `expo-camera` nem endpoint de upload de foto.
- Timeline de vídeo pré/pós-disparo do protótipo não implementada — backend só captura uma
  imagem por disparo (MVP da Fase 2), nunca um buffer contínuo.
- Botões de arme/desarme continuam visuais (mesma limitação desde a Sprint 2) — depende de uma
  decisão de produto sobre qual central é "a central da casa" quando há mais de uma.
- Nomes de fabricante (Control iD/JFL/Intelbras) continuam visíveis em Ajustes → Minha
  Propriedade — unificar exigiria mudança de backend, fora do escopo desta Sprint.

Ver detalhes completos no relatório de entrega da Sprint 16 e ADR 0019.

## [Sprint 15 — Integração Intelbras: Prova Definitiva da Arquitetura] — 2026-07-25

Objetivo: não integrar Intelbras por si só, mas provar que a arquitetura das Sprints 11-14 (ADR
0014/0015/0016/0017) é genuinamente genérica e extensível — um terceiro fabricante deveria se
integrar sem alterar nenhuma camada compartilhada. Auditoria (Fase 0) confirmou "nenhuma
alteração" em 9 das 10 camadas; a Central de Eventos ganhou só a adição prevista pelo próprio
mecanismo de extensão (novo valor de enum + nova fonte via DI). Um achado real de arquitetura foi
descoberto e corrigido genericamente durante a validação (ver "Corrigido" abaixo). Ver ADR 0018.

### Adicionado
- **`IIntelbrasProvider`/`IntelbrasProvider`** (Application/Infrastructure) — modelado como API
  HTTP local (mesmo padrão dial-out do Control iD, ADR 0014) com vocabulário de comando de
  central de alarme (Armar/Desarmar/Status/Eventos, ADR 0015) — decisão consciente por não haver
  documentação oficial pública nem uma referência já investigada neste projeto para um protocolo
  TCP proprietário Intelbras.
- **`IntelbrasComandoServico`/`CentraisIntelbrasController`**: testar conexão, consultar status,
  armar/desarmar por partição, importar eventos. Inteiramente paralelo a
  `EquipamentoIntegracaoServico`/`JflComandoServico` — zero linha alterada neles.
- **`IntelbrasFonteEventos`** (`IFonteEventos`): reaproveita `EventoEquipamento` (já genérico
  desde a Sprint 11) com semântica de Alarme (Origem=Intelbras, Categoria=Alarme) — prova que a
  Central de Eventos já suportava essa combinação sem mudança de contrato.
- `backend/tools/IntelbrasSimulator`: simulador HTTP descartável (fora do domínio de produção),
  usado para validar toda a comunicação via HTTP real.
- Mobile: telas `CentraisIntelbrasScreen`/`DetalhesCentralIntelbrasScreen` (cadastro Ip/Porta/
  Senha, ações de arme/desarme/status/eventos) — Dashboard/Timeline/Central Operacional/Saúde da
  Propriedade 100% reaproveitados, sem nenhuma alteração.

### Corrigido
- **Achado real de arquitetura**: `EquipamentoFonteEventos` nunca escopava sua query base por
  `Fabricante` — funcionava por coincidência enquanto Control iD era o único fabricante escrevendo
  em `EventoEquipamento`. A chegada de Intelbras (reaproveitando a mesma tabela, por design)
  expôs a lacuna: eventos apareciam duplicados (uma vez corretamente via `IntelbrasFonteEventos`,
  outra vez incorretamente rotulados como Origem=ControlId). Corrigido adicionando o filtro de
  Fabricante à query base — correção genérica, beneficia qualquer fabricante futuro que
  reaproveite `EventoEquipamento`, nunca uma adaptação específica de Intelbras. Ver ADR 0018
  Decisão 5.

### Dívida técnica
- Validação contra hardware Intelbras físico real pendente (mesma classe de Control iD/JFL).
- PGM/Inibição de Zona não implementados para Intelbras (marcados "se suportado").
- `EquipamentoIntegracaoServico.ResolverProvider` continua hardcoded a `IControlIdProvider?` — não
  exercitado por esta Sprint, registrado para um futuro segundo fabricante dial-out-com-
  sincronização.

Ver detalhes completos em `docs/reviews/SPRINT_015.md` e ADR 0018.

## [Sprint 14 — Tempo Real (SignalR)] — 2026-07-25

Objetivo: notificar automaticamente Dashboard/Mobile quando algo operacional mudar, eliminando a
dependência de atualização manual — reutilizando integralmente a Camada Operacional (Sprint 13,
ADR 0016), sem integrar fabricante novo, sem alterar o domínio, sem alterar protocolos existentes.
SignalR é exclusivamente transporte: nenhuma regra de negócio vive no Hub, nenhum Provider conhece
SignalR. Ver ADR 0017.

### Adicionado
- **`IOperacionalEventoPublicador`** (Application/Operacional): porta de publicação em tempo
  real — se nenhuma implementação real for registrada, o sistema continua funcionando
  integralmente, só sem notificação automática.
- **`SnapshotOperacionalServico.RegenerarEPublicarAsync`**: regenera o Snapshot (mesmo fluxo
  Estado Bruto → Classificador da Sprint 13) e publica, disparado por mutações já existentes em
  `EquipamentoIntegracaoServico` (Control iD), `JflComandoServico` (comandos JFL) e
  `AlarmEventProcessor` (o único gatilho verdadeiramente assíncrono — evento de alarme disparado
  por uma central via TCP, fora de qualquer requisição HTTP).
- **`OperacionalHub`/`OperacionalHubPublicador`** (`Api/Realtime/`): único ponto que conhece
  SignalR. Grupos exclusivamente por Propriedade (`propriedade:{id}`) — ownership validado ao
  entrar no grupo, mesma checagem de posse de todo Controller. Debounce simples em memória
  (750ms) contra publicações duplicadas em sequência rápida.
- Eventos publicados: `OperacionalAtualizado` (Snapshot completo + motivo) e
  `NovoEventoOperacional` (evento novo da Central de Eventos).
- JWT aceito via querystring (`?access_token=`) exclusivamente nas rotas `/hubs` (handshake
  WebSocket não permite header customizado); rate limiting no negotiate/conexão.
- Mobile: `RealtimeContext`/`RealtimeProvider` (`@microsoft/signalr`, reconexão automática) —
  Dashboard, Central Operacional, Saúde da Propriedade e Central de Eventos atualizam
  automaticamente; refresh manual/pull-to-refresh preservado como fallback independente.

### Corrigido
- Enums de negócio (`EstadoOperacional`, `FabricanteEquipamento`) chegavam como número cru no
  payload do SignalR — o protocolo JSON do Hub tem configuração de serialização própria,
  independente de `AddControllers().AddJsonOptions()` (ADR 0005). Corrigido com
  `AddSignalR().AddJsonProtocol(...)`.

### Dívida técnica
- Item 25: grupos SignalR por Perfil de usuário não implementados — domínio não tem RBAC (item
  6). Grupos por Propriedade (reais) implementados normalmente.
- Item 26: disparo assíncrono do alarme (`AlarmEventProcessor`) validado por revisão de código +
  mecanismo compartilhado já comprovado ao vivo, não por um evento TCP real de ponta a ponta
  neste ambiente (exigiria uma `Central` cadastrada, sem CRUD via API).

Ver detalhes completos em `docs/reviews/SPRINT_014.md` e ADR 0017.

## [Sprint 13 — Camada Operacional Unificada] — 2026-07-25

Objetivo: consolidar os dados já produzidos pelas integrações Control iD (Sprint 11) e JFL
(Sprint 12) numa camada operacional única e reutilizável — sem integrar novo fabricante, sem
novo protocolo, sem alterar regra de negócio já homologada. Nenhuma interface (Dashboard, Mobile,
API futura) pode consultar Providers diretamente a partir de agora. Ver ADR 0016.

### Adicionado
- **`EstadoOperacional`** (enum: Saudável/Atenção/Crítico/Offline) e **`SnapshotOperacional`**
  (Domain, rollup 1:1 por Propriedade, mesmo padrão upsert de `StatusCentralJfl`): data de
  geração, saúde consolidada, equipamentos online/offline, última comunicação, eventos hoje,
  alarmes ativos, falhas detectadas.
- **`IClassificadorOperacionalServico`/`ClassificadorOperacionalServico`** (Application/
  Operacional): único ponto que decide os 4 estados, por equipamento e por Propriedade (alarme
  ativo tem prioridade sobre qualquer outra condição).
- **`ISnapshotOperacionalServico`/`SnapshotOperacionalServico`**: `ObterAsync` gera o snapshot na
  primeira leitura (nunca chama um Provider — Estado Bruto vem só do que as Sprints 11/12 já
  persistem) e devolve o cache depois; `AtualizarAsync` sempre recalcula (ação explícita do
  usuário). A classificação por equipamento é sempre recomputada na leitura, mesmo com o rollup
  em cache.
- **`OperacionalController`**: `GET`/`POST .../operacional/snapshot(/atualizar)`.
- **Central de Eventos**: `FiltroEventos` ganha `EquipamentoId`/`Fabricante`/`Origem`/
  `Categoria`/`Severidade` — cada `IFonteEventos` decide por conta própria o que fazer com cada
  campo (mesmo desenho da Sprint 3). "Timeline Operacional" reaproveita esse mesmo endpoint —
  nenhum domínio de eventos novo foi criado.
- Dashboard: `Saude`, `QuantidadeEventosHoje`, `QuantidadeAlarmesAtivos`,
  `UltimaAtualizacaoOperacionalUtc` — aditivos, nenhum campo/componente existente removido.
  `QuantidadeEquipamentosOnline`/`Offline` agora vêm do Snapshot (eliminado o cálculo duplicado).
- Mobile: telas `CentralOperacionalScreen` (resumo + atualizar manualmente + atalhos) e
  `SaudePropriedadeScreen` (drill-down por equipamento); `CardSnapshotOperacional` novo no
  Dashboard; `utils/estadoOperacional.ts` centraliza rótulo/cor/emoji dos 4 estados.

### Migração de banco
- `CamadaOperacionalUnificada`: 100% aditiva — 1 tabela nova (`SnapshotsOperacionais`) + 1 índice
  único (`PropriedadeId`). Sem operação destrutiva (ver `ALTERACOES_BANCO.md`).

### Dívida técnica
- Item 24: eventos de transição de conectividade ("Equipamento Offline"/"Reconectado") não são
  registrados como evento auditável hoje (só sobrescrita de `Equipamento.Status`) — implementá-lo
  exigiria uma fonte de eventos nova, o que a missão desta Sprint proibiu explicitamente.

Ver detalhes completos em `docs/reviews/SPRINT_013.md` e ADR 0016.

## [Sprint 12 — Migração da Integração JFL Active 100 Bus] — 2026-07-22

Objetivo: migrar os comandos de superusuário JFL Active 100 Bus (Armar, Desarmar, Armar Stay/
Away, PGM, Inibir/Desinibir Zona, Consultar Status) da referência `Integra-o-FL` para a
arquitetura oficial de integrações do AppMorador (ADR 0014), completando uma migração que já
havia sido parcialmente feita antes das Sprints numeradas (handshake/keep-alive/recebimento de
eventos já existiam). Achado crítico da Fase 1: a missão descreveu a referência como tendo
"comunicação homologada com central real" para os comandos migrados — a própria documentação da
referência marca isso como validado só via simulador, hardware real pendente (só handshake/
keep-alive/status têm evidência real de hardware físico). Ver ADR 0015.

### Adicionado
- **Comandos JFL migrados** (`AppMorador.Jfl`): bytes `Status (0x4D)`, `Armar (0x4E)`,
  `Desarmar (0x4F)`, `AcionarPgm (0x50)`, `DesacionarPgm (0x51)`, `InibirZonas (0x52)`,
  `ArmarStay (0x53)`, `ArmarAway (0x54)`. Parsers da resposta "tela monitorar" (seção 4.10):
  `CentralStatusResponse` + sub-parsers (partições, zonas, PGMs, bateria, eletrificador,
  problemas). `JflSession.SendAndWaitAsync` ativado (mecanismo de correlação servidor-inicia-
  comando que já existia como scaffolding dormente).
- **`IJflProvider`/`JflProvider`** — único ponto que conhece o protocolo JFL de superusuário.
  Diferente do Control iD (Sprint 11), nunca disca para o equipamento: localiza a sessão TCP já
  aberta pela central (via `SessionManager`, existente desde a Fase 1) e envia o comando dentro
  dela.
- **`JflComandoServico`**: testar conexão, consultar status, armar/desarmar (com Stay/Away),
  acionar/desligar PGM, inibir/desinibir zona (consulta o estado atual e reenvia o conjunto
  completo — o protocolo substitui, não soma). Auto-vínculo de leitura entre o Equipamento
  (Fabricante=Jfl) e a `Central` já usada pelo pipeline de eventos, por Número de Série.
- **`StatusCentralJfl`** (nova entidade, rollup 1:1 com Equipamento): partições armadas/
  desarmadas e problema ativo, persistido só por ação explícita do usuário — usado pelo
  Dashboard.
- `Equipamento.Ip/Porta/Usuario/SenhaCriptografada` agora opcionais (Domain) — JFL não usa
  nenhum desses campos (só o Número de Série, que já existia como `Identificador`).
- CRUD de centrais JFL reaproveita `EquipamentosController` (Sprint 11) sem alteração; novo
  `CentraisJflController` — ações de comando (`api/equipamentos/{id}/jfl/...`).
- Dashboard: `QuantidadeCentraisJflOnline`, `QuantidadeCentraisJflOffline`,
  `QuantidadeParticoesArmadas`, `QuantidadeParticoesDesarmadas`, `QuantidadeProblemasAtivosJfl`.
- Mobile: telas `CentraisJflScreen` (cadastro reduzido — só Nome/Modelo/Número de série) e
  `DetalhesCentralJflScreen` (testar conexão, consultar status, armar/desarmar por partição,
  PGMs, inibir/desinibir zonas). `CardCentraisJfl` novo no Dashboard. `SelecionarPropriedadeScreen`
  ganha atalho para gerenciar centrais JFL. `EquipamentosScreen` genérica passa a excluir centrais
  JFL da lista (têm tela própria agora).
- `backend/tools/JflSimulator`: simulador TCP simplificado descartável (fora do domínio de
  produção), com estado em memória, usado para validar toda a comunicação via TCP real.

### Migração de banco
- `MigracaoJflActive100Bus`: relaxamento de nullability em `Equipamentos.{Ip,Porta,Usuario,
  SenhaCriptografada}` (NOT NULL → nullable, aditivo/não-destrutivo) + 1 tabela nova
  (`StatusCentraisJfl`). Sem operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_012.md`.

## [Sprint 11 — Migração da Integração Control iD] — 2026-07-22

Objetivo: migrar a integração Control iD existente na referência `Teste-portaria-main1` para a
arquitetura Clean Architecture do AppMorador, estabelecendo o padrão OFICIAL de integração de
fabricante para todas as futuras (Intelbras, Hikvision, Dahua, JFL). Fase 1 (Descoberta
obrigatória) encontrou um achado crítico: apesar de a missão descrever o legado como "comportamento
já homologado", a investigação não confirma isso — dados de seed fake, nenhuma evidência de
validação contra hardware real. Legado tratado como referência de protocolo, não como
comportamento a preservar byte-a-byte. Validado contra um simulador HTTP local (sem equipamento
real disponível neste ambiente) — pendência de validação contra hardware físico registrada
explicitamente. Ver ADR 0014.

### Adicionado
- **`Equipamento`** (nova entidade): pertence direto à Propriedade (mesmo padrão de
  `PontoAcesso`/`Vaga`/`Visitante`). Campos genéricos de fabricante: Nome, Modelo, `Fabricante`
  (enum `ControlId`/`Jfl`/`Intelbras`/`Hikvision`/`Dahua`/`Outro`), IP, Porta, Usuário, Senha
  (cifrada em repouso via Data Protection API — nunca devolvida pela API, nem cifrada),
  Identificador, `Status` (`Desconhecido`/`Online`/`Offline`), Última sincronização.
- **`IControlIdProvider`/`ControlIdProvider`** — único ponto do sistema que conhece o protocolo
  REST do Control iD (`login.fcgi`, `system_information.fcgi`, `load_objects.fcgi`,
  `create_objects.fcgi`). DTOs internos (`Application/ControlId`) totalmente separados dos DTOs de
  wire-format Control iD (`Infrastructure/ControlId`), com um mapper como única fronteira de
  tradução.
- **`EquipamentoIntegracaoServico`**: testar conexão, consultar informações, sincronizar
  Moradores/Credenciais/Permissões (usando o domínio já existente — Sprints 6/7, nenhum domínio
  novo), importar eventos.
- **`EventoEquipamento`** (nova entidade, auditoria pura — sem soft delete) + segunda
  implementação real de `IFonteEventos` (`EquipamentoFonteEventos`) — eventos importados alimentam
  a Central de Eventos já existente (Sprint 3, ADR 0006), nunca uma estrutura paralela. Valor
  aditivo `ControlId` em `OrigemEvento`. `EventosServico` agora agrega múltiplas fontes reais
  (antes assumia uma só) — consultadas sequencialmente (nunca `Task.WhenAll`, que corrompia a
  instância `Scoped` compartilhada de `AppDbContext`).
- CRUD completo: `EquipamentosController` — Create/List aninhados sob a Propriedade
  (`api/properties/{id}/equipamentos`); Get/Update/Delete + ações de integração (testar-conexao/
  informacoes/sincronizar-moradores/sincronizar-credenciais/sincronizar-permissoes/
  importar-eventos) por Id (`api/equipamentos/{id}/...`).
- Exclusão lógica (soft delete, ADR 0009) em `Equipamento`, com cascade explícito (Propriedade
  agora também alcança Equipamentos).
- Dashboard: `QuantidadeEquipamentosOnline`, `QuantidadeEquipamentosOffline`,
  `UltimaSincronizacaoUtc`, `UltimoEventoEquipamentoRecebidoUtc` (contadores/datas reais, aditivos
  ao contrato existente).
- Mobile: telas `EquipamentosScreen` (lista/cadastro/edição/exclusão) e
  `DetalhesEquipamentoScreen` (testar conexão, consultar informações, sincronizar cada domínio,
  importar eventos). `CardEquipamentos` novo no Dashboard. `SelecionarPropriedadeScreen` ganha
  atalho para gerenciar equipamentos.
- `backend/tools/ControlIdSimulator`: simulador HTTP local descartável do protocolo Control iD
  (fora do domínio de produção), usado para validar toda a comunicação do Provider via
  requisições HTTP reais.

### Migração de banco
- `AdicionarEquipamentosIntegracaoControlId`: 2 tabelas novas (`Equipamentos`,
  `EventosEquipamento`). Sem operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_011.md`.

## [Sprint 10 — Entregas e Correspondências] — 2026-07-22

Objetivo: construir o domínio completo de Entregas e Correspondências — registrar, acompanhar e
finalizar entregas destinadas a moradores — sem nenhuma integração física. Diferente das Sprints
8 e 9, a própria missão já eliminou a ambiguidade de status ("não utilizar jobs automáticos"),
então o Status da Entrega é 100% manual — sem a calculadora híbrida computado/manual usada em
Autorizacao (ADR 0011) e Vaga (ADR 0012). Ver ADR 0013.

### Adicionado
- **`Entrega`** (nova entidade): Morador destinatário + Unidade (validados consistentes entre
  si), `Tipo` (enum `TipoEntrega`: Correspondencia/Encomenda/Delivery/Documento/Mercado/Outro),
  Descrição, Recebido por (texto livre), Data de recebimento/retirada (nulas até as ações
  correspondentes), Observações, `Status` (enum `StatusEntrega`: AguardandoRecebimento/
  DisponivelParaRetirada/Retirada/Cancelada).
- Máquina de estados explícita: AguardandoRecebimento → DisponivelParaRetirada (preenche Data de
  recebimento + Recebido por) ou → Cancelada; DisponivelParaRetirada → Retirada (preenche Data de
  retirada) ou → Cancelada; Retirada/Cancelada são terminais (nem Status nem os demais campos
  podem ser alterados depois). Qualquer transição fora dessa tabela é rejeitada explicitamente.
- **`HistoricoEntrega`** (nova entidade, auditoria pura — sem soft delete): registra criação/
  alteração/recebimento/retirada/cancelamento/exclusão. Sem tela/endpoint de leitura nesta Sprint.
- CRUD completo: `EntregasController` — Create/List aninhados sob a Propriedade (visão unificada
  de todas as entregas, não por morador individual — ver ADR 0013): `api/properties/{id}/entregas`;
  Get/Update/Status/Delete por Id: `api/entregas/{id}`, `api/entregas/{id}/status`.
- Exclusão lógica (soft delete, ADR 0009) em `Entrega`, com cascade explícito (Propriedade/
  Unidade/Morador agora também alcançam Entregas).
- Dashboard: `QuantidadeEntregasPendentes`, `QuantidadeEntregasDisponiveis`,
  `QuantidadeEntregasRetiradas`, `QuantidadeCorrespondenciasCadastradas` (contadores reais,
  aditivos ao contrato existente).
- Mobile: telas `EntregasScreen` (visão unificada da propriedade, seleção em cascata Unidade→
  Morador no cadastro, mesmo padrão de `AutorizacoesScreen`) e `DetalhesEntregaScreen` (consulta
  completa, ações de status por transição válida, edição, confirmação antes de cancelar/excluir).
  `CardEntregas` novo no Dashboard. `SelecionarPropriedadeScreen` ganha atalho para gerenciar
  entregas.

### Migração de banco
- `AdicionarEntregasECorrespondencias`: 2 tabelas novas (`Entregas`, `HistoricoEntregas`). Sem
  operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_010.md`.

## [Sprint 9 — Veículos e Garagens] — 2026-07-21

Objetivo: construir o domínio completo de Veículos e Garagens — cadastro de veículos, vagas
independentes, vínculo Veículo↔Vaga com histórico, e permissões veiculares — sem nenhuma
integração com equipamento físico. Fase 1 (planejamento) apresentada e ajustada com o usuário
antes de código — 2 decisões confirmadas: Status da Vaga é híbrido (Livre/Ocupada computados a
partir do vínculo ativo, sem job/scheduler; Bloqueada/Reservada manuais); Permissões Veiculares
reaproveitam `PontoAcesso` (Sprint 7) via um novo campo `Tipo` (Geral/Veicular), em vez de um enum
próprio de áreas. Ver ADR 0012.

### Adicionado
- **`Veiculo`** (nova entidade): pertence obrigatoriamente a um `Morador`; Placa (normalizada,
  única entre veículos não excluídos — validada em código, não por índice físico), Marca, Modelo,
  Cor, Ano, Observações, `Tipo` (enum `TipoVeiculo`: Carro/Moto/Caminhonete/Van/Caminhao/
  Bicicleta/Outro), `Status` (enum `StatusVeiculo`: Ativo/Suspenso/Inativo).
- **`Vaga`** (nova entidade, domínio independente — nunca pertence ao Morador): pertence direto à
  `Propriedade`; Número, Bloco, Andar, Coberta, `Tipo` (enum `TipoVaga`: Morador/Visitante/
  Comercial/Servico), Status efetivo híbrido (Livre/Ocupada computados; Bloqueada/Reservada
  manuais).
- **`VinculoVeiculoVaga`** (nova entidade, vínculo temporal): `DataInicioUtc`/`DataFimUtc` — cada
  linha é um período de ocupação, nunca sobrescrita. "Vincular" e "Alterar vaga" são a mesma
  operação: vincular a uma vaga diferente encerra o vínculo antigo e cria um novo.
- **`PermissaoVeicular`** (nova entidade): vínculo `Veiculo` ↔ `PontoAcesso`, exigindo que o Ponto
  de Acesso tenha o novo `Tipo = Veicular`.
- **`HistoricoVeiculo`/`HistoricoVaga`** (novas entidades, auditoria pura — sem soft delete):
  registram criação/alteração/remoção de Veículo e Vaga, vinculação/desvinculação, bloqueio/
  reserva/liberação de Vaga e alteração de Permissão Veicular. Sem tela/endpoint de leitura nesta
  Sprint (domínio preparado, não exposto).
- `PontoAcesso` ganha o campo `Tipo` (enum `TipoPontoAcesso`: Geral/Veicular) — pontos já
  cadastrados recebem `Geral` no backfill; a tela mobile de Pontos de Acesso ganha um seletor.
- CRUD completo: `VeiculosController` (`api/moradores/{id}/veiculos`, `api/veiculos/{id}`),
  `VagasController` (`api/properties/{id}/vagas`, `api/vagas/{id}`, `api/vagas/{id}/status`),
  `VeiculoVagaController` (`api/veiculos/{id}/vinculo` para vincular/desvincular,
  `api/veiculos/{id}/vinculos` para o histórico), `PermissoesVeicularesController`
  (`api/veiculos/{id}/permissoes-veiculares`).
- Validação: placa duplicada rejeitada (case/espaço normalizados); vaga já ocupada por outro
  veículo rejeitada; vaga bloqueada/reservada rejeitada para vincular; Status Ocupada nunca
  aceito manualmente (sempre computado).
- Exclusão lógica (soft delete, ADR 0009) em `Veiculo`/`Vaga`/`VinculoVeiculoVaga`/
  `PermissaoVeicular`, com cascade explícito em todo o agregado (Propriedade/Unidade/Morador/
  PontoAcesso agora também alcançam Veículos, Vagas, Vínculos e Permissões Veiculares).
- Dashboard: `QuantidadeVeiculos`, `QuantidadeVeiculosAtivos`, `QuantidadeVagas`,
  `QuantidadeVagasLivres`, `QuantidadeVagasOcupadas` (contadores reais, computados via
  `VagaStatusCalculator`, aditivos ao contrato existente).
- Mobile: telas `VeiculosScreen` (com painel de vínculo/desvínculo por vaga carregado sob
  demanda) e `VagasScreen` (status manual via confirmação). `CardVeiculos` novo no Dashboard.
  `SelecionarPropriedadeScreen` ganha atalho para gerenciar vagas; `MoradoresScreen` ganha atalho
  para ver veículos de cada morador.

### Migração de banco
- `AdicionarVeiculosEGaragens`: 6 tabelas novas + 1 coluna nova (`PontosAcesso.Tipo`). Sem
  operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_009.md`.

## [Sprint 8 — Visitantes e Autorizações] — 2026-07-21

Objetivo: construir o domínio completo de Visitantes e Autorizações — quem pode visitar uma
propriedade, quando pode entrar e quem autorizou o acesso — sem nenhuma integração com
equipamento físico. Fase 1 (planejamento) apresentada e ajustada com o usuário antes de código —
2 decisões confirmadas: `Visitante` pertence direto à Propriedade (reaproveitável entre unidades,
mesmo padrão de `PontoAcesso`), e Status da Autorização é híbrido — Pendente/Ativa/Expirada
computados a partir das datas em tempo de leitura (sem job/scheduler), Cancelada/Utilizada são
ações manuais explícitas. Ver ADR 0011.

### Adicionado
- **`Visitante`** (nova entidade): pertence direto à `Propriedade`; Nome, Documento, Telefone,
  Observações, `FotoPath` preparado (nunca preenchido — sem reconhecimento facial real).
- **`Autorizacao`** (nova entidade): vínculo Morador responsável + Unidade + Visitante, com
  `Tipo` (enum `TipoVisita`: Visitante/PrestadorServico/Entregador/Evento/Temporario — atributo da
  visita, não da pessoa), `DataInicial`/`DataFinal`, `HorarioInicial`/`HorarioFinal` opcionais, e
  Status efetivo híbrido (Pendente/Ativa/Expirada computados; Cancelada/Utilizada manuais).
  Validação: o Morador responsável precisa pertencer à Unidade informada.
- **`HistoricoVisitante`** (nova entidade, auditoria pura — sem soft delete): registra criação/
  alteração/cancelamento/uso de autorização e remoção de visitante. Sem tela/endpoint de leitura
  nesta Sprint (domínio preparado, não exposto).
- CRUD completo: `VisitantesController` (`api/properties/{id}/visitantes`,
  `api/visitantes/{id}`), `AutorizacoesController` (`api/visitantes/{id}/autorizacoes`,
  `api/autorizacoes/{id}`, `api/autorizacoes/{id}/status`).
- Exclusão lógica (soft delete, ADR 0009) em `Visitante`/`Autorizacao`, com cascade explícito:
  excluir uma Propriedade cascateia até seus Visitantes e Autorizações; excluir uma Unidade ou um
  Morador (responsável) cascateia até as Autorizações correspondentes; excluir um Visitante
  cascateia até suas Autorizações.
- Dashboard: `QuantidadeVisitantesAtivos`, `QuantidadeAutorizacoesPendentes`,
  `QuantidadeAutorizacoesExpiradas` (contadores reais, computados via `StatusAutorizacaoCalculator`,
  aditivos ao contrato existente).
- Mobile: telas `VisitantesScreen` e `AutorizacoesScreen` (seleção de Unidade/Morador em cascata,
  tipo de visita, período de validade, confirmação antes de cancelar autorização). `CardVisitantes`
  novo no Dashboard. `SelecionarPropriedadeScreen` ganha atalho para gerenciar visitantes.

### Migração de banco
- `AdicionarVisitantesEAutorizacoes`: 3 tabelas novas (`Visitantes`, `Autorizacoes`,
  `HistoricoVisitantes`). Sem operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_008.md`.

## [Sprint 7 — Controle de Acesso Inteligente (Domínio)] — 2026-07-21

Objetivo: construir o domínio completo de Controle de Acesso do AppMorador (credenciais,
permissões, regras de acesso, pontos de acesso), sem nenhuma integração com equipamento físico —
a "inteligência" de negócio pronta para receber Control iD/Intelbras/Hikvision/JFL numa Sprint
futura. Fase 1 (planejamento) apresentada e ajustada com o usuário antes de código — 2 decisões
confirmadas: `Credencial.Status` (kill-switch geral) + `PermissaoAcesso` (regras por Ponto de
Acesso) como entidade própria, e `PontoAcesso` pertencente direto à Propriedade (nunca à Unidade).
Ver ADR 0010.

### Adicionado
- **`Credencial`** (nova entidade): pertence obrigatoriamente a um `Morador`; `Tipo` (enum
  `TipoCredencial`: Facial/TagRfid/QrCode/Pin/Biometria/ChaveVirtual, imutável após criação);
  `Status` (enum `StatusCredencial`: Ativa/Suspensa/Expirada/Revogada).
- **`PontoAcesso`** (nova entidade): pertence direto à `Propriedade` (ex.: Portão Principal,
  Garagem, Piscina, Salão de Festas).
- **`PermissaoAcesso`** (nova entidade): vínculo `Credencial` ↔ `PontoAcesso`, com
  `DiasPermitidos` (enum `[Flags] DiaSemana`), `HorarioInicial`/`HorarioFinal` (`TimeOnly?`) e
  `DataInicial`/`DataFinal` (`DateTime?`, vigência). Valida que o Ponto de Acesso pertence à mesma
  propriedade da Credencial antes de vincular.
- **`HistoricoCredencial`** (nova entidade, auditoria pura — sem soft delete): registra
  criação/suspensão/reativação/revogação/expiração de credencial e criação/alteração/exclusão de
  permissão. Sem tela/endpoint de leitura nesta Sprint (domínio preparado, não exposto).
- CRUD completo: `CredenciaisController` (`api/moradores/{id}/credenciais`,
  `api/credenciais/{id}/status`, `api/credenciais/{id}`), `PontosAcessoController`
  (`api/properties/{id}/pontos-acesso`, `api/pontos-acesso/{id}`), `PermissoesAcessoController`
  (`api/credenciais/{id}/permissoes`, `api/permissoes/{id}`).
- Exclusão lógica (soft delete, ADR 0009) em `Credencial`/`PontoAcesso`/`PermissaoAcesso`, com
  cascade explícito em todos os níveis: excluir uma Propriedade/Unidade/Morador agora também
  cascateia até suas Credenciais e PermissoesAcesso; excluir um Ponto de Acesso cascateia até as
  Permissões que apontavam para ele; excluir uma Credencial cascateia até suas Permissões.
- Dashboard: `QuantidadeCredenciais`, `QuantidadeCredenciaisAtivas`, `QuantidadeCredenciaisSuspensas`,
  `QuantidadePontosAcesso` (contadores reais, aditivos ao contrato existente).
- Mobile: telas `CredenciaisScreen` (Facial/Tag RFID/QR Code/PIN/Biometria/Chave Virtual, troca de
  status com confirmação antes de revogar), `PermissoesScreen` (ponto de acesso, dias da semana,
  horário) e `PontosAcessoScreen` (loading/estado vazio/validação/feedback de erro, confirmação
  antes de excluir em todas). `CardControleAcesso` novo no Dashboard. `SelecionarPropriedadeScreen`
  ganha atalho para gerenciar pontos de acesso; `MoradoresScreen` ganha navegação para as
  credenciais de cada morador.

### Alterado
- `MoradorServico.DeleteAsync`/`UnidadeServico.DeleteAsync` (Sprint 6): cascade expandido para
  também alcançar Credenciais/PermissoesAcesso do Morador/Unidade excluído.

### Migração de banco
- `AdicionarControleDeAcesso`: 4 tabelas novas (`Credenciais`, `PontosAcesso`, `PermissoesAcesso`,
  `HistoricoCredenciais`). Sem operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_007.md`.

## [Sprint 6 — Domínio do Produto: Propriedades, Unidades e Moradores] — 2026-07-21

Objetivo: estabelecer o agregado principal do AppMorador (`Propriedade` → `Unidade` → `Morador`),
base para todas as funcionalidades futuras do domínio (Visitantes, Veículos, Entregas, Controle
de Acesso). Fase 1 (planejamento) apresentada e ajustada com o usuário antes de código — decisões
que mudaram o plano: exclusão lógica (soft delete) em vez de cascade físico, com preparação
explícita para uma futura Lixeira/Restauração; `TipoUnidade` mais amplo que os exemplos originais
(8 valores em vez de 5, para reduzir migrations futuras).

### Adicionado
- **`Unidade`** e **`Morador`** (novas entidades): `Unidade` pertence obrigatoriamente a uma
  `Propriedade`; `Morador` pertence obrigatoriamente a uma `Unidade`. Novos enums `TipoUnidade`
  (Casa/Apartamento/Loja/SalaComercial/Galpao/Quiosque/Escritorio/Outro) e `StatusMorador`
  (Ativo/Inativo). `Morador.FotoPath` preparado (nunca preenchido nesta Sprint — sem biometria).
- **`EntidadeComSoftDelete`** (`AppMorador.Domain.Common`): base para exclusão lógica —
  `Excluido`/`DataExclusaoUtc`/`ExcluidoPorUsuarioId`. `Propriedade`, `Unidade` e `Morador` a
  herdam; query filter global garante que nenhum registro excluído aparece em consulta normal.
  Ver ADR 0009.
- CRUD completo (Create/List/Update/Delete) para os 3 níveis: `UnidadesController`
  (`api/properties/{id}/unidades`, `api/unidades/{id}`), `MoradoresController`
  (`api/unidades/{id}/moradores`, `api/moradores/{id}`); `PropertiesController` ganhou `Delete`
  (antes só tinha Create/List/Update).
- Exclusão de uma Propriedade ou Unidade cascateia logicamente para seus filhos (Unidades e
  Moradores marcados como excluídos junto, nunca fisicamente removidos).
- Mobile: telas `UnidadesScreen`/`MoradoresScreen` novas (loading/estado vazio/validação/
  feedback de erro, confirmação antes de excluir); `SelecionarPropriedadeScreen` evoluída com
  edição/exclusão de propriedade, contagem total de propriedades e atalho para gerenciar
  unidades.
- `DevelopmentSeeder`: conta Morador (Fernanda) ganha 1 Unidade + 2 Moradores de exemplo.

### Alterado
- `DashboardResponse.QuantidadePessoas`: antes sempre `1` (hardcoded, "só o dono"), agora é a
  contagem real de Moradores ativos da propriedade. Novo campo `QuantidadeUnidades` (aditivo).
- `CardResumoInstalacao` (mobile): passa a exibir Unidades e Moradores junto de Alarmes/Câmeras/
  Sensores.
- Corrigido durante a integração: a condição de "instalação vazia" do Dashboard não considerava
  Unidades/Moradores — uma propriedade com moradores cadastrados mas sem equipamento de
  segurança ainda cairia no estado vazio, escondendo o dado real recém-cadastrado.

### Migração de banco
- `AdicionarUnidadesEMoradoresESoftDelete`: 3 colunas novas em `Propriedades`
  (`Excluido`/`DataExclusaoUtc`/`ExcluidoPorUsuarioId`), 2 tabelas novas (`Unidades`,
  `Moradores`) com FK `Restrict`. 100% aditivo, sem operação destrutiva (ver
  `docs/ALTERACOES_BANCO.md`).

### Fora de escopo (registrado como backlog)
Controle de acesso, Visitantes, Entregas, Veículos, QR Code, reconhecimento facial, Push
Notifications, WebSocket, integrações com hardware, Analytics — nada disso foi implementado,
conforme pedido. Ver `docs/roadmap/ROADMAP.md`.

Ver detalhes completos em `docs/reviews/SPRINT_006.md`.

## [Sprint 5 — Alinhamento Visual ao Protótipo] — 2026-07-20

Objetivo: aproximar a interface do Dashboard ao protótipo visual fornecido pelo usuário,
preservando arquitetura, lógica, navegação e contratos. Achado inicial: a paleta de cores do
protótipo já é idêntica, valor por valor, a `theme/tokens.ts` — o Design System já nasceu deste
protótipo; o trabalho foi 100% composição/estilo de componentes existentes.

### Evoluído
- `CardSaude`: hero de status reestilizado — anel "respirando" (Reanimated) e glow radial
  aproximado atrás do ícone, ícone maior. Mesmo dado (`pontuacaoSaude`/`protegido`).
- `ItemEvento` (Central de Eventos + timeline do Dashboard): ícone passou de círculo para caixa
  arredondada neutra (fundo/borda constantes, só o ícone muda de cor por severidade) — visual do
  protótipo, aplicado num único ponto compartilhado pelas duas telas.
- `HeaderDashboard`: ícone de localização (`MapPin`) adicionado antes do nome da propriedade —
  sem novo dado, só pista visual.
- `CardResumoInstalacao`: ícones dos indicadores agora em caixa (mesmo padrão do `ItemEvento`).
- `AcoesRapidas`: terceiro estado **"Noturno"** adicionado (visual-only, sem comando real à
  central — mesmo padrão dos outros dois desde a Sprint 1). `armado: boolean` virou
  `modo: ModoArme` (`'total' | 'noturno' | 'desarmado'`) — mudança de estado local do mobile, sem
  tocar backend/contrato.
- `RecursosFuturos`: "Visualização ao vivo" adicionada como 5º item discreto — câmeras já têm
  contagem real (`CardResumoInstalacao`), mas nenhum stream de vídeo existe; mostrar "AO VIVO"
  sem vídeo real inclui o mesmo risco de confundir usuário que os outros 4 itens da seção.

### Design System
- `tokens.ts`: `colors.warnLine` (faltava o 3º tom do padrão safe/warn/danger já usado em
  safeLine/dangerLine) e `motion.duration.ambient` (pulso lento/contínuo — categoria nova, as
  durações existentes eram todas de feedback de interação, nenhuma servia para uma animação
  ambiente de ~1,8s).

### Fora de escopo (presente no protótipo, decisão do usuário de não implementar)
- Grade de câmeras com thumbnail/vídeo "AO VIVO" real.
- Tela "Acessos" (facial, QR code provisório).
- Overlay de "Alarme disparado" com timeline de vídeo pré/pós-roll.
- Navegação por abas fixas — mantida a stack atual.

Ver detalhes completos em `docs/reviews/SPRINT_005.md`.

## [Sprint 4 — Dashboard Operacional Inteligente] — 2026-07-20

Objetivo: leitura do estado da propriedade em poucos segundos ao abrir o app. Zero mudança de
backend/schema/contrato — toda a Sprint reaproveita dados e endpoints já existentes.

### Evoluído (componentes existentes, não substituídos)
- `CardResumoInstalacao`: indicador de Câmeras passa a incorporar a contagem de Gravadores no
  mesmo item (ex.: "Câmeras • 2 gravadores"), em vez de uma coluna separada; "Centrais" renomeado
  para "Alarmes" (vocabulário do usuário, mesmo dado).
- `CardUltimaAtividade`: de "último evento único" para uma timeline curta (3 eventos mais
  recentes), com busca própria (`GET /api/properties/{id}/eventos?tamanhoPagina=3` — endpoint da
  Sprint 3, sem alteração), independente do carregamento do resto do Dashboard — uma falha aqui
  nunca impede o resto da tela de aparecer.
- `ItemEvento` (Central de Eventos): ganhou uma variante `compacto` opcional (sem borda/fundo
  próprios) para ser reaproveitado dentro de `CardUltimaAtividade` sem empilhar "card dentro de
  card". Uso existente na tela de Eventos inalterado (prop opcional, default `false`).

### Adicionado
- `RecursosFuturos`: seção discreta no fim do Dashboard para Portões, Controle de acesso,
  Entregas e Visitantes — nenhum tem integração real hoje. Peso visual deliberadamente menor que
  os cards de dado real (sem número, opacidade reduzida) para não competir pela atenção com
  informação de segurança de verdade — decisão orientada pelo objetivo de "compreensão em menos
  de 5 segundos".

### Corrigido (achado durante a validação, não relacionado ao Dashboard)
- Repositório Git local com `HEAD` corrompido (zerado, provável interferência de sincronização de
  nuvem na pasta `Documents`) — impedia até `dotnet build` (SourceLink lendo metadata do git).
  Refs/objetos/tag estavam intactos; só `HEAD` foi restaurado.

Ver detalhes completos em `docs/reviews/SPRINT_004.md`.

## [v0.3.0-alpha] — 2026-07-20

Primeira baseline estável do projeto, publicada após as Sprints 1, 2, 2.1, 3 e 3.1 e a
estabilização do ambiente de desenvolvimento (migrations automáticas no startup, seed com 4
personas). Esta entrada marca a preparação do repositório para publicação — não introduz
funcionalidade nova:

- Auditoria completa do repositório: removidos `admin/` (diretório vazio) e `mobile/LICENSE`
  (licença do Expo/650 Industries mal-atribuída ao projeto).
- `.gitignore` revisado em 3 níveis (raiz, `backend/`, `mobile/`): corrigido para não mais
  ignorar `appsettings.Development.json` (não tem segredo, é necessário pro setup documentado
  funcionar num clone novo); `mobile/.env` passa a ser ignorado (com `.env.example` versionado no
  lugar); `snapshots/` (imagens capturadas de câmera) adicionado como dado sensível nunca
  versionável.
- `README.md` criado na raiz — primeira vez que o projeto tem um ponto de entrada único para um
  desenvolvedor novo.
- Verificação de segurança: nenhum segredo real encontrado versionável (`Jwt:Key` e connection
  string real só existem em `dotnet user-secrets`, fora da árvore do repositório).
- `mobile/package.json`: versão alinhada para `0.3.0-alpha` (antes trazia um resquício da versão
  do SDK Expo do template).

Ver `docs/setup/RELATORIO_PUBLICACAO_v0.3.0-alpha.md` para o relatório completo desta preparação.

## [Sprint 3.1 — Homologação/Estabilização, v0.3.0-alpha] — 2026-07-19

Sprint exclusivamente de estabilização — nenhuma funcionalidade nova. Objetivo: base executável,
estável e previsível para homologação manual por qualquer desenvolvedor.

### Corrigido
- **JFL Server derrubava a Api inteira em conflito de porta**: `JflTcpServer.Start()` não tratava
  `SocketException` e `JflServerHostedService.StartAsync()` deixava a exceção se propagar — como
  falhas em `IHostedService.StartAsync` são fatais para o Generic Host do ASP.NET Core, qualquer
  falha de bind do JFL (ex.: porta 8085 já ocupada por uma instância zumbi) derrubava a Api toda,
  não só o listener JFL. Corrigido: exceção de bind é capturada, logada com contexto acionável e
  a Api segue subindo normalmente. Verificado com duas instâncias reais rodando em paralelo.
- `PropriedadeRepositorio.ListByOwnerAsync` rastreava entidades numa listagem somente-leitura —
  único problema de performance real encontrado nesta Sprint; corrigido com `.AsNoTracking()`.

### Adicionado
- Seed de desenvolvimento idempotente (`DevelopmentSeeder`), executado automaticamente em
  `Development`: 1 usuário de teste, 1 propriedade, 1 central, 2 zonas, 5 ocorrências. Uma falha
  no seed nunca derruba a Api (mesma filosofia do JFL Server).
- `docs/setup/SETUP_AMBIENTE.md`: guia completo de configuração do zero (banco, segredos via
  user-secrets, migrations, seed, execução de Backend/Mobile, troubleshooting).
- `docs/TESTES_FUNCIONAIS.md`: checklist de homologação executado com resultado de cada cenário.

### Alterado
- Histórico de migrations (5 arquivos) squashado num único `InitialCreate` refletindo o schema
  final — validado sem divergência via `dotnet ef migrations has-pending-model-changes`. O banco
  de desenvolvimento real não foi recriado nem alterado (ver ADR 0007).

### Homologado (sem alteração de código, comportamento confirmado correto)
- Autenticação completa: login, claims do JWT, expiração, refresh com rotação, logout, lockout
  após 5 tentativas, isolamento de ownership entre usuários.
- CRUD de Propriedades: criar/listar/editar, validação de entrada, ownership.
- Dashboard: estado com dados reais e estado vazio (propriedade sem equipamento).
- Central de Eventos: paginação, busca por zona, filtro de período, estado vazio.
- Mobile: `tsc --noEmit` limpo; bundle web compila integralmente; CORS validado contra a origem
  real do Metro web.

### Escopo intencionalmente não coberto (registrado, não escondido)
- CRUD de Usuários além de Cadastro/Login/Refresh/Logout, e qualquer sistema de Perfis/Papéis —
  não existem no código; decisão explícita do usuário foi homologar só o que existe. Ver
  `docs/DIVIDA_TECNICA.md` itens 6 e 7.

Ver detalhes completos em `docs/reviews/SPRINT_003_1.md`.

## [Sprint 3 — Central de Eventos Inteligente] — 2026-07-19

### Adicionado
- Central de Eventos: `GET /api/properties/{id}/eventos` com paginação, busca de texto livre e
  filtro de período (`desdeUtc`/`ateUtc`).
- Modelo unificado por fontes plugáveis: `EventoTimeline`/`IFonteEventos`
  (`AppMorador.Application/Eventos`) — a Timeline nunca conhece `Ocorrencia` diretamente; única
  implementação real hoje é `JflFonteEventos` (Infrastructure). Ver ADR 0006.
- Mobile: tela `Eventos` (scroll infinito, chips de período, busca, skeleton, estado vazio),
  acessível via novo atalho no Dashboard (`AtalhoEventos`).
- Componente `EstadoVazio` generalizado em `src/components/` (substitui o
  `EstadoVazioDashboard` específico do Dashboard, reutilizável por qualquer tela).

### Investigado (sem implementação nesta Sprint)
- Integração de controle de acesso (Control iD/Intelbras ASI): investigada a referência
  `Teste-portaria-main1` a pedido do usuário. Conclusão: nenhuma integração real e validada
  contra hardware existe lá para portar diretamente; vira fase própria futura. Ver
  `docs/DIVIDA_TECNICA.md` item 5 e `docs/roadmap/ROADMAP.md`.

### Migração de banco
- `AdicionarIndiceOcorrenciaPropriedadeData`: índice composto `(PropriedadeId, CreatedAtUtc)` em
  `Ocorrencias`, substituindo o índice automático de FK. Sem operação destrutiva (ver
  `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_003.md`.

## [Sprint 2 — Dashboard Premium / Product Sprint] — 2026-07-19

### Adicionado
- Enum `TipoPropriedade` (Residencial/Comercial/Condominio/Rural/Outro) — campo `Tipo` em
  `Propriedade`, propagado por `CriarPropriedadeRequest`/`AtualizarPropriedadeRequest`/
  `PropriedadeResponse`.
- `DashboardResponse` enriquecido: `Nome`, `Tipo`, `QuantidadeCentrais`, `QuantidadeGravadores`
  (o Dashboard nunca contava `Gravadores` antes).
- Dashboard mobile componentizado (`src/screens/dashboard/`): `HeaderDashboard`, `CardSaude`
  (Health Score animado + rótulo por faixa), `CardResumoInstalacao`, `CardUltimaAtividade`,
  `AcoesRapidas`, `EstadoVazioDashboard`, `SkeletonDashboard`.
- Design System formalizado em `theme/tokens.ts` (fonte única: cores, espaçamento, raio,
  tipografia, motion, sombra, opacidade, z-index, tamanho de ícone); `theme.ts` importa e
  reexporta.
- `ServicoFeedbackTatil` centralizando `expo-haptics`; `react-native-reanimated` adotado como
  biblioteca padrão de animação do projeto.
- `TipoPropriedadeSelector` (seletor em chips) na tela de criar propriedade.

### Corrigido (Sprint 2.1 — hotfix)
- `POST /api/properties` retornava 400 ao enviar `tipo` como string (ex.: `"Comercial"`) —
  `System.Text.Json` não desserializa enum a partir de texto sem um `JsonStringEnumConverter`
  registrado. Corrigido com registro global em `Program.cs`
  (`AddControllers().AddJsonOptions(...)`), cobrindo todo enum de negócio exposto pela Api, não
  só `TipoPropriedade`. Ver ADR 0005.

### Migração de banco
- `AdicionarTipoPropriedade`: adiciona coluna `Tipo` em `Propriedades` (backfill `"Outro"` para
  linhas existentes). Sem operação destrutiva (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_002.md`.

## [Sprint de Padronização] — 2026-07-19

### Alterado
- Domínio de negócio inteiro renomeado de inglês para português (pt-BR): entidades, enums,
  DTOs, interfaces de repositório/serviço, pastas da camada Application, `DbSet`s do
  `AppDbContext`, tabelas e colunas do banco de dados.
- Contrato JSON da API mudou nos campos de negócio (ex.: `password`→`senha`, `userId`→`usuarioId`,
  `healthScore`→`pontuacaoSaude`). Rotas HTTP (`/api/auth/...`, `/api/properties/...`) não mudaram.
- App mobile (`types.ts`, `AuthContext.tsx`, `DashboardScreen.tsx`, `SelecionarPropriedadeScreen.tsx`)
  atualizado para consumir os novos nomes de campo.

### Mantido (exceções deliberadas)
- Nomes de infraestrutura/ecossistema .NET: `Controllers`, `Middleware`, `Program.cs`,
  `DbContext`, `RefreshToken`, JWT/OAuth, HTTP/HTTPS, Swagger.
- Protocolo JFL (`AppMorador.Jfl`) e integração CGI/ISAPI/Digest Auth (`AppMorador.Infrastructure/Snapshots`)
  — nomes de protocolo/hardware, não vocabulário de negócio.
- `ContactIdCatalog`/`ContactIdDefinition` — nome de padrão externo (Ademco Contact ID).

### Migração de banco
- `PadronizacaoDominioPtBr`: renomeia 8 tabelas e suas colunas/índices/FKs. Sem perda de dados
  (ver `ALTERACOES_BANCO.md`).

Ver detalhes completos em `docs/reviews/SPRINT_001_1.md`.

## [Sprint 1] — 2026-07-19
- Autenticação (cadastro/login/refresh/logout com JWT + BCrypt + lockout), CRUD de Propriedade,
  Dashboard com Health Score. App mobile Expo com 5 telas (Splash, Login, Cadastro,
  Selecionar Propriedade, Dashboard).

## [Fases 0–2.2] — 2026-07-18
- Pivot para "Segurança Conectada". Pipeline de eventos JFL (protocolo, ACK, Contact ID,
  Occurrence/auditoria) e captura de snapshot síncrona no disparo (CGI Dahua/Intelbras,
  ISAPI Hikvision).
