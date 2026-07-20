# Relatório — Sprint 3 (Central de Eventos Inteligente)

**Data de conclusão**: 2026-07-19

## Escopo entregue

### Backend
- Modelo unificado por fontes plugáveis: `EventoTimeline`, `IFonteEventos`, `FiltroEventos`,
  enums `OrigemEvento`/`CategoriaEvento`/`SeveridadeEvento` (`AppMorador.Application/Eventos`).
- `JflFonteEventos` (Infrastructure) — única implementação real hoje, único ponto do sistema que
  traduz `Ocorrencia` para o formato comum da timeline.
- `EventosServico`/`IEventosServico` — ownership check (mesmo padrão de `DashboardServico`),
  paginação, mapeamento para `EventoResponse` (DTO em linguagem de produto: `Titulo`, `Descricao`,
  `OcorridoEmUtc`, `Destaque` — sem expor a taxonomia interna).
- `EventosController`: `GET /api/properties/{id}/eventos` com `pagina`, `tamanhoPagina`, `busca`,
  `desdeUtc`, `ateUtc`.
- Migration `AdicionarIndiceOcorrenciaPropriedadeData`: índice composto `(PropriedadeId,
  CreatedAtUtc)` — corrigida manualmente a ordem `CreateIndex`/`DropIndex` (o MySQL rejeita
  derrubar o índice de suporte de uma FK antes de criar o substituto).

### Mobile
- Tela `Eventos` (`src/screens/eventos/`): `EventosScreen` (orquestrador com scroll infinito),
  `ItemEvento`, `FiltrosEventos` (chips de período + busca), `SkeletonEventos`.
- `EstadoVazio` generalizado (`src/components/`) — substitui `EstadoVazioDashboard`, reutilizado
  por Dashboard e Eventos.
- `AtalhoEventos` no Dashboard — ponto de entrada para a nova tela.
- Rota `Eventos` adicionada à navegação.

## Investigação realizada (a pedido do usuário)

Antes de modelar, investigado o estado real do domínio de eventos e a referência
`Teste-portaria-main1` (integração Control iD/Intelbras). Achados principais:
- Só 1 código Contact ID homologado hoje (`1130`).
- Nenhuma integração de controle de acesso existe no AppMorador.
- A integração Control iD na referência é um cliente HTTP real, mas nunca validada contra
  hardware (seed com dado fake). A integração validada contra hardware real lá (Intelbras/CGI)
  acabou sendo um leitor de controle de acesso, com ajustes documentados como pendentes no
  próprio repositório de referência.

Decisão resultante (aprovada pelo usuário, ver ADR 0006 em `docs/adr/0006-modelo-eventos-fontes-plugaveis.md`): implementar a arquitetura extensível
agora, com a integração de hardware de acesso real como fase futura própria — não uma remoção de
escopo, mas o dimensionamento correto do trabalho.

## Validação executada

- `dotnet build`: 0 erros, 0 avisos.
- `npx tsc --noEmit`: limpo.
- Migration revisada e aplicada (resumo técnico apresentado; falha inicial de ordem
  `Drop`/`Create` corrigida antes de reaplicar).
- Fluxo via `curl`: propriedade sem eventos (estado vazio, `totalItens=0`); 3 `Ocorrencia` de
  teste inseridas via SQL (sem painel JFL real conectado neste ambiente) cobrindo paginação
  (página 1 de 2 com `tamanhoPagina=2`, página 2 com o restante), fallback "Evento registrado"
  para um código Contact ID não homologado, e filtro `desdeUtc` (últimas 24h) retornando só o
  evento correspondente.
- Sem browser disponível — verificação visual do scroll infinito/chips fica limitada a
  `tsc`/log do dev server, mesma limitação já registrada em Sprints anteriores.

## Escopo explicitamente fora desta Sprint

- Integração real de controle de acesso (Control iD/Intelbras ASI) — fase futura própria.
- Filtro por zona na Central de Eventos — depende de endpoint de listagem de zonas inexistente.
- Agregação entre múltiplas fontes de `IFonteEventos` — só há 1 fonte real hoje.

## Dívida técnica gerada

Ver `docs/DIVIDA_TECNICA.md` itens 3–5: filtro por zona cortado do v1; agregação cross-fonte
adiada; integração de controle de acesso real como fase futura (com os achados da investigação
já documentados, para não precisar recomeçar do zero).

## Decisões arquiteturais adicionadas

- ADR 0006 (`docs/adr/0006-modelo-eventos-fontes-plugaveis.md`, numerado ADR-003 no momento desta
  Sprint, renumerado na consolidação de ADRs pré-Sprint-4): modelo unificado por fontes plugáveis
  para a Central de Eventos.

## Pendências

Nenhuma pendência bloqueadora identificada. Itens de escopo futuro estão documentados em
`docs/roadmap/ROADMAP.md`/`docs/DIVIDA_TECNICA.md`, não como trabalho inacabado desta Sprint.

## Parecer do Reviewer

Ver seção própria "Parecer do Reviewer" apresentada ao final da entrega desta Sprint (fora deste
arquivo, conforme o formato de entrega solicitado) — a Sprint só é considerada concluída após essa
aprovação explícita.