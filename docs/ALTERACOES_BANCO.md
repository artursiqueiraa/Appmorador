# Alterações de Banco de Dados

Registro de toda migration aplicada ao banco `appmorador`, com o resumo técnico apresentado
antes da aplicação (protocolo permanente de revisão).

## 20260726212346_RbacMaster — 2026-07-26

**Operações**: 3 `AddColumn` (`Usuarios.Ativo`, `Usuarios.RoleGlobal`, `Equipamentos.ModeloEquipamentoId`)
+ 7 `CreateTable` (`AuditoriaMaster`, `ModelosEquipamento`, `PropriedadesFeatureFlag`,
`Provisionamentos`, `UsuariosPropriedade`, `ModelosEquipamentoCapacidade`,
`UsuariosPropriedadePermissao`) + **2 operações de backfill de dados escritas manualmente** (a
geração automática do `dotnet ef migrations add` não as inclui) + 1 `DropColumn`
(`Equipamentos.Modelo`, texto livre).

**Impacto nos dados**: `Equipamento.Modelo` (texto) migrado para `ModeloEquipamentoId` (FK) via
backfill em 2 passos — `INSERT` em `ModelosEquipamento` a partir de todo par distinto
`(Fabricante, Modelo)` já cadastrado, seguido de `UPDATE` que popula `Equipamentos.ModeloEquipamentoId`
via `JOIN` — **antes** do `DropColumn`, preservando 100% dos dados existentes (nenhum "Modelo" já
cadastrado se perde). Verificado após aplicar: 9 Equipamentos existentes, 5 com `Modelo` preenchido
→ 4 `ModelosEquipamento` distintos criados, todos os 5 `Equipamento.ModeloEquipamentoId`
corretamente resolvidos; os 4 Equipamentos sem `Modelo` preenchido permaneceram `NULL` (esperado).
**Achado corrigido antes de aplicar**: a coluna `Usuarios.Ativo` nasceria com `defaultValue: false`
gerado automaticamente pelo EF (satisfaz o `NOT NULL`, mas desativaria login de toda conta já
existente) — adicionado `UPDATE Usuarios SET Ativo = 1;` logo após o `AddColumn`, e confirmado após
aplicar: 14/14 contas existentes permaneceram `Ativo = 1`. As 7 tabelas novas nascem vazias
(populadas depois via seed/backfill de aplicação, não pela migration).

**Operações destrutivas**: 1 (`DropColumn Equipamentos.Modelo`) — mitigada pelo backfill acima,
sem perda de dado. `Down()` recria a coluna mas não restaura os valores de texto originais
(limitação aceita de rollback, mesmo padrão de qualquer migration deste projeto).

**Avaliação de segurança**: nenhuma coluna sensível nova (`AuditoriaMaster` não tem FK para
`Usuario` por design — `UsuarioId`/`UsuarioNome` são um snapshot desnormalizado, para a trilha de
auditoria sobreviver mesmo se a conta de origem for excluída depois).

**Recomendação**: aplicada com sucesso em 2026-07-26 (Sprint 21) após resumo técnico apresentado e
2 achados corrigidos antes de `dotnet ef database update` contra o banco real (backfill de Modelo e
correção do default de `Ativo`). Pós-aplicação: `dotnet build`/`dotnet test` (44/44) limpos, seed de
desenvolvimento executado (conta Master criada, backfill de `UsuarioPropriedade` para as 12
propriedades pré-existentes com 72 permissões do Plano Básico). Warnings benignos de
`Model.Validation[10622]` (mesma classe já registrada desde a Sprint 6) aparecem para
`UsuarioPropriedade`/`PropriedadeFeatureFlag`/`Provisionamento` — sem impacto funcional.

## 20260725115333_CamadaOperacionalUnificada — 2026-07-25

**Operações**: 100% `CreateTable`/`CreateIndex` — 1 tabela nova (`SnapshotsOperacionais`), 1
índice único (`PropriedadeId`). `SnapshotsOperacionais` (FK `Restrict` para `Propriedades`, sem
soft delete — rollup substituível por upsert, não auditoria; `Saude` mapeado como texto via
conversão global de enum, ADR 0005).

**Impacto nos dados**: nenhuma tabela existente é alterada — só criação de tabela nova, vazia.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. Nenhuma coluna
sensível nova (nenhuma senha/token/dado pessoal — só contadores e um enum de estado).

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-25 (Sprint 13), sem operação
destrutiva — resumo técnico apresentado e validado antes de `dotnet ef database update` contra o
banco real. Um warning benigno de validação de modelo do EF Core (`Model.Validation[10622]`)
aparece no startup por `Propriedade`↔`SnapshotOperacional` não terem query filter simétrico —
mesma classe de warning já registrada nas Sprints 6-12, sem impacto funcional confirmado.

## 20260722040841_MigracaoJflActive100Bus — 2026-07-22

**Operações**: 4 `AlterColumn` (`Equipamentos.{Usuario,SenhaCriptografada,Porta,Ip}`: NOT NULL →
nullable) + 1 `CreateTable` (`StatusCentraisJfl`) + 1 `CreateIndex` único.
`StatusCentraisJfl` (FK `Restrict` para `Equipamentos`, sem soft delete — snapshot substituível,
não auditoria).

**Impacto nos dados**: nenhum — relaxar uma coluna de NOT NULL para nullable nunca afeta linhas
existentes (os Equipamentos Control iD já cadastrados continuam com esses campos preenchidos).
Tabela nova fica vazia.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro.
`SenhaCriptografada` continua nunca em texto puro; ficar opcional não muda essa garantia (JFL
simplesmente não usa senha nos comandos migrados).

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-22 (Sprint 12), sem operação
destrutiva — resumo técnico apresentado e validado antes de `dotnet ef database update` contra o
banco real. Um warning benigno de validação de modelo do EF Core (`Model.Validation[10622]`)
aparece no startup por `Equipamento`↔`StatusCentralJfl` não terem query filter simétrico — mesma
classe de warning já registrada nas Sprints 6-11, sem impacto funcional confirmado.

## 20260722024341_AdicionarEquipamentosIntegracaoControlId — 2026-07-22

**Operações**: 100% `CreateTable`/`CreateIndex` — 2 tabelas novas (`Equipamentos`,
`EventosEquipamento`), 2 índices de suporte de FK. `Equipamentos` (FK `Restrict` para
`Propriedades`, colunas de soft delete ADR 0009, `SenhaCriptografada` nunca em texto puro,
`UltimaSincronizacaoUtc` nullable). `EventosEquipamento` (FK `Restrict` para `Equipamentos`, sem
soft delete — auditoria pura, mesmo padrão de `Ocorrencia`).

**Impacto nos dados**: nenhuma tabela existente é alterada — só criação de tabelas novas, vazias.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. FK `Restrict` em
ambos os relacionamentos, consistente com o padrão já estabelecido. `Equipamentos.SenhaCriptografada`
é a única coluna sensível nova — nunca recebe texto puro (cifrada pela camada Application via
Data Protection API antes de qualquer `SaveChangesAsync`).

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-22 (Sprint 11), sem operação
destrutiva — resumo técnico apresentado e validado antes de `dotnet ef database update` contra o
banco real. Um warning benigno de validação de modelo do EF Core (`Model.Validation[10622]`)
aparece no startup por `Equipamento`↔`EventoEquipamento` não terem query filter simétrico —
mesma classe de warning já registrada nas Sprints 6-10, sem impacto funcional confirmado.

## 20260722012326_AdicionarEntregasECorrespondencias — 2026-07-22

**Operações**: 100% `CreateTable`/`CreateIndex` — 2 tabelas novas (`Entregas`, `HistoricoEntregas`),
3 índices de suporte de FK. `Entregas` (FKs `Restrict` para `Moradores` e `Unidades`, colunas de
soft delete ADR 0009, `DataRecebimentoUtc`/`DataRetiradaUtc` nullable). `HistoricoEntregas` (FK
`Restrict` para `Entregas`, sem soft delete — auditoria pura).

**Impacto nos dados**: nenhuma tabela existente é alterada — só criação de tabelas novas, vazias.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. FKs `Restrict`
em todos os relacionamentos, consistente com o padrão já estabelecido.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-22 (Sprint 10), sem operação
destrutiva — resumo técnico apresentado e validado antes de `dotnet ef database update` contra o
banco real. Um warning benigno de validação de modelo do EF Core (`Model.Validation[10622]`)
aparece no startup por `Entrega`↔`HistoricoEntrega` não terem query filter simétrico —
documentado em `docs/DIVIDA_TECNICA.md` item 19 (mesma classe de warning já registrada nas
Sprints 6-9), sem impacto funcional confirmado.

## 20260721100710_AdicionarVeiculosEGaragens — 2026-07-21

**Operações**: `AddColumn` em `PontosAcesso` (`Tipo` longtext NOT NULL, default `'Geral'` —
backfill para linhas já existentes). 5 `CreateTable` (`Vagas`, `Veiculos`, `HistoricoVagas`,
`HistoricoVeiculos`, `PermissoesVeiculares`, `VinculosVeiculoVaga` — 6 tabelas ao todo), 9
`CreateIndex` de suporte de FK. `Vagas` (FK `Restrict` para `Propriedades`, colunas de soft
delete ADR 0009). `Veiculos` (FK `Restrict` para `Moradores`, colunas de soft delete).
`VinculosVeiculoVaga` (FKs `Restrict` para `Veiculos` e `Vagas`, colunas de soft delete,
`DataInicioUtc`/`DataFimUtc` — entidade temporal, cada linha é um período de ocupação).
`PermissoesVeiculares` (FKs `Restrict` para `Veiculos` e `PontosAcesso`, colunas de soft delete).
`HistoricoVeiculos`/`HistoricoVagas` (FK `Restrict`, sem soft delete — auditoria pura).

**Impacto nos dados**: pontos de acesso já cadastrados recebem `Tipo='Geral'` (comportamento
correto — nenhum ponto existente era pensado como garagem). Nenhuma outra tabela existente perde
coluna ou linha.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. FKs `Restrict` em
todos os relacionamentos, consistente com o padrão já estabelecido.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-21 (Sprint 9), sem operação destrutiva
— resumo técnico apresentado e validado antes de `dotnet ef database update` contra o banco real.
Dois warnings benignos de validação de modelo do EF Core (`Model.Validation[10622]`) aparecem no
startup por `Veiculo`↔`HistoricoVeiculo` e `Vaga`↔`HistoricoVaga` não terem query filter simétrico
— documentado em `docs/DIVIDA_TECNICA.md` item 17 (mesma classe de warning já registrada nas
Sprints 6, 7 e 8), sem impacto funcional confirmado.

## 20260721024105_AdicionarVisitantesEAutorizacoes — 2026-07-21

**Operações**: 100% `CreateTable`/`CreateIndex` — 3 tabelas novas. `Visitantes` (FK `Restrict`
para `Propriedades`, colunas de soft delete ADR 0009). `Autorizacoes` (FKs `Restrict` para
`Moradores`, `Unidades` e `Visitantes`, colunas de soft delete, `Tipo`/`StatusManual` armazenados
como texto via `HasConversion<string>()`, `HorarioInicial`/`HorarioFinal` como `time(6)`).
`HistoricoVisitantes` (FK `Restrict` para `Visitantes`, FK `SetNull` para `Autorizacoes` — nulo
só no evento VisitanteRemovido —, sem soft delete, auditoria pura). Índices de suporte de FK em
todas as novas colunas de relacionamento.

**Impacto nos dados**: nenhuma tabela existente é alterada — só criação de tabelas novas, vazias.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. FKs `Restrict`
mantidas para os relacionamentos obrigatórios; `SetNull` só onde o campo já é opcional por design
(`HistoricoVisitantes.AutorizacaoId`); exclusão lógica de verdade acontece em código de aplicação.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-21 (Sprint 8), sem operação destrutiva
— resumo técnico apresentado e validado antes de `dotnet ef database update` contra o banco real.
Um warning benigno de validação de modelo do EF Core (`Model.Validation[10622]`) aparece no
startup por `Visitante`↔`HistoricoVisitante` não terem query filter simétrico — documentado em
`docs/DIVIDA_TECNICA.md` item 16 (mesma classe de warning já registrada nas Sprints 6 e 7), sem
impacto funcional confirmado.

## 20260721020503_AdicionarControleDeAcesso — 2026-07-21

**Operações**: 100% `CreateTable`/`CreateIndex` — 4 tabelas novas. `Credenciais` (FK `Restrict`
para `Moradores`, colunas de soft delete ADR 0009). `PontosAcesso` (FK `Restrict` para
`Propriedades`, colunas de soft delete). `PermissoesAcesso` (FKs `Restrict` para `Credenciais` e
`PontosAcesso`, colunas de soft delete, `DiasPermitidos` armazenado como texto via
`HasConversion<string>()`, `HorarioInicial`/`HorarioFinal` como `time(6)`). `HistoricoCredencial`
(FK `Restrict` para `Credenciais`, sem soft delete — auditoria pura, nunca excluída). Índices de
suporte de FK em todas as novas colunas de relacionamento.

**Impacto nos dados**: nenhuma tabela existente é alterada — só criação de tabelas novas, vazias.

**Operações destrutivas**: nenhuma.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. FKs `Restrict`
mantidas (nunca `Cascade`), consistente com o padrão já estabelecido; exclusão lógica de verdade
acontece em código de aplicação.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-21 (Sprint 7), sem operação destrutiva
— resumo técnico apresentado e validado antes de `dotnet ef database update` contra o banco real.
Dois warnings benignos de validação de modelo do EF Core (`Model.Validation[10622]`) aparecem no
startup por `Credencial`/`HistoricoCredencial` não terem query filter simétrico — documentado em
`docs/DIVIDA_TECNICA.md` item 12, sem impacto funcional confirmado.

## 20260721013056_AdicionarUnidadesEMoradoresESoftDelete — 2026-07-21

**Operações**: `AddColumn` em `Propriedades` (`Excluido` bool default `false`, `DataExclusaoUtc`
nullable, `ExcluidoPorUsuarioId` nullable) — 3 colunas novas. `CreateTable Unidades` (FK
`Restrict` para `Propriedades`) e `CreateTable Moradores` (FK `Restrict` para `Unidades`), cada
uma já nascendo com as mesmas 3 colunas de soft delete (ADR 0009). 2 índices de suporte de FK
(`IX_Unidades_PropriedadeId`, `IX_Moradores_UnidadeId`).

**Impacto nos dados**: nenhuma linha existente alterada além do backfill implícito de
`Excluido = false` nas Propriedades já cadastradas (valor correto — nenhuma propriedade real
nasce excluída). Nenhuma tabela existente perde coluna, nenhum dado é removido/transformado.

**Operações destrutivas**: nenhuma. 100% `AddColumn`/`CreateTable`/`CreateIndex`.

**Avaliação de segurança**: sem mudança de superfície de ataque — schema puro. FKs `Restrict`
(não `Cascade`) mantêm integridade física mesmo que a aplicação tente algo inesperado; a exclusão
lógica de verdade acontece em código de aplicação, nunca no banco.

**Recomendação**: seguro para aplicar. Aplicada em 2026-07-21 (Sprint 6), sem operação
destrutiva — resumo técnico apresentado e validado (`dotnet ef migrations has-pending-model-changes`
sem divergência) antes de `dotnet ef database update` contra o banco real.

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
