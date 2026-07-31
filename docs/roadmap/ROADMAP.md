# Roadmap — Segurança Conectada

## Concluído

- **Fase 0** — Pivot de portaria virtual para app self-service de segurança (B2C/B2B).
- **Fases 1–2.2** — Pipeline de eventos JFL: ACK imediato, `Occurrence`, `AlarmEventLog` de
  auditoria, catálogo Contact ID extensível, captura síncrona de snapshot no disparo
  (Dahua/Intelbras via CGI, Hikvision via ISAPI).
- **Sprint 1** — Autenticação (JWT + refresh + BCrypt + lockout), CRUD de Propriedade, Dashboard
  com Health Score. App mobile Expo (Splash, Login, Cadastro, Selecionar Propriedade, Dashboard).
- **Sprint de Padronização** — Domínio de negócio inteiro traduzido para português (pt-BR),
  preservando arquitetura e dados existentes. Ver `docs/reviews/SPRINT_001_1.md`.
- **Sprint 2 — Dashboard Premium / primeira Sprint de Produto** — Dashboard enriquecido
  (tipo de propriedade, resumo da instalação, Health Score com rótulo amigável), mobile
  componentizado e com Design System formalizado (`theme/tokens.ts`), `react-native-reanimated`
  adotado como padrão. Concluída na Sprint 2.1 após hotfix de serialização de enum. Ver
  `docs/reviews/SPRINT_002.md`.
- **Sprint 3 — Central de Eventos Inteligente** — timeline de eventos paginada (período/busca),
  modelo unificado por fontes plugáveis (`IFonteEventos`/`EventoTimeline`), única fonte real hoje
  (`JflFonteEventos`, central de alarme). Ver `docs/reviews/SPRINT_003.md` e ADR 0006.
- **Sprint 3.1 — Homologação/Estabilização** — JFL Server nunca mais derruba a Api (falha de bind
  vira log, não exceção fatal); histórico de migrations squashado num único `InitialCreate`
  validado sem divergência (ADR 0007); seed de desenvolvimento idempotente; autenticação, CRUD de
  Propriedades, Dashboard e Central de Eventos homologados fim a fim via requests reais; único
  problema de performance real encontrado corrigido (`AsNoTracking` em listagem). Ver
  `docs/reviews/SPRINT_003_1.md` e `docs/TESTES_FUNCIONAIS.md`.
- **Sprint 4 — Dashboard Operacional Inteligente** — Dashboard evoluído para leitura do estado da
  propriedade em poucos segundos: resumo da instalação com Câmeras e Gravadores combinados num só
  indicador, "último evento" evoluído para timeline de eventos recentes (reaproveita o endpoint de
  eventos da Sprint 3, sem mudança de contrato), seção discreta para recursos ainda sem integração
  real (Portões, Controle de acesso, Entregas, Visitantes) com peso visual propositalmente menor
  que o dado real. Zero mudança de backend/schema/contrato — 100% reuso do que já existia. Ver
  `docs/reviews/SPRINT_004.md`.
- **Sprint 5 — Alinhamento Visual ao Protótipo** — Dashboard reestilizado a partir de um protótipo
  visual fornecido pelo usuário (hero de status com anel pulsando/glow, ícones em caixa,
  indicadores com estado "Noturno"), preservando arquitetura/navegação/contratos — 100%
  composição/estilo de componentes já existentes. Elementos do protótipo que implicariam
  funcionalidade nova (câmeras "AO VIVO", tela de Acessos facial/QR, overlay de disparo com vídeo,
  navegação por abas) explicitamente não implementados. Ver `docs/reviews/SPRINT_005.md`.
- **Sprint 6 — Domínio do Produto: Propriedades, Unidades e Moradores** — agregado principal do
  AppMorador estabelecido: `Propriedade` → `Unidade` → `Morador`, CRUD completo nos 3 níveis com
  exclusão lógica (soft delete, ADR 0009) em vez de física. Dashboard passa a exibir contadores
  reais de unidades/moradores (antes `quantidadePessoas` era sempre `1`, hardcoded). Telas mobile
  novas (Unidades, Moradores) e `SelecionarPropriedadeScreen` evoluída (editar/excluir/total de
  propriedades). Zero funcionalidade fora do escopo (sem controle de acesso, visitantes, entregas,
  veículos, QR Code, facial, push, WebSocket, hardware real, analytics — tudo registrado como
  backlog). Ver `docs/reviews/SPRINT_006.md`.
- **Sprint 7 — Controle de Acesso Inteligente (Domínio)** — domínio completo de Controle de
  Acesso construído: `Credencial` (Facial/Tag RFID/QR Code/PIN/Biometria/Chave Virtual, status
  Ativa/Suspensa/Expirada/Revogada), `PermissaoAcesso` (dia da semana/horário/data por Ponto de
  Acesso) e `PontoAcesso` (pertence direto à Propriedade). CRUD completo nos 3 níveis com
  exclusão lógica cascateada (ADR 0009), Dashboard com contadores reais de credenciais/pontos de
  acesso, telas mobile novas (Credenciais, Permissões, Pontos de Acesso). Zero comunicação com
  hardware real (Control iD, Intelbras, Hikvision, JFL) — "o cérebro do Controle de Acesso, não os
  conectores", fica para uma Sprint futura própria. Ver `docs/reviews/SPRINT_007.md` e ADR 0010.
- **Sprint 8 — Visitantes e Autorizações** — domínio completo construído: `Visitante` (pertence
  direto à Propriedade, reaproveitável entre unidades) e `Autorizacao` (Morador responsável +
  Unidade + Visitante + período de validade + tipo de visita), Status efetivo híbrido — Pendente/
  Ativa/Expirada computados a partir das datas em tempo de leitura (sem job/scheduler), Cancelada/
  Utilizada são ações manuais. CRUD completo com exclusão lógica cascateada (ADR 0009), Dashboard
  com contadores reais de visitantes ativos/autorizações pendentes/expiradas, telas mobile novas
  (Visitantes, Autorizações). Zero comunicação com hardware real — mesmo racional da Sprint 7.
  Ver `docs/reviews/SPRINT_008.md` e ADR 0011.
- **Sprint 9 — Veículos e Garagens** — domínio completo construído: `Veiculo` (pertence a um
  Morador) e `Vaga` (domínio independente, pertence direto à Propriedade), `VinculoVeiculoVaga`
  (entidade temporal — vincular/alterar vaga preserva histórico completo), `PermissaoVeicular`
  (reaproveita `PontoAcesso`, que ganhou o campo `Tipo` Geral/Veicular). Status da Vaga é híbrido
  — Livre/Ocupada computados a partir do vínculo ativo, sem job/scheduler; Bloqueada/Reservada
  manuais. CRUD completo com exclusão lógica cascateada (ADR 0009), validação de placa duplicada
  e vaga indisponível, Dashboard com contadores reais de veículos/vagas, telas mobile novas
  (Veículos, Vagas). Zero comunicação com hardware real — mesmo racional das Sprints 7-8.
  Ver `docs/reviews/SPRINT_009.md` e ADR 0012.
- **Sprint 10 — Entregas e Correspondências** — domínio completo construído: `Entrega` (Morador
  destinatário + Unidade, tipo, descrição, recebido por, datas de recebimento/retirada,
  observações), Status 100% manual (sem calculadora híbrida — a própria missão fechou essa
  ambiguidade: "não utilizar jobs automáticos"), máquina de estados explícita
  (AguardandoRecebimento → DisponivelParaRetirada/Cancelada; DisponivelParaRetirada →
  Retirada/Cancelada; terminais depois). CRUD completo (visão unificada por Propriedade, não por
  morador — ver ADR 0013) com exclusão lógica cascateada (ADR 0009), Dashboard com contadores
  reais de entregas pendentes/disponíveis/retiradas/cadastradas, telas mobile novas (Entregas,
  Detalhes da Entrega). Zero comunicação física (QR Code, assinatura digital, foto,
  transportadoras). Ver `docs/reviews/SPRINT_010.md` e ADR 0013.
- **Sprint 11 — Migração da Integração Control iD** — primeira integração real de hardware do
  projeto: `Equipamento` (pertence direto à Propriedade, campos genéricos de fabricante) +
  `IControlIdProvider`/`ControlIdProvider` (única implementação real, protocolo isolado em
  Infrastructure), DTOs internos totalmente separados dos DTOs de wire-format Control iD.
  Sincronização manual de Moradores/Credenciais/Permissões reaproveitando o domínio já existente
  (Sprints 6/7). Importação de eventos construída do zero como segunda fonte real de
  `IFonteEventos` (Sprint 3), alimentando a Central de Eventos já existente. Senha do equipamento
  cifrada em repouso (Data Protection API) — corrige a falha de segurança real encontrada no
  legado investigado (senha em texto puro). Dashboard com contadores/datas reais. Validado via
  simulador HTTP local (`backend/tools/ControlIdSimulator`) — hardware físico real pendente (ver
  `DIVIDA_TECNICA.md` item 20). Esta Sprint estabelece o padrão OFICIAL de integração de
  fabricante para o projeto (ver ADR 0014) — toda integração futura (Intelbras, Hikvision, Dahua,
  JFL para controle de acesso) deve seguir a mesma arquitetura. Ver `docs/reviews/SPRINT_011.md`.
- **Sprint 12 — Migração da Integração JFL Active 100 Bus** — comandos de superusuário
  (Armar/Desarmar/Armar Stay/Armar Away/PGM/Inibir Zona/Consultar Status) migrados da referência
  `Integra-o-FL` para a arquitetura oficial de integrações (ADR 0014), generalizando o padrão
  para um protocolo de conexão invertida (a central disca para o AppMorador, nunca o contrário —
  ver ADR 0015): `IJflProvider` localiza a sessão TCP já aberta (`SessionManager`, existente
  desde a Fase 1) em vez de discar para fora. Completa uma migração que já havia começado antes
  das Sprints numeradas (handshake/keep-alive/eventos já existiam desde a Fase 1). `Equipamento`
  (Sprint 11) reaproveitado sem entidade nova — Ip/Porta/Usuario/Senha viraram opcionais para
  acomodar um fabricante sem esses campos aplicáveis. Rollup de status (`StatusCentralJfl`) novo
  para o Dashboard. `Central`/`Ocorrencia`/`Zona` (pipeline de eventos, Fase 1) permanecem
  intocados, com vínculo automático de leitura por Número de Série. Validado via simulador TCP
  simplificado (`backend/tools/JflSimulator`) — hardware físico real pendente (ver
  `DIVIDA_TECNICA.md` item 22). Ver `docs/reviews/SPRINT_012.md` e ADR 0015.

- **Sprint 13 — Camada Operacional Unificada** — consolidação arquitetural: `Estado Bruto`
  (leitura pura do que Control iD/JFL já persistem) → `IClassificadorOperacionalServico` (4
  estados: Saudável/Atenção/Crítico/Offline) → `SnapshotOperacional` (rollup 1:1 por Propriedade,
  upsert) → Dashboard/Mobile/APIs futuras. Nenhuma interface consulta Providers diretamente a
  partir de agora (regra vinculante, ver ADR 0016). Central de Eventos ganhou 5 filtros novos
  (Equipamento/Fabricante/Origem/Categoria/Severidade); "Timeline Operacional" reaproveita essa
  mesma Central, sem domínio de eventos novo. Ver `docs/reviews/SPRINT_013.md` e ADR 0016.
- **Sprint 14 — Tempo Real (SignalR)** — Dashboard/Mobile passam a ser notificados
  automaticamente quando algo operacional muda, eliminando a dependência de atualização manual.
  `IOperacionalEventoPublicador` (porta) + `OperacionalHub`/`OperacionalHubPublicador`
  (`Api/Realtime/`, único ponto que conhece SignalR) publicam o Snapshot Operacional (Sprint 13)
  já pronto para o grupo da Propriedade correspondente, disparado por mutações já existentes em
  Control iD/JFL e pelo único gatilho verdadeiramente assíncrono do projeto
  (`AlarmEventProcessor`, evento de alarme via TCP). Grupos só por Propriedade — grupos por Perfil
  de usuário ficaram de fora por o domínio não ter RBAC (ver `DIVIDA_TECNICA.md` item 25).
  Validado com um cliente SignalR real (não uma chamada em memória). Ver
  `docs/reviews/SPRINT_014.md` e ADR 0017.
- **Sprint 15 — Integração Intelbras: Prova Definitiva da Arquitetura** — terceiro fabricante
  (AMT 8000, modelado via API HTTP local com vocabulário de comando de alarme) integrado sem
  alterar nenhuma camada compartilhada além do próprio mecanismo de extensão já previsto
  (`OrigemEvento` ganhou um valor aditivo, uma nova `IFonteEventos` foi registrada via DI).
  Auditoria de arquitetura (Fase 0) confirmou "nenhuma alteração" em 9 das 10 camadas. Um achado
  real foi descoberto durante a validação — `EquipamentoFonteEventos` não escopava sua query por
  Fabricante, causando eventos Intelbras duplicados — e corrigido genericamente (benefício para
  qualquer fabricante futuro que reaproveite `EventoEquipamento`), nunca uma adaptação específica
  de Intelbras. `IntelbrasComandoServico`/`CentraisIntelbrasController` são inteiramente paralelos
  a Control iD/JFL, provando que fabricantes não competem por serviços compartilhados. Validado
  via `backend/tools/IntelbrasSimulator` (HTTP real) e um cliente SignalR real. Ver
  `docs/reviews/SPRINT_015.md` e ADR 0018.
- **Sprint 16 — Implementação do Design System Mobile Oficial UX001** — transição de "sistema
  tecnicamente robusto" para "produto centrado no usuário" (morador comum, 18-70 anos, baixo
  conhecimento técnico). Bottom Navigation de 4 abas (Início/Câmeras/Acessos/Ajustes) substitui a
  navegação anterior; HeroCard com status único derivado de dados já existentes; Onboarding
  Wizard persistente (7 etapas, recuperável por 3 caminhos); vocabulário técnico
  (Intelbras/JFL/Snapshot/PGM/Zona/Partição) eliminado do uso diário do morador. Corrigido um bug
  real de navegação: telas de detalhe ficavam inalcançáveis depois de entrar no Dashboard. Nenhuma
  alteração em backend/domínio/API/integrações/SignalR/ADRs 0014-0018. Ver
  `docs/reviews/SPRINT_016.md` e ADR 0019.
- **Sprint 17 — Refinamento da Experiência do Morador** — 7 fricções encontradas numa validação em
  dispositivo real sobre a Sprint 16 corrigidas: erros técnicos mapeados centralmente
  (`api/client.ts` + `utils/errorMapper.ts`), cadastro facial com anti-duplicação e captura real
  (sem persistência de foto — dívida técnica), aba Acessos ganhou Painel de Controle real (só
  centrais JFL), Detalhes do Equipamento e Minha Propriedade com RBAC de UI (perfil
  morador/técnico, preferência local — domínio ainda sem RBAC real), HeroCard com indicador de
  conectividade útil, Empty States com CTA em 13 telas, Ajustes reorganizado. Nenhuma alteração em
  backend/domínio/API/integrações/SignalR/ADRs 0014-0019. Ver `docs/reviews/SPRINT_017.md` e ADR
  0020.
- **Sprint 17.5 — Release 0.9.0, Backup, Portabilidade e Disaster Recovery** — ponto de
  restauração completo do projeto: auditoria de ambiente, backup/restore de banco (scripts +
  dumps), inventário de armazenamento, variáveis de ambiente documentadas, 7 scripts PowerShell
  de automação, documentação de arquitetura/setup/release/disaster recovery, tag `v0.9.0`. A
  própria verificação de portabilidade (clone real, do zero) encontrou e corrigiu um **bug crítico
  pré-existente**: 16 arquivos de código-fonte (integração de câmera,
  `AppMorador.Domain`/`Infrastructure`/`Snapshots/`) nunca haviam sido versionados por uma colisão
  de padrão no `.gitignore` (case-insensitive no Windows) — invisível em qualquer inspeção do
  repositório local, só um clone genuinamente limpo revelava. Também corrigidos:
  `core.longpaths` ausente (quebrava checkout de migrations em caminhos aninhados no Windows) e
  branch padrão do GitHub apontando para uma release antiga. Zero alteração em
  domínio/API/integrações/UX. Ver `docs/reviews/SPRINT_017_5.md`.
- **Sprint 18 — Experiência em Tempo Real** — RealtimeContext dividido em 3 (Regra 5: cada
  consumidor só re-renderiza pelo que usa); Timeline com inserção real de eventos (Fade+Slide,
  scroll preservado, badge "Novo", cache máximo 50); Toasts inteligentes (só fora da tela onde o
  evento já é visível, via rastreamento de tela em foco); Painel de Controle com máquina de
  estados de comando (Normal/Enviando/Sucesso/Falha, timeout 10s) e status Online/Offline ao vivo;
  reconexão com backoff exponencial customizado (1s/2s/5s/10s/30s) + "Tentar novamente" manual;
  cache descartado corretamente na troca de propriedade (achado real: telas guardavam estado local
  próprio que não era limpo). Achado arquitetural: comandos JFL são síncronos (sem canal de
  confirmação assíncrona via SignalR) — a máquina de estados foi desenhada para essa realidade, não
  para uma suposta assíncrona que não existe. Zero alteração em domínio/API/integrações/arquitetura
  de backend. Ver `docs/reviews/SPRINT_018.md` e ADR 0022.
- **Sprint 18.1 — Hotfix: Correções Críticas de UX e Estabilidade** — bugs críticos da validação
  em dispositivo físico da Sprint 18 corrigidos: causa raiz real de "propriedades não carregam" e
  "não sai da conta" era `fetch` sem timeout (backend inalcançável travava a requisição para
  sempre) — corrigido com timeout de 15s; logout agora sempre limpa a sessão local (`finally`);
  `SafeAreaProvider` adicionado globalmente (faltava em toda a árvore, só `BottomNavigation.tsx`
  usava safe area); "Suas propriedades" ganhou máquina de estados de carregamento (Skeleton/Erro/
  Sucesso) que não existia. Infraestrutura de testes automatizados (Jest) criada do zero. Dois
  bugs reportados (overlay "DSW", texto "Tetd") investigados extensivamente e não reproduzidos
  como defeito de código — registrados com hipóteses, sem correção especulativa. Ver
  `docs/reviews/BUG_DEBT_018_1.md`.
- **Sprint 19 — Notificações Push** — `DispositivoPush` (multi-dispositivo, preferências por
  canal), `INotificationProvider`/`FirebaseNotificationProvider` (modo sem-op documentado — sem
  credenciais Firebase reais disponíveis nesta sessão), `NotificationDispatcher`/`NotificationService`
  com debounce de 60s em memória; hooks reais de disparo adicionados a `AlarmEventProcessor`,
  `JflComandoServico`, `AutorizacaoServico` e `EntregaServico` (os dois últimos nunca publicavam
  evento algum antes desta Sprint). Mobile: `expo-notifications` com token nativo FCM, 3 canais
  Android, deep link com retry de prontidão, tela Ajustes → Notificações. Primeiro projeto de
  testes automatizados do backend (`AppMorador.Tests`, 27 testes) + 22 novos testes no Mobile.
  Achado arquitetural: o domínio não tinha granularidade para distinguir a maioria dos 8 eventos
  assumidos pela missão — resolvido com hooks diretos na Aplicação, não no pipeline genérico de
  eventos. iOS e agrupamento de notificações (Fase 8.1) ficam como dívida técnica. Ver ADR 0023 e
  `docs/reviews/SPRINT_019.md`.
- **Sprint 20 — Visualização de Câmeras** — aba Câmeras funcional (era Empty State desde a Sprint
  16). `Camera` evoluída com `StatusCamera`/dois timestamps de captura; endpoints de lista/
  snapshot (metadados vs. captura sob demanda)/imagem (autenticada, content-type sniffado)/status;
  `ISnapshotCaptureService` extraída para permitir captura sob demanda por Id (o fluxo de alarme
  já existente segue intacto); evento SignalR leve `CameraStatusAlterado`, separado do Snapshot
  Operacional. Mobile: `expo-image`, grid 2 colunas com skeleton/pull-to-refresh, tela de detalhe
  com "Atualizar imagem". Achado arquitetural: a entidade/captura de snapshot já existiam desde
  antes da Sprint 1, mas só serviam ao fluxo de alarme — sem endpoint, sem forma de servir a
  imagem por HTTP, sem seed. Wording honesto ("última imagem há X", nunca "offline desde X") por
  não haver monitoramento contínuo. Streaming ao vivo, PTZ, cadastro de câmera no mobile e
  detecção de movimento (nenhum gravador emite esse sinal) ficam fora de escopo/dívida técnica.
  Ver ADR 0024 e `docs/reviews/SPRINT_020.md`.
- **Sprint 21 — RBAC Master (Base de Permissões da Plataforma)** — RBAC completo para papéis
  internos (Master/Técnico/Suporte): Policies via `RequireAssertion`, impersonation (token de
  15min sem refresh, 100% auditado), auditoria centralizada, Permissões Funcionais + Feature
  Flags por Propriedade, `ModeloEquipamento` real (substitui `Equipamento.Modelo` texto, com
  backfill de dados na migration) + Capacidades Dinâmicas, Provisionamento (registro de
  metadados). Cliente (Administrador) mantém `ProprietarioId` como fonte de verdade —
  `UsuarioPropriedade` nasce como abstração preparatória, não substituição (Multiusuário fica para
  Sprint futura dedicada, decisão explícita do usuário). Mobile: `usePermissao`, UI condicional em
  5 telas, `GET /api/properties` enriquecido com perfil/permissões/features. 43 novos testes de
  backend (87 total) + 16 novos testes de mobile (57 total), zero regressão. Painel Web fica fora
  de escopo (Sprint 22). Ver ADRs 0021/0025/0026/0027/0028 e `docs/audits/AUDIT_RBAC_021.md`.
- **Sprint 22A — Fundação do Painel Web** — projeto novo `PainelWeb/` (React 19 + Vite + TS +
  Zustand + TanStack Query + MUI). Autenticação, Dashboard Operacional (Master/Suporte) e Técnico,
  Clientes (lista/detalhe), módulo Suporte (impersonation ponta a ponta com banner/timer, Sessões
  Ativas inferidas via Auditoria, Diagnóstico da Propriedade, Logs). Achado crítico: bug de
  `MapInboundClaims` fazia toda Policy de papel interno falhar com 403 desde a Sprint 21, corrigido
  nesta Fase 0. 3 endpoints novos no backend (`/api/proprietarios`, `/api/proprietarios/{id}`,
  `/api/dashboard-operacional`) — gaps genuínos, não "1 micro-endpoint" como a missão assumia.
  Sessões Ativas sem revogação real de token (impersonation é stateless); CRUD completo de cliente
  fica fora de escopo (sem endpoint de gestão de conta cliente). 9 novos testes de backend (96
  total) + 28 testes de Painel Web, zero regressão. Ver ADRs 0029/0030 e
  `docs/painel/mapeamento-api.md`.
- **Sprint 22B — Equipamentos, Provisionamentos e Diagnóstico** — três módulos administrativos
  novos no Painel Web. Equipamentos: CRUD global paginado (`api/painel/equipamentos`), Número de
  Série único por Propriedade, `EstadoOperacionalEquipamento` novo (paralelo a `StatusEquipamento`,
  conectividade). Provisionamentos: `VinculoEquipamentoPropriedade` (entidade nova, deliberadamente
  separada do `Provisionamento` já existente da Sprint 21/ADR 0028), regra de 1 vínculo ativo por
  equipamento garantida em Servico, troca sempre encerra o antigo + cria novo preservando
  histórico, tudo auditado. Diagnóstico: `GET /api/diagnostico/equipamentos/status` estritamente
  somente leitura, agregando 3 tabelas numa única consulta projetada. CorrelationId/UsuarioId
  passam a enriquecer todo log estruturado. 20 novos testes de backend (116 total) + 24 novos
  testes de Painel Web (52 total), zero regressão — homologação formal completa registrada em
  `docs/testing/Sprint22B.md`. Frontend do Painel Web (módulos Equipamentos/Provisionamentos/
  Diagnóstico) entregue na mesma Sprint. Ver ADR 0031.

## Próximo

- **Sprint 22C — Plataforma de Execução de Comandos de Hardware**: ainda não iniciada. Decisão
  arquitetural já aprovada pela ARB (ADR 0033, Rev. 2) — `ProviderType`/`HardwareCommandType`
  como identificadores estáveis (não strings soltas), Command Registry central
  (capability→payload→converter→handler), idempotência com janela explícita
  (`IdempotencyExpiresAt`, sem a armadilha de `DATE(CriadoEm)`), `ExecutionPipeline` com
  curto-circuito/ordem formalizados, `MockProvider` validado contra 3 naturezas de equipamento
  (JFL/DVR/Control iD). Vai substituir os mocks visuais do módulo Diagnóstico (Sprint 22B) por
  execução real, equipamento por equipamento. Pontos ainda em aberto para o início da
  implementação (registrados na própria ADR, não bloqueantes): tradução do schema de
  PostgreSQL para MySQL, relação entre a nova trilha `AuditoriaComandos` e as trilhas de
  auditoria já existentes (`AuditoriaMaster`/`HistoricoX`), e mapeamento das novas permissões
  para o RBAC já existente (`PermissaoFuncionalidade`/`Policies`) em vez de um sistema paralelo.
- **v0.3.0-alpha**: marco de estabilização atingido na Sprint 3.1 — base executável, previsível e
  documentada para homologação manual.
- **Release 0.9.0**: ponto de restauração completo atingido na Sprint 17.5 — Release do GitHub
  preparada, pendente só de o usuário rodar o comando `gh release create` já entregue (bloqueio do
  classificador de segurança do ambiente de execução, não do processo).
- **Push real ponta a ponta**: pendente de credenciais Firebase reais (projeto no Firebase Console
  + `google-services.json` + conta de serviço) — ver `docs/DIVIDA_TECNICA.md` item 38.
- **Streaming ao vivo de câmeras**: Sprint 21+, base arquitetural (Gravador/Camera/providers por
  fabricante) já pronta para receber sem retrabalho — ver ADR 0024.

## Não iniciado / backlog

- **CRUD completo de Usuários (cliente)** — hoje só existe Cadastro/Login/Refresh/Logout para o
  Administrador (dono da propriedade). Editar dados, alterar senha, desativar/reativar conta,
  excluir conta continuam sem CRUD próprio. A Sprint 21 (ADR 0021/0025) já entrega um `perfil`
  **real**, vindo da API (`GET /api/properties` → `perfil`/`permissoes`/`features`, consumido via
  `usePermissao`) — mas o `perfil` **local** de `profilePreference.ts` (preferência de UI, ver ADR
  0020) não foi removido nem substituído por ele nesta Sprint, para não expandir o escopo além do
  pedido; os dois coexistem (um é preferência de UI, o outro é RBAC de verdade). CRUD completo de
  Usuário/Morador com login próprio fica para a Sprint dedicada ao Multiusuário (ver ADR 0021).
  Ver `docs/DIVIDA_TECNICA.md`.

- **Integração de Controle de Acesso e Portões — Intelbras, Hikvision, Dahua, JFL** (Control iD já
  concluído na Sprint 11, ver abaixo) — o padrão OFICIAL de integração está estabelecido (ADR
  0014): cada fabricante restante implementa só um `I<Fabricante>Provider`/Provider próprio,
  reaproveitando a mesma entidade `Equipamento` e o mesmo fluxo de sincronização/importação de
  eventos — nunca altera o domínio. Investigação da Sprint 3 (`DIVIDA_TECNICA.md` item 5) também
  encontrou uma integração Intelbras/CGI na referência `Teste-portaria-main1` que na prática é um
  leitor de acesso família ASI, com ajustes documentados como pendentes lá — ponto de partida para
  quando essa fase acontecer.
- **Validação da integração Control iD contra hardware físico real** (Sprint 11 validou só contra
  um simulador HTTP local — ver `DIVIDA_TECNICA.md` item 20) — repetir a mesma bateria de testes
  (testar conexão, informações, sincronização, importação de eventos) contra um equipamento
  Control iD genuíno assim que um estiver disponível.
- **Validação da integração JFL Active 100 Bus contra hardware físico real** (Sprint 12 validou
  só contra um simulador TCP simplificado — ver `DIVIDA_TECNICA.md` item 22) — repetir a mesma
  bateria de testes (handshake, keep-alive, status, armar/desarmar/PGM/zonas) contra uma central
  Active 100 Bus genuína assim que uma estiver disponível.
- **Eventos de transição de conectividade na Timeline Operacional** (Sprint 13 não implementou —
  ver `DIVIDA_TECNICA.md` item 24) — "Equipamento Offline"/"Equipamento Reconectado" hoje são só
  uma sobrescrita de `Equipamento.Status`, sem registro auditável; implementar exigiria uma nova
  fonte de eventos, fora do escopo de consolidação da Sprint 13.
- **Unificação de `Central` (pipeline de eventos) e `Equipamento` (Fabricante=Jfl, comandos)** —
  hoje são cadastros separados, ligados só por um vínculo de leitura por Número de Série (ver ADR
  0015 e `DIVIDA_TECNICA.md` item 23). Migrar `Ocorrencia.CentralId`/`Zona.CentralId` para
  `Equipamento`, ou dar a `Central` um CRUD que crie o `Equipamento` correspondente no mesmo
  fluxo.
- **Integração de Entregas e Correspondências** (portaria virtual, notificações, automação de
  transportadora) — fase própria, mesmo racional das integrações de Controle de Acesso/Veículos
  acima. O domínio de negócio (`Entrega`, máquina de estados, histórico) já foi construído na
  Sprint 10 (ver ADR 0013) — falta só os conectores: webhook/integração de transportadora
  chamando a mesma `AtualizarStatusAsync` que a tela mobile chama hoje, notificação push ao
  morador quando uma entrega fica disponível. O Dashboard (Sprint 10) já reserva contadores reais
  de entregas — falta só o gatilho automático.
- **Integração de Veículos e Garagens** (OCR de placas, portão automático, Control iD, Intelbras,
  Hikvision ou JFL) — fase própria, mesmo racional da integração de Controle de Acesso acima. O
  domínio de negócio (`Veiculo`/`Vaga`/`VinculoVeiculoVaga`/`PermissaoVeicular`) já foi construído
  na Sprint 9 (ver ADR 0012) — falta só os conectores: leitura automática de placa, liberação
  automática de portão/catraca, reconhecimento veicular. O Dashboard (Sprint 9) já reserva
  contadores reais de veículos/vagas — falta só a comunicação com o equipamento físico.
- **Reconhecimento facial de Moradores** — `Morador.FotoPath` já existe no domínio (Sprint 6),
  preparado mas nunca preenchido — falta pipeline de upload/armazenamento e integração com
  hardware de reconhecimento facial. A Sprint 17 implementou a captura/pré-visualização real
  (`expo-image-picker`) e a credencial `Facial` em si, mas sem persistir a foto no backend (ver
  `docs/DIVIDA_TECNICA.md` item 32) — falta só o endpoint de upload e o preenchimento real de
  `FotoPath`.

- **Visualização ao vivo de câmeras** — indicador reservado no Dashboard (Sprint 5), sem stream
  de vídeo real. Depende de um pipeline de vídeo ao vivo (protocolo com o DVR/NVR, player mobile)
  — dimensão própria, não um item dentro de uma Sprint de UI.
- Filtro por zona na Central de Eventos (hoje só período + busca de texto) — depende de um
  endpoint de listagem de zonas que ainda não existe.
- Módulo de Clips/vídeo (substituído pelo MVP de snapshot por ora).
- **Armar/Desarmar via `QuickAction`** (Sprint 16, HeroCard do Início — antes `AcoesRapidas`,
  removido) ainda só visual — o comando real já existe desde a Sprint 12/15 (telas Centrais
  JFL/Intelbras), mas não está associado às ações rápidas do Início (ambíguo qual central usar
  quando a Propriedade tem mais de uma). Falta decidir a regra de central "padrão" antes de
  conectar.
- Cartão de auto-cadastro facial (câmera + upload) do protótipo UX001 — captura/pré-visualização
  implementadas na Sprint 17 via `expo-image-picker`; falta só o endpoint de upload de foto (ver
  `docs/DIVIDA_TECNICA.md` item 32).
- Timeline de vídeo pré/pós-disparo do protótipo UX001 (Sprint 16) — backend só captura uma
  imagem por disparo (MVP da Fase 2), nunca um buffer contínuo de vídeo.
- Compartilhamento de propriedade entre múltiplos usuários.
- Homologação de novos códigos Contact ID / novos fabricantes de DVR.
- Registrar formalmente como ADR as decisões técnicas da Sprint 2 (adoção do Reanimated como
  padrão, `TipoPropriedade`) — ver `DIVIDA_TECNICA.md`.
- **Push Notification (Firebase/APNs)** — citada como Sprint 19 pela missão da Sprint 18. Hoje o
  app só reage em tempo real enquanto aberto (SignalR); notificações com o app fechado dependem
  desse trabalho novo.
- Telemetria de cache hit/miss (recomendação da Sprint 18, não implementada — ver ADR 0022).
- Validação em dispositivo físico real da Sprint 18 (reconexão ao cair Wi-Fi/trocar de rede,
  profiler de performance real) — pendente do usuário, mesmo padrão das Sprints 15-17.
