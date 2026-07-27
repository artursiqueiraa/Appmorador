# ADR 0028 — Provisionamento como Registro de Metadados (Sprint 21)

**Data**: 2026-07-26

## Contexto

A missão descreve `Provisionamento` como "pacote de instalação" com uma árvore completa de
equipamentos vinculados (Centrais[], Gravadores[], Câmeras[], Leitores[], PGMs[], Zonas[],
Vínculos[]) — uma modelagem equivalente, em escala, a retrofitar uma coluna de FK em cada entidade
de hardware já existente (`Equipamento`, `Gravador`, `Camera`, `Central`, `Zona`...).

## Problema

Como entregar o registro de Provisionamento pedido pela missão sem expandir esta Sprint (já
grande — RBAC completo + migração destrutiva + mobile) para também redesenhar todo o
relacionamento entre hardware e "pacote de instalação"?

## Alternativas consideradas

- **Árvore completa de vínculos** (como a missão descreve): mais completa, mas exigiria adicionar
  `ProvisionamentoId` (nullable) a `Equipamento`, `Gravador`, `Camera`, `Central`, `Zona` — 5
  migrations adicionais em entidades já em produção, risco desproporcional ao valor entregue nesta
  Sprint.
- **`Provisionamento` como registro de metadados puro** (escolhida): `Id, PropriedadeId, Nome,
  Template (enum), Status (enum), CreatedAtUtc, AtualizadoEmUtc` — sem nenhuma FK de hardware
  apontando para ele. Serve para Técnico/Master registrarem "há um pacote de instalação em
  andamento para esta propriedade, do tipo X, no status Y" — sem tentar modelar a árvore completa.

## Decisão

`Provisionamento` nasce como metadado (Nome/Template/Status), gerenciado por Técnico/Master via
`POST/GET /api/properties/{id}/provisionamentos` + `POST /api/provisionamentos/{id}/arquivar`. A
árvore de vínculos de hardware da missão original é explicitamente adiada — dívida técnica
registrada, não corte silencioso.

## Consequências

- Esta Sprint entrega o registro/rastreamento de "existe um provisionamento para esta
  propriedade, neste status" — não a automação de "criar todo o hardware de uma vez a partir de
  um template".
- Uma Sprint futura dedicada a wizard de provisionamento com templates (já presente no roadmap da
  missão como item separado) é o lugar natural para decidir se/como vincular hardware a um
  Provisionamento específico.

## Impactos

Painel Web (Sprint 22) pode listar/criar/arquivar provisionamentos desde já — só não pode (ainda)
mostrar "o que foi instalado dentro deste pacote", porque esse vínculo não existe.

## Arquivos afetados

`AppMorador.Domain/Entities/{Provisionamento,TemplateProvisionamento,StatusProvisionamento}.cs`,
`AppMorador.Domain/Repositories/IProvisionamentoRepositorio.cs`,
`AppMorador.Application/Provisionamentos/*`, `AppMorador.Api/Controllers/ProvisionamentosController.cs`.

## Como revisar futuramente

Revisar quando o wizard de provisionamento com templates for planejado (mission roadmap, Sprint
22+) — decidir ali se a árvore de vínculos de hardware é necessária e como retrofitá-la sem
migração destrutiva nas entidades de hardware já em produção.
