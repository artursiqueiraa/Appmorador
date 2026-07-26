# ADR 0016 — Camada Operacional Unificada

**Data**: 2026-07-25

## Contexto

Depois das Sprints 11 (Control iD) e 12 (JFL Active 100 Bus), o AppMorador tinha duas
integrações reais seguindo a arquitetura oficial de Providers (ADR 0014/0015), cada uma
alimentando o Dashboard com seus próprios campos calculados diretamente dentro de
`DashboardServico.GetAsync` (ex.: `QuantidadeEquipamentosOnline`/`Offline` computados por uma
consulta; `QuantidadeCentraisJflOnline`/`QuantidadeParticoesArmadas` computados por outra,
específica de JFL). Cada integração nova replicava essa lógica de agregação ad-hoc dentro do
Dashboard, sem um ponto único de consolidação — e nenhuma noção formal de "saúde" além do
`PontuacaoSaude` (0-100) já existente desde a Sprint 2, que não considerava as integrações novas.

A Sprint 13 pediu a consolidação dessas informações numa única camada operacional reutilizável
por Dashboard, Mobile, Web e APIs futuras — sem integrar nenhum fabricante novo, sem alterar
regras de negócio já homologadas.

## Decisão 1 — Fluxo obrigatório: Estado Bruto → Classificador Operacional → Snapshot Operacional

```
Equipamento.Status / StatusCentralJfl (já persistidos pelas Sprints 11/12)
        │
        ▼
EstadoBrutoEquipamento (Application/Operacional) — DTO interno, nunca exposto
        │
        ▼
IClassificadorOperacionalServico — único ponto que decide Saudável/Atenção/Crítico/Offline
        │
        ▼
SnapshotOperacional (Domain, persistido 1:1 por Propriedade)
        │
        ▼
Dashboard / Mobile / APIs futuras
```

Nenhum Provider (`IControlIdProvider`/`IJflProvider`) é consumido por esta camada — o Estado
Bruto é lido exclusivamente de dados que as Sprints 11/12 já persistem quando o usuário executa
uma ação explícita (testar conexão, sincronizar, armar/desarmar, consultar status). Isso significa
que gerar um Snapshot Operacional nunca tem custo de rede/latência de hardware — é pura agregação
de banco, o que permite gerá-lo livremente numa leitura (ver Decisão 4) sem violar "evitar
consultas repetidas aos Providers" (a Sprint nunca as consulta, ponto).

## Decisão 2 — Classificador Operacional: regras centralizadas, nunca duplicadas

`IClassificadorOperacionalServico` (Application/Operacional) é o único lugar do sistema que sabe
o que cada estado significa:

**Por equipamento** (`ClassificarEquipamento`):
| Situação | Estado |
|---|---|
| `Status == Offline` (tentativa de comunicação falhou) | Offline |
| `Status == Desconhecido` (nunca testado) | Atenção |
| `Status == Online` e com um problema ativo (`StatusCentralJfl.TemProblemaAtivo`) | Crítico |
| `Status == Online` e sem problema ativo | Saudável |

**Da Propriedade inteira** (`ClassificarPropriedade`), em ordem de prioridade:
1. Nenhum equipamento cadastrado → **Saudável** (nada a reportar; não alarmar por uma integração
   que o usuário simplesmente não configurou ainda — mesmo espírito da "instalação vazia" do
   Dashboard desde a Sprint 6).
2. Qualquer alarme/problema ativo (`QuantidadeAlarmesAtivos > 0`) → **Crítico** — tem prioridade
   sobre tudo, mesmo com a maioria dos equipamentos online.
3. Nenhum equipamento online → **Offline**.
4. Ao menos um, mas não todos, online → **Atenção**.
5. Todos online, sem alarme → **Saudável**.

## Decisão 3 — Snapshot Operacional é um rollup 1:1 por Propriedade (upsert), não um histórico

Mesma decisão de design já usada por `StatusCentralJfl` (Sprint 12): `SnapshotOperacional` guarda
só o último estado conhecido (substituído a cada geração), nunca uma linha por geração. Um
histórico auditável de snapshots ao longo do tempo não foi pedido pela missão e adicionaria
complexidade sem necessidade concreta hoje (mesma régua de simplicidade já aplicada desde o MVP).

## Decisão 4 — Geração sob demanda (bootstrap na leitura), atualização manual explícita

`ISnapshotOperacionalServico.ObterAsync` gera um Snapshot na hora se a Propriedade ainda não tiver
nenhum (primeira consulta) — como a geração nunca chama um Provider, isso não tem custo/risco.
`AtualizarAsync` sempre recalcula (ação explícita do usuário, botão "Atualizar" no mobile). Nenhum
job/scheduler/polling gera Snapshots automaticamente — mesma regra permanente de simplicidade em
MVPs já estabelecida desde a Fase 0.

A classificação por equipamento (`SnapshotOperacionalResponse.Equipamentos`, usada pela tela
"Saúde da Propriedade") é sempre recalculada na leitura, mesmo quando o rollup numérico vem do
cache — é agregação em memória sobre dado já carregado, custo desprezível, e mantém o
detalhamento sempre atual mesmo quando os contadores agregados são do último "Atualizar" explícito.

## Decisão 5 — Timeline Operacional reaproveita a Central de Eventos, nunca um domínio novo

A missão pediu uma "Timeline Operacional" com eventos de múltiplas origens (Control iD, JFL)
ordenados cronologicamente — exatamente o que a Central de Eventos já faz desde a Sprint 3 (ADR
0006), agora com duas fontes reais desde a Sprint 11. Em vez de criar uma estrutura paralela
(proibido explicitamente pela missão), a Timeline Operacional **é** a Central de Eventos,
exposta através de `EventosController` (inalterado) — a Camada Operacional nunca duplica essa
consulta.

**Recorte consciente de escopo**: os exemplos de timeline na missão incluíam eventos de transição
de conectividade ("Equipamento Offline"/"Equipamento Reconectado") que não são hoje uma linha de
`Ocorrencia`/`EventoEquipamento` — são só uma mudança de `Equipamento.Status`, nunca registrada
como evento de auditoria. Adicionar isso exigiria uma nova fonte de eventos (ou uma tabela de
transições), o que começa a se parecer com o "novo domínio de eventos" que a missão proibiu
explicitamente. Registrado como dívida técnica (`DIVIDA_TECNICA.md` item 24), não implementado
silenciosamente.

## Decisão 6 — Central de Eventos ganha filtros novos, cada fonte decide o que fazer com eles

`FiltroEventos` ganhou `EquipamentoId`, `Fabricante`, `Origem`, `Categoria`, `Severidade`. Mantendo
o desenho original da Sprint 3 ("cada fonte decide por conta própria como aplicar cada campo"),
cada `IFonteEventos` implementa esses filtros nos seus próprios termos:
- `JflFonteEventos` (sempre Origem=Jfl, Categoria=Alarme, Fabricante=Jfl): devolve vazio se o
  filtro pedir qualquer outro valor nesses três campos. `EquipamentoId` é resolvido via o
  auto-vínculo por Número de Série já existente desde a Sprint 12 (ADR 0015) — sem isso, não
  haveria como ligar um `Equipamento` (Fabricante=Jfl) a uma `Ocorrencia` (que só conhece
  `Central`). `Severidade` é traduzida para a condição SQL equivalente (`Critico` ⇔
  `StatusResolucao.Resolvido`; nunca `Informativo`).
- `EquipamentoFonteEventos` (sempre Origem=ControlId, Categoria=Acesso): mesmo princípio,
  `EquipamentoId`/`Fabricante` filtram direto pela FK/join já existente; `Severidade` traduzida
  para a mesma condição de texto ("negado") já usada no mapeamento, agora também na consulta SQL.

`OrigemEvento`, antes documentado como "nunca exposto na Api", passou a ser aceito como filtro de
consulta (não no corpo da resposta) — o usuário pode legitimamente querer separar eventos de
alarme de eventos de controle de acesso.

## Decisão 7 — Dashboard consome o Snapshot para os campos consolidados, sem remover nada existente

Por instrução explícita da missão ("reutilizar todos os componentes existentes, nenhum
redesign"), os campos específicos de fabricante já existentes (`QuantidadeEquipamentosOnline`/
`Offline` da Sprint 11, `QuantidadeCentraisJflOnline`/`QuantidadeParticoesArmadas`/etc. da Sprint
12) permanecem no `DashboardResponse` exatamente como estavam — os componentes mobile que os
consomem (`CardEquipamentos`, `CardCentraisJfl`) não mudam. A única refatoração real:
`QuantidadeEquipamentosOnline`/`Offline` (que já representavam exatamente o mesmo conceito do
Snapshot) passaram a vir do Snapshot em vez de uma consulta separada, eliminando o cálculo
duplicado. Os campos novos (`Saude`, `QuantidadeEventosHoje`, `QuantidadeAlarmesAtivos`,
`UltimaAtualizacaoOperacionalUtc`) são aditivos, exibidos por um componente novo
(`CardSnapshotOperacional`), nunca substituindo um card existente.

## Impactos

`Domain/Entities/{EstadoOperacional,SnapshotOperacional}.cs`;
`Domain/Repositories/ISnapshotOperacionalRepositorio.cs`;
`Infrastructure/Persistence/{SnapshotOperacionalRepositorio,AppDbContext}.cs`;
`Application/Operacional/*.cs` (DTOs, `IClassificadorOperacionalServico`,
`ISnapshotOperacionalServico`); `Application/Eventos/{FiltroEventos,OrigemEvento}.cs`;
`Infrastructure/Eventos/{JflFonteEventos,EquipamentoFonteEventos}.cs` (filtros novos);
`Api/Controllers/{OperacionalController,EventosController}.cs`;
`Application/Dashboard/{DashboardResponse,DashboardServico}.cs`.

## Como revisar futuramente

Ao integrar um fabricante novo (Intelbras, Hikvision, Dahua), nenhuma mudança é necessária nesta
camada — `EstadoBrutoEquipamento` já é agnóstico de fabricante (`Equipamento.Fabricante`/`Status`
já cobrem qualquer Provider novo que siga a ADR 0014/0015). Se um fabricante novo tiver seu
próprio conceito de "problema ativo" (como `StatusCentralJfl.TemProblemaAtivo` para JFL), seguir o
mesmo padrão: uma entidade de rollup própria do fabricante, consultada por
`SnapshotOperacionalServico` sem que o Classificador precise conhecer o fabricante por trás.
