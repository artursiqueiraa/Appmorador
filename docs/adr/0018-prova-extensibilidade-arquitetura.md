# ADR 0018 — Prova de Extensibilidade da Arquitetura (Integração Intelbras)

**Data**: 2026-07-25

## Contexto

Depois de 4 Sprints construindo a arquitetura de integração de fabricante (ADR 0014, Control iD),
generalizando-a para conexão invertida (ADR 0015, JFL), consolidando-a numa Camada Operacional
(ADR 0016) e adicionando transporte em tempo real (ADR 0017), a Sprint 15 teve um objetivo
diferente de todas as anteriores: **não** integrar um fabricante por si só, mas usar uma terceira
integração (Intelbras) como prova de que a arquitetura já construída é genérica o suficiente para
receber um novo fabricante **sem alterar nenhuma camada compartilhada** — só corrigindo, de forma
documentada e genérica, qualquer limitação real encontrada.

## Auditoria da Arquitetura (Fase 0, antes de qualquer código)

Percorridas as 10 camadas pedidas pela missão, confirmadas em código (não de memória):

| Camada | Alteração | Detalhe |
|---|---|---|
| Entidades do Domínio | Não | `Equipamento.Fabricante` já incluía `Intelbras` desde a Sprint 11; `EventoEquipamento` já era genérico (chave só por `EquipamentoId`). |
| Contratos da Aplicação | Não (só arquivos novos) | `IFonteEventos`, `IEquipamentoRepositorio`, `IEventoEquipamentoRepositorio` inalterados. |
| SnapshotOperacionalServico | Não | Já consome `IEquipamentoRepositorio` (fabricante-agnóstico) — um Equipamento Intelbras contribui automaticamente para os contadores. |
| OperacionalHub | Não | Só conhece `PropriedadeId`. |
| Timeline | Não | Continua só `EventosController`/`IEventosServico`. |
| Central de Eventos | Não (adição, não alteração) | Nova implementação de `IFonteEventos` + um valor aditivo em `OrigemEvento` — o próprio doc comment do enum já antecipava isso desde a Sprint 13. |
| Dashboard | Não | Decisão deliberada de não replicar campos dedicados tipo `quantidadeCentraisIntelbrasOnline` (o que a Sprint 12 fez para JFL) — Intelbras aparece só via campos já genéricos. |
| Mobile | Adição, não alteração | 2 telas novas; Dashboard/Timeline/Central Operacional/Saúde da Propriedade 100% reaproveitados. |
| SignalR | Não | `OperacionalHub`/`OperacionalHubPublicador` não sabem de fabricante nenhum. |
| Banco de Dados | Não | Nem uma tabela aditiva — decisão de design (ver Decisão 3) evitou até isso. |

**Resultado da auditoria**: "Nenhuma alteração" em 9 das 10 camadas — a única exceção prevista
(Central de Eventos) é a exercitação do próprio mecanismo de extensão já desenhado, não uma
violação. **Um achado real emergiu durante a implementação** (não na auditoria estática) — ver
Decisão 5.

## Fase 1 — Descoberta: modelo e protocolo escolhidos

**Fabricante/modelo**: Intelbras AMT 8000 — usado como referência arquitetural, não como alvo de
engenharia reversa de protocolo.

**Protocolo — decisão consciente, divergindo da forma JFL**: diferente de JFL (Sprint 12, ADR
0015), este projeto não tem nenhuma referência real investigada de um protocolo TCP proprietário
Intelbras para comandos de central — não existe um repositório legado com bytes documentados
equivalente ao que existia para JFL (`Integra-o-FL-main`), nem à API REST real do Control iD
(`Teste-portaria-main1`). Inventar um protocolo binário sem uma fonte verificável significaria
apresentar como "real" algo que não foi genuinamente validado. Decisão: modelar a central Intelbras
como uma API HTTP local (mesmo padrão dial-out do Control iD, ADR 0014), com vocabulário de
comando de alarme (Armar/Desarmar/Status/Eventos — vocabulário de JFL, ADR 0015). Isso testa
exatamente o que a Sprint pede: prova que a *direção de conexão* (dial-out) e o *vocabulário de
comando* (alarme) são eixos **independentes** que a arquitetura já desacopla.

**Operações implementadas**: Testar conexão, Consultar Status, Armar, Desarmar, Importar Eventos
(poll-based, mesmo padrão de `ImportarEventosAsync` do Control iD). PGM e Inibição de Zona: não
implementados (marcados como "se suportado" pela missão) — registrado como backlog.

## Decisão 1 — IIntelbrasProvider segue ADR 0014, com vocabulário de comando de ADR 0015

```
Application/Intelbras/IIntelbrasProvider  (porta)
        │
        ▼
Infrastructure/Intelbras/IntelbrasProvider  (única implementação real, HttpClient dial-out)
        │
        ▼
API HTTP local simulada (backend/tools/IntelbrasSimulator)
```

Prova que a arquitetura de Provider (ADR 0014) e o vocabulário de comando de alarme (ADR 0015) são
combináveis livremente — nenhuma das duas ADRs previa essa combinação especificamente, e nenhuma
precisou ser alterada para permiti-la.

## Decisão 2 — IntelbrasComandoServico é um serviço novo e paralelo, nunca uma alteração de EquipamentoIntegracaoServico/JflComandoServico

`EquipamentoIntegracaoServico` (Control iD) e `JflComandoServico` (JFL) permanecem **inteiramente
intocados** — zero linha alterada. `IntelbrasComandoServico`/`CentraisIntelbrasController` são
arquivos inteiramente novos, resolvendo o Equipamento por `Fabricante == Intelbras` e
`Ip`/`Porta` presentes (dial-out), decifrando a senha via `ICriptografiaSimetrica` já existente.
Prova que adicionar um terceiro fabricante não exige generalizar `EquipamentoIntegracaoServico`
nem `JflComandoServico` — cada um continua dono do seu próprio fabricante.

**Observação registrada, não corrigida nesta Sprint**: `EquipamentoIntegracaoServico.
ResolverProvider` devolve `IControlIdProvider?` — hoje suporta só um fabricante dial-out-com-
sincronização. Esta Sprint não aciona esse método (Intelbras usa `IntelbrasComandoServico`
próprio), então o gap existe mas não foi exercitado — registrado como dívida técnica para quando
um **segundo** fabricante quiser reaproveitar exatamente o fluxo de sincronização de Moradores/
Credenciais/Permissões do Control iD (não apenas o vocabulário de alarme).

## Decisão 3 — Sem tabela de rollup própria (diferente de StatusCentralJfl) — decisão deliberada, não uma limitação

JFL tem `StatusCentralJfl` (Sprint 12) porque o Dashboard precisava de contadores persistidos sem
custo de consulta ao vivo. Para Intelbras, decisão deliberada de **não** criar uma tabela
equivalente: a missão pediu explicitamente "nenhuma alteração funcional" no Dashboard, o que
significa não introduzir campos dedicados por fabricante ali. `TemProblemaAtivo`/contadores de
partição do Intelbras ficam disponíveis só na tela de Detalhes (consulta ao vivo via
`ObterDetalhesAsync`/`ConsultarStatusAsync`), nunca persistidos — o Snapshot Operacional
(Sprint 13) continua recebendo a contribuição de Intelbras só através do genérico
`Equipamento.Status` (online/offline), suficiente para a Saúde da Propriedade. Prova que um
fabricante pode integrar-se plenamente sem exigir nem uma migration.

## Decisão 4 — IntelbrasFonteEventos reaproveita EventoEquipamento com semântica de Alarme, provando que a Central de Eventos já suportava a combinação

`EventoEquipamento` (Sprint 11) já era genérico — nenhuma alteração nele. `IntelbrasFonteEventos`
(Infrastructure/Eventos) é uma terceira implementação de `IFonteEventos`, mapeando para
`Origem=Intelbras` (valor aditivo no enum, mesmo mecanismo já usado por `ControlId`) e
`Categoria=Alarme` (valor já existente, usado até então só por JFL) — provando que a mesma tabela
genérica e a mesma porta plugável já suportavam uma combinação Origem×Categoria que nenhuma
integração anterior havia exercitado.

## Decisão 5 — Achado real corrigido: EquipamentoFonteEventos não era escopado por Fabricante

**O único ponto em que a arquitetura realmente precisou de correção.** Durante a validação (não na
auditoria estática), a consulta "todos os eventos, sem filtro" retornou cada evento Intelbras
**duplicado** — uma vez corretamente via `IntelbrasFonteEventos` (Origem=Intelbras), e outra vez
incorretamente via `EquipamentoFonteEventos` (rotulado como Origem=ControlId).

**Causa raiz**: `EquipamentoFonteEventos.ConsultarEventosAsync` filtrava a query base só por
`PropriedadeId` — nunca por `Equipamento.Fabricante == ControlId`. Isso funcionava por coincidência
enquanto Control iD era o único fabricante escrevendo em `EventoEquipamento`; a chegada de um
segundo fabricante reaproveitando a mesma tabela genérica (Intelbras, por design da Decisão 4)
expôs a lacuna.

**Correção** (`Infrastructure/Eventos/EquipamentoFonteEventos.cs`): adicionado
`&& e.Equipamento!.Fabricante == FabricanteEquipamento.ControlId` à query base — mesma disciplina
já usada por `JflFonteEventos` (implicitamente, via `NumeroSeriePainel`) e por
`IntelbrasFonteEventos` (explicitamente). **Isso não é uma adaptação específica de Intelbras** — é
uma correção em `EquipamentoFonteEventos`, que passa a se auto-escopar corretamente ao seu próprio
fabricante, beneficiando qualquer fabricante futuro que reaproveite `EventoEquipamento`.

## Lições aprendidas

1. A auditoria estática (Fase 0) não encontra todos os problemas — o achado real da Decisão 5 só
   apareceu na validação com um segundo fabricante genuíno escrevendo na mesma tabela. Testes de
   extensibilidade precisam de uma segunda instância real, não só uma revisão de código.
2. "Nenhuma alteração esperada" nem sempre significa "zero incremento" — significa "nenhuma
   mudança de contrato/comportamento para quem já existe". Um enum ganhando um valor aditivo, ou
   uma fonte de eventos nova sendo registrada via DI, são o próprio mecanismo de extensão
   funcionando, não uma exceção à regra.
3. Decisões de escopo mínimo (Decisão 3: sem rollup para Intelbras) são tão válidas quanto
   decisões de paridade total — nem todo fabricante precisa replicar toda a superfície de um
   fabricante anterior para se integrar de verdade.

## Recomendações para futuras integrações (Hikvision, Dahua, ou outro)

Seguir exatamente o processo desta Sprint: (1) Fase 0 de auditoria camada a camada antes de
qualquer código; (2) Fase 1 de descoberta definindo modelo/protocolo com justificativa honesta
sobre o que é real vs. simulado; (3) implementar via um Provider+serviço+Controller **novos**,
nunca alterando os de fabricantes já existentes; (4) reaproveitar `EventoEquipamento`/
`IFonteEventos` para eventos, mas **sempre** escopar a query base ao próprio fabricante desde o
primeiro dia (não repetir o gap da Decisão 5); (5) só criar uma tabela de rollup própria se o
Dashboard realmente precisar de contadores persistidos daquele fabricante especificamente — não
por paridade automática com o fabricante anterior.

## Impactos

**Novos**: `Application/Intelbras/*.cs`; `Infrastructure/Intelbras/*.cs`;
`Infrastructure/Eventos/IntelbrasFonteEventos.cs`; `Api/Controllers/CentraisIntelbrasController.cs`;
`backend/tools/IntelbrasSimulator/`; mobile `screens/centraisIntelbras/*.tsx`.

**Modificados** (todos aditivos/correção, nunca reescrita): `Application/Eventos/OrigemEvento.cs`
(valor aditivo); `Infrastructure/Eventos/EquipamentoFonteEventos.cs` (correção da Decisão 5);
`Application/Equipamentos/EquipamentoServico.cs` (branch de validação Intelbras, mesmo padrão já
usado para JFL); `Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (registro DI);
mobile `api/types.ts`, `navigation/{types,RootNavigator}.tsx`,
`screens/equipamentos/EquipamentosScreen.tsx` (exclusão da lista genérica),
`screens/SelecionarPropriedadeScreen.tsx` (atalho).

**Zero alteração**: `Domain/Entities/*`, `SnapshotOperacionalServico`, `OperacionalHub`,
`OperacionalHubPublicador`, `EventosServico`, `EventosController`, `DashboardServico`,
`DashboardResponse`, `EquipamentoIntegracaoServico`, `JflComandoServico`, `AppDbContext` (nenhuma
migration nesta Sprint).
