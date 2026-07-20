# ADR 0004 — Rename de tabela via migration manual, nunca Drop+Create

**Data**: 2026-07-19

## Contexto

`dotnet ef migrations add` não detecta rename de tabela/coluna automaticamente — ao renomear uma
entidade ou propriedade, o scaffold do EF Core gera `DropTable`+`CreateTable` (ou
`DropColumn`+`AddColumn`), o que apagaria qualquer dado real já existente na tabela/coluna antiga.

## Problema

Como aplicar um rename de tabela/coluna em produção (ou em qualquer banco com dados reais) sem
perder os dados que o scaffold automático do EF destruiria?

## Alternativas consideradas

- **Aceitar o `DropTable`+`CreateTable` gerado automaticamente**: mais rápido de escrever, mas
  destrutivo — inaceitável em qualquer banco com dado real.
- **Editar manualmente a migration gerada**, substituindo as operações `Drop`+`Create` por
  `RenameTable`/`RenameColumn`/`RenameIndex` e `DropForeignKey`+`AddForeignKey` para as
  constraints dependentes: preserva os dados, mas exige revisão manual de cada migration de
  rename antes de aplicar.

## Decisão

Toda migration que renomeia uma entidade/tabela é revisada manualmente e as operações
`DropTable`+`CreateTable`/`DropColumn`+`AddColumn` geradas pelo EF são substituídas por
`RenameTable`/`RenameColumn`/`RenameIndex`, mais `DropForeignKey`+`AddForeignKey` para acompanhar
o novo nome de constraint. Processo usado com sucesso na Sprint 1 (`Site`→`Property`) e na Sprint
de Padronização (8 tabelas, com dados reais já presentes).

## Consequências

Toda migration de rename precisa do resumo técnico do protocolo de revisão (ver memória de
processo/feedback) antes de ser aplicada — nunca se assume que o SQL gerado pelo EF é seguro sem
inspeção manual linha a linha.

## Impactos

Qualquer Sprint futura que renomeie uma entidade, tabela ou coluna já existente com dado real
segue este processo. Não se aplica a tabelas/colunas novas (sem dado a preservar).

## Arquivos afetados

Processo aplicado em `backend/src/AppMorador.Infrastructure/Persistence/Migrations/` sempre que
uma migration de rename for gerada.

## Como revisar futuramente

Válida enquanto o projeto usar EF Core Migrations como mecanismo de schema. Se o projeto migrar
para outra ferramenta de migration, revisitar se essa ferramenta detecta rename nativamente antes
de reaplicar o mesmo processo manual.
