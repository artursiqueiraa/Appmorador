# Relatório — Sprint 9 (Veículos e Garagens)

**Data de conclusão**: 2026-07-21

## Resumo executivo

Construído o domínio completo de Veículos e Garagens: `Veiculo` (pertence a um Morador, placa
normalizada e validada como única entre veículos não excluídos) e `Vaga` (domínio independente,
pertence direto à Propriedade), com `VinculoVeiculoVaga` como entidade temporal (cada linha um
período de ocupação, nunca sobrescrita — "vincular" e "alterar vaga" são a mesma operação) e
`PermissaoVeicular` reaproveitando `PontoAcesso` (que ganhou o campo `Tipo` Geral/Veicular) em vez
de um enum próprio de áreas. Status efetivo da Vaga é híbrido — Livre/Ocupada computados em tempo
de leitura a partir do vínculo ativo (sem job/scheduler), Bloqueada/Reservada são ações manuais
explícitas que sempre vencem o cálculo. CRUD completo (Create/List/Update/Delete), exclusão lógica
cascateada em todo o agregado (Propriedade → Unidade/Morador → Veiculo, Vaga → VinculoVeiculoVaga,
PontoAcesso → PermissaoVeicular), Dashboard com 5 contadores reais novos, e 2 telas mobile novas
seguindo o Design System já estabelecido. Zero comunicação com hardware físico — só o domínio de
negócio. Fase 1 (planejamento) apresentada e ajustada com o usuário — 2 decisões confirmadas antes
de qualquer código (ver `docs/sprints/SPRINT_009.md`). Fase 2 implementada em etapas pequenas,
cada uma validada com build/curl antes de avançar.

## Arquivos criados

**Backend**:
- `Domain/Entities/{Veiculo,TipoVeiculo,StatusVeiculo,Vaga,TipoVaga,StatusVaga,
  VinculoVeiculoVaga,PermissaoVeicular,HistoricoVeiculo,TipoEventoHistoricoVeiculo,HistoricoVaga,
  TipoEventoHistoricoVaga,TipoPontoAcesso}.cs`
- `Domain/Repositories/{IVeiculoRepositorio,IVagaRepositorio,IVinculoVeiculoVagaRepositorio,
  IPermissaoVeicularRepositorio,IHistoricoVeiculoRepositorio,IHistoricoVagaRepositorio}.cs`
- `Infrastructure/Persistence/{VeiculoRepositorio,VagaRepositorio,VinculoVeiculoVagaRepositorio,
  PermissaoVeicularRepositorio,HistoricoVeiculoRepositorio,HistoricoVagaRepositorio}.cs`
- `Application/Veiculos/{Dtos,IVeiculoServico,VeiculoServico}.cs`
- `Application/Vagas/{Dtos,IVagaServico,VagaServico,VagaStatusCalculator}.cs`
- `Application/VinculosVeiculoVaga/{Dtos,IVeiculoVagaServico,VeiculoVagaServico}.cs`
- `Application/PermissoesVeiculares/{Dtos,IPermissaoVeicularServico,PermissaoVeicularServico}.cs`
- `Api/Controllers/{VeiculosController,VagasController,VeiculoVagaController,
  PermissoesVeicularesController}.cs`
- Migration `20260721100710_AdicionarVeiculosEGaragens`

**Mobile**:
- `screens/veiculos/VeiculosScreen.tsx`, `screens/vagas/VagasScreen.tsx`
- `components/{TipoVeiculoSelector,TipoVagaSelector}.tsx`
- `screens/dashboard/CardVeiculos.tsx`

**Documentação**:
- `docs/adr/0012-dominio-veiculos-garagens.md`
- `docs/sprints/SPRINT_009.md`, `docs/reviews/SPRINT_009.md` (este relatório)

## Arquivos modificados

**Backend**: `Domain/Entities/PontoAcesso.cs` (campo `Tipo` novo), `Infrastructure/Persistence/
AppDbContext.cs` (6 DbSets, relacionamentos, query filters, conversão de `PontoAcesso.Tipo`),
`Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (DI dos 6 repositórios + 4 serviços
novos), `Application/PontosAcesso/{Dtos,PontoAcessoServico}.cs` (campo `Tipo`, cascade expandido
para `PermissaoVeicular`), `Application/Propriedades/PropriedadeServico.cs`, `Application/
Unidades/UnidadeServico.cs`, `Application/Moradores/MoradorServico.cs` (cascade expandido para
Veiculo/Vaga/VinculoVeiculoVaga/PermissaoVeicular), `Application/Dashboard/{DashboardResponse,
DashboardServico}.cs` (5 contadores novos, reaproveitando `VagaStatusCalculator`).

**Mobile**: `api/types.ts` (novos DTOs: `VeiculoResponse`, `VagaResponse`,
`VinculoVeiculoVagaResponse`; tipos `TipoVeiculo`/`StatusVeiculo`/`TipoVaga`/`StatusVaga`/
`TipoPontoAcesso`; 5 campos novos em `DashboardResponse`; `tipo` novo em `PontoAcessoResponse`),
`navigation/{types,RootNavigator}.tsx` (rotas Veiculos/Vagas), `screens/pontosAcesso/
PontosAcessoScreen.tsx` (seletor Geral/Veicular), `screens/moradores/MoradoresScreen.tsx` (atalho
para ver veículos), `screens/SelecionarPropriedadeScreen.tsx` (atalho para gerenciar vagas),
`screens/dashboard/DashboardScreen.tsx` (novo `CardVeiculos`).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (itens 17 e 18), `docs/adr/README.md`.

## Arquitetura

Nenhuma camada nova — mesma Clean Architecture de sempre (`Domain → Application → Infrastructure
→ Api`). Ownership resolvido subindo a cadeia até `Propriedade.ProprietarioId` (a partir de
`Veiculo` via `Morador→Unidade→Propriedade`, e a partir de `Vaga` diretamente). Exclusão lógica
via query filter global do EF Core (ADR 0009) reaproveitada sem alteração. Decisão estrutural nova
documentada em ADR 0012: Vaga como domínio independente + Status híbrido (mesmo padrão do ADR
0011) + `PermissaoVeicular` reaproveitando `PontoAcesso` em vez de um enum próprio — nenhuma regra
de "lugar com acesso controlado" duplicada no sistema. `VagaStatusCalculator` é o único ponto que
implementa a regra de status, consumido tanto por `VagaServico` (DTO) quanto por `DashboardServico`
(contadores).

## Fluxos homologados (via requests reais)

- Login → criar Propriedade/Unidade/Morador → criar Veículo (placa normalizada para maiúscula) →
  criar Vaga (Livre por padrão) → Dashboard reflete 1 veículo/1 vaga livre — confirmado via
  `curl`.
- Validação de placa duplicada: recadastro da mesma placa (variando case/espaço) → **rejeitado**
  ("Já existe um veículo cadastrado com essa placa"), confirmando normalização + unicidade.
- Vincular veículo à vaga → Status efetivo da vaga muda automaticamente para `Ocupada` (sem
  nenhuma ação manual adicional) — confirmado via listagem de vagas e contadores do Dashboard.
- Tentativa de vincular um 2º veículo à mesma vaga já ocupada → **rejeitada** ("Vaga já está
  ocupada por outro veículo").
- "Alterar vaga" (vincular o mesmo veículo a uma vaga diferente): vaga antiga volta a `Livre`,
  vaga nova fica `Ocupada`, histórico completo preservado (`GET /vinculos` mostra os 2 registros,
  um com `dataFimUtc` preenchido).
- Status manual da vaga: `Bloqueada` aplicado e confirmado; tentativa de setar `Status=Ocupada`
  manualmente → **rejeitada** ("Ocupada é sempre computado... nunca definido manualmente");
  `Livre` usado para limpar o override manual.
- Permissão Veicular: criação usando um `PontoAcesso` do tipo `Geral` → **rejeitada** ("Ponto de
  acesso não encontrado", mensagem genérica que não revela o tipo real); criação usando um
  `PontoAcesso` do tipo `Veicular` → confirmada.
- **Cascade completo**: excluir a Propriedade → Veículo, Vaga, Vínculo e Permissão Veicular todos
  ficam inacessíveis (confirmado via tentativa de update pós-exclusão retornando 404).
- Regressão: Login, Propriedades, Dashboard — contratos inalterados, 200 em todos.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (entidades+migration,
  serviços+cascade expandido, DI+controllers, Dashboard).
- `npx tsc --noEmit` (mobile): 0 erros após todas as telas/tipos novos.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2630 módulos, sem erro),
  confirmando que `VeiculosScreen`/`VagasScreen`/`CardVeiculos` compilam e empacotam
  corretamente. Sem browser disponível neste ambiente para verificação visual direta — mesma
  limitação já registrada em Sprints anteriores.
- Migration revisada (resumo técnico: `AddColumn` com backfill + 100% `CreateTable`/
  `CreateIndex`, sem operação destrutiva) antes de `dotnet ef database update` contra o banco
  real — protocolo permanente desde a Sprint 3.1.
- Verificação específica da regra híbrida de Status da Vaga: vincular/desvincular/realocar um
  veículo confirmado mudando o Status efetivo automaticamente em cada operação, sem nenhum
  processo em segundo plano — validando o ADR 0012 na prática.

## Pendências

Nenhuma bloqueadora. Registradas como dívida técnica (não implementadas por decisão de escopo):

- Item 17 (`DIVIDA_TECNICA.md`): soft delete não retrofitado em `Veiculo`↔`HistoricoVeiculo` e
  `Vaga`↔`HistoricoVaga` — warnings benignos do EF Core no startup, sem impacto funcional hoje.
- Item 18 (`DIVIDA_TECNICA.md`): unicidade de Placa validada em código, não por índice físico —
  decisão deliberada (ADR 0012) para permitir recadastro de placa após exclusão lógica do veículo
  antigo; risco de corrida concorrente considerado baixo para o volume de uso esperado.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 9 concluída; item de
backlog "Veículos" substituído por "Integração de Veículos e Garagens", já com o domínio
construído), `docs/ALTERACOES_BANCO.md` (nova migration documentada), `docs/DIVIDA_TECNICA.md`
(itens 17 e 18), `docs/adr/0012-dominio-veiculos-garagens.md` + índice atualizado,
`docs/sprints/SPRINT_009.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Clean Architecture respeitada em todas as camadas; nenhuma solução paralela; ownership e soft delete reaproveitam o padrão já estabelecido (ADR 0009). As três decisões estruturais reais (Vaga independente, Status híbrido, reuso de PontoAcesso) foram documentadas em ADR 0012 com alternativas consideradas, não implícitas no código. `VagaStatusCalculator` extraído como fonte única da regra de status, evitando duplicação entre `VagaServico` e `DashboardServico`. |
| **Segurança** | ✅ Aprovado. Ownership testado explicitamente em todos os níveis de cascade; validação de placa duplicada e vaga indisponível confirmadas; `PermissaoVeicular` rejeita Pontos de Acesso do tipo errado com mensagem genérica que não vaza o tipo real. `AtualizarStatusAsync` rejeita explicitamente qualquer tentativa de setar Ocupada manualmente. |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (propriedade → unidade → morador → veículo → vaga → vincular → alterar vaga → Dashboard) homologado via requests reais, com dado real persistido. Nada de "Fora de Escopo" foi implementado (confirmado: sem OCR, sem portão automático, sem integração de hardware). |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State (copy tranquilizadora), validação (botão desabilitado até campos obrigatórios), feedback de erro (mensagem inline) presentes nas 2 telas novas. Confirmação antes de excluir presente em Veículos/Vagas. Painel de vínculo carregado sob demanda evita N+1 desnecessário na listagem. |
| **Performance** | ✅ Aprovado. Status efetivo computado em memória sobre uma lista já filtrada por propriedade (sem N+1); Dashboard usa consultas dedicadas por propriedade para os 5 contadores novos. |
| **Manutenibilidade** | ✅ Aprovado. `VagaStatusCalculator` como único ponto de verdade da regra de status; reuso de `PontoAcesso` em vez de um enum paralelo reduz superfície de manutenção; repositórios com métodos `ListByX`/`ListByVeiculosAsync` específicos por granularidade de cascade tornam explícito qual nível cada consulta serve. |
| **Documentação** | ✅ Aprovado. ADR 0012 documenta as três decisões estruturais com alternativas consideradas; 2 itens novos de dívida técnica registram os recortes de escopo conscientes; CHANGELOG/ROADMAP refletem exatamente o que foi entregue, incluindo a atualização do item de backlog de Veículos para a fase de integração física. |
| **Regressões** | ✅ Nenhuma. Login, Propriedades e Dashboard revalidados via requests reais após toda a implementação — resultados idênticos aos anteriores. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — CRUD funcional, vinculação Veículo↔Vaga funcional com histórico
completo, Dashboard com dado real, soft delete funcionando em cascade completo, sem regressão,
documentação completa.
