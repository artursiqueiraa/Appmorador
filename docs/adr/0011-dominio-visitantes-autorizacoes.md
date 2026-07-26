# ADR 0011 — Domínio de Visitantes e Autorizações: escopo do Visitante e Status híbrido

**Data**: 2026-07-21

## Contexto

A Sprint 8 pediu o domínio completo de Visitantes e Autorizações: cadastro de visitantes,
autorizações de acesso (morador responsável, unidade, visitante, período de validade, status) e
histórico interno — sem nenhuma integração com equipamento físico (Control iD, Intelbras,
Hikvision, JFL ficam para Sprint futura). Duas decisões de modelo precisavam ser tomadas antes de
qualquer código: onde o `Visitante` vive na hierarquia do domínio, e como o `Status` da
`Autorizacao` (Pendente/Ativa/Expirada/Cancelada/Utilizada) funciona sem que esta Sprint
implemente jobs/schedulers (regra permanente de simplicidade em MVPs).

## Problema 1 — Escopo do Visitante

O `ROADMAP.md` (Sprint 6) especulou anteriormente "Visitante ligado a uma Unidade, autorizado por
um Morador", mas o precedente do ADR 0010 (`PontoAcesso` pertence direto à `Propriedade`, não à
`Unidade`, por ser infraestrutura compartilhada) sugeria o oposto para `Visitante` — a mesma
pessoa (ex.: um prestador de serviço recorrente) pode visitar unidades diferentes da mesma
propriedade ao longo do tempo.

### Alternativas consideradas

- **Visitante pertence à Unidade**: mais simples, mas obriga recadastro da mesma pessoa a cada
  unidade que ela visita — sem reuso.
- **Visitante pertence à Propriedade** (decisão adotada, confirmada com o usuário na Fase 1):
  cadastrado uma vez, reaproveitável em autorizações de unidades diferentes. A `Autorizacao` já
  amarra `UnidadeId` + `MoradorResponsavelId`, então o vínculo específico por visita não se perde
  — só o cadastro da pessoa é compartilhado.

### Decisão

`Visitante` pertence direto à `Propriedade` (mesmo padrão de `PontoAcesso`, ADR 0010).
`Autorizacao` é quem carrega `UnidadeId` e `MoradorResponsavelId` — o vínculo com uma unidade e um
responsável específico é por autorização, não por visitante. Validação de consistência (decisão
autônoma, mesmo espírito do ADR 0010): o `MoradorResponsavelId` de uma `Autorizacao` precisa
pertencer à `UnidadeId` informada — não é possível um morador de uma unidade autorizar uma visita
"em nome" de outra unidade.

## Problema 2 — Status da Autorização sem scheduler

`StatusAutorizacao` tem 5 valores (Pendente/Ativa/Expirada/Cancelada/Utilizada), mas 3 deles
(Pendente/Ativa/Expirada) descrevem uma janela de tempo (`DataInicial`/`DataFinal`/`HorarioInicial`/
`HorarioFinal`) — sem um job periódico, nada faria uma autorização "virar" Expirada
automaticamente no momento exato em que a janela se fecha.

### Alternativas consideradas

- **Totalmente manual, como `Credencial.Status`** (Sprint 7): o usuário troca o status via
  endpoint. Simples, mas Expirada nunca aconteceria sozinha — alguém precisaria lembrar de marcar
  manualmente após o prazo passar, o que na prática nunca seria feito.
- **Status híbrido** (decisão adotada, confirmada com o usuário na Fase 1): Pendente/Ativa/
  Expirada são **computados em tempo de leitura** a partir de `DataInicial`/`DataFinal`/
  `HorarioInicial`/`HorarioFinal` comparados a `DateTime.UtcNow` — nunca gravados no banco, nunca
  exigem job/scheduler. `StatusManual` (campo interno, só assume `Cancelada` ou `Utilizada`) é
  gravado quando o usuário cancela a autorização ou a marca como utilizada — um valor manual
  sempre vence o cálculo por data.

### Decisão

`Autorizacao.StatusManual` (`StatusAutorizacao?`, nulo por padrão) é o único campo persistido.
`StatusAutorizacaoCalculator.CalcularEfetivo(autorizacao, agoraUtc)` (`Application.Autorizacoes`)
é a única fonte da regra: se `StatusManual` tem valor, retorna ele; senão, compara
`DataInicial + HorarioInicial` (ou `00:00` se horário não informado) e
`DataFinal + HorarioFinal` (ou `23:59:59` se não informado) contra `agoraUtc`, retornando
Pendente/Ativa/Expirada. Usado tanto por `AutorizacaoServico` (mapeamento de DTO) quanto por
`DashboardServico` (contadores) — nunca duplicado entre os dois (`CLAUDE.md`: "não duplicar
regras de negócio").

`AtualizarStatusAsync` só aceita `Cancelada` ou `Utilizada` no request — qualquer outro valor é
rejeitado explicitamente, porque Pendente/Ativa/Expirada nunca são definidos manualmente.
`UpdateAsync` (editar datas/horário/tipo) é bloqueado se a autorização já tem `StatusManual`
`Cancelada` ou `Utilizada` (decisão autônoma: um estado terminal não deveria ser editado
silenciosamente).

## Consequências

- Nenhum job/scheduler foi introduzido — mantém a regra permanente de simplicidade em MVPs
  (sem filas/workers antecipados).
- "Autorização expirada" **não é registrada automaticamente** no histórico
  (`HistoricoVisitante`) — o evento `AutorizacaoExpirada` existe no enum (domínio preparado), mas
  nada o produz hoje, porque isso exigiria exatamente o scheduler que esta decisão evita. Registrado
  como dívida técnica (`docs/DIVIDA_TECNICA.md` item 15) em vez de forçar uma solução frágil
  (ex.: gravar o evento como efeito colateral de uma leitura).
- O domínio está pronto para receber uma integração real (Control iD, Intelbras, Hikvision, JFL)
  numa Sprint futura sem redesenho: a "inteligência" de quem pode visitar, quando, e quem
  autorizou já existe — falta só o conector físico (liberação automática de portão/catraca).

## Impactos

`Domain/Entities/{Visitante,Autorizacao,TipoVisita,StatusAutorizacao,HistoricoVisitante,
TipoEventoHistoricoVisitante}.cs`; `Domain/Repositories/{IVisitanteRepositorio,
IAutorizacaoRepositorio,IHistoricoVisitanteRepositorio}.cs`; `Infrastructure/Persistence/
{VisitanteRepositorio,AutorizacaoRepositorio,HistoricoVisitanteRepositorio,AppDbContext}.cs`;
`Application/{Visitantes,Autorizacoes}/*.cs` (incluindo `StatusAutorizacaoCalculator`);
`Application/Propriedades/PropriedadeServico.cs`, `Application/Unidades/UnidadeServico.cs`,
`Application/Moradores/MoradorServico.cs` (cascade expandido); `Application/Dashboard/
{DashboardResponse,DashboardServico}.cs`; `Api/Controllers/{VisitantesController,
AutorizacoesController}.cs`.

## Como revisar futuramente

Ao implementar a integração real de hardware (fase futura já anunciada no `ROADMAP.md`),
reaproveitar `Visitante`/`Autorizacao` como estão. Se um dia um job de expiração automática for
implementado (ex.: para notificar o morador quando uma autorização vence), ele deve **gravar**
`AutorizacaoExpirada` em `HistoricoVisitante` no momento da transição real — não antes disso, para
não registrar um evento que nunca "aconteceu" de fato do ponto de vista do sistema.
