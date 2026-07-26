# Relatório — Sprint 10 (Entregas e Correspondências)

**Data de conclusão**: 2026-07-22

## Resumo executivo

Construído o domínio completo de Entregas e Correspondências: `Entrega` (Morador destinatário +
Unidade, validados consistentes entre si; Tipo; Descrição; Recebido por; Data de recebimento/
retirada; Observações; Status), com uma máquina de estados 100% manual — sem calculadora híbrida
como Autorizacao (Sprint 8) ou Vaga (Sprint 9), porque a própria missão fechou essa ambiguidade
("não utilizar jobs automáticos"). `HistoricoEntrega` como auditoria pura. CRUD completo, com
Create/List no nível da Propriedade (visão unificada de "central de entregas", variação
deliberada do padrão de aninhamento sob o Morador usado em Sprints anteriores), exclusão lógica
cascateada em todo o agregado (Propriedade → Unidade/Morador → Entrega), Dashboard com 4
contadores reais novos, e 2 telas mobile novas (Entregas, Detalhes da Entrega) seguindo o Design
System já estabelecido. Zero comunicação com hardware/serviço externo. Fase 1 não exigiu
perguntas de esclarecimento — a missão já resolveu as ambiguidades usuais no próprio texto (ver
`docs/sprints/SPRINT_010.md`). Fase 2 implementada em etapas pequenas, cada uma validada com
build/curl antes de avançar.

## Arquivos criados

**Backend**:
- `Domain/Entities/{Entrega,TipoEntrega,StatusEntrega,HistoricoEntrega,
  TipoEventoHistoricoEntrega}.cs`
- `Domain/Repositories/{IEntregaRepositorio,IHistoricoEntregaRepositorio}.cs`
- `Infrastructure/Persistence/{EntregaRepositorio,HistoricoEntregaRepositorio}.cs`
- `Application/Entregas/{Dtos,IEntregaServico,EntregaServico}.cs`
- `Api/Controllers/EntregasController.cs`
- Migration `20260722012326_AdicionarEntregasECorrespondencias`

**Mobile**:
- `screens/entregas/{EntregasScreen,DetalhesEntregaScreen}.tsx`
- `components/TipoEntregaSelector.tsx`
- `screens/dashboard/CardEntregas.tsx`

**Documentação**:
- `docs/adr/0013-dominio-entregas-correspondencias.md`
- `docs/sprints/SPRINT_010.md`, `docs/reviews/SPRINT_010.md` (este relatório)

## Arquivos modificados

**Backend**: `Infrastructure/Persistence/AppDbContext.cs` (2 DbSets, relacionamentos, query
filters), `Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (DI dos 2 repositórios + 1
serviço novo), `Application/Propriedades/PropriedadeServico.cs`, `Application/Unidades/
UnidadeServico.cs`, `Application/Moradores/MoradorServico.cs` (cascade expandido para Entrega),
`Application/Dashboard/{DashboardResponse,DashboardServico}.cs` (4 contadores novos).

**Mobile**: `api/types.ts` (novos DTOs: `EntregaResponse`; tipos `TipoEntrega`/`StatusEntrega`; 4
campos novos em `DashboardResponse`), `navigation/{types,RootNavigator}.tsx` (rotas Entregas/
DetalhesEntrega), `screens/SelecionarPropriedadeScreen.tsx` (atalho para gerenciar entregas),
`screens/dashboard/DashboardScreen.tsx` (novo `CardEntregas`).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (item 19), `docs/adr/README.md`.

## Arquitetura

Nenhuma camada nova — mesma Clean Architecture de sempre (`Domain → Application → Infrastructure
→ Api`). Ownership resolvido subindo a cadeia até `Propriedade.ProprietarioId` (a partir de
`Entrega` via `MoradorDestinatario→Unidade→Propriedade`, mesmo padrão seguro de `Autorizacao`,
Sprint 8 — resolvido pelo caminho do Morador, não pelo Include secundário de Unidade, para
garantir que a navegação `Propriedade` esteja sempre populada). Exclusão lógica via query filter
global do EF Core (ADR 0009) reaproveitada sem alteração. Decisão estrutural nova documentada em
ADR 0013: Status 100% manual (sem duplicar o padrão híbrido das Sprints 8-9 onde não se aplica) e
visão unificada por Propriedade em vez de aninhamento sob o Morador.

## Fluxos homologados (via requests reais)

- Login → criar Propriedade/Unidade/Morador → registrar Entrega (Status inicial
  `AguardandoRecebimento`, `DataRecebimento`/`DataRetirada` nulas, conforme ADR 0013) → Dashboard
  reflete 1 entrega pendente e 1 correspondência cadastrada — confirmado via `curl`.
- Marcar disponível para retirada (`recebidoPor: "Portaria"`) → `DataRecebimentoUtc` preenchida
  automaticamente, `Status = DisponivelParaRetirada`.
- Registrar retirada → `DataRetiradaUtc` preenchida automaticamente, `Status = Retirada`.
- Tentativa de alterar o status de uma entrega já `Retirada` → **rejeitada** ("Não é possível
  mudar de Retirada para Cancelada"), confirmando o estado terminal.
- Tentativa de pular direto de `AguardandoRecebimento` para `Retirada` (sem passar por
  `DisponivelParaRetirada`) → **rejeitada** ("Não é possível mudar de AguardandoRecebimento para
  Retirada"), confirmando que a máquina de estados só aceita transições válidas.
- Cancelamento direto de `AguardandoRecebimento` → **aceito** (transição válida).
- Tentativa de editar (`UpdateAsync`) uma entrega cancelada → **rejeitada** ("Entrega retirada ou
  cancelada não pode ser alterada").
- Validação de consistência Morador↔Unidade: tentativa de usar um Morador de uma Unidade como
  destinatário de uma Entrega de outra Unidade → **rejeitada** ("Morador não encontrado", mesma
  mensagem genérica de "não encontrado"/"não pertence").
- **Cascade completo**: excluir a Propriedade → Entrega fica inacessível (confirmado via GET
  pós-exclusão retornando 404).
- Regressão: Login, Propriedades, Dashboard, Eventos — contratos inalterados, 200 em todos.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (entidades+migration,
  serviço+cascade expandido, DI+controller, Dashboard).
- `npx tsc --noEmit` (mobile): 0 erros após todas as telas/tipos novos.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2634 módulos, sem erro),
  confirmando que `EntregasScreen`/`DetalhesEntregaScreen`/`CardEntregas` compilam e empacotam
  corretamente. Sem browser disponível neste ambiente para verificação visual direta — mesma
  limitação já registrada em Sprints anteriores.
- Migration revisada (resumo técnico: 100% `CreateTable`/`CreateIndex`, sem operação destrutiva)
  antes de `dotnet ef database update` contra o banco real — protocolo permanente desde a
  Sprint 3.1.
- Verificação específica da máquina de estados: testados os 2 caminhos válidos completos
  (Aguardando→Disponível→Retirada, e Aguardando→Cancelada), e 2 tentativas inválidas (pular
  etapa, transição a partir de estado terminal) — todos com o resultado esperado.

## Pendências

Nenhuma bloqueadora. Registrada como dívida técnica (não implementada por decisão de escopo):

- Item 19 (`DIVIDA_TECNICA.md`): soft delete não retrofitado em `Entrega`↔`HistoricoEntrega` —
  warning benigno do EF Core no startup, sem impacto funcional hoje.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 10 concluída; item de
backlog "Entregas" substituído por "Integração de Entregas e Correspondências", já com o domínio
construído), `docs/ALTERACOES_BANCO.md` (nova migration documentada), `docs/DIVIDA_TECNICA.md`
(item 19), `docs/adr/0013-dominio-entregas-correspondencias.md` + índice atualizado,
`docs/sprints/SPRINT_010.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Clean Architecture respeitada em todas as camadas; nenhuma solução paralela; ownership e soft delete reaproveitam o padrão já estabelecido (ADR 0009). A decisão estrutural real (Status 100% manual + visão unificada por Propriedade) foi documentada em ADR 0013 com o racional explícito de por que não repete o padrão híbrido das Sprints 8-9. Máquina de estados centralizada em `AtualizarStatusAsync` — único ponto de manutenção da regra, nunca duplicada. |
| **Segurança** | ✅ Aprovado. Ownership testado explicitamente (validação Morador↔Unidade rejeitada com mensagem genérica) e no cascade da Propriedade. Máquina de estados rejeita explicitamente qualquer transição inválida, incluindo tentativas de "pular etapa" ou agir sobre um estado terminal. |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (propriedade → unidade → morador → registrar entrega → marcar disponível → registrar retirada → Dashboard) homologado via requests reais, com dado real persistido. Nada de "Fora de Escopo" foi implementado (confirmado: sem QR Code, assinatura digital, foto, integração de transportadora). |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State (copy tranquilizadora), validação (botão desabilitado até campos obrigatórios), feedback de erro (mensagem inline) presentes nas 2 telas novas. Confirmação antes de cancelar/excluir implementada via `Alert.alert` com opção destrutiva destacada. Ação "marcar disponível" usa um mini-formulário inline (não `Alert.prompt`, que não é multiplataforma) para capturar "Recebido por" opcionalmente. |
| **Performance** | ✅ Aprovado. Contagens do Dashboard via `CountByPropriedadeAsync` dedicado (sem carregar listas inteiras) — mais simples que Sprints 8-9 por não precisar de cálculo em memória (Status é gravado diretamente). |
| **Manutenibilidade** | ✅ Aprovado. Máquina de estados expressa como um único `switch`/pattern match em `AtualizarStatusAsync`, fácil de auditar; `EntregaResponse` denormalizado (nomes de Morador/Unidade) evita N+1 no mobile. |
| **Documentação** | ✅ Aprovado. ADR 0013 documenta as duas decisões estruturais com o racional de por que este domínio diverge dos anteriores; 1 item novo de dívida técnica registra o recorte de escopo consciente; CHANGELOG/ROADMAP refletem exatamente o que foi entregue, incluindo a atualização do item de backlog de Entregas para a fase de integração física. |
| **Regressões** | ✅ Nenhuma. Login, Propriedades, Dashboard e Eventos revalidados via requests reais após toda a implementação — resultados idênticos aos anteriores. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — CRUD funcional, histórico funcionando, Dashboard com dado real, soft
delete funcionando em cascade completo, sem regressão, documentação completa.
