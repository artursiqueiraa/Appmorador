# Variáveis de Ambiente

Sprint 17.5 (Release 0.9.0). Duas superfícies, dois mecanismos diferentes — nenhum dos dois lê um
arquivo `.env` automaticamente hoje; os dois `.env.example` deste projeto (`backend/.env.example`,
`mobile/.env.example`) são **templates de referência**, não arquivos carregados por uma lib
`dotenv`.

## Backend (`backend/.env.example`)

ASP.NET Core lê variáveis de ambiente reais do processo (convenção `__` para seções aninhadas do
`appsettings.json`). Em desenvolvimento local, o caminho recomendado é `dotnet user-secrets` (fora
da árvore do repositório, nunca versionado) — ver `docs/setup/SETUP_AMBIENTE.md` seção 2. Em
produção, defina as mesmas chaves como variáveis de ambiente reais do processo/serviço.

| Variável | Obrigatória | Padrão | Descrição |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | **Sim** | — (placeholder no `appsettings.json`) | Connection string MySQL. Sem ela (ou com credencial errada), a Api falha ao subir — dependência dura, por design. |
| `Jwt__Key` | **Sim** | — | Chave de assinatura dos tokens JWT. Sem ela, a Api lança `InvalidOperationException` no startup e se recusa a subir (intencional). Gerar com `openssl rand -base64 64` ou equivalente. |
| `Jwt__Issuer` | Não | `AppMorador` | Emissor do token. |
| `Jwt__Audience` | Não | `AppMoradorApp` | Audiência esperada do token. |
| `Jwt__AccessTokenMinutes` | Não | `20` | Validade do access token. |
| `Jwt__RefreshTokenDays` | Não | `30` | Validade do refresh token. |
| `Jfl__Porta` | Não | `8085` | Porta TCP onde o servidor JFL (protocolo de centrais de alarme) escuta. Se ocupada, a Api sobe normalmente mesmo assim (log de erro específico, não derruba o processo). |
| `Jfl__IntervaloKeepAliveMinutos` | Não | `5` | Intervalo de keep-alive esperado das centrais conectadas. |
| `Snapshots__BasePath` | Não | `snapshots` | Pasta onde imagens capturadas no disparo de alarme são salvas (relativa ao diretório de execução). Ver `docs/STORAGE.md`. |
| `Snapshots__TimeoutSeconds` | Não | `5` | Timeout da captura de snapshot via CGI/ISAPI. |
| `Cors__AllowedOrigins__0`, `__1`, ... | Não | `[]` (produção) / `http://localhost:8081` (Development, hardcoded em `appsettings.Development.json`) | Origens permitidas por CORS. ASP.NET Core não aceita array direto via env var — cada item precisa de um índice próprio (`__0`, `__1`, ...). |
| `ASPNETCORE_ENVIRONMENT` | Não | `Production` (implícito fora de dev) | `Development` habilita Swagger (`/swagger`) e o seed automático de desenvolvimento (`DevelopmentSeeder`). **Nunca usar `Development` em produção** — criaria contas de teste com senha conhecida publicamente (ver `docs/setup/SETUP_AMBIENTE.md`). |

## Mobile (`mobile/.env.example`)

Expo lê `.env` automaticamente via `EXPO_PUBLIC_*` (convenção do próprio Expo SDK — variáveis com
esse prefixo ficam embutidas no bundle JS, então **nunca** colocar segredo aqui, só configuração
pública como a URL da API).

| Variável | Obrigatória | Padrão | Descrição |
|---|---|---|---|
| `EXPO_PUBLIC_API_URL` | **Sim** | `http://localhost:5027` | URL base da Api. Em emulador Android, `localhost` do host não é acessível diretamente — use o IP da máquina na rede local (ex.: `http://192.168.100.3:5027`, mesmo valor usado no perfil `preview`/`production` de `eas.json`) para testar num celular físico. |

## Por que não existe `.env.example` combinado na raiz

Backend e Mobile têm mecanismos de configuração diferentes (env vars reais do processo vs. Expo
`.env`) — um `.env.example` único na raiz sugeriria (incorretamente) que as duas stacks
compartilham o mesmo carregamento de configuração. Cada stack mantém o próprio template, mais
próximo de onde é de fato consumido.

## Segurança

Nenhum segredo real aparece em nenhum `.env.example` ou `appsettings.json` versionado — sempre
`CHANGE_ME` ou placeholder equivalente. Segredos reais vivem em: `dotnet user-secrets` (backend,
dev), variáveis de ambiente do processo (backend, produção), ou nunca existem no caso do Mobile
(não há segredo real do lado do cliente — a Api nunca confia em nada vindo do app sem validar
autenticação/posse no próprio backend).
