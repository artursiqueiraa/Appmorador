# Mapeamento da Api — Sprint 22A (Fase 0, Painel Web)

Auditoria obrigatória antes de qualquer código do Painel Web (ver `SPRINT_022A.md`, Fase 0).
Levantamento feito lendo os 28 Controllers reais do backend (`backend/src/AppMorador.Api/Controllers/`),
não o Swagger renderizado — mesmo resultado, fonte mais rápida de auditar.

## 🔴 Bug crítico encontrado e corrigido nesta auditoria

Testando `POST /api/auth/impersonar` contra o backend real (não só os testes unitários com Moq da
Sprint 21), o Master de verdade recebeu **403 Forbidden** — e o mesmo aconteceria em qualquer
endpoint `[Authorize(Policy = RequerMaster/Tecnico/Suporte/Interno)]`, para qualquer usuário
interno, sempre. Causa raiz: `AddJwtBearer` não tinha `MapInboundClaims = false` — o ASP.NET Core
remapeia por padrão a claim curta `"role"` (emitida por `JwtTokenService`) para a URI longa
`ClaimTypes.Role`, e `ClaimsPrincipalExtensions.GetRoleGlobal()` procura literalmente por `"role"`
— nunca encontrava nada depois da validação real do token. Os testes de `ClaimsPrincipalExtensions`
da Sprint 21 não pegaram isso porque construíam o `ClaimsPrincipal` manualmente, sem passar pelo
`JwtBearerHandler` de verdade.

**Corrigido**: `options.MapInboundClaims = false;` adicionado em `Program.cs`
(`AddJwtBearer`). Reverificado ponta a ponta após o fix: `POST /api/auth/impersonar` (200),
token de impersonation acessando `GET /api/properties` do cliente (200, dado correto), `POST
/api/auth/impersonar/encerrar` (204), `GET /api/usuarios-internos` — RequerMaster (200), `GET
/api/auditoria` — RequerSuporte (200, inclusive mostrando as tentativas anteriores que falharam
com 403 registradas como `FalhaAutorizacao`, confirmando que o handler de auditoria de falha
também funciona). `dotnet test` — 87/87 continuam passando (nenhuma regressão). Este achado **é
exatamente o valor de "testar Impersonation contra o backend real"** como item obrigatório da
Fase 0 — nenhum teste automatizado da Sprint 21 exercitava o pipeline real de autenticação JWT.

## Como ler esta matriz

**Policy** é a checagem GROSSEIRA de papel (`[Authorize(Policy = ...)]`, ver ADR 0021). Vazio
(`[Authorize]` só) significa "qualquer usuário autenticado, sem distinção de papel" — a checagem
fina de posse (ex.: `propriedade.ProprietarioId == usuarioId`) acontece DENTRO do Servico, nunca
aparece como atributo. Onde marcado **RequerX**, só esse papel (ou os listados) acessa o endpoint
de jeito nenhum, mesmo sendo dono do recurso.

## Autenticação

| Rota | Verbo | Policy | Request | Response |
|---|---|---|---|---|
| `/api/auth/register` | POST | — (anônimo) | `CadastrarUsuarioRequest{Nome,Email,Senha}` | `{id}` |
| `/api/auth/login` | POST | — (anônimo) | `EntrarRequest{Email,Senha}` | `EntrarResponse{AccessToken,RefreshToken,ExpiresInSeconds,UsuarioId,Nome,Email}` |
| `/api/auth/refresh` | POST | — (anônimo) | `RefreshRequest{RefreshToken}` | `EntrarResponse` (mesmo shape) |
| `/api/auth/logout` | POST | `[Authorize]` | `SairRequest{RefreshToken}` | 204 |
| `/api/auth/impersonar` | POST | **RequerSuporte** (Master∪Suporte) | `ImpersonarRequest{PropriedadeId}` | `ImpersonarResponse{AccessToken,ExpiresInSeconds,PropriedadeId,PropriedadeNome,ClienteNome}` |
| `/api/auth/impersonar/encerrar` | POST | **RequerSuporte** | `ImpersonarRequest{PropriedadeId}` | 204 |

**⚠️ Achado**: `EntrarResponse`/o JWT **não carregam o `Nome` do usuário na claim** — o JWT só tem
`sub`/`email`/`securityStamp`/`jti`/(`role` se interno). O Painel Web precisa persistir `Nome` a
partir do BODY da resposta de login (mesmo padrão já usado pelo app mobile, `AuthContext.tsx` →
`StoredUser`), nunca tentar decodificar isso do token.

## RBAC — Usuários Internos, Permissões, Feature Flags, Modelos, Provisionamento

| Rota | Verbo | Policy | Request | Response |
|---|---|---|---|---|
| `/api/usuarios-internos` | POST | **RequerMaster** | `CriarUsuarioInternoRequest{Nome,Email,Senha,RoleGlobal}` | `UsuarioInternoResponse{Id,Nome,Email,RoleGlobal,Ativo,CreatedAtUtc}` |
| `/api/usuarios-internos` | GET | **RequerMaster** | — | `UsuarioInternoResponse[]` |
| `/api/usuarios-internos/{id}/desativar` | POST | **RequerMaster** | — | 204 |
| `/api/auditoria` | GET | **RequerSuporte** | `?quantidade=N` | `AuditoriaMaster[]` (entidade direta, ver nota) |
| `/api/auditoria/usuarios/{usuarioId}` | GET | **RequerSuporte** | `?inicio&fim` | `AuditoriaMaster[]` |
| `/api/auditoria/propriedades/{propriedadeId}` | GET | **RequerSuporte** | `?inicio&fim` | `AuditoriaMaster[]` |
| `/api/properties/{id}/features` | GET | **RequerTecnico** | — | `FeatureFlag[]` (ativas) |
| `/api/properties/{id}/features/{feature}` | PUT | **RequerTecnico** | `{Ativo:bool}` | `FeatureFlag[]` (atualizada) |
| `/api/properties/{id}/usuarios/{usuarioId}/permissoes` | GET | **RequerTecnico** | — | `PermissaoFuncionalidade[]` |
| `/api/properties/{id}/usuarios/{usuarioId}/permissoes` | PUT | **RequerTecnico** | `{Permissoes:[]}` | `PermissaoFuncionalidade[]` |
| `/api/modelos-equipamento` | POST | **RequerTecnico** | `{Fabricante,Nome}` | `ModeloEquipamentoResponse` |
| `/api/modelos-equipamento` | GET | **RequerTecnico** | `?fabricante` | `ModeloEquipamentoResponse[]` |
| `/api/modelos-equipamento/{id}/capacidades` | PUT | **RequerTecnico** | `{Capacidades:[]}` | `ModeloEquipamentoResponse` |
| `/api/equipamentos/{id}/capacidades` | GET | `[Authorize]` (ownership) | — | `EquipamentoCapacidade[]` |
| `/api/properties/{id}/provisionamentos` | POST | **RequerTecnico** | `{Nome,Template}` | `ProvisionamentoResponse` |
| `/api/properties/{id}/provisionamentos` | GET | **RequerTecnico** | — | `ProvisionamentoResponse[]` |
| `/api/provisionamentos/{id}/arquivar` | POST | **RequerTecnico** | — | `ProvisionamentoResponse` |

**Enums envolvidos** (serializados como string): `RoleSistema` (Master/Tecnico/Suporte),
`PerfilPropriedade` (Administrador/Morador), `PermissaoFuncionalidade` (12 valores:
CadastrarMorador/CadastrarFacial/CadastrarTag/CadastrarCamera/AlterarLeitor/AlterarGravador/
ConfigurarPgm/ConfigurarIntegracoes/VerLogs/AbrirPortao/VerCameras/CriarVisitante),
`FeatureFlag` (8 valores: Facial/Cameras/Pgm/Push/Snapshot/InterfoneSip/StreamingAoVivo/Ia),
`EquipamentoCapacidade` (9 valores: Face/Tag/QrCode/Senha/Armar/Desarmar/Pgm/Streaming/Ptz),
`TemplateProvisionamento` (Residencia/Loja/Escritorio), `StatusProvisionamento`
(Rascunho/Ativo/Arquivado).

**⚠️ Achado**: `AuditoriaController` devolve a entidade `AuditoriaMaster` diretamente (não um DTO
próprio) — campos: `Id, UsuarioId (Guid, sem FK), UsuarioNome, Acao, Entidade?, EntidadeId?,
Detalhes?, IpAddress?, DataHoraUtc`. Suficiente para "Logs Resumidos"/"Últimos Acessos" do
Dashboard (filtrar `Acao == "Login"` no cliente), mas sem paginação server-side — só
`?quantidade=N` (top-N mais recentes). Para listas grandes/filtros combinados o Painel Web terá
que paginar no cliente ou o backend precisará de um parâmetro a mais (mudança pequena, não
coberta nesta Sprint).

## Propriedades (escopo do cliente autenticado — nunca global)

| Rota | Verbo | Policy | Response |
|---|---|---|---|
| `/api/properties` | POST | `[Authorize]` | `PropriedadeResponse` |
| `/api/properties` | GET | `[Authorize]` | `PropriedadeResponse[]` — **só as propriedades do usuário logado** (`ListByOwnerAsync(usuarioId)`) |
| `/api/properties/{id}` | PUT/DELETE | `[Authorize]` (ownership) | `PropriedadeResponse` / 204 |

`PropriedadeResponse` (Sprint 21): `{Id,Nome,Tipo,Endereco?,Perfil,Permissoes[],Features[]}`.

**🔴 Achado crítico — bloqueia Fase 3 e Fase 5 do Sprint 22A**: **não existe nenhum endpoint que
liste propriedades ou usuários-clientes de forma global/cross-tenant.** `GET /api/properties`
é estritamente escopado ao `ProprietarioId` do chamador — um Master logado não vê as propriedades
de outros clientes por aqui, só as suas próprias (hoje o Master não tem nenhuma propriedade
própria, então essa lista vem vazia para ele). `IUsuarioRepositorio` não tem nenhum
`ListClientesAsync`/`ListAllAsync` — só `GetByEmailAsync`, `GetByIdAsync`, `ListInternosAsync`
(que **exclui** clientes por definição) e `ExisteAlgumMasterAsync`. `IPropriedadeRepositorio` só
tem `GetByIdAsync` e `ListByOwnerAsync(proprietarioId)` — nenhum "listar todas". Ver seção
"Gaps que bloqueiam Fases inteiras" abaixo.

## Equipamentos, Câmeras, Centrais (por Propriedade — nunca global)

| Rota | Verbo | Policy |
|---|---|---|
| `/api/properties/{id}/equipamentos` | POST/GET | `[Authorize]` (ownership) |
| `/api/equipamentos/{id}` | GET/PUT/DELETE | `[Authorize]` (ownership) |
| `/api/equipamentos/{id}/testar-conexao` \| `/informacoes` \| `/sincronizar-*` \| `/importar-eventos` | POST/GET | `[Authorize]` (ownership) |
| `/api/properties/{id}/cameras`, `/api/cameras/{id}/*` | GET/POST | `[Authorize]` (ownership) |
| `/api/equipamentos/{id}/jfl/*`, `/api/equipamentos/{id}/intelbras/*` | GET/POST | `[Authorize]` (ownership) |

Mesmo padrão do achado acima: tudo escopado por Propriedade/usuário dono, nada cross-tenant.
"Total de Equipamentos"/"Equipamentos Offline" (Fase 3) e "Equipamentos por status" (gráfico) não
têm de onde vir sem uma consulta agregada nova.

## Domínio operacional do cliente (Unidades/Moradores/Credenciais/Visitantes/Veículos/Vagas/Entregas/Eventos)

Todos seguem o mesmo padrão: `[Authorize]` simples + ownership check dentro do Servico, escopados
por Propriedade/Unidade/Morador. Irrelevantes para o Painel Web nesta Sprint (Fases 22B/22C) —
listados aqui só para registrar que foram auditados e não têm Policy especial.

## Dispositivos Push, Dashboard, Operacional/Snapshot, Eventos

`DashboardController` (`GET /api/properties/{id}/dashboard`) e
`OperacionalController`/`EventosController` são **por Propriedade**, feitos para o app mobile de
um cliente — não servem de fonte para um Dashboard Operacional GLOBAL (Master vendo a plataforma
inteira). `DispositivosPushController` é de gestão do próprio dispositivo do usuário logado,
irrelevante para o Painel Web.

## Gaps que bloqueiam Fases inteiras do Sprint 22A (não é "1 micro-endpoint")

| Fase | O que a missão pede | O que existe hoje | Gap |
|---|---|---|---|
| 3 — Dashboard Operacional | Total de Clientes, Total de Propriedades, Total de Equipamentos, Equipamentos Offline, gráficos (novos clientes/mês, propriedades por status, equipamentos por status) | Nada disso é consultável — cada recurso só existe escopado por dono | Precisa de endpoint(s) agregados novos, Master/Suporte-only |
| 4 — Dashboard Técnico | "Minhas Instalações" = Provisionamentos atribuídos ao técnico | `Provisionamento` não tem nenhum campo de atribuição a um Técnico específico (só `PropriedadeId/Nome/Template/Status`) | Precisa de um campo novo (`TecnicoResponsavelId`?) ou reinterpretar "meus" como "todos" nesta Sprint |
| 5 — Clientes (lista/CRUD/detalhes) | Lista paginada de TODOS os clientes da plataforma | Nenhum endpoint lista usuários-clientes globalmente | Precisa de endpoint novo, Master/Suporte-only |
| 6 — Sessões Ativas | Lista de impersonations ativas agora + forçar logout de outra sessão | Impersonation é 100% stateless (JWT auto-contido, sem registro de sessão viva) — só existe o rastro em `AuditoriaMaster` (Início/Fim) | Sem uma tabela de sessão ativa, não há como listar "ativas agora" nem revogar um access token já emitido (seria preciso um mecanismo de revogação que não existe) |

**Nenhum destes é resolvível só com frontend.** O item da missão "Tipo: Frontend + 1
micro-endpoint backend" subestima o tamanho real do gap — são pelo menos 2-3 endpoints novos
(lista de clientes, agregado de dashboard, e uma decisão sobre sessões ativas), não 1.

## Endpoints novos criados para resolver os gaps acima (ver ADR 0029)

Confirmado com o usuário: adicionar o mínimo necessário, só-leitura, Master/Suporte-only.

| Rota | Verbo | Policy | Response |
|---|---|---|---|
| `/api/proprietarios` | GET | **RequerSuporte** | `ProprietariosPaginadosResponse{Itens[],PaginaAtual,TotalPaginas,TotalItens}` — `?pagina&tamanhoPagina&busca` |
| `/api/dashboard-operacional` | GET | **RequerSuporte** | `DashboardOperacionalResponse{TotalClientes,TotalPropriedades,TotalEquipamentos,TotalEquipamentosOffline,NovosClientesPorMes[],PropriedadesPorTipo{},EquipamentosPorStatus{}}` |
| `/api/proprietarios/{id}` | GET | **RequerSuporte** | `ProprietarioDetalheResponse{Id,Nome,Email,Ativo,CreatedAtUtc,Propriedades:[{Id,Nome,Tipo}]}` — usado pela tela de detalhe do cliente E pela escolha de propriedade ao impersonar (Master não tinha nenhuma forma de ver as propriedades de um cliente específico antes disto) |

Verificado contra o banco real (14 clientes, 12 propriedades, 6 equipamentos, 3 offline) — dado
correto. **"Sessões Ativas" (Fase 6) não ganhou endpoint novo** — vira leitura client-side de
`GET /api/auditoria` (Início sem Fim correspondente dentro de 15min = "ativa agora"), **sem botão
de forçar logout real** (exigiria revogação de token que não existe — ver `ARQUITETURA_ATUAL.md`).
"Propriedades por Status" da missão virou "Propriedades por Tipo" (Residencial/Comercial/...) —
`Propriedade` não tem nenhum campo de status, só soft delete. "Alarmes Recentes"/"Pendências" não
ganharam card próprio no agregado (nenhuma fonte cross-propriedade de `Ocorrencia` existia e criar
uma seria bem além do escopo combinado) — a seção "Atividade Recente" do Dashboard usa
`GET /api/auditoria` no lugar.
