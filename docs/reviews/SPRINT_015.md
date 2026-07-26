# Relatório — Sprint 15 (Integração Intelbras: Prova Definitiva da Arquitetura)

**Data de conclusão**: 2026-07-25

## Resumo executivo

Sprint com objetivo invertido das anteriores: não integrar Intelbras por si só, mas usar essa
integração como prova de que a arquitetura das Sprints 11-14 (ADR 0014/0015/0016/0017) é
genuinamente genérica. Resultado: 9 das 10 camadas auditadas não precisaram de nenhuma alteração;
a Central de Eventos ganhou uma adição prevista (novo valor de enum + nova fonte via DI, o próprio
mecanismo de extensão já desenhado); e **um achado real de arquitetura foi descoberto durante a
validação** — `EquipamentoFonteEventos` nunca escopava sua query base por `Fabricante`, o que
funcionava por coincidência enquanto Control iD era o único fabricante usando `EventoEquipamento`.
A chegada de um segundo fabricante reaproveitando a mesma tabela genérica (Intelbras, por design)
expôs a lacuna (eventos duplicados/mal-rotulados). Corrigido de forma genérica — beneficia
qualquer fabricante futuro que reaproveite essa tabela, documentado na ADR 0018, nunca uma
adaptação específica de Intelbras.

## Relatório da Auditoria (Fase 0)

Ver ADR 0018 para a tabela completa. Nenhuma correção de ADR foi necessária como resultado da
auditoria estática (todas as 10 respostas foram "nenhuma alteração" ou "adição prevista pelo
próprio mecanismo de extensão"). O achado real (Decisão 5) só emergiu na validação com dado
concreto, não na leitura de código isolada — reforça que auditoria estática e teste de
integração real são complementares, não substitutos um do outro.

## Relatório da Descoberta (Fase 1)

**Modelo escolhido**: Intelbras AMT 8000 (referência arquitetural). **Protocolo**: API HTTP local
(dial-out, ADR 0014) com vocabulário de comando de alarme (ADR 0015) — decisão consciente por não
haver, neste projeto, nenhuma referência real investigada para um protocolo TCP proprietário
Intelbras (diferente do que havia para JFL via `Integra-o-FL-main`). **Justificativa**: testar
exatamente a independência dos dois eixos (direção de conexão × vocabulário de comando) que a
arquitetura afirma já desacoplar. **Limitações conhecidas**: protocolo simulado via HTTP, mesma
pendência de "validação contra hardware real" já registrada para Control iD/JFL; sem PGM/Inibição
de zona (backlog); sem rollup de status persistido (decisão deliberada, ver ADR 0018 Decisão 3).

## Componentes reutilizados

`Equipamento` (entidade, `Fabricante.Intelbras` já existia desde Sprint 11); `EventoEquipamento`+
`IEventoEquipamentoRepositorio` (Sprint 11, já genérico); `IFonteEventos` (porta, Sprint 3/ADR
0006); `ICriptografiaSimetrica` (Sprint 11); `SnapshotOperacionalServico` (Sprint 13, zero
alteração); `OperacionalHub`/`IOperacionalEventoPublicador` (Sprint 14, zero alteração);
`DashboardServico` (zero alteração); todos os componentes mobile de Dashboard/Timeline/Central
Operacional/Saúde da Propriedade.

## Componentes criados

`Application/Intelbras/{Dtos,IIntelbrasProvider,CentralIntelbrasResponse,IIntelbrasComandoServico,
IntelbrasComandoServico}.cs`; `Infrastructure/Intelbras/{IntelbrasWireDtos,IntelbrasMapper,
IntelbrasProvider}.cs`; `Infrastructure/Eventos/IntelbrasFonteEventos.cs`;
`Api/Controllers/CentraisIntelbrasController.cs`; `backend/tools/IntelbrasSimulator/`; mobile
`screens/centraisIntelbras/{CentraisIntelbrasScreen,DetalhesCentralIntelbrasScreen}.tsx`.

## Arquivos modificados (todos aditivos ou correção, nunca reescrita)

`Application/Eventos/OrigemEvento.cs` (valor aditivo `Intelbras`); `Infrastructure/Eventos/
EquipamentoFonteEventos.cs` (correção do achado real, Decisão 5 da ADR 0018);
`Application/Equipamentos/EquipamentoServico.cs` (branch de validação Intelbras, mesmo padrão já
usado para JFL desde a Sprint 12); `Infrastructure/Identity/AuthServiceCollectionExtensions.cs`
(registro DI); mobile `api/types.ts` (tipos Intelbras — `FabricanteEquipamento` já incluía
`'Intelbras'`), `navigation/{types,RootNavigator}.tsx`, `screens/equipamentos/
EquipamentosScreen.tsx` (exclusão da lista genérica, mesmo padrão de JFL),
`screens/SelecionarPropriedadeScreen.tsx` (atalho).

## Evidências da comunicação (HTTP real, não mock em memória)

- **Cadastro**: `POST /api/properties/{id}/equipamentos` com `fabricante=Intelbras,
  ip=localhost, porta=9600, senha=1234` → 201, equipamento criado com `status=Desconhecido`.
- **Testar conexão**: `POST /api/equipamentos/{id}/intelbras/testar-conexao` → `sucesso: true`
  contra `backend/tools/IntelbrasSimulator` real (login com senha real, sessão real).
- **Consultar status inicial**: 2 partições, ambas desarmadas, sem problema ativo.
- **Armar partição 1** → `armada: true` confirmado; **Desarmar partição 1** → `armada: false`
  confirmado — estado realmente alterado pelo simulador, não um eco estático.
- **Armar partição 2** (deixada armada) → refletida no Snapshot Operacional e no Dashboard.
- **Importar eventos** → `quantidadeImportada: 2` (Disparo de zona 01 + Restauração de zona 01),
  gravados em `EventoEquipamento`.

## Evidências dos testes (Central de Eventos, Snapshot, Dashboard, SignalR)

- **Filtro `origem=Intelbras`**: 2 itens, exatamente os importados.
- **Filtro `categoria=Alarme&severidade=Critico`**: 1 item ("Disparo de zona 01", `destaque:
  true`) — mapeamento de severidade correto.
- **Filtro `fabricante=ControlId`** (após a correção): 2 itens, exatamente os do Control iD, sem
  contaminação de Intelbras — confirma a correção da Decisão 5.
- **Sem filtro nenhum**: 4 itens (2 Intelbras + 2 Control iD), cada um aparecendo **exatamente
  uma vez** — antes da correção apareciam 6 itens (Intelbras duplicado).
- **Snapshot Operacional** (`POST .../snapshot/atualizar`): `quantidadeEquipamentosOnline` subiu
  de 2 para 3 automaticamente ao cadastrar o equipamento Intelbras, sem nenhuma alteração de
  código; equipamento Intelbras aparece na lista de classificação individual com
  `estado: "Saudavel"`.
- **Dashboard**: `quantidadeEquipamentosOnline/Offline` e `saude` refletem o novo equipamento;
  nenhum campo novo foi adicionado ao `DashboardResponse`.
- **SignalR** (cliente real, `@microsoft/signalr` via Node): ao desarmar a partição 2 via API,
  o cliente conectado ao grupo da Propriedade recebeu `OperacionalAtualizado` com
  `motivo: "EquipamentoStatusAlterado"` e o Snapshot completo, incluindo o equipamento Intelbras
  — mesma infraestrutura da Sprint 14, zero alteração no Hub/Publicador.

## Regressão (Control iD/JFL)

- `GET .../informacoes` (Control iD, via `ControlIdSimulator` real): 200, dado real do simulador.
- `GET .../jfl` (detalhes JFL): 200, auto-vínculo preservado.
- `GET .../equipamentos`, `GET .../dashboard`, `GET .../operacional/snapshot`: todos 200,
  contratos inalterados.
- Filtro `fabricante=ControlId` isolado corretamente após a correção da Decisão 5 (ver acima).

## Evidências de build

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa.
- `backend/tools/IntelbrasSimulator`: build limpo, projeto fora do `AppMorador.sln` de produção
  (mesmo padrão de `ControlIdSimulator`/`JflSimulator`).
- `npx tsc --noEmit` (mobile): limpo (precisou de `--stack-size` maior no Node 24 para não
  estourar a pilha do compilador — limitação de ambiente, não um erro de tipo real).
- `npx expo export --platform web`: bundle gerado com sucesso (2675 módulos).

## Pendências / dívida técnica

- Validação contra hardware Intelbras físico real não realizada (protocolo simulado via HTTP) —
  mesma classe de pendência já registrada para Control iD (item 20) e JFL (item 22).
- PGM e Inibição de Zona não implementados para Intelbras (marcados "se suportado" pela missão).
- `EquipamentoIntegracaoServico.ResolverProvider` continua hardcoded a `IControlIdProvider?` — não
  exercitado por esta Sprint (Intelbras usa serviço próprio), mas registrado para quando um
  segundo fabricante quiser reaproveitar o fluxo de sincronização de Moradores/Credenciais.
- Sem rollup de status persistido para Intelbras (decisão deliberada, não uma lacuna).

## Atualizações de documentação

`docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/DIVIDA_TECNICA.md` (novos itens),
`docs/adr/0018-prova-extensibilidade-arquitetura.md` + índice atualizado,
`docs/sprints/SPRINT_015.md`, este relatório. Nenhuma migration nesta Sprint.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. 9 das 10 camadas auditadas confirmaram "nenhuma alteração" real. O único achado (`EquipamentoFonteEventos`) foi corrigido genericamente, nunca mascarado por uma adaptação específica de Intelbras — exatamente o critério de aceite mais rigoroso da missão. `IntelbrasComandoServico`/`CentraisIntelbrasController` são paralelos e independentes, provando que fabricantes não competem por um serviço compartilhado. |
| **Segurança** | ✅ Aprovado. Senha cifrada em repouso via `ICriptografiaSimetrica` já existente — nenhuma exceção nova. Ownership validado em toda ação (mesmo padrão de Control iD/JFL). |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (cadastrar → testar conexão → armar → desarmar → eventos → Snapshot → Dashboard → Timeline → Mobile → SignalR) homologado com dado real do simulador. Nada de "Fora de Escopo" foi implementado. |
| **UX/UI** | ✅ Aprovado. Telas mobile seguem exatamente o Design System e a estrutura já usada por Centrais JFL — nenhuma inconsistência visual entre os 3 fabricantes. |
| **Performance** | ✅ Aprovado. Nenhuma consulta ao vivo disparada implicitamente pelo Dashboard/Snapshot; debounce/broadcast por grupo da Sprint 14 continuam funcionando sem alteração. |
| **Manutenibilidade** | ✅ Aprovado. A correção da Decisão 5 melhora a manutenibilidade geral do projeto (não só desta Sprint) — `EquipamentoFonteEventos` agora se auto-escopa corretamente, um bug latente que teria afetado qualquer fabricante futuro reaproveitando `EventoEquipamento`. |
| **Documentação** | ✅ Aprovado. ADR 0018 documenta a auditoria completa, a descoberta, as 5 decisões (incluindo o achado real com causa raiz e correção), lições aprendidas e recomendações para as próximas integrações. |
| **Regressões** | ✅ Nenhuma, após a correção da Decisão 5 (validada explicitamente: filtro `fabricante=ControlId` isolado corretamente, sem contaminação Intelbras). Control iD e JFL revalidados via requests reais contra seus simuladores. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. A arquitetura das Sprints 11-14 se
provou genuinamente extensível — um terceiro fabricante foi integrado tocando apenas 4 arquivos
fora do seu próprio módulo (1 enum aditivo, 1 correção genérica de bug latente, 1 branch de
validação já precedentada, 1 registro de DI), nenhum deles uma adaptação específica de Intelbras.
ADR 0018 criada, critérios de aceite atendidos integralmente.
