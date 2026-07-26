# Setup — AppMorador

Ponto de entrada único para configurar o ambiente completo (Sprint 17.5, Release 0.9.0). Este
documento orquestra e referencia os guias já existentes — não duplica o conteúdo detalhado.

## Requisitos

Ver `docs/AUDITORIA_AMBIENTE.md` para a lista completa de versões confirmadas. Resumo:

- .NET 8 SDK
- Node.js 20+ e npm
- MySQL Server 8.0+
- `dotnet-ef` (`dotnet tool install --global dotnet-ef`)
- Conta Expo gratuita (só necessária para gerar builds EAS/APK — não para desenvolvimento local)

## 1. Clonar o projeto

```powershell
git clone https://github.com/artursiqueiraa/Appmorador.git
cd Appmorador
```

## 2. Instalação automatizada

```powershell
.\scripts\setup_project.ps1
```

Verifica as ferramentas obrigatórias, cria `mobile\.env` a partir do template, e restaura os
pacotes (`dotnet restore` + `npm install`). Ao final, imprime os 2 passos que continuam manuais
por decisão de segurança (criar o banco, configurar segredos) — ver seção 3.

## 3. Banco de dados e segredos (manual, por design)

Detalhado em `docs/setup/SETUP_AMBIENTE.md` (criação do banco/usuário restrito) e
`docs/ENVIRONMENT.md` (significado de cada variável). Resumo:

```powershell
# Uma vez, com usuário privilegiado (root ou equivalente):
mysql -u root -p -e "CREATE DATABASE appmorador CHARACTER SET utf8mb4; CREATE USER 'appmorador'@'localhost' IDENTIFIED BY '<senha-forte-local>'; GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, ALTER, INDEX, REFERENCES, DROP ON appmorador.* TO 'appmorador'@'localhost'; FLUSH PRIVILEGES;"

# A partir de backend/src/AppMorador.Api/:
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=appmorador;User=appmorador;Password=<senha-forte-local>;"
dotnet user-secrets set "Jwt:Key" "<chave-aleatoria-de-64-bytes>"
```

**Se você está restaurando um backup existente** em vez de partir de um banco vazio, siga
`database/README.md`/`docs/DISASTER_RECOVERY.md` em vez de deixar a Api criar o schema do zero.

## 4. Executar o Backend

```powershell
.\scripts\start_backend.ps1
```

Migrations e seed de desenvolvimento (4 contas de teste, ver `docs/setup/SETUP_AMBIENTE.md`) são
aplicados automaticamente no primeiro `dotnet run` — nenhum comando manual de migration é
necessário. Swagger em `http://localhost:5027/swagger` (ambiente Development).

## 5. Executar o Mobile

```powershell
.\scripts\start_mobile.ps1
```

Ajuste `mobile\.env` (`EXPO_PUBLIC_API_URL`) para o IP da sua máquina na rede local se for testar
num celular físico (não em emulador/simulador na mesma máquina) — ver `docs/ENVIRONMENT.md`.

## 6. Frontend Web

Não existe — o projeto tem só Backend + Mobile. Ver `scripts/start_frontend.ps1` (documenta essa
ausência) e `docs/ARCHITECTURE.md`.

## 7. Gerar um APK

```powershell
cd mobile
npx eas-cli build --platform android --profile preview --non-interactive
```

Detalhado (perfis, assinatura, download do artefato) em `docs/DISASTER_RECOVERY.md` seção
"Geração de APK".

## 8. Publicar uma nova versão

Ver `docs/DISASTER_RECOVERY.md` seção "Publicação de Nova Versão" (bump de versão, CHANGELOG,
tag Git, Release GitHub).

## 9. Limpar o ambiente

```powershell
.\scripts\clean_project.ps1
```

Remove `bin/`/`obj/` (backend) e `node_modules/`/`.expo`/`dist` (mobile) — tudo reconstruível via
`setup_project.ps1`. Nunca toca no banco de dados nem em segredos.

## Documentos relacionados

- `docs/setup/SETUP_AMBIENTE.md` — guia detalhado original (Sprint 3.1), passo a passo completo
  com sequência de log esperada e troubleshooting.
- `docs/ENVIRONMENT.md` — cada variável de ambiente, o que significa e quando é obrigatória.
- `docs/STORAGE.md` — o que é persistido fora do controle de versão e onde.
- `docs/ARCHITECTURE.md` — visão de componentes e fluxos.
- `docs/DISASTER_RECOVERY.md` — recuperação de desastre, geração de APK, publicação de release,
  atualização de dependências, plano de contingência.
- `database/README.md` — backup/restore do banco.
