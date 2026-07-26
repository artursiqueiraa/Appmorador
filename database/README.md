# Banco de Dados — Backup, Schema e Seed

Sprint 17.5 (Release 0.9.0). SGBD: **MySQL 8.0+** (Pomelo.EntityFrameworkCore.MySql — não há
suporte a SQL Server neste projeto, ver `docs/setup/SETUP_AMBIENTE.md`).

## Estrutura

```
database/
├── backup/   # dump completo (schema + dados) — NÃO versionado, ver "Por que backup/ é ignorado"
├── schema/   # schema.sql — versionado (só estrutura, sem dado real)
├── seed/     # seed_data.sql — versionado (dados de desenvolvimento já documentados em SETUP_AMBIENTE.md)
└── README.md
```

## Por que `backup/` é ignorado pelo git (decisão desta Sprint)

Um dump completo é um artefato pontual (schema + todo o dado do momento da geração) — versionar
isso no git faria o histórico crescer a cada geração, sem nenhum ganho de diff (é um arquivo SQL
gigante, não algo revisável linha a linha como o schema). Em vez de comitar,
o dump completo gerado nesta Sprint (`appmorador_full_20260725.sql`) foi anexado como **asset
binário na Release `v0.9.0` do GitHub** — é o "ponto de restauração" oficial desta versão
congelada, sem inflar o repositório para sempre. `schema.sql` e `seed_data.sql` continuam
versionados normalmente (pequenos, diffáveis, úteis para acompanhar mudança de estrutura ao longo
do tempo).

## Como gerar

Use `scripts/backup_database.ps1` (lê a senha de uma variável de ambiente, nunca pede para
digitar em texto plano no histórico do shell):

```powershell
$env:APPMORADOR_DB_PASSWORD = "<sua-senha-local>"
.\scripts\backup_database.ps1
```

Gera 3 arquivos com timestamp em `database/backup/`:
- `appmorador_schema_<data>.sql` — só estrutura (`--no-data`)
- `appmorador_seed_<data>.sql` — só dados (`--no-create-info --complete-insert`)
- `appmorador_full_<data>.sql` — schema + dados completo

Equivalente manual (a partir da raiz do projeto, com `mysqldump` no PATH ou usando o caminho
completo do MySQL Server 8.0 — ver `docs/AUDITORIA_AMBIENTE.md`):

```powershell
$env:MYSQL_PWD = "<sua-senha-local>"
mysqldump -u appmorador --no-data --no-tablespaces --routines --triggers --skip-comments appmorador > database/schema/schema.sql
mysqldump -u appmorador --no-create-info --no-tablespaces --skip-comments --complete-insert appmorador > database/seed/seed_data.sql
mysqldump -u appmorador --no-tablespaces --routines --triggers --skip-comments --complete-insert appmorador > database/backup/appmorador_full_<data>.sql
Remove-Item Env:\MYSQL_PWD
```

`MYSQL_PWD` evita passar a senha como argumento de linha de comando (apareceria em texto plano no
histórico do shell/`ps`) — é a forma recomendada pela própria documentação do MySQL Client.

## Como restaurar

Use `scripts/restore_database.ps1` (pede confirmação explícita antes de sobrescrever um banco
existente — nunca restaura silenciosamente por cima de dado real):

```powershell
$env:APPMORADOR_DB_PASSWORD = "<sua-senha-local>"
.\scripts\restore_database.ps1 -ArquivoSql database\backup\appmorador_full_20260725.sql -NomeBanco appmorador
```

Equivalente manual — **o usuário de runtime (`appmorador`) não tem `CREATE DATABASE`** (restrição
de segurança preexistente, ver ADR 0008 e `docs/DIVIDA_TECNICA.md` item 8) — criar um banco novo
exige um usuário privilegiado (`root` ou equivalente) uma única vez:

```powershell
# Só se o banco de destino ainda não existir (usuário privilegiado, uma vez):
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS appmorador CHARACTER SET utf8mb4;"

# Restaurar (usuário appmorador já tem acesso total ao próprio banco):
$env:MYSQL_PWD = "<sua-senha-local>"
mysql -u appmorador appmorador < database\backup\appmorador_full_20260725.sql
Remove-Item Env:\MYSQL_PWD
```

## Como validar

Depois de restaurar, confirme que a estrutura e os dados chegaram inteiros:

```powershell
mysql -u appmorador appmorador -e "SELECT COUNT(*) AS tabelas FROM information_schema.tables WHERE table_schema='appmorador';"
# Esperado: 32 (31 tabelas de domínio + __EFMigrationsHistory)

mysql -u appmorador appmorador -e "SELECT COUNT(*) AS usuarios FROM usuarios; SELECT COUNT(*) AS propriedades FROM propriedades;"
```

Depois de subir o Backend (`dotnet run`), confirme no log: `Banco localizado, nenhuma migration
pendente.` — se aparecer `Migration(ns) pendente(s) encontrada(s)`, o dump restaurado é de uma
versão de schema mais antiga que o código atual (normal ao restaurar um backup antigo depois de
Sprints novas) — a Api aplica a migration pendente automaticamente no mesmo `dotnet run` (ADR
0008).

## Validação real feita nesta Sprint (o que foi e o que não foi confirmado)

**Confirmado nesta sessão**: os 3 dumps foram gerados com sucesso contra o banco de desenvolvimento
real (`appmorador`, MySQL 8.0.46) e validados **estruturalmente** — `schema.sql` contém as 32
`CREATE TABLE` esperadas (uma por tabela de domínio + `__EFMigrationsHistory`); `seed_data.sql`
contém `INSERT INTO` para as tabelas com dado real (usuários, propriedades, moradores, etc.).

**Não confirmado nesta sessão**: uma restauração completa ponta a ponta contra um banco **novo**
não foi executada neste ambiente — o usuário MySQL disponível (`appmorador`) não tem privilégio
`CREATE DATABASE` (mesma restrição documentada em `SETUP_AMBIENTE.md`/ADR 0008), e nenhuma
credencial de usuário privilegiado (`root`) estava disponível para criar um banco de teste
descartável. **Ação pendente para quem tiver acesso `root` local**: rodar os 2 comandos da seção
"Como restaurar" contra um banco novo (`appmorador_restore_test`, por exemplo) e confirmar a
contagem de tabelas/linhas da seção "Como validar" — depois, `DROP DATABASE
appmorador_restore_test;` para limpar. Ver `docs/DISASTER_RECOVERY.md` para o procedimento
completo de recuperação de desastre.
