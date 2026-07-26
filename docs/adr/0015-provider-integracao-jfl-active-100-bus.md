# ADR 0015 — Migração JFL Active 100 Bus: Provider para protocolo de conexão invertida

**Data**: 2026-07-22

## Contexto

A Sprint 12 pediu a migração da integração de comandos JFL Active 100 Bus (Armar, Desarmar,
Armar Stay/Away, Acionar/Desligar PGM, Inibir/Desinibir Zona, Consultar Status) da referência
`Integra-o-FL` (projeto "CentralHub") para a arquitetura oficial de integrações do AppMorador,
estabelecida na Sprint 11 (ADR 0014).

A Fase 1 (Descoberta obrigatória) encontrou dois achados que moldaram esta decisão:

1. **O AppMorador já tinha uma migração parcial deste mesmo SDK**, feita antes da Sprint 11
   (Fase 1/1.1 do projeto, ver `docs/fase1-confiabilidade-relatorio.md`): `AppMorador.Jfl`
   já portava handshake (`0x21`/`0x2A`), keep-alive (`0x40`) e recebimento de eventos (`0x24`),
   com os comandos de superusuário (status/armar/PGM/zonas) deliberadamente removidos na época
   ("não fazem parte do objetivo desta fase"). A Sprint 12 completa essa migração já iniciada,
   em vez de começar do zero.
2. **Achado crítico sobre a alegação de "comportamento já homologado"**: a missão descreveu a
   referência como tendo comunicação homologada com central real para Armar/Desarmar/PGM/Inibição
   de zonas. A investigação encontrou evidência real de hardware físico para handshake/keep-alive/
   status (fingerprint de painel real documentado, logs de conexão capturados, relato de
   troubleshooting de campo genuíno), mas os próprios documentos da referência marcam
   Armar/Desarmar/PGM/Zonas explicitamente como **"validado via simulador, hardware real
   pendente"** — e o recebimento de eventos (`0x24`) na referência é só um stub sem ACK (bug real
   que causaria reconexões em cascata). Conclusão: tratar a referência como fonte de protocolo
   confiável para os bytes/formato de comando, não como "comportamento a preservar
   byte-a-byte" para os comandos de superusuário especificamente.

## Decisão 1 — O Provider JFL não disca para o equipamento; localiza uma sessão já aberta

Diferente do Control iD (ADR 0014, onde o AppMorador é cliente HTTP e disca para o
equipamento), o protocolo JFL Active 100 Bus é **invertido**: a central é sempre quem abre e
mantém a conexão TCP com o AppMorador (`AppMorador.Jfl.Server.JflTcpServer`, existente desde a
Fase 1). Por isso `IJflProvider` (Application/Jfl) não tem um método "conectar" — cada método
recebe o `NumeroSerie` da central, localiza a sessão TCP já registrada em
`SessionManager.TryGet` (Sprint 11 já não existia; esse mecanismo já era usado só para eventos,
agora reaproveitado para comandos), e envia o comando dentro dela via
`JflSession.SendAndWaitAsync` — um mecanismo de correlação por SEQ que já existia como
scaffolding dormente desde a Fase 1 (comentário no próprio código: "nesta base não há nenhum
comando iniciado pelo servidor ainda... mantido para compatibilidade futura") e foi ativado nesta
Sprint.

Isso significa que **o padrão da ADR 0014 se generaliza**: `IControlIdProvider`/`IJflProvider`
são ambos "o único ponto que conhece o protocolo do fabricante", mas a direção da conexão
(quem disca para quem) é um detalhe de implementação interno ao Provider — nunca vaza para
`Application/Equipamentos` ou para os Controllers.

## Decisão 2 — Comandos JFL reaproveitam a infraestrutura de protocolo já existente, adicionando só o que faltava

Portado de `Integra-o-FL/SDK/CentralHub.SDK/Jfl/` para `AppMorador.Jfl`:
- `JflCommand`: adicionados os bytes `Status (0x4D)`, `Armar (0x4E)`, `Desarmar (0x4F)`,
  `AcionarPgm (0x50)`, `DesacionarPgm (0x51)`, `InibirZonas (0x52)`, `ArmarStay (0x53)`,
  `ArmarAway (0x54)` — antes deliberadamente ausentes.
- `Messages/Status/*`: `CentralStatusResponse` e sub-parsers (`PartitionStatus`, `ZoneStatus`,
  `PgmStatus`, `ElectrifierStatus`, `BatteryStatus`, `ProblemFlags`) — o formato de resposta
  "tela monitorar" (seção 4.10 do manual), compartilhado por todos os comandos de superusuário.
- `Server/{ArmCommandService,PgmCommandService,ZoneInhibitCommandService,
  CentralStatusQueryService}`: cada um localiza a sessão via `SessionManager` e usa
  `SendAndWaitAsync` — nenhum disca para fora.
- `JflSession.SendAndWaitAsync`: ativado (gera SEQ via `NextSeq()`, registra um
  `TaskCompletionSource` correlacionado, envia, aguarda com timeout) — o mecanismo de
  correlação (`TryCompletePendingRequest`) já estava plugado no loop de leitura do
  `JflTcpServer` desde a Fase 1, só nunca era populado.

**Nada do pipeline de eventos já existente foi tocado**: `EventoCommandHandler`,
`AlarmEventProcessor`, `Central`, `Zona`, `Ocorrencia`, `ContactIdCatalog` e `JflFonteEventos`
continuam exatamente como estavam — a Sprint 12 é aditiva sobre a infraestrutura de protocolo,
nunca uma reescrita do que já funcionava.

## Decisão 3 — Equipamento (Sprint 11) reaproveitado com campos opcionais, nunca uma entidade nova de JFL

Conforme pedido pela missão, nenhuma entidade específica de JFL foi criada — uma central JFL é
cadastrada como `Equipamento` (Fabricante=Jfl). Como o protocolo é invertido (a central disca
para o AppMorador, nunca o contrário), `Equipamento.Ip`/`Porta`/`Usuario`/`SenhaCriptografada`
não fazem sentido para este fabricante — por isso os quatro campos passaram de obrigatórios
(Sprint 11, só Control iD existia) para **opcionais** no Domain, com a obrigatoriedade real
validada em `EquipamentoServico` condicionalmente por `Fabricante` (Control iD: todos
obrigatórios; JFL: só `Identificador`, o Número de Série, que é a chave de correlação com a
sessão TCP). Esta é uma migração de schema aditiva e não-destrutiva (relaxar NOT NULL → nullable
nunca afeta linhas existentes).

## Decisão 4 — Central (Fase 1) e Equipamento (Sprint 11) coexistem; vínculo automático por Número de Série

O AppMorador já tinha uma entidade `Central` (Fase 1, alvo de `Ocorrencia.CentralId`/
`Zona.CentralId` no pipeline de eventos) antes desta Sprint. Cadastrar a central também como
`Equipamento` (Fabricante=Jfl) criaria, em tese, um cadastro duplicado da mesma central física.

Confirmado com o usuário (Fase 1, pergunta de esclarecimento): em vez de unificar as duas
entidades (mudança de schema invasiva no pipeline de eventos já em produção, fora do escopo desta
Sprint) ou exigir dois cadastros manuais sem nenhuma ligação entre eles, a tela "Detalhes da
Central" busca automaticamente uma `Central` já existente com o mesmo Número de Série na mesma
Propriedade e exibe o vínculo (nome da Central de eventos) como informação somente leitura — sem
alterar `Central`/`Ocorrencia`/`Zona` de forma alguma. `ICentralRepositorio` foi criado
minimamente só para esta consulta (Central continua sem CRUD via API — cadastro de Central
permanece fora do escopo do produto até hoje, ver dívida técnica).

## Decisão 5 — Rollup de status (`StatusCentralJfl`) é uma entidade nova, separada de Equipamento

Partições armadas/desarmadas e problemas ativos são conceitos exclusivos de alarme — não
generalizam para Control iD ou qualquer fabricante futuro de controle de acesso. Por isso viram
uma entidade nova e específica, `StatusCentralJfl` (1:1 com `Equipamento`, sem soft delete — é um
snapshot substituível, não um registro de auditoria), em vez de campos genéricos em `Equipamento`
— o que violaria a regra da ADR 0014 de que nenhum fabricante altera a entidade compartilhada.
Atualizado só por ação explícita do usuário (qualquer comando de superusuário devolve o status
completo) — nunca por polling automático.

## Decisão 6 — Inibição de zona é uma composição de leitura + substituição, montada na orquestração

O comando `0x52` substitui o conjunto inteiro de zonas inibidas (não soma) — confirmado pela
documentação do protocolo. `InibirZonaAsync`/`DesinibirZonaAsync` (zona única, a ação que a tela
mobile expõe) primeiro consultam o status atual para saber quais zonas já estão inibidas, somam ou
removem a zona pedida, e reenviam o conjunto completo — exatamente como a documentação da
referência já recomendava ("essa lógica fica no Backend, que já tem acesso à consulta de status").

## Decisão 7 — Validação contra um simulador TCP simplificado, escrito do zero (não o Simulator do legado)

Confirmado com o usuário (Fase 1): sem uma central Active 100 Bus real disponível neste ambiente,
construir um simulador simplificado (`backend/tools/JflSimulator`, projeto console descartável,
fora do `AppMorador.sln` de produção) em vez de portar o `CentralHub.Simulator` completo do
repositório legado. O simulador mantém estado em memória (partições/PGMs/zonas inibidas) para que
Armar/Desarmar/PGM/Inibir Zona realmente alterem o próximo status consultado — validado via TCP
real (`TcpClient` conectando de verdade na porta 8085), não uma chamada em memória. Pendência de
validação contra hardware físico real registrada explicitamente em `docs/DIVIDA_TECNICA.md`.

## Impactos

`Jfl/Protocol/JflCommand.cs`; `Jfl/Messages/{JflBcd,Status/*}.cs`; `Jfl/Server/JflSession.cs`
(`SendAndWaitAsync`); `Jfl/Server/{ArmCommandService,PgmCommandService,
ZoneInhibitCommandService,CentralStatusQueryService}.cs`; `Jfl/JflServiceCollectionExtensions.cs`;
`Domain/Entities/{Equipamento (Ip/Porta/Usuario/SenhaCriptografada opcionais),
StatusCentralJfl}.cs`; `Domain/Repositories/{IStatusCentralJflRepositorio,ICentralRepositorio}.cs`;
`Infrastructure/Persistence/{StatusCentralJflRepositorio,CentralRepositorio,AppDbContext}.cs`;
`Application/Equipamentos/{Dtos,EquipamentoServico}.cs` (validação condicional por fabricante);
`Application/Jfl/*.cs` (porta + DTOs + orquestração); `Infrastructure/Jfl/{JflProvider,
JflStatusMapper}.cs`; `Api/Controllers/CentraisJflController.cs`;
`Application/Dashboard/{DashboardResponse,DashboardServico}.cs`;
`backend/tools/JflSimulator/` (ferramenta de teste, fora do domínio).

## Como revisar futuramente

Ao implementar Intelbras/Hikvision/Dahua (protocolos que discam para o equipamento, como Control
iD), seguir a ADR 0014 diretamente. Ao implementar um fabricante que também inverte a conexão
(central disca para o AppMorador), seguir esta ADR 0015 como referência: `SessionManager`-style
lookup por identificador, nunca discagem de saída, e um `SendAndWaitAsync`-style de correlação se
o protocolo precisar de comandos servidor-inicia-pergunta.
