# Relatório — Sprint 11 (Migração da Integração Control iD)

**Data de conclusão**: 2026-07-22

## Resumo executivo

Migrada a integração Control iD para a arquitetura oficial de integrações do AppMorador (ver ADR
0014): `Application → Interfaces → Infrastructure → Provider → API Control iD`. Construído o
domínio genérico `Equipamento` (pertence direto à Propriedade, campos deliberadamente agnósticos
de fabricante), o Provider `IControlIdProvider`/`ControlIdProvider` (única implementação real,
via `HttpClient`), DTOs internos totalmente separados dos DTOs de wire-format Control iD (com um
mapper como única fronteira de tradução), criptografia da senha do equipamento via Data Protection
API (corrigindo a falha de segurança confirmada no legado — senha em texto puro), sincronização
manual de Moradores/Credenciais/Permissões reaproveitando o domínio já existente (Sprints 6/7),
importação de eventos construída do zero como segunda fonte real de `IFonteEventos` (alimentando a
Central de Eventos já existente, Sprint 3, sem estrutura paralela), Dashboard com 2 contadores +
2 datas novas, 2 telas mobile novas (Equipamentos, Detalhes do Equipamento), e um simulador HTTP
local descartável (`backend/tools/ControlIdSimulator`) usado para validar toda a comunicação via
requisições HTTP reais, já que nenhum equipamento físico está disponível neste ambiente (decisão
confirmada com o usuário na Fase 1 — ver `docs/sprints/SPRINT_011.md`).

## Relatório da migração (Fase 1)

**Componentes reutilizados** (como referência de protocolo, reescritos na nova arquitetura):
formato de payload e sequência de chamadas (`login.fcgi` → `session` → `load_objects.fcgi`/
`create_objects.fcgi`), padrão de sessão única por operação em lote, lista de endpoints REST.

**Componentes descartados** (não migrados): `IControlIdService`/`IDeviceProvider` como
interfaces (vazavam o tipo do fabricante no controller — exatamente o que este ADR proíbe); JSON
via string interpolation + `JsonDocument` ad hoc (substituído por DTOs tipados); armazenamento de
senha em texto puro (substituído por Data Protection API); `DeviceSyncQueue`/fila com bug de
cobertura parcial (substituída por sincronização manual síncrona, mais simples e sem esse bug);
mecanismo de "importação de eventos" (não existia — construído do zero).

**Evidências de comunicação real**: toda chamada em `ControlIdProvider` usa `HttpClient` via
`IHttpClientFactory` contra um simulador HTTP local rodando em processo separado (`localhost:9500`)
— requisições de rede genuínas (`login.fcgi`, `system_information.fcgi`, `create_objects.fcgi`,
`load_objects.fcgi`), não chamadas em memória a um mock do `IControlIdProvider`. Validado também
o caminho de falha real (porta inatingível → `HttpRequestException` capturada, `Equipamento.Status`
atualizado para `Offline`, mensagem de erro amigável devolvida). **Pendência explícita**: validação
contra hardware Control iD físico real não foi possível neste ambiente — registrada em
`docs/DIVIDA_TECNICA.md`.

## Arquivos criados

**Backend**:
- `Domain/Entities/{Equipamento,FabricanteEquipamento,StatusEquipamento,EventoEquipamento}.cs`
- `Domain/Repositories/{IEquipamentoRepositorio,IEventoEquipamentoRepositorio}.cs`
- `Infrastructure/Persistence/{EquipamentoRepositorio,EventoEquipamentoRepositorio}.cs`
- `Application/Equipamentos/{Dtos,IEquipamentoServico,EquipamentoServico,IntegracaoDtos,
  IEquipamentoIntegracaoServico,EquipamentoIntegracaoServico,ICriptografiaSimetrica}.cs`
- `Application/ControlId/{Dtos,IControlIdProvider}.cs`
- `Infrastructure/ControlId/{ControlIdWireDtos,ControlIdMapper,ControlIdProvider}.cs`
- `Infrastructure/Identity/DataProtectionCriptografiaSimetrica.cs`
- `Infrastructure/Eventos/EquipamentoFonteEventos.cs`
- `Api/Controllers/EquipamentosController.cs`
- Migration `20260722024341_AdicionarEquipamentosIntegracaoControlId`
- `backend/tools/ControlIdSimulator/` (ferramenta de teste descartável, fora do domínio de
  produção — simulador HTTP do protocolo Control iD)

**Mobile**:
- `screens/equipamentos/{EquipamentosScreen,DetalhesEquipamentoScreen}.tsx`
- `components/FabricanteEquipamentoSelector.tsx`
- `screens/dashboard/CardEquipamentos.tsx`

**Documentação**:
- `docs/adr/0014-provider-integracao-control-id.md`
- `docs/sprints/SPRINT_011.md`, `docs/reviews/SPRINT_011.md` (este relatório)

## Arquivos modificados

**Backend**: `Infrastructure/Persistence/AppDbContext.cs` (2 DbSets, relacionamentos, query
filter), `Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (DI dos repositórios/
serviços/Provider/HttpClient novos), `Application/Propriedades/PropriedadeServico.cs` (cascade
expandido para Equipamento), `Application/Eventos/{OrigemEvento,EventosServico}.cs` (valor
`ControlId` + agregação sequencial multi-fonte — correção de um bug de concorrência de
`AppDbContext` encontrado durante a validação desta Sprint), `Application/Dashboard/
{DashboardResponse,DashboardServico}.cs` (4 campos novos), `Api/Program.cs`
(`AddDataProtection`).

**Mobile**: `api/types.ts` (novos DTOs: `EquipamentoResponse`, `TesteConexaoResponse`,
`InformacoesEquipamentoResponse`, `SincronizacaoResponse`, `ImportacaoEventosResponse`; tipos
`FabricanteEquipamento`/`StatusEquipamento`; 4 campos novos em `DashboardResponse`),
`navigation/{types,RootNavigator}.tsx` (rotas Equipamentos/DetalhesEquipamento),
`screens/SelecionarPropriedadeScreen.tsx` (atalho para gerenciar equipamentos),
`screens/dashboard/DashboardScreen.tsx` (novo `CardEquipamentos`).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (itens 20 e 21), `docs/adr/README.md`.

## Arquitetura

Nova sub-camada de integração dentro da Clean Architecture já existente — `Application/ControlId`
(porta) e `Infrastructure/ControlId` (Provider), mesmo padrão já usado por `IFonteEventos`/
`JflFonteEventos` (ADR 0006) e agora formalizado como o padrão OFICIAL para qualquer fabricante
futuro (ver ADR 0014). `Equipamento` segue o padrão de ownership direto à Propriedade (mesmo de
`PontoAcesso`/`Vaga`/`Visitante`) e soft delete (ADR 0009). `EventoEquipamento` segue o padrão de
auditoria pura sem soft delete (mesmo espírito de `Ocorrencia`).

## Fluxos homologados (via requests reais)

- Login → criar Propriedade → cadastrar Equipamento (fabricante Control iD, apontando para o
  simulador local) → senha nunca aparece na resposta (nem cifrada) — confirmado via `curl`.
- **Testar conexão** → requisição HTTP real ao simulador (`login.fcgi`) → `sucesso: true` →
  `Equipamento.Status` atualizado para `Online` — confirmado via `curl` e releitura do
  Equipamento.
- **Consultar informações** → requisição HTTP real (`system_information.fcgi`) → versão/nome/
  número de série do simulador devolvidos corretamente.
- **Sincronizar moradores/credenciais/permissões** → domínio real (Unidade → Morador → Credencial)
  criado via `curl`, sincronizado com sucesso (`quantidadeProcessada` correta em cada chamada),
  `Equipamento.UltimaSincronizacaoUtc` atualizada.
- **Importar eventos** → 2 eventos fixos do simulador importados, persistidos em
  `EventoEquipamento`, e confirmados na Central de Eventos (`GET /eventos`) com títulos traduzidos
  ("Acesso liberado"/"Acesso negado") e origem `ControlId` — mesclados corretamente com a fonte JFL
  já existente.
- **Dashboard** → `quantidadeEquipamentosOnline`/`Offline`/`ultimaSincronizacaoUtc`/
  `ultimoEventoEquipamentoRecebidoUtc` todos com dado real, confirmados via `curl`.
- **Falha de conexão real** → equipamento apontando para uma porta fechada → `sucesso: false` com
  mensagem amigável, `Status` atualizado para `Offline` — confirmado via `curl`.
- **Fabricante sem Provider real** (ex.: `Jfl`) → ação de integração rejeitada com mensagem clara
  ("Integração real para o fabricante Jfl ainda não foi implementada"), nunca falha silenciosa.
- **Cascade completo**: excluir a Propriedade → Equipamento fica inacessível (confirmado via GET
  pós-exclusão retornando 404).
- Regressão: Login, Propriedades, Dashboard, Eventos (paginação/filtro/busca) — contratos
  inalterados, 200 em todos.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (entidades+migration,
  Provider/DTOs/mapper, serviço de integração, DI+controller, Dashboard).
- `npx tsc --noEmit` (mobile): 0 erros após todas as telas/tipos novos.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2638 módulos, sem erro).
  Sem browser disponível neste ambiente para verificação visual direta — mesma limitação já
  registrada em Sprints anteriores.
- Migration revisada (resumo técnico: 100% `CreateTable`/`CreateIndex`, sem operação destrutiva)
  antes de `dotnet ef database update` contra o banco real — protocolo permanente desde a
  Sprint 3.1.
- Validação end-to-end completa via `curl` contra o simulador HTTP local rodando em processo
  separado: CRUD de Equipamento, teste de conexão, consulta de informações, sincronização dos 3
  domínios, importação de eventos, Dashboard, Central de Eventos, cascade de exclusão, e 2 cenários
  de falha (porta fechada; fabricante sem Provider).

## Pendências

- Item 20 (`DIVIDA_TECNICA.md`): validação contra hardware Control iD físico real não foi possível
  neste ambiente — a comunicação foi validada via HTTP real contra um simulador local, não contra
  o equipamento genuíno. Fica como pendência explícita para quando um dispositivo real estiver
  acessível.
- Item 21 (`DIVIDA_TECNICA.md`): sincronização de credencial envia `Valor: null` ao Provider — o
  domínio de Credencial (Sprint 7) nunca modelou um valor físico de tag/PIN, então não há dado real
  para sincronizar hoje além do tipo.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 11 concluída; item de
backlog "Integração de Controle de Acesso e Portões" atualizado — Control iD agora tem Provider
real, demais fabricantes seguem o mesmo padrão), `docs/ALTERACOES_BANCO.md` (nova migration
documentada), `docs/DIVIDA_TECNICA.md` (itens 20 e 21),
`docs/adr/0014-provider-integracao-control-id.md` + índice atualizado,
`docs/sprints/SPRINT_011.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Clean Architecture respeitada; `IControlIdProvider` é a única porta que conhece o fabricante, resolvida por `Fabricante` em `EquipamentoIntegracaoServico`; DTOs internos e de wire-format totalmente separados por um mapper único; nenhum Controller/entidade de domínio importa tipo do namespace `Infrastructure.ControlId`. Padrão documentado em ADR 0014 como vinculante para integrações futuras. |
| **Segurança** | ✅ Aprovado. Senha do equipamento cifrada em repouso (Data Protection API) e nunca devolvida pela API, nem cifrada — corrige a falha de segurança real encontrada no legado (senha em texto puro exposta e vazada em log de auditoria). Ownership testado explicitamente em todas as ações de integração (equipamento de outra propriedade → "não encontrado", mesma mensagem genérica já padronizada). |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (cadastrar equipamento → testar conexão → consultar informações → sincronizar moradores/credenciais/permissões → importar eventos → Dashboard) homologado via requests reais contra o simulador. Nada de "Fora de Escopo" foi implementado (confirmado: sem WebSocket/SignalR/Push/atualização automática/reconhecimento facial/controle remoto de porta). |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State, mensagens de erro/sucesso inline, confirmação antes de excluir presentes nas 2 telas novas. Status Online/Offline com ícone e cor consistentes com o Design System (`colors.safe`/`colors.mute`). |
| **Performance** | ✅ Aprovado (com ressalva registrada). `HttpClient` com timeout curto (5s) evita travar a requisição do usuário quando o equipamento está inacessível. A agregação multi-fonte da Central de Eventos (2 fontes reais agora) busca o top-N de cada fonte por página — custo cresce com o número da página; aceitável na escala atual, registrado como limitação conhecida caso o volume cresça. |
| **Manutenibilidade** | ✅ Aprovado. `ControlIdMapper` é o único ponto de tradução DTO interno ↔ wire-format; `EquipamentoIntegracaoServico.ResolverProvider` é o único ponto de resolução de fabricante — adicionar um novo Provider não exige tocar em nenhum Controller/serviço de domínio existente. |
| **Documentação** | ✅ Aprovado. ADR 0014 documenta a arquitetura completa e a diretriz vinculante para fabricantes futuros; relatório de migração (reaproveitado/descartado/evidências) documentado nesta revisão; 2 itens novos de dívida técnica registram exatamente os recortes conscientes de escopo. |
| **Regressões** | ✅ Nenhuma. Login, Propriedades, Dashboard e Central de Eventos (incluindo paginação/filtro/busca com 2 fontes agora) revalidados via requests reais após toda a implementação — resultados corretos. Um bug real de concorrência em `AppDbContext` (introduzido pela própria mudança desta Sprint, `Task.WhenAll` sobre um `DbContext` `Scoped` não thread-safe) foi encontrado e corrigido durante a própria validação, antes da conclusão. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — arquitetura de Provider estabelecida como padrão oficial, comunicação
real validada (via simulador, hardware físico real pendente), sincronização/importação de eventos
funcionais, Dashboard com dado real, sem regressão, documentação completa.
