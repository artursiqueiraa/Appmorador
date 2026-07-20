# ADR 0008 — Migrations aplicadas automaticamente no startup da Api

**Data**: 2026-07-20

## Contexto

Um requisito de infraestrutura pediu que qualquer desenvolvedor consiga clonar o projeto,
restaurar dependências e rodar o Backend sem passos manuais de banco — incluindo aplicar
migrations pendentes. Até aqui, `dotnet ef database update` era um passo manual documentado em
`docs/setup/SETUP_AMBIENTE.md`.

## Problema

Como aplicar migrations pendentes automaticamente ao iniciar a Api, sem risco de recriar ou
apagar um banco/dado já existente, e mantendo a mesma disciplina já usada para outras
dependências duras do startup (Jwt:Key, connection string)?

## Alternativas consideradas

- **Manter só o passo manual** (`dotnet ef database update` documentado): simples, mas exige que
  todo desenvolvedor lembre de rodar esse comando antes do primeiro `dotnet run`, e novamente a
  cada nova migration — exatamente o atrito que a missão de infraestrutura pediu para eliminar.
- **`Database.EnsureCreated()`**: cria o schema a partir do modelo atual sem usar o mecanismo de
  migrations — incompatível com um projeto que já usa migrations (`EnsureCreated` e `Migrate` não
  podem ser combinados no mesmo banco) e não aplicaria migrations futuras corretamente.
- **`Database.MigrateAsync()` no startup** (decisão adotada): API nativa do EF Core que cria o
  banco se ele não existir e aplica só as migrations registradas como pendentes em
  `__EFMigrationsHistory` — nunca recria nem apaga um banco/dado existente. Idempotente: rodar
  novamente sem migration pendente é um no-op rápido.

## Decisão

`Program.cs` chama `db.Database.MigrateAsync()` logo após `builder.Build()`, em **todos os
ambientes** (não só Development), com log explícito de quais migrations foram encontradas
pendentes antes de aplicar. Uma falha aqui é tratada como dependência dura (mesmo critério que
`Jwt:Key` ausente): a exceção não é capturada, interrompe a inicialização — nunca é silenciada
como a falha do JFL Server (que é um serviço opcional).

**Limitação conhecida, não resolvida por esta decisão**: o usuário MySQL usado pela aplicação
(`appmorador`) é deliberadamente restrito ao próprio banco (sem privilégio `CREATE DATABASE`,
decisão de segurança de uma sessão anterior). Isso significa que `MigrateAsync()` só consegue
criar tabelas/aplicar migrations dentro de um banco **que já existe** (mesmo que vazio) — ele não
consegue criar o banco em si com esse usuário. Criar o banco vazio continua sendo um passo manual
único (`CREATE DATABASE`), documentado em `docs/setup/SETUP_AMBIENTE.md`. Resolver isso
completamente exigiria conceder `CREATE DATABASE` ao usuário da aplicação — uma regressão de
segurança que não foi feita silenciosamente; ver `docs/DIVIDA_TECNICA.md`.

## Consequências

Qualquer schema pendente (tabelas novas, colunas novas) é aplicado automaticamente no próximo
`dotnet run`, em qualquer ambiente. Isso elimina o passo manual de `dotnet ef database update`
para todos os casos exceto a criação inicial do banco vazio. Times que rodam múltiplas réplicas da
Api simultaneamente devem estar cientes de que todas tentarão aplicar migrations no boot — seguro
porque `MigrateAsync` é idempotente e usa lock a nível de schema, mas vale revisar se o projeto
migrar para múltiplas instâncias concorrentes em produção.

## Impactos

`backend/src/AppMorador.Api/Program.cs`. Nenhuma mudança em `AppDbContext` ou nas migrations em
si.

## Arquivos afetados

`backend/src/AppMorador.Api/Program.cs`.

## Como revisar futuramente

Se o projeto adotar deploy com múltiplas instâncias simultâneas em produção, reavaliar se aplicar
migrations no startup de cada instância ainda é seguro, ou se deve migrar para um step de deploy
separado (ex.: job de migration rodando uma vez antes do rollout). Se a restrição de privilégio do
usuário MySQL mudar (ex.: usuário de deploy com `CREATE DATABASE`), revisitar se a criação do
banco também pode ser automatizada.
