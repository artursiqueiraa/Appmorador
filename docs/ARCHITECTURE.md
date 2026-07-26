# Arquitetura — AppMorador

Sprint 17.5 (Release 0.9.0). Visão de componentes e fluxos — decisões detalhadas vivem nos ADRs
(`docs/adr/`), este documento é o mapa para orientação rápida.

## Componentes

```
┌─────────────────────┐        HTTP/REST + SignalR        ┌──────────────────────────┐
│   Mobile (Expo)      │ ────────────────────────────────► │  Backend (ASP.NET Core)  │
│  React Native 0.86    │ ◄──────────────────────────────── │  .NET 8, Clean Arch.     │
└─────────────────────┘                                    └────────────┬─────────────┘
                                                                          │ EF Core (Pomelo)
                                                                          ▼
                                                              ┌──────────────────────────┐
                                                              │   MySQL 8.0              │
                                                              └──────────────────────────┘
                    ┌──────────────────────────────────────────────────┐
                    │  Integrações de fabricante (fora do processo)     │
                    │  JFL (TCP, conexão invertida) · Control iD (HTTP) │
                    │  Intelbras (HTTP/CGI) · Dahua/Hikvision (snapshot)│
                    └──────────────────────────────────────────────────┘
```

**Não existe Frontend Web** — só Backend + Mobile (confirmado na auditoria, ver
`docs/AUDITORIA_AMBIENTE.md`).

## Backend — Clean Architecture (5 projetos)

```
AppMorador.Domain          → entidades e regras puras, zero dependência externa
AppMorador.Application     → casos de uso, DTOs, portas (interfaces) — orquestra Domain
AppMorador.Infrastructure  → EF Core, JWT, integrações de fabricante (Providers), implementa as portas da Application
AppMorador.Jfl             → servidor TCP do protocolo JFL (biblioteca própria, referenciada por Infrastructure/Api)
AppMorador.Api             → controllers finos, composition root (Program.cs), SignalR Hub
```

Regra de dependência: `Api` → `Application` + `Infrastructure` → `Domain`. `Domain` nunca
referencia nenhum outro projeto. `Application` nunca referencia `Infrastructure` diretamente — só
através de portas (interfaces) que `Infrastructure` implementa e `Api` registra via DI
(`Program.cs`/`*ServiceCollectionExtensions.cs`).

### Módulos de `Application` (por domínio de negócio)

`Autenticacao`, `Propriedades`, `Unidades`, `Moradores`, `Dashboard`, `Eventos`,
`Credenciais`/`PermissoesAcesso`/`PontosAcesso` (Controle de Acesso, ADR 0010), `Visitantes`/
`Autorizacoes` (ADR 0011), `Veiculos`/`Vagas`/`VinculosVeiculoVaga`/`PermissoesVeiculares` (ADR
0012), `Entregas` (ADR 0013), `Equipamentos`/`ControlId`/`Jfl`/`Intelbras` (integrações de
fabricante, ADR 0014/0015/0018), `Operacional` (Camada Operacional Unificada, ADR 0016).

### Fluxo de eventos/tempo real

```
Provider (JFL/ControlId/Intelbras) → mutação de estado (Equipamento/Ocorrencia/StatusCentralJfl)
  → SnapshotOperacionalServico.RegenerarEPublicarAsync (ADR 0016)
  → IOperacionalEventoPublicador (porta, Application)
  → OperacionalHubPublicador (Api/Realtime/, ADR 0017)
  → SignalR → grupo `propriedade:{id}` → Mobile (RealtimeContext)
```

SignalR é só transporte — nenhum tipo `Microsoft.AspNetCore.SignalR` aparece em `Domain`/
`Application` (ADR 0017). Se a implementação da porta for removida, o domínio continua
funcionando integralmente (pull manual via REST continua existindo em toda tela).

### Autenticação

JWT (access + refresh token) + BCrypt para hash de senha. Sem RBAC/Perfil real no domínio (dívida
técnica item 6) — qualquer usuário autenticado tem acesso igual às próprias Propriedades
(ownership check via `ProprietarioId`, não papel). O `perfil` morador/técnico introduzido na
Sprint 17 é só uma preferência de UI local no Mobile (ADR 0020), sem relação nenhuma com
autorização real no Backend.

## Mobile — React Native/Expo

```
src/
├── api/          cliente HTTP (client.ts, ApiError, mapeamento de erro) + tipos (types.ts)
├── auth/          AuthContext, secureStorage, profilePreference (perfil morador/técnico)
├── realtime/      RealtimeContext (cliente SignalR)
├── navigation/    RootNavigator, tipos de rota
├── screens/       uma pasta por domínio (home, acessos, credenciais, equipamentos, ajustes, ...)
├── components/    Design System (HeroCard, EstadoVazio, Toast, PrimaryButton, ...)
├── theme/         tokens (colors, spacing, typography, ...) — fonte única de verdade visual
├── acessos/       CommandCard + pgmLabels (Painel de Controle, Sprint 17)
├── credenciais/    fotoFacialLocal (Sprint 17)
├── onboarding/    Onboarding Wizard persistente (Sprint 16)
├── services/      utilidades de plataforma (feedback tátil, etc.)
└── utils/         formatação, mapeamento de erro (errorMapper.ts, Sprint 17)
```

Sem lógica de negócio duplicada — o Mobile só apresenta dado e gerencia estado de sessão/UI; toda
regra vive no Backend. Consome a API via REST/JSON (`api/client.ts`) e recebe atualização em tempo
real via SignalR (`realtime/RealtimeContext.tsx`).

## Fluxos principais

1. **Login** → `POST /api/auth/login` → JWT (access + refresh) → `AuthContext` guarda em
   `expo-secure-store` (`auth/secureStorage.ts`).
2. **Seleção de Propriedade** → `GET /api/properties` → `AuthContext.selectProperty`.
3. **Início (HeroCard)** → `GET /api/properties/{id}/dashboard` no load + atualização automática
   via SignalR (sem refresh manual) — status derivado de `saude`/`quantidadeAlarmesAtivos`/
   `quantidadeParticoesArmadas`.
4. **Painel de Controle (Acessos, Sprint 17)** → `GET /api/equipamentos/{id}/jfl/status` (só
   fabricante JFL) → `POST /api/equipamentos/{id}/jfl/pgm/acionar` para cada comando.
5. **Central de Eventos** → `GET /api/properties/{id}/eventos` (paginado, filtro por período/
   busca) — fonte plugável (`IFonteEventos`, ADR 0006), hoje só `JflFonteEventos`.

## Banco de Dados

MySQL 8.0, 31 tabelas de domínio + `__EFMigrationsHistory`. Migrations aplicadas automaticamente
no startup da Api (ADR 0008). Soft delete é o padrão para o domínio principal desde a Sprint 6
(ADR 0009). Ver `docs/AUDITORIA_AMBIENTE.md` para a lista completa de tabelas e
`database/README.md` para backup/restore.

## Decisões arquiteturais completas

Ver `docs/adr/README.md` para o índice completo (0001-0020) — cada decisão relevante desta
arquitetura tem um registro próprio com contexto, alternativas consideradas e consequências.
