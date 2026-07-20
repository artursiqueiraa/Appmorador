# Alterações de Banco de Dados

Registro de toda migration aplicada ao banco `appmorador`, com o resumo técnico apresentado
antes da aplicação (protocolo permanente de revisão).

## Squash pré-v0.3.0-alpha (Sprint 3.1) — 2026-07-19

**As 5 migrations abaixo não existem mais como arquivos.** Foram substituídas por um único
`20260718230440_InitialCreate` que cria o schema final diretamente (10 tabelas em português,
coluna `Tipo`, índice composto de `Ocorrencias`), validado sem divergência contra o modelo atual
via `dotnet ef migrations has-pending-model-changes`. Motivo e detalhamento completo em
`ADR 0007` (`docs/adr/0007-squash-migrations-v030-alpha.md`).

**O banco de desenvolvimento real não foi recriado nem perdeu dados** — seu
`__EFMigrationsHistory` continua com as 5 migrations originais marcadas como aplicadas, e o EF
nunca as reexecuta a partir daqui. O squash só importa para quem parte de um banco vazio (novo
clone, CI, homologação). O histórico abaixo é mantido como registro de contexto/decisão de cada
mudança de schema — não reflete mais arquivos existentes em `Persistence/Migrations/`.

## 20260719191745_AdicionarIndiceOcorrenciaPropriedadeData — 2026-07-19 *(squashed, ver acima)*

**Operações**: substitui o índice automático `IX_Ocorrencias_PropriedadeId` (criado por convenção
do EF na FK) por um índice composto `IX_Ocorrencias_PropriedadeId_CreatedAtUtc` — cobre a mesma
FK e adiciona a coluna de ordenação usada pela consulta paginada da Central de Eventos
(`JflFonteEventos`).

**Impacto nos dados**: nenhum — operação de índice, nenhuma linha lida/alterada/removida.

**Operações destrutivas**: nenhuma.

**Nota técnica de execução**: o scaffold do EF gerou `DropIndex` antes do `CreateIndex`, o que o
MySQL rejeita ("needed in a foreign key constraint" — a FK sempre precisa de um índice de suporte
válido). Corrigido manualmente invertendo a ordem (`CreateIndex` antes do `DropIndex`) antes de
aplicar; a migration original falhou de forma limpa, sem alterar nada, antes da correção.

**Avaliação de segurança**: sem impacto.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-19 (Sprint 3), sem operação destrutiva.

## 20260719140653_AdicionarTipoPropriedade — 2026-07-19 *(squashed, ver acima)*

**Operações**: `ALTER TABLE Propriedades ADD Tipo longtext NOT NULL DEFAULT 'Outro'` — uma única
coluna nova, sem alterar nenhuma tabela existente.

**Impacto nos dados**: as propriedades reais já cadastradas (2, no momento da aplicação) receberam
o backfill `Tipo = 'Outro'`. Nenhuma linha foi removida ou transformada — verificado por consulta
direta antes e depois da migration.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — mudança de schema pura.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-19 (Sprint 2), sem operação destrutiva.

## 20260719132937_PadronizacaoDominioPtBr — 2026-07-19 *(squashed, ver acima)*

**Operações**: renomeia 8 tabelas (`Users`→`Usuarios`, `Properties`→`Propriedades`,
`AlarmPanels`→`Centrais`, `Dvrs`→`Gravadores`, `Zones`→`Zonas`, `Occurrences`→`Ocorrencias`,
`ZoneCameraLinks`→`VinculosZonaCamera`, `AlarmEventLogs`→`RegistrosEventoAlarme`), renomeia
colunas nessas tabelas e em `Cameras`/`RefreshTokens` (que não mudam de nome), e renomeia 15
índices + 9 constraints de FK para acompanhar a nova nomenclatura. 100% `RENAME TABLE`/
`RENAME COLUMN`/`RENAME INDEX`/`DROP+ADD FOREIGN KEY` — nenhum `DROP TABLE` ou `DROP COLUMN`.

**Impacto nos dados**: banco tinha 2 `Users`, 1 `Property` e 5 `RefreshTokens` reais (sessões de
teste da Sprint 1) no momento da aplicação. Todas as linhas e valores foram preservados —
verificado por consulta direta antes e depois da migration.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — apenas nomenclatura de schema.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-19 com aprovação explícita do usuário.

## 20260719123002_Sprint1AuthPropertyDashboard — 2026-07-19 *(squashed, ver acima)*

Cria as tabelas de Auth/Property/Dashboard (`Users`, `Properties`, `RefreshTokens`) e renomeia
`Site`→`Property` (rename manual via `RenameTable`, mesma técnica). Ajustes pedidos antes da
aplicação: removido `defaultValue: Guid.Empty` de `Property.OwnerUserId` (tabela vazia, sem
necessidade de default fake); `DeleteBehavior` de `Property`→`User` alterado de `Cascade` para
`Restrict` (apagar usuário nunca deve apagar suas propriedades em cascata).

## 20260718230440_InitialCreate — 2026-07-18 *(conteúdo original, squashed/substituído acima)*

Schema inicial do pipeline de alarme JFL: `AlarmPanels`, `Zones`, `Occurrences`,
`AlarmEventLogs`, `Dvrs`, `Cameras`, `ZoneCameraLinks`. Este era o schema em inglês, pré-ADR 0003 —
o arquivo `InitialCreate` atual (mesmo ID de migration) já nasce com o schema final em português.
