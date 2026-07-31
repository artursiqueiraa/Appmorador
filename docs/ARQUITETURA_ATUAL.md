# AppMorador — Arquitetura Atual

Documento de referência para quem chega sem contexto prévio (ex.: time do Painel Web, Sprint 22A).
Descreve o que **existe hoje** — para decisões e histórico, ver `docs/adr/` (ADRs) e
`docs/roadmap/ROADMAP.md`. Para a lista completa de endpoints, ver `docs/painel/mapeamento-api.md`.

## Visão Geral

Clean Architecture em 4 camadas — `Domain` (entidades/regras puras, sem dependência de
framework) → `Application` (casos de uso/Servicos/DTOs/portas de repositório) → `Infrastructure`
(EF Core/MySQL, JWT, protocolo JFL, integrações de fabricante) → `Api` (Controllers finos, só
tradução HTTP↔Servico). Mobile (React Native/Expo) e, a partir da Sprint 22A, o Painel Web (React)
são clientes HTTP dessa mesma Api — nenhuma lógica de negócio duplicada em nenhum dos dois.

```
Api (Controllers)
   ↓ chama
Application (Servicos, DTOs, portas de Repositorio)
   ↓ implementado por
Infrastructure (EF Core, JWT, Providers de fabricante, SignalR, Firebase)
   ↓ persiste em
Domain (Entidades — não depende de nada acima)
```

## RBAC — Autorização (Sprint 21, ADR 0021/0025/0026/0027)

### Papéis Globais (`RoleSistema`) — só para a equipe interna da plataforma

| Papel | Pode |
|---|---|
| **Master** | Irrestrito — cria/exclui contas internas, impersonation, vê tudo, vê auditoria |
| **Tecnico** | Configura equipamentos/modelos/capacidades/provisionamento — **não** tem impersonation |
| **Suporte** | Impersonation, vê tudo, vê auditoria/logs — **não** cria/exclui contas internas |

Um cliente (dono de propriedade) **nunca** tem `RoleGlobal` — a claim JWT `"role"` só existe no
token de um usuário interno. Isso é o que as Policies checam (ver abaixo).

### Perfil de Propriedade (`PerfilPropriedade`) — só para clientes

| Perfil | Pode |
|---|---|
| **Administrador** | Dono da propriedade — cadastra moradores/facial/tag/visitantes, abre portão, vê câmeras, ocorrências |
| **Morador** | Só existe como valor de enum hoje — **sem login próprio ainda** (ver "O que NÃO existe" abaixo) |

### Policies (checagem GROSSEIRA de papel, `Api/Auth/Policies.cs`)

`RequerMaster`, `RequerTecnico` (Master∪Tecnico), `RequerSuporte` (Master∪Suporte — Tecnico
excluído de propósito), `RequerInterno` (qualquer um dos 3), `RequerCliente`/`RequerAdministrador`
(hoje idênticas — só Administrador loga), `RequerMorador` (reservada, inalcançável hoje).
Registradas via `RequireAssertion` num único bloco em `Program.cs` — nenhum Controller checa
papel manualmente.

**Checagem fina** (permissão específica sobre um recurso específico) é sempre feita DENTRO do
Servico/Controller, via `IPermissaoService` — nunca como atributo de rota.

### Claims do JWT

`sub` (UsuarioId), `email`, `securityStamp`, `jti`, e **condicionalmente** `role` (só se
`RoleGlobal != null`). Impersonation adiciona `impersonating=true`, `impersonatedBy` (Guid do
Master/Suporte), `impersonatedByNome`. **O JWT nunca carrega o `Nome`** — vem só no corpo da
resposta de login (`EntrarResponse.Nome`), precisa ser persistido pelo cliente separadamente.

**Achado corrigido na Fase 0 do Sprint 22A**: `AddJwtBearer` precisava de
`options.MapInboundClaims = false` — sem isso, o ASP.NET Core remapeia a claim curta `"role"` para
uma URI longa por padrão, e toda Policy de papel interno falhava com 403 sempre (nenhum teste
unitário pegava isso, só testando contra o pipeline HTTP real). Corrigido em `Program.cs`.

### Impersonation

Master/Suporte "entram como" um cliente: `POST /api/auth/impersonar {propriedadeId}` → token
próprio, **15 minutos fixos, nunca gera refresh token**, sem claim `role`. Toda ação de
impersonation é auditada com o `UsuarioId` de quem impersonou (nunca do cliente). `POST
/api/auth/impersonar/encerrar` registra o fim antes do token expirar sozinho.

### Permissões Funcionais (`PermissaoFuncionalidade`, 12 valores) e Feature Flags (`FeatureFlag`, 8 valores)

Dois eixos ortogonais: Permissão = "o que ESTE usuário pode fazer nesta propriedade" (varia por
plano/concessão). Feature Flag = "o que ESTA propriedade contratou" (Cameras/Facial/Pgm/Push/...).
Nenhum dos dois depende do papel/perfil — um Administrador pode ter ou não `CadastrarCamera`
dependendo do plano contratado.

### Capacidades Dinâmicas de Equipamento

`ModeloEquipamento` (catálogo, `Fabricante`+`Nome`) tem um conjunto de `EquipamentoCapacidade` (9
valores: Face/Tag/QrCode/Senha/Armar/Desarmar/Pgm/Streaming/Ptz). App/painel nunca hardcodam UI
por fabricante — sempre consultam `GET /api/equipamentos/{id}/capacidades`.

### O que **NÃO** existe ainda (dívida técnica explícita, não esquecimento)

- **Morador não tem login próprio** — `UsuarioPropriedade`/Permissões/Features já estão prontos
  para quando essa evolução acontecer (Sprint de Multiusuário dedicada, ver ADR 0021), mas hoje só
  o Administrador (dono, via `ProprietarioId`) se autentica.
- **Nenhum endpoint lista clientes/propriedades globalmente** (cross-tenant) — tudo é escopado por
  dono. Um Master não vê "todos os clientes da plataforma" em lugar nenhum hoje.
- **Sessões de impersonation não são rastreadas como "ativas"** — o token é 100% stateless; o
  único rastro é o log de auditoria (Início/Fim), sem um mecanismo de revogação de token já
  emitido.

## Estrutura do Banco de Dados (conceitual — sem SQL)

Agrupado por domínio, não por ordem alfabética:

**Conta e Propriedade**: `Usuarios` (conta de acesso — cliente OU interno, nunca os dois),
`RefreshTokens`, `Propriedades` (dono = `ProprietarioId`), `Unidades` (dentro da Propriedade).

**RBAC (Sprint 21)**: `UsuariosPropriedade` (vínculo Usuario↔Propriedade + Perfil — hoje espelha
`ProprietarioId`, preparação para Multiusuário), `UsuariosPropriedadePermissao`,
`PropriedadesFeatureFlag`, `ModelosEquipamento`, `ModelosEquipamentoCapacidade`,
`Provisionamentos` (metadado — sem árvore de vínculos de hardware ainda), `AuditoriaMaster`
(trilha de auditoria, sem FK para `Usuarios` de propósito — sobrevive à exclusão da conta).

**Moradores e Controle de Acesso**: `Moradores` (dentro da Unidade), `Credenciais` (Facial/Tag/
QrCode/Pin/Biometria/ChaveVirtual), `PontosAcesso`, `PermissoesAcesso`, `HistoricoCredenciais`.

**Visitantes**: `Visitantes`, `Autorizacoes` (status híbrido computado/manual), `HistoricoVisitantes`.

**Veículos e Garagens**: `Veiculos`, `Vagas`, `VinculosVeiculoVaga`, `PermissoesVeiculares`,
`HistoricoVeiculos`, `HistoricoVagas`.

**Entregas**: `Entregas`, `HistoricoEntregas`.

**Equipamentos e Integrações**: `Equipamentos` (genérico, por fabricante — Control iD/JFL/
Intelbras/outros; Sprint 22B adicionou `EstadoOperacional` administrativo — Ativo/EmManutencao/
Inativo/Defeituoso — paralelo e independente do `Status` de conectividade, mais `MacAddress`/
`Observacoes`), `EventosEquipamento`, `StatusCentraisJfl`.

**Alocação de Hardware (Sprint 22B, ADR 0031)**: `VinculosEquipamentoPropriedade` — vínculo
Equipamento↔Propriedade com histórico (nunca apagado), usado pelo Painel Web
(Provisionamentos). Deliberadamente separado de `Provisionamentos` (metadado da Sprint 21,
ADR 0028) — mesmo termo de negócio, entidades diferentes. "Disponível"/"Provisionado" é
derivado de `DataFimUtc IS NULL`, nunca um campo próprio.

**Alarme (JFL)**: `Centrais`, `Zonas`, `Ocorrencias` (eventos Contact ID), `RegistrosEventoAlarme`.

**Câmeras**: `Gravadores`, `Cameras`, `VinculosZonaCamera`.

**Camada Operacional**: `SnapshotsOperacionais` (rollup de saúde da propriedade, substituível por
upsert, não é auditoria).

**Push**: `DispositivosPush`.

Todas as entidades do domínio principal (Propriedade/Unidade/Morador/Credencial/PontoAcesso/
Visitante/Autorizacao/Veiculo/Vaga/Entrega/Equipamento) usam **soft delete** (ADR 0009) — nunca
saem fisicamente do banco.

## Estrutura do Backend

- **`AppMorador.Api`**: `Controllers/` (33 hoje — finos, só extraem `User.GetUsuarioId()`/params e
  chamam um Servico, mapeiam `Result`→`IActionResult`), `Auth/` (Policies, extensões de
  `ClaimsPrincipal`, handler de auditoria de falha), `Middleware/` (CorrelationId + tempo de
  execução, enriquecimento de log com o usuário logado — Sprint 22B), `Realtime/` (publicadores
  SignalR), `Program.cs` (composição — DI, Policies, pipeline).
- **`AppMorador.Application`**: um namespace por domínio (`Propriedades/`, `Equipamentos/`,
  `Rbac/`, `Auditoria/`, `Provisionamentos/`, `Cameras/`, `Notificacoes/`, etc.) — cada um com
  `IXServico`/`XServico` (regra de negócio + ownership check), `Dtos.cs` (Request/Response),
  portas de repositório (`IXRepositorio`, interface, implementação em Infrastructure).
- **`AppMorador.Domain`**: `Entities/` (classes puras, sem EF Core/atributo nenhum de
  framework), `Repositories/` (só as interfaces), `Common/` (`EntidadeComSoftDelete`),
  `Snapshots/` (portas de captura de câmera).
- **`AppMorador.Infrastructure`**: `Persistence/` (`AppDbContext`, implementações de
  repositório, `Migrations/`, `Seed/DevelopmentSeeder`), `Identity/` (JWT, BCrypt, DI),
  `Jfl/`/`ControlId/`/`Intelbras/` (Providers de fabricante), `Notifications/` (Firebase),
  `Snapshots/` (captura HTTP/Digest Auth).
- **Middlewares**: autenticação JWT Bearer, autorização (Policies), rate limiting (`api/auth/*`),
  CORS, tratamento central de erro.

Convenção de nomes: sufixo, não prefixo (`PropriedadeServico`, não `ServicoPropriedade`;
`EquipamentoRepositorio`, não `RepositorioEquipamento`) — consistente nas 21 Sprints já
implementadas.

## Estrutura do Mobile (Design System, para replicar no Painel Web)

**Paleta oficial** (`theme/colors.ts`, tema escuro único hoje — sem toggle claro/escuro no
mobile): `primary #25C98D`, `background #0A0E13`, `surface #141C25`, `textPrimary #F2F6FA`,
`textSecondary #93A1B1`, `success #25C98D`, `warning #F5A524`, `error #FF4D4D`, `info #3DD6C4`,
`border #26313E`.

**Espaçamento** (`theme/spacing.ts`): `xs=4, sm=8, md=12, lg=16, xl=20, xxl=24, xxxl=32`.

**Raio de borda** (`theme/borders.ts`): `sm=8, md=12, lg=16, xl=20, full=999`.

**Tipografia** (`theme/typography.ts`): `hero 26/800`, `h1 22/700`, `h2 18/700`, `h3 16/600`,
`body 14/400`, `caption 12/400`, `label 11/600` (tamanho/peso/altura de linha juntos).

**Componentes reutilizáveis** (`src/components/`, 27 hoje): `PrimaryButton`, `TextField`,
`Skeleton`, `EstadoVazio`, `Toast`, `StatusChip`, `HeroCard`, `ActivityCard`, `PropertyCard`,
seletores de tipo (`TipoPropriedadeSelector`, `TipoCredencialSelector`, etc.).

**Navegação**: `RootNavigator` (autenticado vs. não-autenticado) → `MainTabNavigator` (4 abas
fixas — Início/Câmeras/Acessos/Ajustes, Navegação Previsível, ADR 0019, nunca esconde uma aba
dinamicamente).

## Referências

- Todas as decisões arquiteturais: `docs/adr/README.md` (28 ADRs).
- Roadmap e histórico de Sprints: `docs/roadmap/ROADMAP.md`.
- Lista completa de endpoints + Policy de cada um: `docs/painel/mapeamento-api.md`.
- Dívida técnica registrada: `docs/DIVIDA_TECNICA.md`.
