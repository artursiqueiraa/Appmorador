# ADR 0029 — Endpoints de Leitura Global para o Painel Web (Sprint 22A)

**Data**: 2026-07-27

## Contexto

A Sprint 22A (Painel Web — Fundação) partiu do princípio "zero mudança no backend, só consumir a
infra existente da Sprint 21" (RBAC Master). A Fase 0 (auditoria obrigatória, ver
`docs/painel/mapeamento-api.md`) confirmou que isso não é totalmente possível: nenhum endpoint do
backend é cross-tenant — todo domínio (Propriedade, Equipamento, Usuario) é escopado por dono
(`ProprietarioId`) ou por papel interno específico (`ListInternosAsync` exclui clientes por
definição). Um Master logado no Painel Web não tem hoje nenhuma forma de ver "todos os clientes da
plataforma" nem "totais agregados" — ambos são pré-requisitos literais da missão (Fase 3 Dashboard
Operacional, Fase 5 Clientes).

## Problema

Como entregar o Dashboard Operacional e a listagem de Clientes exigidos pela Sprint sem construir
infraestrutura desproporcional ao que foi pedido ("+1 micro-endpoint")?

## Alternativas consideradas

- **Zero mudança no backend, adaptar o frontend** (rejeitada): sem dado real nenhum para os cards/
  gráficos de Dashboard nem para a listagem de Clientes — as duas Fases mais importantes da
  missão ("Dashboard é operacional, não decorativo") ficariam vazias ou fabricadas, o que viola o
  princípio já estabelecido do projeto de nunca fingir um dado que o sistema não tem de verdade.
- **Endpoints genéricos de "admin query"/GraphQL-like** (rejeitada): resolveria o problema geral,
  mas é desproporcional — infraestrutura nova inteira para 2 telas.
- **2 endpoints novos, cirúrgicos, só-leitura** (escolhida): `GET /api/proprietarios` (lista
  paginada de clientes) e `GET /api/dashboard-operacional` (agregado). Ambos Master/Suporte-only
  (`Policies.RequerSuporte`), aditivos (nenhuma rota/DTO existente muda), sem escrita.

## Decisão

Dois Controllers novos (`ProprietariosController`, `DashboardOperacionalController`), um novo
namespace `AppMorador.Application.Painel` (Servicos + DTOs), e 5 métodos novos de agregação
adicionados às portas de repositório já existentes (`IUsuarioRepositorio.ListProprietariosAsync`/
`ContarClientesAsync`/`ContarClientesPorMesAsync`, `IPropriedadeRepositorio.ContarPorTipoAsync`/
`ContarPorProprietariosAsync`, `IEquipamentoRepositorio.ContarPorStatusGlobalAsync`) — nenhum
método existente foi alterado, só métodos novos adicionados às interfaces já existentes.

**"Sessões Ativas" (Fase 6) não ganhou endpoint novo** — decisão explícita do usuário de não
construir um mecanismo de rastreamento/revogação de sessão (impersonation é 100% stateless via
JWT, ver ADR 0021). A tela lê `GET /api/auditoria` e infere "ativa agora" por um `ImpersonationInicio`
sem `ImpersonationFim` correspondente dentro dos 15 minutos de vida do token — **sem** um botão de
"forçar logout" funcional (isso exigiria revogar um token já emitido, mecanismo que não existe).

**"Propriedades por Status" virou "Propriedades por Tipo"** — `Propriedade` não tem nenhum campo
de status (Ativo/Inativo/Pendente), só soft delete (ADR 0009). O breakdown real disponível é por
`TipoPropriedade` (Residencial/Comercial/...).

**"Alarmes Recentes"/"Pendências" não viraram cards do agregado** — não existe nenhuma porta de
repositório cross-propriedade para `Ocorrencia` hoje, e criar uma seria desproporcional ao
"mínimo necessário" combinado. A seção "Atividade Recente" do Dashboard usa `GET /api/auditoria`
(já existente) no lugar.

## Consequências

- O Painel Web tem dado real para Dashboard Operacional e Clientes desde o primeiro dia.
- `Usuario`/`Propriedade`/`Equipamento` ganharam consultas de agregação que **nenhum outro
  domínio do sistema tinha antes** (sempre eram escopados por dono) — precedente para futuras
  telas administrativas globais (Propriedades/Equipamentos completos, Sprint 22B).
- Sessões Ativas é uma funcionalidade parcial por design — documentado, não escondido. Se o
  produto precisar de revogação de token real no futuro, será uma Sprint própria (token
  blocklist/sessão persistida), não uma extensão trivial desta.

## Impactos

Sprint 22B (Propriedades/Equipamentos/Provisionamentos completos) provavelmente precisará dos
mesmos padrões de agregação cross-tenant — reaproveitar a mesma convenção (`Policies.RequerSuporte`
ou `RequerTecnico`, métodos de contagem nas portas já existentes) em vez de criar um padrão novo.

## Arquivos afetados

`AppMorador.Domain/Repositories/{IUsuarioRepositorio,IPropriedadeRepositorio,IEquipamentoRepositorio}.cs`,
`AppMorador.Infrastructure/Persistence/{UsuarioRepositorio,PropriedadeRepositorio,EquipamentoRepositorio}.cs`,
`AppMorador.Application/Painel/*` (novo namespace),
`AppMorador.Api/Controllers/{ProprietariosController,DashboardOperacionalController}.cs`,
`AppMorador.Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (registro DI).

## Como revisar futuramente

Revisar quando o Sprint 22B/22C precisar de mais telas administrativas globais — confirmar que o
padrão de agregação cross-tenant estabelecido aqui continua adequado, ou se nesse ponto já
justifica uma camada de "consulta administrativa" mais genérica.
