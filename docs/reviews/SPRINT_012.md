# Relatório — Sprint 12 (Migração da Integração JFL Active 100 Bus)

**Data de conclusão**: 2026-07-22

## Resumo executivo

Migrados os comandos de superusuário JFL Active 100 Bus (Armar, Desarmar, Armar Stay/Away,
Acionar/Desligar PGM, Inibir/Desinibir Zona, Consultar Status) para a arquitetura oficial de
integrações do AppMorador (ADR 0014), completando uma migração que já havia começado antes desta
Sprint (Fase 1/1.1 do projeto já portara handshake/keep-alive/recebimento de eventos, deixando os
comandos de superusuário deliberadamente para depois). Diferente do Control iD (Sprint 11), o
protocolo JFL é invertido — a central sempre disca para o AppMorador — então `IJflProvider` nunca
abre uma conexão: localiza a sessão TCP já registrada pelo Número de Série e envia o comando
dentro dela, usando um mecanismo de correlação (`SendAndWaitAsync`) que já existia como
scaffolding dormente desde a Fase 1 e foi ativado agora. `Equipamento` (Sprint 11) foi reaproveitado
sem criar entidade específica de JFL — seus campos de conexão (Ip/Porta/Usuario/Senha) viraram
opcionais para acomodar um fabricante que não os usa. Um rollup de status (`StatusCentralJfl`)
persiste partições armadas/problemas ativos para o Dashboard. `Central`/`Ocorrencia`/`Zona` (Fase
1, pipeline de eventos) permanecem intocados, com um vínculo automático de leitura por Número de
Série exibido na tela de detalhes. Validado via um simulador TCP simplificado
(`backend/tools/JflSimulator`) que mantém estado em memória — Armar/Desarmar/PGM/Inibir Zona
realmente alteram o próximo status consultado, tudo via TCP real.

## Relatório da migração (Fase 1)

**Componentes reutilizados** (migração direta, adaptados ao namespace `AppMorador.Jfl`): formato
de payload da resposta "tela monitorar" (seção 4.10) e seus sub-parsers (`PartitionStatus`,
`ZoneStatus`, `PgmStatus`, `ElectrifierStatus`, `BatteryStatus`, `ProblemFlags`); bytes de comando
(`0x4D`/`0x4E`/`0x4F`/`0x50`/`0x51`/`0x52`/`0x53`/`0x54`); convenção de bitmap MSB-first do
comando de inibir zonas (documentada como oposta à convenção LSB-first do campo P-INIB da
resposta de status — uma armadilha sutil preservada como estava); padrão `SendAndWaitAsync`
(SEQ próprio + `TaskCompletionSource` correlacionado + timeout).

**Componentes descartados** (não migrados): `OperationService`/`AdapterFactory`/`JflAdapter` do
legado (arquitetura de discagem de saída, incorreta para este protocolo, e 100% simulada por
dentro mesmo aparentando ser real); `KeepAliveService` (poller de 30s conflitante com o modelo
já event-driven do AppMorador); autorização "libera qualquer serial" do legado (o AppMorador já
tinha seu próprio `ICentralAuthorizationProvider`, inalterado).

**Evidências de comunicação real**: toda comunicação em `JflProvider` usa a sessão TCP real já
aberta pelo `JflTcpServer` (porta 8085) — validada com um simulador TCP simplificado
(`backend/tools/JflSimulator`) conectando de verdade via `TcpClient`, não uma chamada em memória.
Handshake, keep-alive, teste de conexão, consulta de status (99 zonas + 16 partições + 16 PGMs +
bateria + problemas), armar/desarmar/armar-stay, PGM on/off e inibir/desinibir zona (com
composição correta de leitura + substituição do conjunto) foram todos exercitados via `curl`
contra o backend real comunicando com o simulador real. Cenário de desconexão (simulador
encerrado) também validado — `testar-conexao` detecta corretamente "sem sessão ativa (offline)".
**Pendência explícita**: validação contra hardware Active 100 Bus físico real não foi possível
neste ambiente — registrada em `docs/DIVIDA_TECNICA.md`.

## Arquivos criados

**Backend**:
- `AppMorador.Jfl/Messages/JflBcd.cs`
- `AppMorador.Jfl/Messages/Status/{BatteryStatus,ElectrifierStatus,PartitionState,ZoneState,
  PgmStatus,ProblemFlags,CentralStatusResponse}.cs`
- `AppMorador.Jfl/Server/{CentralStatusQueryService,ArmCommandService,PgmCommandService,
  ZoneInhibitCommandService}.cs`
- `Domain/Entities/StatusCentralJfl.cs`
- `Domain/Repositories/{IStatusCentralJflRepositorio,ICentralRepositorio}.cs`
- `Infrastructure/Persistence/{StatusCentralJflRepositorio,CentralRepositorio}.cs`
- `Application/Jfl/{Dtos,IJflProvider,CentralJflResponse,IJflComandoServico,JflComandoServico}.cs`
- `Infrastructure/Jfl/{JflProvider,JflStatusMapper}.cs`
- `Api/Controllers/CentraisJflController.cs`
- Migration `20260722040841_MigracaoJflActive100Bus`
- `backend/tools/JflSimulator/` (ferramenta de teste descartável, fora do domínio de produção —
  simulador TCP simplificado escrito do zero)

**Mobile**:
- `screens/centraisJfl/{CentraisJflScreen,DetalhesCentralJflScreen}.tsx`
- `screens/dashboard/CardCentraisJfl.tsx`

**Documentação**:
- `docs/adr/0015-provider-integracao-jfl-active-100-bus.md`
- `docs/sprints/SPRINT_012.md`, `docs/reviews/SPRINT_012.md` (este relatório)

## Arquivos modificados

**Backend**: `AppMorador.Jfl/Protocol/JflCommand.cs` (8 comandos novos), `AppMorador.Jfl/Server/
JflSession.cs` (`SendAndWaitAsync` ativado), `AppMorador.Jfl/JflServiceCollectionExtensions.cs`
(4 serviços de comando registrados), `Domain/Entities/Equipamento.cs` (Ip/Porta/Usuario/
SenhaCriptografada opcionais), `Application/Equipamentos/{Dtos,EquipamentoServico,
EquipamentoIntegracaoServico}.cs` (validação condicional por fabricante), `Infrastructure/
Persistence/AppDbContext.cs` (DbSet + relacionamento StatusCentralJfl), `Infrastructure/Identity/
AuthServiceCollectionExtensions.cs` (DI dos repositórios/serviços/Provider novos), `Application/
Dashboard/{DashboardResponse,DashboardServico}.cs` (5 campos novos).

**Mobile**: `api/types.ts` (novos DTOs: `CentralJflResponse`, `StatusCentralJflInfo`,
`ParticaoStatusInfo`, `ZonaStatusInfo`, `PgmStatusInfo`, `ResultadoComandoJfl`,
`ResultadoTesteConexaoJfl`; `EquipamentoResponse.{ip,porta,usuario}` agora opcionais; 5 campos
novos em `DashboardResponse`), `navigation/{types,RootNavigator}.tsx` (rotas CentraisJfl/
DetalhesCentralJfl), `screens/SelecionarPropriedadeScreen.tsx` (atalho para gerenciar centrais
JFL), `screens/dashboard/DashboardScreen.tsx` (novo `CardCentraisJfl`), `screens/equipamentos/
EquipamentosScreen.tsx` (filtra Fabricante=Jfl da lista genérica — tela própria).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (itens 22 e 23), `docs/adr/README.md`.

## Arquitetura

Mesma arquitetura oficial de integrações (ADR 0014) — `Application/Jfl` (porta) e
`Infrastructure/Jfl` (Provider) — adaptada para um protocolo de conexão invertida (ver ADR 0015):
`IJflProvider` nunca disca para o equipamento, localiza a sessão TCP já aberta via
`SessionManager` (existente desde a Fase 1). `Equipamento` (ownership direto à Propriedade, soft
delete ADR 0009) reaproveitado sem alteração estrutural — só relaxamento de nullability para
acomodar um fabricante sem campos de conexão de saída. `StatusCentralJfl` segue o padrão de
rollup específico de fabricante (nunca poluindo a entidade genérica `Equipamento`).

## Fluxos homologados (via requests reais)

- Login → criar Propriedade → cadastrar Central JFL (Fabricante=Jfl, só Nome/Modelo/Número de
  série — Ip/Porta/Usuario/Senha nunca exigidos) → conectar o simulador TCP real com o mesmo
  Número de Série → **Testar conexão** → `sucesso: true`, `Equipamento.Status` → `Online`.
- **Consultar status** → resposta completa decodificada corretamente: 16 partições, 99 zonas,
  16 PGMs, bateria (Lítio 80%), eletrificador, sem problemas ativos.
- **Armar partição 1** → `armada: true` confirmado na resposta; **Desarmar partição 1** →
  `armada: false` confirmado — estado realmente flipado pelo simulador, não um eco estático.
- **Armar Stay partição 2** → `armadaStay: true` confirmado.
- **Acionar PGM 3** → `acionada: true`; **Desligar PGM 3** → `acionada: false` — confirmado.
- **Inibir zona 5, depois inibir zona 7** → ambas as zonas aparecem inibidas simultaneamente
  (confirma a composição correta de leitura + substituição do conjunto completo, nunca perdendo
  uma zona já inibida). **Desinibir zona 5** → zona 5 volta a "Fechada", zona 7 continua
  "Inibida" — confirmado.
- **Dashboard** → `quantidadeCentraisJflOnline`/`quantidadeParticoesArmadas`/
  `quantidadeParticoesDesarmadas`/`quantidadeProblemasAtivosJfl` todos com dado real, refletindo
  exatamente o estado do simulador no momento da última ação.
- **Detalhes da central** → `centralVinculadaId`/`centralVinculadaNome` corretamente `null`
  quando nenhuma `Central` (pipeline de eventos) com o mesmo Número de Série existe ainda — a
  ramificação "encontrado" foi validada por revisão de código (consulta LINQ direta por
  `PropriedadeId`+`NumeroSerie`), não por um teste ao vivo, já que `Central` não tem CRUD via API
  (fora do escopo desta Sprint) e criar uma linha de teste exigiria acesso direto ao banco não
  disponível neste ambiente.
- **Desconexão** → simulador encerrado → `testar-conexao` retorna `sucesso: false,
  "Central não possui conexão ativa (offline)"` — sem exceção, sem travar a requisição.
- Regressão: Login, Propriedades, Dashboard, Central de Eventos, Equipamentos (Control iD,
  Sprint 11) — contratos inalterados, 200 em todos; lista de Equipamentos genérica corretamente
  não mostra mais centrais JFL (filtradas para a tela própria).

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (protocolo/parsers,
  ativação de `SendAndWaitAsync` + command services, domínio + migration, Provider/mapper,
  orquestração + DI + controller, Dashboard).
- `npx tsc --noEmit` (mobile): 0 erros após todas as telas/tipos novos e o ajuste de nullability
  em `EquipamentoResponse`.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2641 módulos, sem erro).
- Migration revisada (resumo técnico: 4 `AlterColumn` de relaxamento de nullability + 1
  `CreateTable` + 1 índice único, sem operação destrutiva) antes de `dotnet ef database update`
  contra o banco real — protocolo permanente desde a Sprint 3.1.
- Validação end-to-end completa via `curl` contra o simulador TCP real rodando em processo
  separado: cadastro, teste de conexão, consulta de status, armar/desarmar/armar-stay, PGM
  on/off, inibir/desinibir zona (com verificação de estado real, não apenas resposta HTTP 200),
  Dashboard, detalhe com auto-vínculo, e o cenário de desconexão.

## Pendências

- Item 22 (`DIVIDA_TECNICA.md`): validação contra hardware Active 100 Bus físico real não foi
  possível neste ambiente — a comunicação foi validada via TCP real contra um simulador local
  simplificado, não contra a central genuína.
- Item 23 (`DIVIDA_TECNICA.md`): `Central` (pipeline de eventos, Fase 1) e `Equipamento`
  (Fabricante=Jfl, comandos, Sprint 12) permanecem cadastros separados que o usuário precisa
  manter sincronizados manualmente pelo mesmo Número de Série — o auto-vínculo é só leitura.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 12 concluída; item de
backlog do fabricante JFL para controle de acesso removido da lista de integrações pendentes —
JFL agora tem Provider real para comandos de alarme), `docs/ALTERACOES_BANCO.md` (nova migration
documentada), `docs/DIVIDA_TECNICA.md` (itens 22 e 23),
`docs/adr/0015-provider-integracao-jfl-active-100-bus.md` + índice atualizado,
`docs/sprints/SPRINT_012.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. `IJflProvider` segue rigorosamente o padrão da ADR 0014, adaptado com clareza documentada (ADR 0015) para um protocolo de conexão invertida — nenhum Controller ou serviço de domínio conhece o protocolo JFL. Reaproveitamento real e verificável da infraestrutura já existente desde a Fase 1 (`JflSession`, `SessionManager`, `JflTcpServer`, framing/checksum) — nenhuma duplicação. `StatusCentralJfl` como entidade separada evita poluir `Equipamento` com conceitos específicos de alarme. |
| **Segurança** | ✅ Aprovado. Nenhuma credencial nova introduzida (o protocolo JFL não exige senha nos comandos migrados). Ownership validado em toda ação (`ResolverEquipamentoJflAsync` confere Propriedade.ProprietarioId e Fabricante=Jfl). Nenhuma informação sensível exposta nas respostas. |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (cadastrar central → testar conexão → consultar status → armar → desarmar → PGM → inibir/desinibir zona → Dashboard) homologado via requests reais contra o simulador, com estado real mudando a cada comando. Nada de "Fora de Escopo" foi implementado (confirmado: sem IA/Analytics/WebSocket/SignalR/Push/comandos novos além dos existentes no legado). |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State, mensagens de erro/sucesso inline presentes nas 2 telas novas. Ações de armar/desarmar/PGM/zona organizadas por seção com ícones consistentes com o Design System (`Lock`/`Unlock`/`Zap`/`ZapOff`, cores `safe`/`warn`). Vínculo com a Central de eventos exibido de forma transparente (nome ou aviso de "sem vínculo"), nunca escondido. |
| **Performance** | ✅ Aprovado. Nenhuma consulta ao vivo é disparada implicitamente pelo Dashboard (usa só o rollup persistido, mesma regra já estabelecida para Equipamentos/Sprint 11). Inibição de zona faz 2 chamadas TCP (consulta + comando) apenas quando o usuário aciona a ação, nunca em loop/polling. |
| **Manutenibilidade** | ✅ Aprovado. `JflStatusMapper` é o único ponto de tradução entre o tipo de protocolo e o DTO interno; `JflComandoServico.ResolverEquipamentoJflAsync`/`ExecutarComandoAsync` centralizam ownership e persistência do rollup, evitando repetição entre os 9 métodos de ação. Portagem de código do legado manteve comentários que documentam armadilhas reais do protocolo (convenção de bits oposta em zonas), preservando conhecimento tácito. |
| **Documentação** | ✅ Aprovado. ADR 0015 documenta a adaptação do padrão para conexão invertida com o racional completo; relatório de migração (reaproveitado/descartado/evidências) documentado nesta revisão; 2 itens novos de dívida técnica registram os recortes conscientes de escopo (hardware real pendente; Central/Equipamento não unificados). |
| **Regressões** | ✅ Nenhuma. Login, Propriedades, Dashboard, Central de Eventos e Equipamentos (Control iD, Sprint 11) revalidados via requests reais após toda a implementação — resultados corretos, incluindo a lista de Equipamentos genérica agora corretamente sem as centrais JFL (que passaram a ter tela própria). |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — arquitetura de Provider generalizada para um segundo modelo de conexão,
comandos de superusuário funcionais e validados com mudança de estado real, Dashboard com dado
real, sem regressão, documentação completa.
