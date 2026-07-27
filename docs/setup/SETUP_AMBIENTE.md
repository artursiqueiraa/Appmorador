# Setup de Ambiente do Zero

Guia para um desenvolvedor sem contexto prévio clonar o projeto, configurar o banco e rodar
Backend + Mobile localmente, sem nenhuma alteração manual de código. Escrito na Sprint 3.1
(homologação/estabilização) e atualizado na tarefa de infraestrutura de ambiente que se seguiu.

## Banco de dados utilizado

**MySQL 8.0+**, via Pomelo.EntityFrameworkCore.MySql (`AppMorador.Infrastructure`). Não há
suporte a SQL Server ou outro provider — a connection string e o EF Core estão configurados
especificamente para MySQL (`ServerVersion.AutoDetect`).

## Pré-requisitos

- .NET 8 SDK
- MySQL Server 8.0+
- Node.js 18+ e Expo CLI (`npx expo`) para o Mobile
- Ferramenta `dotnet-ef` instalada globalmente: `dotnet tool install --global dotnet-ef`

## 1. Banco de dados — criação do banco vazio (passo manual único)

Crie o banco (vazio, sem tabelas) e um usuário dedicado, restrito a esse banco (nunca usar
`root` na aplicação):

```sql
CREATE DATABASE appmorador CHARACTER SET utf8mb4;
CREATE USER 'appmorador'@'localhost' IDENTIFIED BY '<senha-forte-local>';
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES, DROP
  ON appmorador.* TO 'appmorador'@'localhost';
FLUSH PRIVILEGES;
```

Esse usuário **não tem** `CREATE DATABASE` — é uma restrição intencional de segurança (o usuário
de runtime da aplicação só pode operar dentro do próprio banco). **Por isso este passo continua
manual**: a Api aplica migrations automaticamente no startup (seção 3), mas não consegue criar o
banco em si com esse usuário — só um usuário privilegiado (`root` ou equivalente) pode fazer
`CREATE DATABASE`. Ver `docs/adr/0008-migrations-automaticas-no-startup.md` e
`docs/DIVIDA_TECNICA.md` item 8 para o racional completo. Depois deste único comando, **tudo mais
é automático** (tabelas e seed).

Se precisar de um banco de teste separado (ex.: para validar migrations do zero), crie-o também
com um usuário privilegiado e nunca aponte a aplicação de runtime para ele.

## 2. Segredos (nunca em appsettings.json committado)

`appsettings.json` tem placeholders (`User=root;Password=CHANGE_ME`) — a aplicação real usa
`dotnet user-secrets` em desenvolvimento (ou variáveis de ambiente `Jwt__Key` /
`ConnectionStrings__DefaultConnection` em produção). A partir de
`backend/src/AppMorador.Api/`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=appmorador;User=appmorador;Password=<senha-forte-local>;"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 64)"
```

Sem `Jwt:Key` configurada, a API lança `InvalidOperationException` na inicialização e **se
recusa a subir** — isso é intencional (dependência dura, diferente do JFL Server, que é
opcional; ver `docs/adr/`).

## 3. Migrations — aplicadas automaticamente, nenhum comando manual necessário

O schema completo hoje vive em uma única migration, `InitialCreate` (squash de todo o histórico
anterior — ver `ADR 0007` em `docs/adr/0007-squash-migrations-v030-alpha.md` para o porquê).

**A partir desta versão, a Api aplica migrations pendentes automaticamente no startup** (qualquer
ambiente — `Program.cs` chama `Database.MigrateAsync()` logo depois do banco vazio existir; ver
`docs/adr/0008-migrations-automaticas-no-startup.md`). Isso cria as 10 tabelas (`Usuarios`,
`Propriedades`, `Centrais`, `Zonas`, `Gravadores`, `Cameras`, `VinculosZonaCamera`, `Ocorrencias`,
`RegistrosEventoAlarme`, `RefreshTokens`) e o `__EFMigrationsHistory` no primeiro `dotnet run`
(seção 4) — **nenhum `dotnet ef database update` manual é necessário** no fluxo normal.

Rodar manualmente continua útil para:

- **Verificar migrations sem subir a Api**:
  ```bash
  dotnet ef database update --project src/AppMorador.Infrastructure --startup-project src/AppMorador.Api
  ```
- **Validar que não há divergência entre o modelo atual e as migrations, sem tocar em nenhum
  banco**:
  ```bash
  dotnet ef migrations has-pending-model-changes --project src/AppMorador.Infrastructure --startup-project src/AppMorador.Api
  ```
  Deve responder "No changes have been made to the model since the last migration."

## 4. Rodar o Backend

A partir de `backend/src/AppMorador.Api/`:

```bash
dotnet run
```

Sequência de log esperada num ambiente novo (banco vazio recém-criado no passo 1):

```
Verificando banco de dados...
Migration(ns) pendente(s) encontrada(s): 20260718230440_InitialCreate. Aplicando...
Migration(ns) aplicada(s) com sucesso.
Servidor JFL escutando na porta 8085
Executando seed de desenvolvimento...
Seed de desenvolvimento: conta Administrador criada (...).
Seed de desenvolvimento: conta Carlos Henrique criada (...).
Seed de desenvolvimento: conta Juliana Souza criada (...).
Seed de desenvolvimento: conta Morador criada (...) com 1 propriedade, 1 central, 2 zonas, 5 ocorrencias.
Now listening on: http://localhost:5027
Sistema pronto.
```

Em execuções seguintes (banco já em dia), aparece só `Banco localizado, nenhuma migration
pendente.` e `Seed de desenvolvimento: todas as contas ja existem — nada a fazer.` — nenhuma
migration é reaplicada, nenhum dado é duplicado.

Se a porta 8085 (JFL) estiver ocupada por outra instância, a Api sobe normalmente mesmo assim (log
de erro específico, sem derrubar o processo — por design). Swagger fica disponível em
`http://localhost:<porta>/swagger` (só em ambiente Development).

**Falha de migration/banco é fatal, por design**: se o banco não existir (passo 1 não foi feito)
ou a connection string estiver errada, a Api lança uma exceção não tratada e não sobe — é uma
dependência dura, mesmo critério que `Jwt:Key` ausente, nunca deve ser silenciada.

## 5. Rodar o Mobile

A partir de `mobile/`:

```bash
npm install
npx expo start
```

Aponte a URL base da Api (`src/api/client.ts` ou equivalente) para o host/porta onde o Backend
está rodando.

### Gerando um build EAS (preview/production)

`mobile/eas.json` define `EXPO_PUBLIC_API_URL` por perfil de build — é um valor de *build-time*
(a EAS lê direto deste arquivo, não do `.env` local). O valor versionado é um placeholder
(`http://SEU_IP_LOCAL:5027`) de propósito — antes de rodar `eas build`, substitua pelo IP da sua
própria máquina na rede local (`ipconfig`/`ifconfig`), nunca commitando o IP real de volta ao
repositório.

## 6. Dados de teste (seed de desenvolvimento)

Em ambiente Development, a Api roda automaticamente um seed idempotente logo após subir e depois
de aplicar as migrations (`AppMorador.Infrastructure.Persistence.Seed.DevelopmentSeeder`,
disparado em `Program.cs` só quando `IsDevelopment()`). Cada conta é verificada por e-mail antes
de ser inserida — rodar `dotnet run` várias vezes nunca duplica dados. Uma falha no seed nunca
derruba a Api (mesma filosofia do JFL Server: serviço secundário, erro só é logado).

**Contas criadas na primeira execução:**

| Persona | Nome | E-mail | Senha |
|---|---|---|---|
| Administrador | Administrador | `admin@appmorador.local` | `Admin@123` |
| Supervisor | Carlos Henrique | `carlos.henrique@appmorador.local` | `Supervisor@123` |
| Operador | Juliana Souza | `juliana.souza@appmorador.local` | `Operador@123` |
| Morador | Fernanda Oliveira | `fernanda.oliveira@appmorador.local` | `Morador@123` |
| Master (RBAC, Sprint 21) | Master AppMorador | `master@appmorador.local` | `Master@123` |

**Importante**: as 4 primeiras contas (Administrador/Supervisor/Operador/Morador) continuam
funcionalmente idênticas entre si — são todas clientes (nenhuma tem `RoleGlobal`), os nomes só dão
variedade realista aos dados de teste. Desde a Sprint 21 (ADR 0021/0025) o domínio **tem** um
sistema real de RBAC (papéis internos Master/Técnico/Suporte + Permissões Funcionais/Feature
Flags por propriedade) — a conta **Master** acima é a única com acesso aos endpoints internos
(`/api/usuarios-internos`, `/api/auditoria`, impersonation). Ver `docs/DIVIDA_TECNICA.md` item 6
(ainda válido para CRUD completo de usuário cliente/Morador com login próprio).

Só a conta **Morador** (Fernanda Oliveira) tem dados de propriedade vinculados, por ser a única
persona com sentido de "dona de propriedade" no modelo atual (produto B2C self-service):

- **1 propriedade**: "Residencial Jardim das Flores" (tipo Residencial)
- **1 central** (`000002`) com **2 zonas** ("Sala", "Garagem")
- **5 ocorrências** de teste (código Contact ID `1130`, espaçadas 6h entre si — cobrem os
  filtros de período Hoje/7 dias/30 dias/Tudo da Central de Eventos)

Bancos de desenvolvimento que já tinham o seed antigo (usuário único `teste@appmorador.com.br` /
"Residência Modelo") continuam com essa conta intacta — o seed é aditivo, nunca remove dado
existente.

## Como remover o banco local

```sql
DROP DATABASE appmorador;
```

Precisa de um usuário privilegiado (`root`) — o usuário `appmorador` de runtime não tem `DROP
DATABASE` pelo mesmo motivo que não tem `CREATE DATABASE` (privilégio escopado só ao próprio
banco). Isso também apaga todos os dados reais, não só o seed — nunca rodar contra um ambiente
que não seja um banco de desenvolvimento local descartável.

## Como recriar o ambiente do zero

1. Remover o banco local (seção acima).
2. Repetir o passo 1 deste guia (`CREATE DATABASE` + `CREATE USER` + `GRANT`).
3. Rodar `dotnet run` (seção 4) — migrations e seed são aplicados automaticamente, sem nenhum
   outro comando manual.

## Troubleshooting

- **API não sobe, erro de `Jwt:Key`**: rode o passo 2 (user-secrets).
- **API não sobe, `Unhandled exception... Access denied for user 'appmorador'@'localhost' to
  database '<nome>'` logo após "Verificando banco de dados..."**: o banco em si ainda não existe
  (ou o nome/senha na connection string está errado). Migrations são automáticas, mas criar o
  banco vazio continua manual (passo 1) — o usuário de runtime não tem `CREATE DATABASE` por
  design. Rode o passo 1 e tente de novo.
- **Erro `Access denied ... to database` ao tentar criar outro banco com o usuário
  `appmorador`**: mesmo motivo acima — esse usuário só tem privilégio sobre o banco `appmorador`.
  Use um usuário com mais privilégio (ex.: `root`) só para criar bancos de teste avulsos.
- **Porta 8085 (JFL) em uso por outra instância**: a API sobe normalmente mesmo assim; o log
  registra o conflito com uma mensagem específica. Encerre a instância zumbi se quiser que o JFL
  Server funcione nesta instância.
