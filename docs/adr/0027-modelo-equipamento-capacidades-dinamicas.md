# ADR 0027 — ModeloEquipamento + Capacidades Dinâmicas (Sprint 21)

**Data**: 2026-07-26

## Contexto

`Equipamento.Modelo` era um campo de texto livre desde a Sprint 11 — sem estrutura, sem forma de
saber "o que este modelo específico suporta" (facial? tag? PGM? PTZ?) sem hardcodar por
fabricante no app/painel. A missão pede que app/painel **nunca** hardcodem UI por fabricante,
sempre consultando `GET /api/equipamentos/{id}/capacidades`.

## Problema

Como transformar um campo de texto livre num catálogo real de modelos com capacidades, sem
quebrar o contrato existente (`CriarEquipamentoRequest`/`EquipamentoResponse` já usados pelo app
mobile em produção) e sem perder os dados já cadastrados?

## Alternativas consideradas

- **Capacidades no `Fabricante`** (enum já existente): rejeitada por decisão explícita do
  usuário — *"fabricante e modelo representam conceitos diferentes [...] as capacidades
  pertencem ao modelo, não ao fabricante"*. Dois equipamentos do mesmo fabricante podem ter
  modelos com capacidades bem diferentes.
- **Mudar o contrato da Api** (`Modelo` vira um Id de FK direto no request/response): rejeitada —
  forçaria uma mudança correspondente na tela de cadastro do app mobile na mesma Sprint, fora do
  escopo pedido.
- **`ModeloEquipamento` como entidade própria + resolução transparente** (escolhida): o contrato
  da Api continua recebendo/devolvendo `Modelo` como string — por trás, `EquipamentoServico`
  resolve-ou-cria a linha de catálogo correspondente.

## Decisão

`ModeloEquipamento` (`Fabricante` + `Nome`, único por par) + `ModeloEquipamentoCapacidade` (N:N
com o novo enum `EquipamentoCapacidade`: Face/Tag/QrCode/Senha/Armar/Desarmar/Pgm/Streaming/Ptz).
`Equipamento.Modelo` (texto) foi substituído por `Equipamento.ModeloEquipamentoId` (FK, nullable,
`OnDelete: SetNull`). `EquipamentoServico.ResolverOuCriarModeloAsync` mapeia transparentemente o
texto recebido pelo request para uma linha de catálogo (get-or-create por `Fabricante`+`Nome`) —
o app mobile continua enviando/recebendo um campo `Modelo` de texto livre, sem nenhuma mudança de
contrato ou regressão.

### Migration com backfill de dados (não puramente aditiva)

A migration (`RbacMaster`) faz `DropColumn Modelo` — destrutivo por natureza — mas só **depois**
de um backfill de 2 passos escrito manualmente (a geração automática do `dotnet ef migrations add`
não inclui isso): `INSERT` em `ModelosEquipamento` a partir de todo par `(Fabricante, Modelo)`
distinto já cadastrado, seguido de `UPDATE` que popula `Equipamento.ModeloEquipamentoId` via
`JOIN`. Verificado após aplicar contra o banco real: 9 Equipamentos existentes, 5 com `Modelo`
preenchido → 4 `ModelosEquipamento` distintos criados, todos os 5 corretamente resolvidos; os 4
sem `Modelo` preenchido permaneceram `NULL` (esperado) — zero perda de dado. Resumo completo em
`docs/ALTERACOES_BANCO.md`.

### Escopo reduzido: capacidades não são cadastradas automaticamente

Esta Sprint cria o catálogo e a infraestrutura de consulta (`GET /api/equipamentos/{id}/capacidades`,
`IPermissaoService.ListarCapacidadesAsync`), mas **não** popula capacidades reais para os modelos
já existentes (nenhuma fonte confirmada de "o Control iD iDAccess Nano suporta X, Y, Z" — mesmo
princípio já aplicado na Sprint 15: nunca fabricar dado sem fonte verificável). Capacidades ficam
vazias até serem definidas manualmente via `PUT /api/modelos-equipamento/{id}/capacidades`
(Técnico/Master).

## Consequências

- App/painel nunca precisam saber "este fabricante suporta X" — sempre consultam o endpoint de
  capacidades do equipamento específico.
- `JflComandoServico`/`IntelbrasComandoServico` (que expunham `Modelo` como texto em seus próprios
  DTOs) foram atualizados para ler `equipamento.ModeloEquipamento?.Nome` — mesma ripple já
  esperada de qualquer mudança de campo consumido em múltiplos lugares.
- Dívida técnica: capacidades reais por modelo precisam ser cadastradas manualmente por
  Técnico/Master antes que `GET /api/equipamentos/{id}/capacidades` retorne algo útil.

## Impactos

Toda futura integração de fabricante deve popular `ModeloEquipamento`/capacidades em vez de
assumir comportamento fixo por `Fabricante`. Painel Web (Sprint 22) é o consumidor natural da
gestão de catálogo (`ModelosEquipamentoController`).

## Arquivos afetados

`AppMorador.Domain/Entities/{ModeloEquipamento,ModeloEquipamentoCapacidade,EquipamentoCapacidade,Equipamento}.cs`,
`AppMorador.Domain/Repositories/IModeloEquipamentoRepositorio.cs`,
`AppMorador.Application/Equipamentos/{ModeloEquipamentoDtos,IModeloEquipamentoServico,ModeloEquipamentoServico,EquipamentoServico,Dtos}.cs`,
`AppMorador.Application/{Jfl/JflComandoServico,Intelbras/IntelbrasComandoServico}.cs`,
`AppMorador.Api/Controllers/{ModelosEquipamentoController,EquipamentosController}.cs`,
`AppMorador.Infrastructure/Persistence/Migrations/20260726212346_RbacMaster.cs`,
`docs/ALTERACOES_BANCO.md`.

## Como revisar futuramente

Revisar quando a primeira integração real de hardware precisar consultar capacidades de verdade —
confirmar que o catálogo tem dados reais cadastrados (não vazio) antes de qualquer tela depender
disso silenciosamente.
