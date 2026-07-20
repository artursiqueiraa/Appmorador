# Relatório — Sprint de Padronização do Domínio (pt-BR)

**Data**: 2026-07-19
**Objetivo**: renomear o domínio de negócio inteiro (criado em inglês na Sprint 1) para
português, preservando arquitetura, dados existentes e sem regressão. Sprint estritamente
rename-only — nenhuma funcionalidade nova, nenhum campo novo, nenhuma regra de negócio mudou.

## Escopo executado

Sprint inserida antes da Sprint 2 (Dashboard Premium) por decisão explícita do usuário, com uma
exceção combinada: nomes de infraestrutura/ecossistema .NET (Controllers, Middleware,
`Program.cs`, `DbContext`, Entity Framework, `RefreshToken`, JWT, OAuth) ficam em inglês. A regra
de fronteira completa está documentada em `docs/adr/0003-dominio-negocio-pt-br.md` (ADR 0003).

## Tabela de renomeação — Entidades e Enums (Domain)

| Antes | Depois |
|---|---|
| `User` | `Usuario` |
| `Property` | `Propriedade` |
| `AlarmPanel` | `Central` |
| `Zone` | `Zona` |
| `Camera` | `Camera` (mantém — já era português) |
| `Dvr` | `Gravador` |
| `Occurrence` | `Ocorrencia` |
| `ZoneCameraLink` | `VinculoZonaCamera` |
| `AlarmEventLog` | `RegistroEventoAlarme` |
| `RefreshToken` | `RefreshToken` (exceção — vocabulário JWT/OAuth) |
| `ResolutionStatus` (enum) | `StatusResolucao` (valores: `Resolved`/`Unresolved` → `Resolvido`/`NaoResolvido`) |
| `EventProcessingResult` (enum) | `ResultadoProcessamentoEvento` (valor `UnknownContactId` → `CodigoDesconhecido`) |
| `DvrFabricante` (enum) | `FabricanteGravador` |

Campos de negócio renomeados (exemplos principais): `PasswordHash`→`SenhaHash`,
`FailedLoginAttempts`→`TentativasFalhas`, `LockedUntilUtc`→`BloqueadoAteUtc`,
`OwnerUserId`→`ProprietarioId`, `Username`/`Password` (em `Gravador`)→`NomeAcesso`/`Senha`, e
todas as FKs que apontam para entidades renomeadas (`AlarmPanelId`→`CentralId`,
`PropertyId`→`PropriedadeId`, `ZoneId`→`ZonaId`, `DvrId`→`GravadorId`, `UserId`→`UsuarioId`).

## Camadas atualizadas

- **Domain**: 12 arquivos de entidades/enums renomeados; interfaces de repositório
  `IUserRepository`→`IUsuarioRepositorio`, `IPropertyRepository`→`IPropriedadeRepositorio`.
- **Application**: pastas `Auth/`→`Autenticacao/`, `Properties/`→`Propriedades/` (`Dashboard/`
  mantém o nome). `AuthService`→`AutenticacaoServico`, `PropertyService`→`PropriedadeServico`,
  `DashboardService`→`DashboardServico`. DTOs: `RegisterRequest`→`CadastrarUsuarioRequest`,
  `LoginRequest`→`EntrarRequest`, `LoginResult`→`EntrarResponse`, `LogoutRequest`→`SairRequest`,
  `CreatePropertyRequest`→`CriarPropriedadeRequest`, `UpdatePropertyRequest`→
  `AtualizarPropriedadeRequest`, `PropertyDto`→`PropriedadeResponse`, `DashboardDto`→
  `DashboardResponse` (campo `HealthScore`→`PontuacaoSaude`). `RefreshRequest` mantido (vocabulário
  de refresh token, mesma exceção do JWT/OAuth).
- **Infrastructure**: `UserRepository`→`UsuarioRepositorio`, `PropertyRepository`→
  `PropriedadeRepositorio`, `DashboardQueryService`→`ConsultaDashboardServico`.
  `AppDbContext` com todos os `DbSet` renomeados e `OnModelCreating` atualizado.
  `AlarmEventProcessor` com variáveis internas renomeadas (`panel`→`central`, `zone`→`zona`,
  `occurrence`→`ocorrencia`). Providers de snapshot (CGI/ISAPI) mantêm nome de classe (protocolo),
  mas usam `FabricanteGravador` e `camera.Gravador`/`gravador.NomeAcesso`/`gravador.Senha`.
- **Api**: Controllers mantêm nome de classe (exceção .NET); rotas HTTP não mudaram
  (`/api/auth/...`, `/api/properties/...`) — só os DTOs/JSON do corpo. `ClaimsPrincipalExtensions.
  GetUserId()`→`GetUsuarioId()`.
- **Mobile**: `types.ts` (`PropertyDto`→`PropriedadeResponse`, `LoginResult`→`EntrarResponse`,
  `DashboardDto`→`DashboardResponse`, `healthScore`→`pontuacaoSaude`, `userId`→`usuarioId`),
  `AuthContext.tsx` (corpo de login/register agora envia `senha` em vez de `password`),
  `DashboardScreen.tsx`, `SelecionarPropriedadeScreen.tsx`.

## Migration

`PadronizacaoDominioPtBr` — gerada via `dotnet ef migrations add` e corrigida manualmente
(substituindo todo `DropTable`/`DropColumn` que o EF gerou por `RenameTable`/`RenameColumn`/
`RenameIndex` + `DropForeignKey`/`AddForeignKey`). Resumo técnico completo e aprovação em
`ALTERACOES_BANCO.md`. Verificado via `dotnet ef migrations script` que o SQL final é 100%
rename, sem nenhuma operação destrutiva.

## Verificação

1. `dotnet build` (backend) — compilação limpa, 0 erros, 0 avisos.
2. `npx tsc --noEmit` (mobile) — sem erros de tipo.
3. Migration aplicada com aprovação explícita do usuário; dados confirmados intactos antes/depois
   (2 usuários, 1 propriedade com FK correta, 5 refresh tokens).
4. Fluxo completo testado via `curl` contra a API renomeada: cadastro → login → listar
   propriedades → criar propriedade → dashboard → refresh token — todos os endpoints responderam
   corretamente com os novos nomes de campo (`senha`, `usuarioId`, `pontuacaoSaude`, etc.).

## Status

Sprint concluída. Nenhuma funcionalidade nova foi adicionada — apenas nomenclatura. A partir de
agora, todo código novo segue obrigatoriamente o padrão pt-BR (ADR 0003). A Sprint 2 (Dashboard
Premium) permanece aguardando autorização explícita para início.
