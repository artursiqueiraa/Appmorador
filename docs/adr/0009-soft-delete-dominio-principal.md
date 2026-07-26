# ADR 0009 — Soft delete como padrão de exclusão no domínio principal

**Data**: 2026-07-21

## Contexto

A Sprint 6 (Domínio do Produto) introduziu o agregado principal do AppMorador —
`Propriedade` → `Unidade` → `Morador`. Ao definir o comportamento de "excluir" para essas
entidades, surgiu a pergunta de como lidar com exclusão em cascata sem risco de perda de dado:
o padrão já estabelecido no projeto (`Propriedade`→`Usuario`, `Ocorrencia`→`Central`/`Zona`)
sempre evitou `Cascade` físico por segurança, preferindo `Restrict`/`SetNull`.

## Problema

Como excluir uma Propriedade (e suas Unidades/Moradores) de forma que a exclusão pareça completa
e imediata para o usuário, sem o risco de exclusão física em cascata (que apagaria dado real sem
possibilidade de recuperação) nem o incômodo de bloquear a exclusão exigindo que o usuário
apague manualmente cada Unidade/Morador primeiro?

## Alternativas consideradas

- **Cascade físico** (`ON DELETE CASCADE`): exclusão em um comando só, mas irreversível — um
  clique apaga permanentemente todos os dados de moradores de uma propriedade inteira, sem
  possibilidade de auditoria ou recuperação em caso de engano.
- **Restrict** (padrão já usado no projeto): seguro, mas exige que o usuário exclua manualmente
  cada Unidade e Morador antes de poder excluir a Propriedade — atrito alto para uma ação comum
  (ex.: usuário decide não usar mais o app, ou removeu uma propriedade por engano).
- **Soft delete com cascade lógico** (decisão adotada): a exclusão nunca remove a linha do banco
  — só marca `Excluido = true`, `DataExclusaoUtc` e `ExcluidoPorUsuarioId`. A cascata (Propriedade
  → Unidades → Moradores) é feita em código de aplicação (`PropriedadeServico`/`UnidadeServico`),
  não pelo banco — preserva o dado real para auditoria/restauração futura, e a experiência do
  usuário é a de uma exclusão completa e imediata (nada excluído aparece em nenhuma consulta).

## Decisão

`Propriedade`, `Unidade` e `Morador` herdam de `EntidadeComSoftDelete`
(`AppMorador.Domain.Common`), que adiciona `Excluido: bool`, `DataExclusaoUtc: DateTime?` e
`ExcluidoPorUsuarioId: Guid?`. `AppDbContext.OnModelCreating` registra um **query filter global**
(`HasQueryFilter(e => !e.Excluido)`) para as três — nenhuma consulta normal via `DbSet`/LINQ
precisa lembrar de filtrar manualmente; uma futura tela de Lixeira usaria
`.IgnoreQueryFilters()` explicitamente.

As FKs entre as três (`Unidade.PropriedadeId`, `Morador.UnidadeId`) continuam `Restrict` no
banco — soft delete não precisa de cascade físico, e `Restrict` é uma trava adicional contra um
FK órfão em caso de bug. A cascata real acontece em `PropriedadeServico.DeleteAsync`/
`UnidadeServico.DeleteAsync`: cada um busca os registros filhos (via repositório, sem
`AsNoTracking`, para permitir rastrear e salvar a mudança) e marca todos como excluídos na mesma
transação lógica (um único `SaveChangesAsync` no fim).

Exclusão física fica reservada para uma rotina administrativa futura — nunca acontece como
resultado de uma ação do usuário.

## Consequências

- Nenhuma exclusão do usuário é definitiva no banco — sempre reversível por uma futura
  ferramenta de restauração/administração, nunca pelo próprio usuário final nesta Sprint.
- Toda entidade nova do domínio principal (Veículos, Visitantes, Credenciais, Entregas — fases
  futuras) deve seguir o mesmo padrão: herdar `EntidadeComSoftDelete`, registrar o query filter,
  implementar cascade lógico explícito se tiver filhos.
- **Escopo desta Sprint**: só `Propriedade`/`Unidade`/`Morador` ganharam soft delete. Entidades já
  existentes fora do agregado principal (`Central`, `Zona`, `Camera`, `Gravador`, `Ocorrencia`,
  `Usuario`, `RefreshToken`) **não foram retrofitadas** — continuam com exclusão física
  inexistente (não há endpoint de exclusão para elas hoje) ou comportamento anterior. Retrofitar
  soft delete nelas é decisão de uma Sprint futura, não implícita nesta.
- **Limitação técnica conhecida**: como `Propriedade` agora tem query filter mas `Camera`/
  `Central`/`Gravador` (que referenciam `Propriedade` com FK obrigatória) não têm filtro próprio,
  o EF Core emite um warning de validação de modelo no startup (`Model.Validation[10622]`) sobre
  a navegação `Propriedade` nessas três entidades poder ficar inesperadamente nula se alguém um
  dia fizer `.Include(x => x.Propriedade)` nelas. Nenhum código atual faz isso (confirmado por
  busca no repositório) — documentado em `docs/DIVIDA_TECNICA.md`, não corrigido agora para não
  expandir o escopo da Sprint.

## Impactos

`AppMorador.Domain.Common.EntidadeComSoftDelete`; `Propriedade`, `Unidade`, `Morador`;
`AppDbContext` (query filters); `PropriedadeServico`/`UnidadeServico`/`MoradorServico` (lógica de
cascade); todos os repositórios que retornam essas 3 entidades para mutação (sem
`AsNoTracking`, propositalmente).

## Arquivos afetados

`backend/src/AppMorador.Domain/Common/EntidadeComSoftDelete.cs`,
`backend/src/AppMorador.Domain/Entities/{Propriedade,Unidade,Morador}.cs`,
`backend/src/AppMorador.Infrastructure/Persistence/AppDbContext.cs`,
`backend/src/AppMorador.Application/{Propriedades,Unidades,Moradores}/*.cs`.

## Como revisar futuramente

Ao implementar Veículos/Visitantes/Credenciais/Entregas (fases futuras já anunciadas no
`ROADMAP.md`), seguir este mesmo padrão por padrão, não reabrir a decisão. Ao implementar a
funcionalidade de Lixeira/Restauração, revisar se `ExcluidoPorUsuarioId` sozinho é suficiente
para uma auditoria completa, ou se vale um log de exclusões separado.
