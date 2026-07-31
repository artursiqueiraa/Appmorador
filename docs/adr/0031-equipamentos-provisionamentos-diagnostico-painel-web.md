# ADR 0031 — Equipamentos, Provisionamentos e Diagnóstico no Painel Web (Sprint 22B)

**Data**: 2026-07-29

## Contexto

A Sprint 22B expandiu o Painel Web (fundação na Sprint 22A, ADR 0029/0030) com três módulos
novos: gestão global de Equipamentos, alocação Equipamento↔Propriedade ("Provisionamento" na
linguagem de negócio da missão) e um painel de Diagnóstico somente leitura. A Fase 0 (auditoria
obrigatória) encontrou três conflitos reais entre a missão e o sistema já existente — nenhum
deles óbvio a partir da leitura da missão isolada, só visível lendo o código real.

## Problema

Como entregar os três módulos pedidos sem colidir com três contratos/entidades já em produção:
o `EquipamentosController` mobile-facing (rotas `PUT`/`DELETE /api/equipamentos/{id}`), a entidade
`Provisionamento` já existente (ADR 0028, "pacote de instalação" da Propriedade, sem noção de
equipamento) e o campo `StatusEquipamento` (conectividade, Sprint 11/12)?

## Alternativas consideradas

- **Reaproveitar `EquipamentosController`/rotas `/api/equipamentos`** (rejeitada): a missão sugeria
  literalmente `GET/PUT/DELETE /api/equipamentos`, mas essas rotas já servem o Mobile com um DTO
  (`EquipamentoResponse`) e regras (escopo por `PropriedadeId` do dono) diferentes do caso de uso
  admin (cross-propriedade, Master/Técnico). Reaproveitar quebraria ou ramificaria um contrato
  público estável.
- **Renomear `Equipamento.Identificador` para `NumeroSerie`** (rejeitada): o campo é usado para
  correlação de sessão TCP no protocolo JFL (`JflFonteEventos`, `JflComandoServico`) e já faz parte
  do DTO mobile-facing. Renomear é puro risco sem ganho — o nome de negócio "Número de Série" só
  precisa aparecer na camada de DTO do Painel Web.
- **Reaproveitar a entidade `Provisionamento` (ADR 0028)** (rejeitada): já tem um sentido de
  negócio consolidado (Nome/Template/Status de instalação da Propriedade) sem nenhuma noção de
  equipamento. Forçar o conceito novo (vínculo Equipamento↔Propriedade com histórico) dentro dela
  misturaria dois conceitos com o mesmo nome de negócio ("Provisionamento") e formas diferentes.
- **Nova entidade dedicada ao vínculo Equipamento↔Propriedade** (escolhida): `VinculoEquipamentoPropriedade`,
  namespace `AppMorador.Application.Painel.VinculosEquipamento` (nunca
  `AppMorador.Application.Provisionamentos`, que continua sendo só o ADR 0028).
- **Reaproveitar `StatusEquipamento` para o novo estado administrativo** (rejeitada): `StatusEquipamento`
  é estritamente conectividade (Desconhecido/Online/Offline), atualizado só por teste/sync manual.
  Sobrepor "Em Manutenção"/"Defeituoso" nesse mesmo enum misturaria dois eixos ortogonais (um
  equipamento pode estar Online e Em Manutenção ao mesmo tempo).

## Decisão

1. **Rotas Painel Web sempre sob `api/painel/...`**, nunca reaproveitando prefixo já usado pelo
   Mobile: `api/painel/equipamentos` (`EquipamentosAdminController`) e
   `api/painel/provisionamentos` (`ProvisionamentosAdminController`), totalmente isolados de
   `api/equipamentos` (Mobile). `api/diagnostico` é novo por definição (nenhum equivalente mobile).
2. **`Equipamento.Identificador` não foi renomeado.** O novo DTO admin (`EquipamentoAdminResponse.NumeroSerie`)
   mapeia o mesmo campo só na camada de leitura/escrita do Painel Web — zero mudança na Api mobile
   ou no protocolo JFL.
3. **Novo enum `EstadoOperacionalEquipamento`** (Ativo/EmManutencao/Inativo/Defeituoso), campo novo
   e paralelo a `StatusEquipamento` — nunca os dois combinados num só. `StatusEquipamento` continua
   conectividade pura; `EstadoOperacional` é decisão administrativa do Técnico/Master.
4. **Número de Série único por Propriedade** (não existe conceito de "Tenant" no domínio — a
   ambiguidade da missão foi resolvida usando o escopo dominante já existente no projeto,
   `PropriedadeId`). Implementado como índice único composto `(PropriedadeId, Identificador)`.
5. **Nova entidade `VinculoEquipamentoPropriedade`** para o vínculo Equipamento↔Propriedade com
   histórico — "Disponível"/"Provisionado" é **derivado** de `DataFimUtc == null`, nunca um campo
   próprio (evita estado redundante que poderia dessincronizar). A regra de "um equipamento nunca
   tem mais de um vínculo ativo simultâneo" é garantida no Servico (`VinculoEquipamentoServico`),
   não no banco — MySQL não suporta índice único parcial/condicional sem colunas geradas, o que
   seria desproporcional ao escopo desta Sprint (mesma classe de simplificação já registrada em
   decisões anteriores do projeto).
6. **Troca de equipamento nunca edita o vínculo antigo** — sempre encerra (`DataFimUtc = agora`) e
   cria um novo, preservando histórico completo e auditável via `IAuditoriaService` (ADR 0021).
7. **DT-045 (Provisionamento sem Técnico responsável) permanece em aberto para a entidade
   `Provisionamento` (ADR 0028)** — não resolvida por esta Sprint, que criou uma entidade
   diferente. A nova `VinculoEquipamentoPropriedade` já nasce com `CriadoPorUsuarioId`
   (obrigatório, auditável), então não repete essa lacuna para o conceito novo — não foi
   necessária nenhuma tabela associativa adicional.
8. **DT-046 (Sessões Ativas) não ganhou um ADR 0032 novo.** O ADR 0029 (Sprint 22A) já documenta
   integralmente que Sessões Ativas é inferida do log de Auditoria (stateless, sem revogação real
   de token) — criar um ADR 0032 seria pura duplicação, o que a própria missão desta Sprint pede
   para evitar. `docs/DIVIDA_TECNICA.md` item 46 foi atualizado para apontar explicitamente para
   o ADR 0029 como o registro definitivo deste assunto.
9. **DT-047 (CRUD de Proprietários) confirmado ainda somente-leitura** — `ProprietariosController`
   continua só com `GET` (lista + detalhe). Fora do escopo desta Sprint (que trata Equipamentos/
   Provisionamentos/Diagnóstico, não gestão de conta de cliente); a limitação já estava
   documentada com justificativa no item 47 de `DIVIDA_TECNICA.md` desde a Sprint 22A.
10. **Diagnóstico é estritamente somente leitura** — `DiagnosticoController` só expõe `GET`, nunca
    altera `EstadoOperacional`/vínculo/status. Qualquer botão de ação de hardware real no frontend
    é mock visual/desabilitado (ver Fora de Escopo da missão — comunicação direta com hardware é
    Sprint 22C).
11. **Estrutura modular do Painel Web aplicada só aos 3 módulos novos** (confirmado com o usuário):
    `src/compartilhado/{componentes,hooks,tipos}/` + `src/modulos/{equipamentos,provisionamentos,
    diagnostico}/{queries,mutations,queryKeys,adaptadores,types}/`. Dashboard/Clientes/Suporte
    permanecem na estrutura flat da Sprint 22A — migrá-los seria refatoração fora do escopo pedido.
12. **`core/` não foi criado como pasta própria** — o `httpClient`/`extrairMensagemErro`
    centralizados desde a Sprint 22A (`src/services/httpClient.ts`) já cumprem o papel que a
    missão esperava de um `core/`; duplicar/realocar seria uma camada extra sem ganho real.
13. **`DialogoConfirmacao` não foi recriado** — `ConfirmDialog` (Sprint 22A) já cobre exatamente o
    mesmo caso de uso; os 3 módulos novos reaproveitam-no diretamente.
14. **`FiltroPeriodo` não foi construído** — nenhum dos 3 endpoints novos aceita filtro de período;
    construir o componente sem um backend real por trás fabricaria uma funcionalidade inexistente
    (mesmo princípio do filtro de zona cortado do v1 da Central de Eventos, Sprint 3).
15. **`SeletorPropriedade` (novo, compartilhado)** resolve a necessidade de escolher uma
    Propriedade de qualquer cliente (para cadastrar Equipamento ou provisionar) sem um endpoint
    "listar todas as propriedades" — cascata sobre `GET /api/proprietarios` (busca) + `GET
    /api/proprietarios/{id}` (detalhe com propriedades), ambos já existentes desde a Sprint 22A.

## Consequências

- Mobile e Painel Web continuam 100% isolados um do outro em termos de contrato de Api, mesmo
  operando sobre as mesmas entidades (`Equipamento`).
- O termo de negócio "Provisionamento" agora tem dois sentidos técnicos coexistindo no código
  (`Provisionamento`, ADR 0028; `VinculoEquipamentoPropriedade`, este ADR) — deliberadamente
  isolados por namespace para nunca serem confundidos. Qualquer leitura futura desta área do
  código deve checar qual dos dois está em uso antes de estender.
- A regra de "um vínculo ativo por equipamento" vive em código de aplicação, não em constraint de
  banco — um bug futuro no Servico poderia, em teoria, violar essa regra sem o banco rejeitar. Os
  testes automatizados (`VinculoEquipamentoServicoTests`) cobrem os caminhos de rejeição
  conhecidos, mas isso não substitui uma garantia de banco.

## Impactos

Sprint 22C (comunicação direta com hardware) construirá em cima de `EstadoOperacionalEquipamento`
e do histórico de `VinculoEquipamentoPropriedade` — qualquer ação real de hardware deve, no
mínimo, registrar-se como um novo evento/auditoria seguindo o mesmo padrão desta Sprint.

## Arquivos afetados

`AppMorador.Domain/Entities/{EstadoOperacionalEquipamento,VinculoEquipamentoPropriedade}.cs`,
`AppMorador.Domain/Entities/Equipamento.cs` (campos `EstadoOperacional`/`MacAddress`/`Observacoes`),
`AppMorador.Domain/Repositories/{IEquipamentoRepositorio,IVinculoEquipamentoPropriedadeRepositorio,
IDiagnosticoEquipamentoRepositorio}.cs`,
`AppMorador.Infrastructure/Persistence/{EquipamentoRepositorio,VinculoEquipamentoPropriedadeRepositorio,
DiagnosticoEquipamentoRepositorio,AppDbContext}.cs`,
`AppMorador.Infrastructure/Persistence/Migrations/20260729032508_Sprint22BEquipamentosProvisionamentos.cs`,
`AppMorador.Application/Painel/{Equipamentos,VinculosEquipamento,Diagnostico}/*` (namespaces novos),
`AppMorador.Api/Controllers/{EquipamentosAdminController,ProvisionamentosAdminController,DiagnosticoController}.cs`,
`AppMorador.Api/Middleware/{CorrelationIdMiddleware,UsuarioLogadoEnrichmentMiddleware}.cs`,
`AppMorador.Api/Program.cs` (registro dos middlewares + logging com escopos),
`AppMorador.Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (registro DI),
`AppMorador.Tests/Painel/{EquipamentoAdminServicoTests,VinculoEquipamentoServicoTests,DiagnosticoServicoTests}.cs`.

## Como revisar futuramente

Revisar quando o produto precisar de fato de uma segunda fonte de vínculo hardware↔propriedade
(ex.: câmeras com o mesmo padrão) — confirmar se `VinculoEquipamentoPropriedade` deveria generalizar
para um vínculo genérico "Ativo↔Propriedade" ou se cada tipo de hardware justifica sua própria
tabela. Revisar também se a regra de "1 vínculo ativo" ainda pode viver só em Servico quando o
volume de escrita crescer o suficiente para justificar uma constraint de banco real (ex.: migrar
para uma coluna gerada + índice único parcial).
