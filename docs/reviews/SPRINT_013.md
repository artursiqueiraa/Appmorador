# Relatório — Sprint 13 (Camada Operacional Unificada)

**Data de conclusão**: 2026-07-25

## Resumo executivo

Consolidados os dados já produzidos pelas integrações Control iD (Sprint 11) e JFL (Sprint 12)
numa camada operacional única: `Estado Bruto` (leitura pura do que já está persistido —
`Equipamento.Status`, `StatusCentralJfl.TemProblemaAtivo` — nunca um Provider) →
`IClassificadorOperacionalServico` (regras centralizadas de Saudável/Atenção/Crítico/Offline, por
equipamento e por Propriedade) → `SnapshotOperacional` (rollup 1:1 por Propriedade, persistido via
upsert, mesmo padrão de `StatusCentralJfl`) → Dashboard/Mobile. Nenhuma interface consulta
Providers diretamente a partir de agora — regra vinculante para toda integração futura (ver ADR
0016). A Central de Eventos ganhou 5 filtros novos (Equipamento/Fabricante/Origem/Categoria/
Severidade); a "Timeline Operacional" pedida pela missão **é** essa mesma Central de Eventos, sem
nenhum domínio de eventos novo criado. O Dashboard manteve 100% dos componentes/campos existentes
intactos (nenhum redesign), só acrescentando os 4 campos consolidados novos via um card próprio.

## Arquitetura

Fluxo obrigatório (ADR 0016): `Provider → Estado Bruto → Classificador Operacional → Snapshot
Operacional → Dashboard/Mobile/APIs futuras`. `SnapshotOperacionalServico.ObterAsync` gera o
snapshot sob demanda na primeira leitura (geração é pura agregação de banco, sem custo de rede) e
devolve o cache depois; `AtualizarAsync` sempre recalcula, disparado só pela ação explícita
"Atualizar" no mobile — sem job/scheduler/polling, mesma régua de simplicidade em MVPs de sempre.
A quebra por equipamento (`SnapshotOperacionalResponse.Equipamentos`) é recomputada em toda
leitura, mesmo quando o rollup numérico vem do cache, para manter a tela "Saúde da Propriedade"
sempre atual a custo desprezível.

## Fluxos homologados (via requests reais)

- **Propriedade sem nenhum equipamento cadastrado** → `ObterAsync` bootstrap-gera o snapshot →
  `Saude: "Saudavel"`, `quantidadeEquipamentosOnline/Offline: 0` (regra de prioridade 1 do
  Classificador — instalação vazia não é motivo de alarme).
- **Equipamento JFL testado offline** (central sem sessão TCP ativa) → Propriedade classificada
  `Offline` (nenhum equipamento online).
- **Equipamento JFL testado e Online** (via simulador `JflSimulator`) → Propriedade
  `Saudavel`, 1/1 equipamento online, `QuantidadeAlarmesAtivos: 0`.
- **Adicionar um equipamento Control iD inalcançável** (IP não responde) na mesma Propriedade →
  Propriedade recalculada para `Atencao` (1 online / 1 offline), `QuantidadeFalhasDetectadas: 1`
  refletindo o equipamento que falhou o teste de conexão.
- **Central de Eventos — 8 cenários de filtro validados**: `origem=Jfl` retorna só eventos JFL;
  `origem=ControlId` retorna só eventos de acesso; `categoria=Alarme`/`categoria=Acesso`
  equivalentes; `fabricante=Jfl`/`fabricante=ControlId` consistentes com `origem`;
  `severidade=Critico` no filtro JFL retorna só ocorrências com `StatusResolucao=Resolvido`;
  `equipamentoId` de um equipamento JFL resolve corretamente via Número de Série (`Central.
  NumeroSerie`) e devolve só as ocorrências daquela central; combinações contraditórias
  (ex.: `origem=Jfl&categoria=Acesso`) corretamente retornam lista vazia + `Total: 0`, nas duas
  fontes.
- **Dashboard** → `saude`, `quantidadeEventosHoje`, `quantidadeAlarmesAtivos`,
  `ultimaAtualizacaoOperacionalUtc` presentes e consistentes com o snapshot mais recente; todos os
  campos/campos de Sprints anteriores (`quantidadeCentraisJflOnline`, `quantidadeCredenciais` etc.)
  inalterados byte a byte.
- **Regressão**: `properties/{id}` (200), `equipamentos` (200, Control iD ainda listado, JFL
  ausente — comportamento herdado da Sprint 12), `jfl/status` (200), `jfl/detalhes` (200,
  auto-vínculo por Número de Série preservado), `eventos?pagina=2` (200, paginação intacta com os
  novos filtros ausentes da query).

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (Domain + migration,
  Classificador, `SnapshotOperacionalServico`, filtros de `FiltroEventos` nas 2 fontes,
  `OperacionalController` + DI, Dashboard).
- Migration revisada (resumo técnico: 100% `CreateTable` + 1 índice único, sem operação
  destrutiva) antes de `dotnet ef database update` contra o banco real — protocolo permanente
  desde a Sprint 3.1.
- Validação end-to-end via `curl` contra o backend real: bootstrap de snapshot vazio,
  classificação Saudável/Atenção/Offline com equipamentos JFL/Control iD reais (simulador/IP
  inalcançável), os 8 cenários de filtro da Central de Eventos, e a varredura de regressão
  listada acima.
- `npx tsc --noEmit` (mobile): 0 erros após os novos tipos/telas/card.
- `npx expo export --platform web`: bundle Metro gerado com sucesso, sem erro.

## Arquivos criados

**Backend**:
- `Domain/Entities/{EstadoOperacional,SnapshotOperacional}.cs`
- `Domain/Repositories/ISnapshotOperacionalRepositorio.cs`
- `Infrastructure/Persistence/SnapshotOperacionalRepositorio.cs`
- `Application/Operacional/{Dtos,IClassificadorOperacionalServico,ClassificadorOperacionalServico,
  ISnapshotOperacionalServico,SnapshotOperacionalServico}.cs`
- `Api/Controllers/OperacionalController.cs`
- Migration `20260725115333_CamadaOperacionalUnificada`

**Mobile**:
- `screens/operacional/{CentralOperacionalScreen,SaudePropriedadeScreen}.tsx`
- `screens/dashboard/CardSnapshotOperacional.tsx`
- `utils/estadoOperacional.ts`

**Documentação**:
- `docs/adr/0016-camada-operacional-unificada.md`
- `docs/sprints/SPRINT_013.md`, `docs/reviews/SPRINT_013.md` (este relatório)

## Arquivos modificados

**Backend**: `Application/Eventos/{FiltroEventos,OrigemEvento}.cs` (5 filtros novos + doc
atualizada), `Infrastructure/Eventos/{JflFonteEventos,EquipamentoFonteEventos}.cs` (aplicação dos
filtros novos, cada fonte por conta própria), `Api/Controllers/EventosController.cs` (5
`[FromQuery]` novos), `Application/Dashboard/{DashboardResponse,DashboardServico}.cs` (4 campos
consolidados novos + fonte trocada para o Snapshot), `Infrastructure/Persistence/AppDbContext.cs`
(DbSet + relacionamento + índice), `Infrastructure/Identity/AuthServiceCollectionExtensions.cs`
(DI da camada operacional).

**Mobile**: `api/types.ts` (4 campos novos em `DashboardResponse` + `EstadoOperacional`/
`EquipamentoSaudeResponse`/`SnapshotOperacionalResponse`), `navigation/{types,RootNavigator}.tsx`
(rotas `CentralOperacional`/`SaudePropriedade` no branch pós-Dashboard), `screens/dashboard/
DashboardScreen.tsx` (`CardSnapshotOperacional` renderizado após `CardSaude`).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (item 24), `docs/adr/README.md`.

## Pendências

- Item 24 (`DIVIDA_TECNICA.md`): eventos de transição de conectividade ("Equipamento Offline"/
  "Reconectado") não são registrados como evento auditável — implementá-los exigiria uma nova
  fonte de eventos, o que a missão desta Sprint proibiu explicitamente ("não criar novo domínio
  de eventos"). Recorte consciente, não um esquecimento.
- Itens 20/22 (`DIVIDA_TECNICA.md`, já existentes): validação contra hardware Control iD/JFL
  físico real permanece pendente — o Estado Bruto desta Sprint herda essa mesma limitação, já que
  lê exatamente o que essas integrações já persistem.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 13 concluída, novo item de
backlog para o item 24), `docs/ALTERACOES_BANCO.md` (nova migration documentada),
`docs/DIVIDA_TECNICA.md` (item 24), `docs/adr/0016-camada-operacional-unificada.md` + índice
atualizado, `docs/sprints/SPRINT_013.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. O fluxo Estado Bruto → Classificador → Snapshot é respeitado rigorosamente — nenhum Controller/serviço fora de `SnapshotOperacionalServico` toca `IControlIdProvider`/`IJflProvider`. `IClassificadorOperacionalServico` é puro e sem estado, testável isoladamente. Timeline Operacional reaproveitando `EventosController` sem duplicação é a decisão correta diante da restrição explícita "não criar novo domínio de eventos". |
| **Segurança** | ✅ Aprovado. `OperacionalController` valida ownership via o mesmo padrão de `DashboardController`/`EventosController` (compara `Propriedade.ProprietarioId`). Nenhuma credencial/token exposto nos novos DTOs — `SnapshotOperacionalResponse` só contém contadores e enums. |
| **Produto** | ✅ Aprovado. Saúde da Propriedade, Central Operacional e filtros da Central de Eventos funcionam fim a fim com dado real. Nada de "Fora de Escopo" foi implementado (confirmado: sem SignalR/WebSocket/Push/Hikvision/Intelbras/Dahua/Analytics/IA/controle remoto automático). |
| **UX/UI** | ✅ Aprovado. `CardSnapshotOperacional` segue a mesma linguagem visual dos cards existentes (nenhum redesign, conforme exigido); emoji/cor por estado centralizados em `estadoOperacional.ts` garantem consistência entre Dashboard, Central Operacional e Saúde da Propriedade. Botão "Atualizar" com feedback de loading presente. |
| **Performance** | ✅ Aprovado. Geração de snapshot nunca consulta um Provider — é agregação pura de banco, inclusive na leitura de bootstrap. Nenhum polling automático introduzido. Filtros novos da Central de Eventos são aplicados via SQL (nunca em memória), preservando a precisão do `Total` sem sobre-buscar. |
| **Manutenibilidade** | ✅ Aprovado. Regras de classificação centralizadas num único serviço evitam a duplicação que motivou esta Sprint (cada integração antes calculava seu próprio "estado" ad-hoc dentro de `DashboardServico`). Cada `IFonteEventos` mantém responsabilidade isolada sobre seus próprios filtros, sem acoplamento entre fontes. |
| **Documentação** | ✅ Aprovado. ADR 0016 documenta as 7 decisões arquiteturais com racional completo, incluindo o recorte consciente de escopo da Timeline (item 24) registrado de forma transparente, não escondido. |
| **Regressões** | ✅ Nenhuma. Todos os campos/componentes de Dashboard das Sprints 6-12 revalidados sem alteração de forma; Central de Eventos sem os filtros novos continua se comportando exatamente como antes (todos os novos campos de `FiltroEventos` são opcionais). |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — camada operacional funcional, nenhuma interface consultando Providers
diretamente, Dashboard aditivo sem redesign, Timeline Operacional reaproveitando a Central de
Eventos, ADR 0016 criada, sem regressão.
