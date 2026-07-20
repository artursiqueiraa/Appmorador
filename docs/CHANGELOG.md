# Changelog

Registro cronológico das mudanças relevantes do projeto. Cada Sprint/Fase adiciona uma entrada.

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
