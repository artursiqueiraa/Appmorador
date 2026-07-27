# ADR 0021 — RBAC Master: Role Global (interno) vs. Perfil de Propriedade (cliente) (Sprint 21)

**Data**: 2026-07-26

## Contexto

A missão da Sprint 21 pedia a base de autorização da plataforma — papéis internos
(Master/Técnico/Suporte), impersonation, auditoria, permissões funcionais, feature flags,
capacidades de equipamento e provisionamento — assumindo uma arquitetura de multiusuário
(`UsuarioPropriedade` N:N, `Morador` autenticável, claims de papel no JWT, Policies, middleware de
tenant, log de auditoria genérico) que a Fase 0 (auditoria, `docs/audits/AUDIT_RBAC_021.md`)
confirmou **não existir de forma nenhuma**: o modelo real é `Propriedade.ProprietarioId` (dono
único, 1:N), `Morador` sem qualquer capacidade de login, zero Policies/claims/middleware, e nenhum
log de auditoria genérico (só `RegistroEventoAlarme`, específico de JFL).

## Problema

Como entregar a base de RBAC pedida sem forçar uma migração completa do modelo de propriedade
(`ProprietarioId` → `UsuarioPropriedade`) nesta mesma Sprint — um trabalho de escala e risco muito
maiores do que "base de permissões", e que o próprio usuário não pediu para esta entrega?

## Alternativas consideradas

- **Migrar tudo agora** (`UsuarioPropriedade` como fonte de verdade, `Morador` ganha login): mais
  fiel à missão original, mas exige reescrever a checagem de posse (`propriedade.ProprietarioId
  != usuarioId`) em toda `Servico` existente (`EquipamentoServico`, `CameraServico`,
  `PropriedadeServico`, `MoradorServico`, `PermissaoVeicularServico` — dezenas de pontos
  confirmados na auditoria) — risco de regressão alto, escopo de Sprint explode.
- **Só RBAC de internos, cliente inalterado** (escolhida): implementa 100% da infraestrutura
  para Master/Técnico/Suporte (Policies, Auditoria, Impersonation, Permissões, Feature Flags,
  Capacidades, Provisionamento), mas para o cliente mantém `ProprietarioId` como fonte de verdade.
  `UsuarioPropriedade` nasce como abstração preparatória (auto-criada, espelha `ProprietarioId`,
  nunca substitui o check existente).
- **Adiar tudo para depois do Multiusuário**: rejeitada — o Painel Web (Sprint 22) não teria
  nenhuma base de autorização para trabalhar em cima.

## Decisão

RBAC global (papéis internos) 100% implementado nesta Sprint. Para o cliente, `ProprietarioId`
continua sendo a fonte de verdade de posse — nenhuma `Servico` existente foi tocada. `UsuarioPropriedade`
existe como tabela real, auto-criada (1 registro `Administrador` por Propriedade, espelhando
`ProprietarioId`), e é o que as novas peças de RBAC (Policies+`IPermissaoService`, Permissões
Funcionais, impersonation) consultam — mas só código NOVO lê por ela. A evolução para Multiusuário
de verdade (Morador com login próprio, `UsuarioPropriedade` substituindo `ProprietarioId`) fica
para uma Sprint futura dedicada — decisão explícita do usuário: *"o multiusuário não é uma
correção, é uma evolução do negócio [...] isso mantém as Sprints menores, reduz o risco de
regressão e entrega valor mais rapidamente"*.

### Decisão 2 — `RequireAssertion` em vez de `AuthorizationHandler`/`IAuthorizationRequirement`

A missão exemplificava um `MasterAuthorizationHandler : AuthorizationHandler<MasterRequirement>`
completo por Policy. Como toda Policy aqui é uma checagem simples de presença/valor de claim
(`ctx.User.TemAlgumRoleGlobal(...)`), um par Requirement+Handler por Policy seria boilerplate sem
isolamento adicional — as 7 Policies (`RequerMaster/Tecnico/Suporte/Interno/Cliente/Administrador/
Morador`) são registradas via `options.AddPolicy(nome, p => p.RequireAssertion(...))` num único
bloco em `Program.cs`. Simplificação da mesma classe de decisão já registrada em Sprints
anteriores (debounce em memória vs. Redis).

### Decisão 3 — `role` claim só existe para internos

`Usuario.RoleGlobal` (nullable `RoleSistema`) só vira uma claim `"role"` no JWT quando não-nulo — um
token de cliente (Administrador) **não tem claim `role` nenhuma**. `EhInterno()`/`RequerCliente`
são literalmente "não tem claim `role`" — hoje isso é idêntico a `RequerAdministrador` (só
Administrador loga), mas o contrato já está pronto: quando Morador ganhar login próprio, só o
fluxo de login precisa mudar para `RequerMorador` passar a valer para alguém.

### Decisão 4 — Auditoria de falha de autorização centralizada, nunca espalhada

`AuditoriaAuthorizationMiddlewareResultHandler` decora o `AuthorizationMiddlewareResultHandler`
padrão do ASP.NET Core — observa `PolicyAuthorizationResult.Succeeded == false` e chama
`IAuditoriaService.RegistrarFalhaAutorizacaoAsync` antes de delegar ao handler padrão. Nenhum
Controller precisa (nem pode) chamar isso manualmente.

## Consequências

- Zero regressão: nenhuma `Servico`/Controller pré-existente foi alterada para checar
  `UsuarioPropriedade` — confirmado pelos 44 testes de backend pré-existentes continuando a
  passar sem alteração.
- Toda Propriedade nova ganha um `UsuarioPropriedade` (Administrador) + Permissões do Plano
  Básico automaticamente (`PropriedadeServico.CreateAsync`); propriedades pré-existentes foram
  backfilladas via seed (`GarantirVinculosUsuarioPropriedadeAsync`) — 12 propriedades, 72
  permissões concedidas, verificado contra o banco real.
- Dívida técnica explícita: `Morador` continua sem login — todo o desenho de Permissões
  Funcionais/Feature Flags já existe, mas só é exercitado pelo Administrador até essa Sprint
  futura acontecer.

## Impactos

Toda futura feature que precisar checar "o que este usuário pode fazer nesta propriedade" deve
usar `IPermissaoService` (nunca reimplementar a checagem). Toda futura Sprint de Multiusuário deve
ler este ADR antes de decidir como `UsuarioPropriedade` substitui `ProprietarioId`.

## Arquivos afetados

`AppMorador.Domain/Entities/{RoleSistema,Usuario,UsuarioPropriedade,PerfilPropriedade}.cs`,
`AppMorador.Api/Auth/{Policies,ClaimsPrincipalExtensions,AuditoriaAuthorizationMiddlewareResultHandler}.cs`,
`AppMorador.Api/Program.cs`, `AppMorador.Infrastructure/Identity/JwtTokenService.cs`,
`AppMorador.Application/Rbac/*`, `AppMorador.Application/Auditoria/*`,
`docs/audits/AUDIT_RBAC_021.md`.

## Como revisar futuramente

Revisar quando a Sprint de Multiusuário for planejada: nesse momento, decidir se
`UsuarioPropriedade` substitui `ProprietarioId` de fato (exigindo migrar as checagens de posse
espalhadas) ou se os dois continuam coexistindo permanentemente.
