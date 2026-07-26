# Auditoria do Ambiente — Sprint 17.5 (Release 0.9.0)

Mapeamento completo do ambiente de desenvolvimento/execução do AppMorador, gerado por inspeção
direta desta máquina (versões instaladas, não suposições) em 2026-07-25. Base para
`docs/SETUP.md`, `docs/DISASTER_RECOVERY.md` e para qualquer desenvolvedor novo reproduzir o
ambiente do zero.

## Arquitetura (visão geral)

Duas superfícies de cliente, um Backend, um banco:

```
Mobile (Expo/React Native)  ──HTTP/REST + SignalR──►  Backend (ASP.NET Core .NET 8)  ──EF Core──►  MySQL 8.0
                                                              │
                                                              ├── Servidor TCP JFL (porta 8085, protocolo proprietário de centrais de alarme)
                                                              └── Clientes HTTP para Control iD / Intelbras / Dahua / Hikvision / EAS Build (nuvem)
```

**Não existe Frontend Web** — só Backend + Mobile (confirmado por ausência de qualquer diretório
de projeto web no repositório). Ver `docs/ARCHITECTURE.md` para o detalhe de camadas.

## Backend

| Item | Valor confirmado nesta máquina |
|---|---|
| SDK | .NET 8 SDK — `dotnet --version` → `8.0.421` |
| Target Framework | `net8.0` (todos os 5 projetos: Domain, Application, Infrastructure, Jfl, Api) |
| ORM | Microsoft.EntityFrameworkCore `8.0.10` |
| Provider de banco | Pomelo.EntityFrameworkCore.MySql `8.0.2` |
| Autenticação | Microsoft.AspNetCore.Authentication.JwtBearer `8.0.10` + System.IdentityModel.Tokens.Jwt `8.0.2` |
| Hash de senha | BCrypt.Net-Next `4.0.3` |
| Documentação de API | Swashbuckle.AspNetCore `6.6.2` (Swagger, só em `Development`) |
| Ferramenta de migrations | `dotnet-ef` `10.0.9` (global tool) |
| Porta HTTP (dev) | `5027` (perfil `http`, `launchSettings.json`) |
| Porta HTTPS (dev) | `7234` (perfil `https`) |
| Porta TCP JFL | `8085` (configurável via `Jfl:Porta`) |
| SignalR | Embutido no mesmo processo Api (`Api/Realtime/`), mesma porta HTTP — nenhum serviço/porta separada |

## Frontend Web

**Não existe.** Nenhum diretório de projeto web (React/Vue/Angular/Next) no repositório. Ver
`docs/ARCHITECTURE.md`.

## Mobile

| Item | Valor confirmado nesta máquina |
|---|---|
| Node.js | `v24.16.0` |
| npm | `12.0.1` |
| Expo SDK | `~57.0.8` |
| React | `19.2.3` |
| React Native | `0.86.0` |
| React Navigation | `@react-navigation/native ^7.3.11`, `native-stack ^7.18.3` |
| Animação | `react-native-reanimated 4.5.0` |
| Tempo real | `@microsoft/signalr ^10.0.0` |
| Persistência local | `expo-secure-store ~57.0.1` |
| Captura de imagem | `expo-image-picker ~57.0.6` (câmera + galeria — `expo-camera` deliberadamente não instalado, ver `docs/DIVIDA_TECNICA.md` item 33) |
| TypeScript | `~6.0.3` |
| Lint | ESLint `^9.0.0` + `eslint-config-expo` |
| EAS CLI | `20.5.1` instalado localmente via `npx` (a versão exata usada em cada build fica registrada no log do build, ver `docs/DISASTER_RECOVERY.md`) |
| Projeto EAS | slug `appmorador`, `projectId` `575724e0-701a-4d0f-b692-20f84ebffecb`, conta `ostresmosqueteiros` |
| Pacote Android | `com.appmorador.mobile` |
| Porta do Metro bundler (dev) | `8081` (origem liberada em `Cors:AllowedOrigins` do Backend em `appsettings.Development.json`) |

## Banco de Dados

| Item | Valor confirmado nesta máquina |
|---|---|
| SGBD | MySQL Community Server `8.0.46`, instalado via MySQL Installer (Windows), serviço `MySQL80` |
| Ferramentas cliente | `mysql.exe`/`mysqldump.exe` em `C:\Program Files\MySQL\MySQL Server 8.0\bin\` (fora do PATH nesta máquina — ver `scripts/*.ps1`, que resolvem o caminho automaticamente) |
| Porta | `3306` (padrão) |
| Banco | `appmorador`, charset `utf8mb4` |
| Usuário de runtime | `appmorador`, restrito ao próprio banco (sem `CREATE`/`DROP DATABASE` — decisão de segurança, ver ADR 0008) |
| Tabelas (confirmado via `information_schema`) | 32 (31 de domínio + `__EFMigrationsHistory`) |
| Migrations | Aplicadas automaticamente no startup da Api (ADR 0008) — schema atual descende de um único `InitialCreate` squashado (ADR 0007) + migrations incrementais das Sprints 6-17 |

## Ferramentas de desenvolvimento auxiliares (simuladores)

Não são necessárias para rodar o produto, só para testar integrações sem hardware físico real:

| Ferramenta | Local | Simula |
|---|---|---|
| `JflSimulator` | `backend/tools/JflSimulator` | Central de alarme JFL (protocolo TCP de conexão invertida) |
| `ControlIdSimulator` | `backend/tools/ControlIdSimulator` | Equipamento Control iD (API HTTP) |
| `IntelbrasSimulator` | `backend/tools/IntelbrasSimulator` | Central Intelbras (protocolo HTTP/CGI) |

## Serviços externos (nuvem)

| Serviço | Uso | Credencial necessária |
|---|---|---|
| EAS Build (Expo) | Build de APK/AAB assinado do Mobile na nuvem | Login na conta Expo (`ostresmosqueteiros`) via `eas-cli` |
| GitHub | Hospedagem do repositório, Releases | Login `gh` CLI ou credencial git configurada |

Nenhuma outra dependência de nuvem (sem banco gerenciado, sem storage de objeto, sem fila de
mensagens) — todo o backend roda localmente/on-premise por design (ver `docs/adr/` e
`docs/DIVIDA_TECNICA.md` sobre simplicidade de MVP).

## Variáveis de ambiente

Ver `docs/ENVIRONMENT.md` para a lista completa e o significado de cada uma.

## Requisitos mínimos para reproduzir o ambiente

- Windows 10/11 (ambiente validado; Linux/macOS devem funcionar para Backend/Mobile mas não foram
  testados nesta Sprint — o projeto não usa nenhuma API exclusiva do Windows além de caminhos de
  arquivo, que o .NET já normaliza)
- .NET 8 SDK
- Node.js 20+ (validado com `v24.16.0`) e npm
- MySQL Server 8.0+
- `dotnet-ef` (global tool)
- Conta Expo (gratuita) para builds EAS — opcional para só rodar em modo desenvolvimento local
  (`npx expo start`)

## Achados desta auditoria (housekeeping, sem impacto funcional)

1. **`ft/` na raiz do repositório**: pasta com imagens e um componente `.jsx` de protótipo (Sprint
   16, UX001) deixada solta pelo usuário, nunca fez parte do código do app, nunca foi versionada.
   Adicionada ao `.gitignore` nesta Sprint (ver `docs/STORAGE.md`).
2. **Nenhum `backend/.env.example` existia** antes desta Sprint — criado agora (ver
   `docs/ENVIRONMENT.md`).
3. **Backend não tem log em arquivo** (só console) — se isso for necessário no futuro (ex.:
   ambiente de produção real, não dev local), é uma decisão de arquitetura própria, fora do
   escopo desta Sprint (documentação/portabilidade, zero mudança funcional).
4. **`git config core.longpaths` não estava habilitado nesta máquina** — nomes de arquivo de
   migration do EF Core (`*.Designer.cs`) combinados com caminhos de clone razoavelmente
   aninhados ultrapassam o limite de 260 caracteres do Windows, e o `git checkout` falha com
   `Filename too long`. Confirmado na verificação de portabilidade (Fase 9) desta Sprint. Corrigido
   automaticamente por `scripts/setup_project.ps1` (`git config --global core.longpaths true`).
5. **Bug real e crítico encontrado na verificação de portabilidade (Fase 9)**: a regra
   `**/snapshots/` do `backend/.gitignore` (destinada só à pasta de runtime de imagens
   capturadas) colidia, por causa do casamento de padrão sem diferenciar maiúsculas/minúsculas no
   Windows, com os namespaces de código-fonte `Snapshots/` de `AppMorador.Domain` e
   `AppMorador.Infrastructure` — **16 arquivos de código real nunca foram versionados**
   (integração de câmera: `CameraResolver`, providers CGI/ISAPI, `SnapshotCaptureService`, etc.).
   O build local sempre funcionou (os arquivos existem em disco), mas um clone limpo falhava
   imediatamente (`error CS0234: The type or namespace name 'Snapshots' does not exist`). Corrigido
   nesta Sprint: regra reescrita para `src/AppMorador.Api/snapshots/` (escopo específico, sem
   colisão possível), os 16 arquivos adicionados ao git, tag `v0.9.0` recriada apontando para o
   commit corrigido. Ver `docs/reviews/SPRINT_017_5.md` para o relato completo.
6. **Branch padrão do repositório no GitHub era `release/v0.3.0-alpha`**, não `main` — um `git
   clone` sem especificar branch trazia código de antes da Sprint 4. Corrigido nesta Sprint
   (`gh repo edit --default-branch main`).
