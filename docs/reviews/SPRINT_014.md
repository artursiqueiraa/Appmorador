# Relatório — Sprint 14 (Tempo Real — SignalR)

**Data de conclusão**: 2026-07-25

## Resumo executivo

Implementada a camada de comunicação em tempo real do AppMorador sobre a Camada Operacional já
existente (Sprint 13, ADR 0016): toda mutação real de `Equipamento.Status`/`StatusCentralJfl`/
`Ocorrencia` (Control iD, comandos JFL, e o pipeline de eventos assíncrono do JFL) agora dispara,
depois de persistida, uma regeneração do Snapshot Operacional seguida de uma publicação via
SignalR para o grupo da Propriedade correspondente — sem que nenhum Provider, Controller ou
serviço de domínio conheça SignalR diretamente (porta `IOperacionalEventoPublicador`, único ponto
de acoplamento). Dashboard e as 3 telas mobile que já consumiam o Snapshot (Central Operacional,
Saúde da Propriedade, Central de Eventos) passam a atualizar automaticamente, com o botão
"Atualizar"/pull-to-refresh de sempre preservado como fallback independente. Grupos são só por
Propriedade (ownership real) — a missão pedia também grupos por Perfil de usuário, mas o domínio
não tem RBAC (dívida técnica item 6); decisão confirmada com o usuário na Fase 1 foi não simular
isso, registrado como novo item de dívida técnica.

## Relatório de descoberta (Fase 1)

Ver ADR 0017 para o texto completo. Achados principais: `MonitoramentoService` não existe com
esse nome (papel cumprido por `SnapshotOperacionalServico`); "Sprint 13A/13B" citadas na missão
não existem (só Sprint 13); RBAC/Perfil não existe no domínio; o único gatilho verdadeiramente
assíncrono (fora de uma requisição HTTP) é `AlarmEventProcessor`.

## Arquitetura

Fluxo obrigatório (ADR 0017): `Provider → mutação já existente → SnapshotOperacionalServico.
RegenerarEPublicarAsync → IOperacionalEventoPublicador → OperacionalHubPublicador → SignalR →
Dashboard/Mobile`. `OperacionalHub`/`OperacionalHubPublicador` vivem em `Api/Realtime/` (não em
Infrastructure) pelo mesmo motivo dos Controllers — SignalR exige o modelo de hospedagem Web. O
Hub nunca consulta Equipamento/Snapshot/Ocorrencia — a única "consulta ao banco" é a checagem de
posse (`Propriedade.ProprietarioId`) antes de deixar uma conexão entrar num grupo, idêntica à de
todo Controller.

## Fluxos homologados (via requests reais e um cliente SignalR real)

- **Conexão + autorização**: cliente `@microsoft/signalr` conectado com JWT via querystring
  (`?access_token=`), `EntrarNaPropriedade` aceito para uma Propriedade própria.
- **Isolamento/autorização negativa**: `EntrarNaPropriedade` para uma Propriedade inexistente
  (`00000000-0000-0000-0000-000000000000`) rejeitado com `HubException: Propriedade não
  encontrada.` — confirmado que a conexão não entra no grupo.
- **Equipamento JFL — testar conexão real** (via `backend/tools/JflSimulator` conectado por TCP
  real): `POST /api/equipamentos/{id}/jfl/testar-conexao` → equipamento fica `Online` → snapshot
  recalculado → cliente conectado recebe `OperacionalAtualizado` com
  `motivo: "EquipamentoStatusAlterado"` e o Snapshot completo (equipamentos/saude/contadores)
  em menos de 1 segundo após a chamada REST responder.
- **Atualização manual** (`POST .../operacional/snapshot/atualizar`): cliente conectado recebe
  `OperacionalAtualizado` com `motivo: "SnapshotAtualizadoManualmente"` — outros
  dispositivos/abas do mesmo dono também são notificados, não só quem clicou.
- **Bug real encontrado e corrigido durante a validação**: o primeiro teste mostrou os enums
  (`saude`, `fabricante`, `estado`) chegando como número cru (`1`, `0`, `3`) no payload do
  SignalR, quebrando a consistência do contrato de enums-como-texto (ADR 0005) — o protocolo JSON
  do Hub tem configuração de serialização própria, independente de
  `AddControllers().AddJsonOptions()`. Corrigido com
  `AddSignalR().AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new
  JsonStringEnumConverter()))`; reconfirmado com o mesmo cliente real que os enums agora chegam
  como texto (`"Atencao"`, `"Jfl"`, `"Saudavel"`).
- **Regressão**: `GET equipamentos`, `GET dashboard`, `GET .../jfl` (detalhes), `GET
  .../informacoes` (Control iD), `GET .../eventos`, `GET .../operacional/snapshot` — todos 200,
  contratos inalterados.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (porta + `Regenerar
  EPublicarAsync`, Hub/Publicador + wiring, integração nos 3 pontos de mutação, correção do
  enum).
- Validação end-to-end real: backend rodando (`dotnet run`), `backend/tools/JflSimulator`
  conectado via TCP real, e um cliente SignalR real (Node.js + `@microsoft/signalr`, a mesma
  biblioteca depois usada no mobile) — não uma chamada em memória do Hub.
- `npx tsc --noEmit` (mobile): 0 erros após `RealtimeContext` e a integração nas 4 telas.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2673 módulos, sem erro).

## Pendências

- Item 25 (`DIVIDA_TECNICA.md`, novo): grupos SignalR por Perfil de usuário — depende de uma
  futura Sprint de RBAC (domínio não tem Perfil/Papel hoje).
- Item 26 (`DIVIDA_TECNICA.md`, novo): a publicação disparada por `AlarmEventProcessor` (o único
  gatilho verdadeiramente assíncrono) foi validada por revisão de código + reaproveitamento do
  mesmo mecanismo já comprovado ao vivo (idêntica chamada a `RegenerarEPublicarAsync`/
  `PublicarNovoEventoAsync`), mas **não** foi disparada de ponta a ponta via um evento Contact ID
  real enviado por TCP neste ambiente — isso exigiria uma `Central` (pipeline de eventos, Fase 1)
  cadastrada com o mesmo Número de Série do Equipamento de teste, e `Central` não tem CRUD via
  API (dívida técnica item 23) nem havia cliente MySQL disponível neste ambiente para inserir uma
  diretamente.
- Itens 20/22 (já existentes): validação contra hardware Control iD/JFL físico real continua
  pendente — inalterado por esta Sprint.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 14 concluída),
`docs/DIVIDA_TECNICA.md` (itens 25 e 26), `docs/adr/0017-comunicacao-operacional-tempo-real.md` +
índice atualizado, `docs/sprints/SPRINT_014.md` (especificação), este relatório. Nenhuma migration
nesta Sprint (nenhuma mudança de schema — Sprint de infraestrutura de transporte, não de domínio),
então `docs/ALTERACOES_BANCO.md` não recebeu entrada nova.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. `IOperacionalEventoPublicador` isola completamente o domínio de SignalR — confirmado que nenhum tipo `Microsoft.AspNetCore.SignalR` aparece em `Domain`/`Application`. Hub/Publicador em `Api/Realtime/` é uma decisão consciente e documentada (ADR 0017), coerente com onde os Controllers já vivem. Fluxo Provider→Snapshot→Publicador→SignalR respeitado nos 3 pontos de mutação sem exceção. |
| **Segurança** | ✅ Aprovado. Autorização por posse validada com teste negativo real (conexão sem posse rejeitada com `HubException`). JWT via querystring restrito estritamente às rotas `/hubs` (demais rotas continuam exigindo o header Bearer). Rate limiting aplicado ao negotiate/conexão. |
| **Produto** | ✅ Aprovado. Dashboard/Central Operacional/Saúde da Propriedade/Central de Eventos atualizam automaticamente, validado com um cliente real recebendo o payload em tempo real após uma ação real (teste de conexão JFL). Refresh manual preservado como fallback, nunca removido. Nada de "Fora de Escopo" foi implementado. |
| **UX/UI** | ✅ Aprovado. Reconexão automática (`withAutomaticReconnect`) sem exigir nenhuma ação do usuário; ao reconectar, o cliente reentra no grupo automaticamente. Nenhuma tela trava ou exige a conexão em tempo real para funcionar — é estritamente um complemento ao fluxo já existente. |
| **Performance** | ✅ Aprovado. Broadcast sempre por grupo (nunca `Clients.All`). Debounce simples em memória evita publicações duplicadas em sequência rápida (ex.: inibir zona) sem introduzir fila/worker. Geração do Snapshot continua sem custo de Provider (herdado da Sprint 13) mesmo sendo chamada mais vezes agora. |
| **Manutenibilidade** | ✅ Aprovado. Um único ponto (`RegenerarEPublicarAsync`) concentra a lógica de "regenerar e publicar" — os 3 pontos de mutação só chamam esse método, sem duplicar lógica de classificação/serialização. `OperacionalHubPublicador` isola inteiramente os detalhes de SignalR. |
| **Documentação** | ✅ Aprovado. ADR 0017 documenta as 9 decisões com racional completo, incluindo as duas divergências de Fase 1 (nome de serviço inexistente, RBAC inexistente) e o bug de serialização de enum encontrado e corrigido. |
| **Regressões** | ✅ Nenhuma. Endpoints de Control iD, JFL, Dashboard, Eventos e Snapshot Operacional revalidados via requests reais após toda a implementação — todos 200, contratos inalterados. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — SignalR funcional e validado com um cliente real, grupos por
Propriedade com isolamento comprovado, JWT/reconexão automática funcionando, sem regressões,
ADR 0017 criada. Duas dívidas técnicas novas registradas de forma transparente (grupos por
perfil dependem de RBAC futuro; disparo assíncrono do alarme validado por código+mecanismo
compartilhado, não por um evento TCP real de ponta a ponta neste ambiente).
