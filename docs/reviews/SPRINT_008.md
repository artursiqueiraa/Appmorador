# Relatório — Sprint 8 (Visitantes e Autorizações)

**Data de conclusão**: 2026-07-21

## Resumo executivo

Construído o domínio completo de Visitantes e Autorizações: `Visitante` (pertence direto à
Propriedade, reaproveitável entre unidades) e `Autorizacao` (Morador responsável + Unidade +
Visitante + tipo de visita + período de validade), com `HistoricoVisitante` como auditoria interna
preparada (sem tela). Status efetivo da Autorização é híbrido — Pendente/Ativa/Expirada computados
em tempo de leitura a partir das datas (sem job/scheduler), Cancelada/Utilizada são ações manuais
explícitas que sempre vencem o cálculo por data. CRUD completo (Create/List/Update/Delete),
exclusão lógica cascateada em todo o agregado (Propriedade → Unidade/Visitante, Morador
responsável → Autorizacao, Visitante → Autorizacao), Dashboard com 3 contadores reais novos, e 2
telas mobile novas seguindo o Design System já estabelecido. Zero comunicação com hardware físico
— só o domínio de negócio. Fase 1 (planejamento) apresentada e ajustada com o usuário — 2 decisões
confirmadas antes de qualquer código (ver `docs/sprints/SPRINT_008.md`). Fase 2 implementada em
etapas pequenas, cada uma validada com build/curl antes de avançar.

## Arquivos criados

**Backend**:
- `Domain/Entities/{Visitante,Autorizacao,TipoVisita,StatusAutorizacao,HistoricoVisitante,
  TipoEventoHistoricoVisitante}.cs`
- `Domain/Repositories/{IVisitanteRepositorio,IAutorizacaoRepositorio,
  IHistoricoVisitanteRepositorio}.cs`
- `Infrastructure/Persistence/{VisitanteRepositorio,AutorizacaoRepositorio,
  HistoricoVisitanteRepositorio}.cs`
- `Application/Visitantes/{Dtos,IVisitanteServico,VisitanteServico}.cs`
- `Application/Autorizacoes/{Dtos,IAutorizacaoServico,AutorizacaoServico,
  StatusAutorizacaoCalculator}.cs`
- `Api/Controllers/{VisitantesController,AutorizacoesController}.cs`
- Migration `20260721024105_AdicionarVisitantesEAutorizacoes`

**Mobile**:
- `screens/visitantes/VisitantesScreen.tsx`, `screens/autorizacoes/AutorizacoesScreen.tsx`
- `components/TipoVisitaSelector.tsx`
- `screens/dashboard/CardVisitantes.tsx`

**Documentação**:
- `docs/adr/0011-dominio-visitantes-autorizacoes.md`
- `docs/sprints/SPRINT_008.md`, `docs/reviews/SPRINT_008.md` (este relatório)

## Arquivos modificados

**Backend**: `Infrastructure/Persistence/AppDbContext.cs` (3 DbSets, relacionamentos, query
filters), `Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (DI dos 3 repositórios + 2
serviços novos), `Application/Propriedades/PropriedadeServico.cs` (cascade expandido para
Visitante/Autorizacao), `Application/Unidades/UnidadeServico.cs` (cascade expandido para
Autorizacao), `Application/Moradores/MoradorServico.cs` (cascade expandido para Autorizacao onde o
morador é responsável), `Application/Dashboard/{DashboardResponse,DashboardServico}.cs` (3
contadores novos, reaproveitando `StatusAutorizacaoCalculator` — nunca duplicando a regra de
status).

**Mobile**: `api/types.ts` (novos DTOs: `VisitanteResponse`, `AutorizacaoResponse`, tipos
`TipoVisita`/`StatusAutorizacao`; 3 campos novos em `DashboardResponse`),
`navigation/{types,RootNavigator}.tsx` (rotas Visitantes/Autorizacoes),
`screens/SelecionarPropriedadeScreen.tsx` (atalho para Visitantes),
`screens/dashboard/DashboardScreen.tsx` (novo `CardVisitantes`).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (itens 15 e 16), `docs/adr/README.md`.

## Arquitetura

Nenhuma camada nova — mesma Clean Architecture de sempre (`Domain → Application → Infrastructure
→ Api`). Ownership resolvido subindo a cadeia até `Propriedade.ProprietarioId` (a partir de
`Visitante` diretamente, e a partir de `Autorizacao` via `MoradorResponsavel→Unidade→Propriedade`),
mesmo padrão já usado desde a Sprint 6. Exclusão lógica via query filter global do EF Core (ADR
0009) reaproveitada sem alteração. Decisão estrutural nova documentada em ADR 0011: `Visitante`
pertence à Propriedade (não à Unidade) e Status efetivo é híbrido — computado (Pendente/Ativa/
Expirada) via `StatusAutorizacaoCalculator`, único ponto que implementa essa regra, consumido
tanto por `AutorizacaoServico` (DTO) quanto por `DashboardServico` (contadores), nunca duplicado.

## Fluxos homologados (via requests reais)

- Login → criar Propriedade/Unidade/Morador → criar Visitante → criar 3 Autorizações (datas
  passadas/atuais/futuras) → Status efetivo computado corretamente sem nenhum job: Ativa (hoje
  dentro do período), Pendente (período futuro), Expirada (período passado) — confirmado nas 3
  respostas da API, sem qualquer scheduler envolvido.
- Dashboard reflete os 3 contadores corretamente: 1 visitante ativo, 1 autorização pendente, 1
  expirada — confirmado via `curl` antes/depois da criação.
- Transições manuais de status: Cancelar uma autorização Pendente → `Cancelada`; marcar uma
  autorização Ativa como `Utilizada` → confirmado; tentativa de setar `Status=Pendente`
  manualmente → **rejeitada** ("só é possível definir Cancelada ou Utilizada manualmente");
  tentativa de editar uma autorização já `Utilizada` → **rejeitada** ("autorização cancelada ou
  utilizada não pode ser alterada").
- Validação de consistência Morador↔Unidade: tentativa de usar um Morador de uma Unidade como
  responsável de uma Autorização de outra Unidade → **rejeitada** ("Morador não encontrado",
  mesma mensagem genérica de "não encontrado"/"não pertence", nunca vaza qual unidade o morador
  realmente pertence).
- **Cascade em todos os níveis**, cada um confirmado via API:
  - Excluir um `Visitante` → suas `Autorizacao` ficam inacessíveis (404 ao tentar consultar).
  - Excluir o `Morador` responsável → a `Autorizacao` correspondente some da listagem do
    Visitante (lista vazia onde antes aparecia).
  - Excluir a `Propriedade` → `Visitante` (e, por consequência, suas Autorizações) ficam
    inacessíveis; propriedade some da listagem.
- Regressão: Login, Propriedades, Eventos, Dashboard — contratos inalterados, 200/201 em todos.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (entidades+migration,
  serviços+cascade expandido, DI+controllers, Dashboard).
- `npx tsc --noEmit` (mobile): 0 erros após todas as telas/tipos novos.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2625 módulos, sem erro),
  confirmando que `VisitantesScreen`/`AutorizacoesScreen`/`CardVisitantes` compilam e empacotam
  corretamente. Sem browser disponível neste ambiente para verificação visual direta — mesma
  limitação já registrada em Sprints anteriores.
- Migration revisada (resumo técnico: 100% `CreateTable`/`CreateIndex`, sem operação destrutiva)
  antes de `dotnet ef database update` contra o banco real — protocolo permanente desde a
  Sprint 3.1.
- Verificação específica da regra híbrida de Status: 3 autorizações criadas com janelas de data
  deliberadamente diferentes (passada/atual/futura) confirmaram Pendente/Ativa/Expirada corretos
  na resposta da API imediatamente após a criação — validando que a regra funciona sem nenhum
  processo em segundo plano, conforme decidido no ADR 0011.

## Pendências

Nenhuma bloqueadora. Registradas como dívida técnica (não implementadas por decisão de escopo):

- Item 15 (`DIVIDA_TECNICA.md`): evento "Autorização expirada" não é registrado automaticamente
  no histórico — decisão deliberada (ADR 0011) para evitar job/scheduler ou gravação frágil como
  efeito colateral de leitura.
- Item 16 (`DIVIDA_TECNICA.md`): soft delete não retrofitado em `Visitante`↔`HistoricoVisitante`
  — warning benigno do EF Core no startup, sem impacto funcional hoje.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 8 concluída; item de
backlog "Entregas e Visitantes" separado — Visitantes construído, Entregas permanece backlog),
`docs/ALTERACOES_BANCO.md` (nova migration documentada), `docs/DIVIDA_TECNICA.md` (itens 15 e
16), `docs/adr/0011-dominio-visitantes-autorizacoes.md` + índice atualizado,
`docs/sprints/SPRINT_008.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Clean Architecture respeitada em todas as camadas; nenhuma solução paralela; ownership e soft delete reaproveitam o padrão já estabelecido (ADR 0009). A decisão estrutural real (escopo do Visitante + Status híbrido) foi documentada em ADR 0011 com alternativas consideradas, não implícita no código. `StatusAutorizacaoCalculator` extraído como fonte única da regra de status, evitando duplicação entre `AutorizacaoServico` e `DashboardServico`. |
| **Segurança** | ✅ Aprovado. Ownership testado explicitamente (validação Morador↔Unidade rejeitada com mensagem genérica, sem vazar a unidade real do morador) e em todos os níveis de cascade. `AtualizarStatusAsync` rejeita explicitamente qualquer tentativa de setar um status computado (Pendente/Ativa/Expirada) manualmente. |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (propriedade → unidade → morador responsável → visitante → autorização → período → Dashboard) homologado via requests reais, com dado real persistido. Nada de "Fora de Escopo" foi implementado (confirmado: sem QR Code/facial real, sem liberação automática, sem integração de hardware). |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State (copy tranquilizadora), validação (botão desabilitado até campos obrigatórios), feedback de erro (mensagem inline) presentes nas 2 telas novas. Confirmação antes de cancelar autorização implementada via `Alert.alert` com opção destrutiva destacada — nunca uma transição de status silenciosa. Confirmação antes de excluir também presente em Visitantes/Autorizações. |
| **Performance** | ✅ Aprovado. Status efetivo computado em memória sobre uma lista já filtrada por propriedade (sem N+1); Dashboard usa uma única consulta por propriedade para os 3 contadores novos, sem carregar dados de outras propriedades. |
| **Manutenibilidade** | ✅ Aprovado. `StatusAutorizacaoCalculator` como único ponto de verdade da regra de status torna trivial auditar/alterar a regra no futuro sem risco de divergência entre Dashboard e API. Repositórios com métodos `ListByX` específicos por granularidade de cascade (Visitante/Unidade/MoradorResponsavel/Propriedade) tornam explícito qual nível cada consulta serve. |
| **Documentação** | ✅ Aprovado. ADR 0011 documenta as duas decisões estruturais com alternativas consideradas; 2 itens novos de dívida técnica registram os recortes de escopo conscientes (sem job de expiração, warning benigno de query filter); CHANGELOG/ROADMAP refletem exatamente o que foi entregue, incluindo a atualização do item de backlog compartilhado com Entregas. |
| **Regressões** | ✅ Nenhuma. Login, Propriedades, Eventos e Dashboard revalidados via requests reais após toda a implementação — resultados idênticos aos anteriores. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — CRUD funcional, Dashboard com dado real, soft delete funcionando em
cascade completo, histórico funcionando para os eventos manuais, sem regressão, documentação
completa.
