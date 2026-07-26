# Disaster Recovery — AppMorador

Sprint 17.5 (Release 0.9.0). Procedimentos completos de recuperação de desastre — do banco, do
ambiente inteiro, e das operações recorrentes (troca de URL da Api, geração de APK, publicação de
nova versão, atualização de dependências). Escrito para que qualquer pessoa, sem contexto prévio
da equipe, consiga recuperar o projeto sozinha só seguindo este documento.

## RTO e RPO

| Cenário | RTO (tempo até restaurar) | RPO (perda de dado aceitável) |
|---|---|---|
| Perda do banco de dados (servidor MySQL corrompido/apagado) | ~15 min (restaurar dump + subir Backend) | Até o último backup gerado (`scripts/backup_database.ps1`) — **sem rotina de backup agendada hoje**, ver "Plano de Contingência" |
| Perda do repositório local (máquina do desenvolvedor) | ~30 min (clone + setup) | Zero — tudo que importa está no GitHub; só configuração local (`.env`, user-secrets) precisa ser refeita |
| Troca de máquina | ~30-45 min (setup completo do zero) | Zero para código/dado versionado; segredos e `.env` sempre precisam ser reconfigurados (nunca migram automaticamente, por design) |
| Corrupção de dependências (`node_modules`/`bin`/`obj` quebrados) | ~5-10 min (`clean_project.ps1` + `setup_project.ps1`) | Zero |
| Falha após atualização de dependência | ~10-20 min (rollback via git) | Zero, se o rollback for feito antes de commitar |

Estes números são estimativas de engenharia baseadas no tamanho atual do projeto (banco pequeno,
~200KB de dump completo — ver `database/README.md`) e na experiência de configurar o ambiente
nesta Sprint, não medições de um exercício de desastre real cronometrado. Reavaliar quando o banco
de produção crescer significativamente.

## Recuperação do Banco

### Restaurar backup completo

```powershell
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS appmorador CHARACTER SET utf8mb4;"  # só se o banco não existir
$env:APPMORADOR_DB_PASSWORD = "<sua-senha-local>"
.\scripts\restore_database.ps1 -ArquivoSql database\backup\appmorador_full_20260725.sql
```

### Restaurar só o schema

```powershell
.\scripts\restore_database.ps1 -ArquivoSql database\schema\schema.sql
```

Útil para recriar a estrutura sem nenhum dado — o Backend aplicará migrations pendentes
automaticamente no próximo `dotnet run` se o schema restaurado for de uma versão mais antiga (ADR
0008).

### Restaurar só os dados

```powershell
.\scripts\restore_database.ps1 -ArquivoSql database\seed\seed_data.sql
```

Só funciona contra um banco que já tem o schema criado (senão os `INSERT` falham por tabela
inexistente) — restaure o schema primeiro se for um banco novo.

### Validar integridade

```powershell
mysql -u appmorador appmorador -e "SELECT COUNT(*) AS tabelas FROM information_schema.tables WHERE table_schema='appmorador';"
# Esperado: 32

mysql -u appmorador appmorador -e "SELECT table_name, table_rows FROM information_schema.tables WHERE table_schema='appmorador' ORDER BY table_name;"
```

Compare com a tabela de contagem em `docs/AUDITORIA_AMBIENTE.md` (gerada em 2026-07-25) para
detectar perda de dado. Depois, suba o Backend e confirme o checklist pós-restauração (seção
abaixo).

## Recuperação do Ambiente

1. Clonar o projeto: `git clone https://github.com/artursiqueiraa/Appmorador.git`
2. Instalar dependências: `.\scripts\setup_project.ps1` (verifica ferramentas, roda `dotnet
   restore` + `npm install`, cria `mobile\.env`)
3. Restaurar `.env`: `mobile\.env` é criado automaticamente pelo `setup_project.ps1` a partir de
   `.env.example` — ajuste `EXPO_PUBLIC_API_URL` se for testar num celular físico (ver
   `docs/ENVIRONMENT.md`)
4. Restaurar banco: seção anterior
5. Configurar segredos do Backend: `dotnet user-secrets set` (connection string + `Jwt:Key`) —
   ver `docs/setup/SETUP_AMBIENTE.md` seção 2
6. Executar Backend: `.\scripts\start_backend.ps1`
7. Executar Frontend: não aplicável — não existe (ver `docs/ARCHITECTURE.md`)
8. Executar Mobile: `.\scripts\start_mobile.ps1`

## Alteração da URL da API

**Onde alterar**: `mobile/.env` (variável `EXPO_PUBLIC_API_URL`) para desenvolvimento local; para
builds EAS, os perfis `preview`/`production` em `mobile/eas.json` (chave `env.EXPO_PUBLIC_API_URL`
de cada perfil).

**Variáveis afetadas**: só `EXPO_PUBLIC_API_URL` — é o único lugar onde o Mobile guarda a URL
base da Api (`src/api/client.ts` lê via `process.env.EXPO_PUBLIC_API_URL`).

**Validações necessárias**:
1. Confirme que a nova URL é acessível a partir de onde o app vai rodar — em emulador Android,
   `localhost` do host não é acessível diretamente; em celular físico, use o IP da máquina na rede
   local (nunca `localhost`).
2. Confirme que o Backend tem essa origem liberada em `Cors:AllowedOrigins` se for acessar via
   navegador (Expo Web) — não é necessário para o app nativo (React Native não aplica CORS).
3. Depois de alterar, reinicie o Metro bundler (`npx expo start --clear`) — variáveis
   `EXPO_PUBLIC_*` são embutidas no bundle no momento do build/start, não recarregam sozinhas.

**Testes necessários**: abrir o app, confirmar que o login funciona (primeira chamada real à
nova URL) e que o Dashboard/Início carrega dado real.

## Geração de APK

### Build local (desenvolvimento, sem assinatura de release)

```powershell
cd mobile
npx expo run:android
```

Requer Android Studio/SDK instalado localmente — gera um APK de debug, não o mesmo artefato
distribuído via EAS.

### EAS Build (recomendado — mesmo fluxo usado nas Sprints 15-17)

```powershell
cd mobile
npx eas-cli build --platform android --profile preview --non-interactive --no-wait
```

- `--no-wait` retorna o link de acompanhamento imediatamente (`https://expo.dev/accounts/
  ostresmosqueteiros/projects/appmorador/builds/<id>`); sem `--no-wait`, o comando espera o build
  terminar (~10-15 min).
- Acompanhar/baixar depois: `npx eas-cli build:view <id>` (mostra `Application Archive URL`, o
  link direto do `.apk`).

### Perfis disponíveis (`mobile/eas.json`)

| Perfil | Uso | `EXPO_PUBLIC_API_URL` |
|---|---|---|
| `development` | Development client, debug | herda de `mobile/.env` |
| `preview` | Distribuição interna, teste em dispositivo físico | fixo no perfil (IP da rede local do ambiente de teste) |
| `production` | Distribuição final | fixo no perfil, `autoIncrement: true` (incrementa `versionCode` automaticamente) |

### Assinatura

Gerenciada remotamente pela Expo (`Using remote Android credentials (Expo server)`, visto no log
de todo build) — não há keystore local neste repositório. Para gerenciar/rotacionar credenciais:

```powershell
npx eas-cli credentials
```

### Geração de artefatos

`buildType: "apk"` (configurado em todos os perfis Android de `eas.json`) — gera `.apk` direto,
não `.aab` (Android App Bundle). Trocar para `.aab` (necessário para publicar na Play Store) exige
mudar `android.buildType` para `"app-bundle"` no perfil desejado.

## Publicação de Nova Versão

1. **Atualizar versão**: `mobile/app.json` (`expo.version`, `expo.android.versionCode` — bump
   manual, exceto em builds `production` que usam `autoIncrement`) e `mobile/package.json`
   (`version`, deve casar com `app.json`).
2. **Atualizar CHANGELOG**: nova seção no topo de `docs/CHANGELOG.md`, mesmo formato das
   entradas anteriores (Adicionado/Corrigido/Dívida técnica registrada).
3. **Atualizar ROADMAP**: mover a Sprint concluída de "Próximo" para "Concluído" em
   `docs/roadmap/ROADMAP.md`.
4. **Commit**: `git commit -m "release: AppMorador <versao> - <resumo>"`.
5. **Tag Git**: `git tag -a v<versao> -m "<descrição>"` seguido de `git push origin v<versao>`.
6. **Release GitHub**:
   ```powershell
   gh release create v<versao> --title "AppMorador <versao>" --notes-file <arquivo-com-notas> [asset1] [asset2]
   ```
   Anexar como assets: o dump de banco da versão (`database/backup/appmorador_full_<data>.sql`,
   ver `database/README.md`) e, se relevante, o `.apk` gerado (seção anterior).

## Atualização de Dependências

Processo seguro por stack, com rollback documentado:

### .NET (NuGet)

```powershell
cd backend
dotnet list package --outdated          # ver o que está desatualizado
dotnet add src/<Projeto> package <Pacote> --version <versao>  # atualizar uma de cada vez
dotnet build                            # validar antes de seguir para a próxima
```

**Rollback**: `git checkout -- <arquivo>.csproj` (antes de commitar) ou `git revert <commit>`
(depois de commitado) + `dotnet restore`.

### npm (Mobile)

```powershell
cd mobile
npm outdated
npm update <pacote>                      # uma dependência de cada vez, nunca "npm update" geral sem revisar
npm run typecheck && npm run lint && npx expo-doctor
```

**Rollback**: `git checkout -- package.json package-lock.json` + `npm install` (antes de
commitar) ou `git revert <commit>` + `npm install` (depois).

### Expo / React Native

Sempre via `npx expo install <pacote>` (não `npm install` puro) — o Expo resolve a versão
compatível com o SDK atual automaticamente, evitando incompatibilidade nativa silenciosa.
Depois de qualquer atualização:

```powershell
npx expo-doctor    # detecta a maioria dos problemas de incompatibilidade antes de buildar
npx eas-cli build --platform android --profile preview --non-interactive   # build de teste antes de promover a production
```

**Rollback**: mesmo processo do npm acima — `git checkout`/`git revert` + reinstalar.

**Atualização de SDK do Expo** (major, ex.: 57 → 58) é um evento maior, não uma atualização
pontual — sempre em uma branch própria, seguindo o guia oficial de upgrade da versão de destino,
nunca direto em `main`.

## Recuperação de Backup

**Localização dos backups**: `database/backup/` localmente (gitignored, gerado sob demanda por
`scripts/backup_database.ps1`) e como asset anexado em cada Release do GitHub (ponto de
restauração oficial de cada versão congelada — ver `database/README.md`).

**Restauração**: seção "Recuperação do Banco" acima.

**Verificação pós-restore**: seção "Checklist Pós-Restauração" abaixo.

## Checklist Pós-Restauração

Depois de restaurar o ambiente (banco + código + configuração), validar nesta ordem:

- [ ] Backend inicia sem erros (`dotnet run` a partir de `backend/src/AppMorador.Api` — log
      termina em `Sistema pronto.`)
- [ ] Frontend inicia — não aplicável (não existe, ver `docs/ARCHITECTURE.md`)
- [ ] Mobile conecta (`npx expo start`, abrir no Expo Go ou emulador, tela de login aparece)
- [ ] Banco responde (`mysql -u appmorador appmorador -e "SELECT 1;"`)
- [ ] Login funciona (usar uma das 4 contas de desenvolvimento, `docs/setup/SETUP_AMBIENTE.md`)
- [ ] Dashboard/Início carrega (Health Score ou HeroCard aparece com dado real, não em branco)
- [ ] SignalR conecta (log do Backend mostra conexão de Hub; no Mobile, atualização em tempo real
      funciona ao mudar o estado de um equipamento — ou, sem hardware real, confirme que
      `RealtimeContext` não lança erro no console)
- [ ] Integrações continuam operacionais (testar conexão de um Equipamento cadastrado, ou rodar
      um dos simuladores em `backend/tools/` contra a Api)
- [ ] Build gera APK (`npx eas-cli build --platform android --profile preview --non-interactive`
      termina com status `finished`)

## Plano de Contingência

| Cenário | Ação |
|---|---|
| **Perda do banco** | Restaurar o backup mais recente disponível (seção "Recuperação do Banco"). Se não houver nenhum backup, recriar o banco vazio (`docs/setup/SETUP_AMBIENTE.md` seção 1) — perde todo o dado real, só aceitável se for realmente o único caminho. |
| **Perda do repositório local** | Clonar de novo do GitHub (`git clone https://github.com/artursiqueiraa/Appmorador.git`) — nenhuma perda de código, só configuração local (`.env`, user-secrets) precisa ser refeita. |
| **Troca de máquina** | Seguir "Recuperação do Ambiente" do zero nesta máquina nova. |
| **Corrupção de dependências** | `.\scripts\clean_project.ps1` seguido de `.\scripts\setup_project.ps1`. |
| **Falha após atualização** | Reverter via git (`git checkout`/`git revert`, ver "Atualização de Dependências") antes de investigar a causa raiz — nunca depurar em produção com a versão quebrada no ar. |
| **Restauração completa (pior caso: tudo perdido — banco, repositório local, máquina)** | 1) Clonar do GitHub. 2) `setup_project.ps1`. 3) Criar banco vazio com usuário privilegiado. 4) Restaurar o backup mais recente disponível (Release do GitHub ou backup local, se existir). 5) Reconfigurar segredos (`user-secrets`). 6) Rodar Backend + Mobile. 7) Validar com o Checklist Pós-Restauração. |

## Limitação conhecida desta Sprint

**Não há rotina de backup agendada/automática hoje** — `scripts/backup_database.ps1` é manual,
sob demanda. Para um ambiente de produção real, agendar via Task Scheduler do Windows (ou
equivalente) rodando o script periodicamente é a próxima etapa natural — registrado como item de
backlog, fora do escopo desta Sprint (que não introduz infraestrutura de produção nova, só
documenta/automatiza o que já existe localmente).
