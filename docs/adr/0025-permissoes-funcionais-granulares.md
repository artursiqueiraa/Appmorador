# ADR 0025 — Permissões Funcionais Granulares (Sprint 21)

**Data**: 2026-07-26

## Contexto

A missão pede que planos diferentes (Básico/Avançado/TI Própria) sejam representados sem criar
novos papéis — hoje o único "papel" de cliente é Administrador (ver ADR 0021), então a
diferenciação de plano precisa vir de outro eixo, independente de papel.

## Problema

Como modelar "o que este usuário específico pode fazer nesta propriedade específica" de um jeito
que sobreviva à evolução de planos comerciais, sem exigir uma migração de esquema toda vez que um
novo plano for lançado?

## Alternativas consideradas

- **Bitmask/flags num campo único**: mais compacto, mas ilegível em query SQL direta e frágil a
  mudanças (adicionar uma permissão exige recalcular todos os bits existentes).
- **Tabela de junção `UsuarioPropriedadePermissao`** (escolhida): 1 linha por (vínculo, permissão)
  concedida — legível, indexável, sem recomputação ao adicionar uma permissão nova ao enum.
- **Permissão embutida no `PerfilPropriedade`**: rejeitada — um enum de perfil (Administrador/
  Morador) não escala para "Básico tem X, Avançado tem X+Y" sem multiplicar o enum de perfis.

## Decisão

`PermissaoFuncionalidade` (enum, 12 valores) + `UsuarioPropriedadePermissao` (N:N com
`UsuarioPropriedade`, índice único `(UsuarioPropriedadeId, Permissao)`). Concessão via
"replace-all" (`SubstituirAsync` — remove tudo e insere a lista nova), mesmo padrão já usado por
outras listas de vínculo do projeto (ex.: inibição de zona JFL). "Plano Básico" (6 permissões:
`CadastrarMorador, CadastrarFacial, CadastrarTag, AbrirPortao, VerCameras, CriarVisitante`) é
concedido automaticamente a toda Propriedade nova, para nenhuma funcionalidade já esperada pelo
morador comum ficar bloqueada por padrão assim que endpoints futuros passarem a checar isso.

## Consequências

- Qualquer endpoint futuro que precise checar "este usuário pode X nesta propriedade" chama
  `IPermissaoService.TemPermissaoAsync` — nunca reimplementa a checagem.
- Gestão de permissões (`PUT /api/properties/{id}/usuarios/{id}/permissoes`) é exclusiva de
  Técnico/Master nesta Sprint — autoatendimento pelo próprio Administrador fica para quando o
  Painel Web/app expuser essa gestão (fora de escopo, ver "Fora de Escopo" da missão original).
- Propriedades pré-existentes (antes desta Sprint) foram backfilladas com o Plano Básico via
  seed — sem isso, ficariam com zero permissões e qualquer checagem futura as bloquearia por
  engano.

## Impactos

Mobile (`usePermissao`) e futuro Painel Web dependem deste modelo para decidir o que mostrar/
esconder na UI — nunca confiando só no papel/perfil.

## Arquivos afetados

`AppMorador.Domain/Entities/{PermissaoFuncionalidade,UsuarioPropriedadePermissao}.cs`,
`AppMorador.Domain/Repositories/IUsuarioPropriedadePermissaoRepositorio.cs`,
`AppMorador.Application/Rbac/{IPermissaoService,PermissaoService,IUsuarioPropriedadePermissaoServico,UsuarioPropriedadePermissaoServico}.cs`,
`AppMorador.Api/Controllers/UsuarioPropriedadePermissoesController.cs`,
`AppMorador.Application/Propriedades/PropriedadeServico.cs` (Plano Básico automático),
`mobile/src/auth/usePermissao.ts`.

## Como revisar futuramente

Revisar quando um plano comercial novo (ex.: "TI Própria") for lançado de verdade — confirmar que
a lista de permissões concedida automaticamente por plano continua correta e que nenhum endpoint
novo esqueceu de checar a permissão correspondente.
