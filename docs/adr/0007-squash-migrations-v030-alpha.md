# ADR 0007 — Squash do histórico de migrations em um único InitialCreate (pré-v0.3.0-alpha)

**Data**: 2026-07-19

## Contexto

A Sprint 3.1 (homologação/estabilização) validou o setup de banco do zero e encontrou 5
migrations incrementais em `Persistence/Migrations/` (`InitialCreate`,
`Sprint1AuthPropertyDashboard`, `PadronizacaoDominioPtBr`, `AdicionarTipoPropriedade`,
`AdicionarIndiceOcorrenciaPropriedadeData`). Várias existiam só para corrigir nomenclatura em
inglês criada antes do `ADR 0003` (rename `Site`→`Property`, depois 8 tabelas inteiras
`Users`→`Usuarios` etc.) ou para reverter, dentro da mesma Sprint, uma decisão de schema tomada
poucas horas antes (índice de `Ocorrencias` trocado de automático para composto). Replay-ar essa
sequência de idas e vindas em todo ambiente novo (CI, clone limpo, homologação) não tem valor
prático — só o schema final importa para quem parte do zero.

## Decisão

As 5 migrations foram substituídas por um único arquivo `InitialCreate` (mesmo nome/timestamp
`20260718230440_InitialCreate` do arquivo original, conteúdo regerado a partir do modelo atual)
que cria o schema final diretamente: 10 tabelas em português, coluna `Tipo` em `Propriedades`,
índice composto `IX_Ocorrencias_PropriedadeId_CreatedAtUtc`.

Validação de consistência: `dotnet ef migrations has-pending-model-changes` retornou "No changes
have been made to the model since the last migration" — confirma que o arquivo squashed
representa fielmente o `AppDbContext` atual, sem divergência.

**O banco de desenvolvimento real (`appmorador`) não foi recriado nem alterado.** Continua com seu
`__EFMigrationsHistory` original (as 5 migrations antigas registradas como aplicadas) e todos os
dados reais intactos (verificado: 8 Usuarios, 5 Propriedades, 6 Ocorrencias, schema idêntico ao
esperado). Isso é seguro porque o EF Core nunca reexecuta uma migration cujo ID já consta como
aplicado no histórico do banco-alvo, independentemente do conteúdo atual do arquivo em disco — o
squash só tem efeito para quem parte de um banco vazio (novo desenvolvedor, CI, ambiente de
homologação limpo).

## Consequência

`docs/ALTERACOES_BANCO.md` mantém o registro histórico das 5 migrations originais (o que cada uma
fez e por quê) como contexto de decisão, com uma nota explícita de que esses arquivos não existem
mais em disco — o schema neles descrito agora vem inteiro do `InitialCreate` único. Qualquer
migration nova a partir de agora usa esse `InitialCreate` como base. Nenhum ambiente real precisou
ser recriado ou migrado; a mudança é só na representação em arquivo do histórico, nunca nos dados
de um banco existente.
