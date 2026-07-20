# AppMorador — Segurança Conectada

Aplicativo de segurança self-service (B2C/B2B) para residências e pequenos comércios/condomínios:
cadastro, propriedades, central de alarme (protocolo JFL), câmeras e um dashboard que traduz tudo
isso em linguagem simples e tranquilizadora para quem não é técnico.

**Versão atual**: `v0.3.0-alpha` — primeira baseline estável do projeto.

## Arquitetura

Backend em Clean Architecture:

```
Domain (entidades/regras puras)
  → Application (casos de uso, DTOs, portas)
    → Infrastructure (EF Core, JWT, protocolo JFL, integração de câmeras)
      → Api (controllers finos)
```

Mobile é um app React Native/Expo consumindo a Api via REST/JSON, sem lógica de negócio
duplicada — só apresentação e estado de sessão.

Domínio de negócio (entidades, DTOs, serviços) em **português**; infraestrutura/protocolo/
convenção .NET-HTTP em **inglês** — ver
[`docs/adr/0003-dominio-negocio-pt-br.md`](docs/adr/0003-dominio-negocio-pt-br.md) para a regra
de fronteira completa.

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Backend | .NET 8, ASP.NET Core, EF Core (Pomelo/MySQL), JWT + refresh token, BCrypt |
| Mobile | React Native, Expo, TypeScript, React Navigation, Reanimated |
| Banco de dados | MySQL 8.0+ |
| Protocolo de alarme | JFL (TCP proprietário) — infraestrutura, nunca domínio de negócio |

## Requisitos

- .NET 8 SDK
- MySQL Server 8.0+
- Node.js 18+ e Expo CLI (`npx expo`)
- `dotnet-ef`: `dotnet tool install --global dotnet-ef`

## Instalação

```bash
git clone https://github.com/artursiqueiraa/Appmorador.git
cd Appmorador
```

## Configuração

### 1. Banco de dados (único passo manual)

```sql
CREATE DATABASE appmorador CHARACTER SET utf8mb4;
CREATE USER 'appmorador'@'localhost' IDENTIFIED BY '<senha-forte-local>';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES, DROP
  ON appmorador.* TO 'appmorador'@'localhost';
FLUSH PRIVILEGES;
```

O usuário de runtime é deliberadamente restrito ao próprio banco (sem `CREATE DATABASE`) — por
isso este passo é manual. Tudo depois disso (tabelas, seed) é automático. Ver
[`docs/adr/0008-migrations-automaticas-no-startup.md`](docs/adr/0008-migrations-automaticas-no-startup.md).

### 2. Segredos (nunca em `appsettings.json`)

A partir de `backend/src/AppMorador.Api/`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=appmorador;User=appmorador;Password=<senha-forte-local>;"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
```

### 3. Mobile

```bash
cd mobile
cp .env.example .env   # ajuste EXPO_PUBLIC_API_URL se o Backend rodar em outro host/porta
npm install
```

## Banco de dados

O schema completo vive em uma única migration (`InitialCreate`, squash do histórico — ver
[`docs/adr/0007-squash-migrations-v030-alpha.md`](docs/adr/0007-squash-migrations-v030-alpha.md)),
aplicada **automaticamente no startup da Api** — nenhum `dotnet ef database update` manual é
necessário no fluxo normal. Detalhe completo, troubleshooting, como remover/recriar o ambiente:
[`docs/setup/SETUP_AMBIENTE.md`](docs/setup/SETUP_AMBIENTE.md).

## Execução

**Backend** (a partir de `backend/src/AppMorador.Api/`):

```bash
dotnet run
```

Sinais de sucesso: `Verificando banco de dados...` → `Banco localizado, nenhuma migration
pendente.` (ou migrations sendo aplicadas, na primeira vez) → `Seed de desenvolvimento...` →
`Now listening on: http://localhost:5027` → `Sistema pronto.` Swagger disponível em
`http://localhost:5027/swagger` (só em Development).

**Mobile** (a partir de `mobile/`):

```bash
npx expo start
```

## Usuários de desenvolvimento / credenciais de teste

Em ambiente Development, a Api popula automaticamente 4 contas de teste no primeiro `dotnet run`
(idempotente — rodar de novo nunca duplica):

| Persona | E-mail | Senha |
|---|---|---|
| Administrador | `admin@appmorador.local` | `Admin@123` |
| Supervisor | `carlos.henrique@appmorador.local` | `Supervisor@123` |
| Operador | `juliana.souza@appmorador.local` | `Operador@123` |
| Morador | `fernanda.oliveira@appmorador.local` | `Morador@123` |

**As 4 contas são funcionalmente idênticas** — o domínio ainda não tem sistema de Papel/Perfil
(ver [`docs/DIVIDA_TECNICA.md`](docs/DIVIDA_TECNICA.md) item 6). Só a conta **Morador** (Fernanda
Oliveira) tem uma propriedade de exemplo vinculada ("Residencial Jardim das Flores", com central,
zonas e ocorrências de teste) — é a conta certa para explorar Dashboard e Central de Eventos.

## Estrutura do projeto

```
appmorador/
├── backend/            # .NET 8 — Clean Architecture (Domain/Application/Infrastructure/Api)
├── mobile/              # React Native/Expo
├── docs/
│   ├── adr/             # Decisões de arquitetura (ADRs)
│   ├── roadmap/         # ROADMAP.md
│   ├── reviews/         # Relatórios de cada Sprint concluída
│   ├── sprints/         # Especificações de Sprint (a partir da Sprint 4)
│   ├── setup/           # Guia de ambiente + relatórios de infraestrutura
│   ├── CHANGELOG.md
│   └── DIVIDA_TECNICA.md
├── .agents/             # Definições dos agentes de engenharia do projeto
├── .rules/              # Regras permanentes por domínio
└── CLAUDE.md            # Roteador oficial do fluxo de desenvolvimento
```

**Nota sobre testes**: o projeto ainda não tem uma suíte de testes automatizados — toda
homologação até `v0.3.0-alpha` foi feita via requests reais (`curl`) contra uma instância viva,
documentada em [`docs/TESTES_FUNCIONAIS.md`](docs/TESTES_FUNCIONAIS.md) e nos relatórios de
`docs/reviews/`. Uma pasta `tests/` só será criada quando houver testes reais para colocar nela.

## Roadmap

Ver [`docs/roadmap/ROADMAP.md`](docs/roadmap/ROADMAP.md) para o histórico completo de Sprints
concluídas e o backlog priorizado.

## Documentação

- [`CLAUDE.md`](CLAUDE.md) — fluxo oficial de desenvolvimento (como agentes/regras/decisões se
  combinam para qualquer tarefa nova).
- [`docs/adr/`](docs/adr/) — todas as decisões de arquitetura, com contexto e alternativas
  consideradas.
- [`docs/setup/SETUP_AMBIENTE.md`](docs/setup/SETUP_AMBIENTE.md) — guia detalhado de ambiente,
  troubleshooting.
- [`docs/DIVIDA_TECNICA.md`](docs/DIVIDA_TECNICA.md) — toda dívida técnica conhecida, com
  motivo, impacto e prioridade.
- [`docs/CHANGELOG.md`](docs/CHANGELOG.md) — histórico cronológico de mudanças.
- [`docs/reviews/`](docs/reviews/) — relatório e parecer do Reviewer de cada Sprint.

## Licença

Ainda não definida — ver [`docs/DIVIDA_TECNICA.md`](docs/DIVIDA_TECNICA.md).
