# ADR 0012 — Domínio de Veículos e Garagens: escopo da Vaga, Status híbrido e reuso de PontoAcesso

**Data**: 2026-07-21

## Contexto

A Sprint 9 pediu o domínio completo de Veículos e Garagens: cadastro de veículos, vagas
independentes (nunca armazenadas dentro do Veículo), vínculo Veículo↔Vaga com histórico, e
permissões veiculares para pontos de acesso — sem nenhuma integração com equipamento físico (OCR
de placas, portão automático, Control iD, Intelbras, Hikvision, JFL ficam para Sprint futura). Três
decisões de modelo precisavam ser tomadas antes de qualquer código, confirmadas com o usuário na
Fase 1.

## Decisão 1 — Status da Vaga é híbrido (mesmo padrão do ADR 0011)

`StatusVaga` tem 4 valores (Livre/Ocupada/Bloqueada/Reservada). Livre/Ocupada descrevem se a vaga
tem um veículo estacionado agora — sem job/scheduler, nada mudaria isso automaticamente no
momento exato em que um veículo é vinculado/desvinculado, a menos que a aplicação já esteja fazendo
essa transição no mesmo instante da ação do usuário.

**Decisão** (confirmada com o usuário): "Livre" e "Ocupada" são **computados** a partir da
existência de um `VinculoVeiculoVaga` ativo para a vaga (`VagaStatusCalculator.CalcularEfetivo`,
mesma estrutura de `StatusAutorizacaoCalculator` do ADR 0011) — nunca gravados. `Bloqueada` e
`Reservada` são decisões operacionais manuais (`Vaga.StatusManual`) que sempre vencem o cálculo.
Isso significa que vincular/desvincular um veículo já muda o Status efetivo da vaga
instantaneamente (na mesma operação), sem precisar de um segundo passo do usuário para "marcar a
vaga como ocupada" — a regra nunca é duplicada entre a ação de vincular e o status da vaga.

## Decisão 2 — Vínculo Veículo↔Vaga é uma entidade temporal própria

A vaga nunca é armazenada dentro do Veículo (`Veiculo` não tem `VagaId`) — o vínculo é a entidade
`VinculoVeiculoVaga` (`VeiculoId`, `VagaId`, `DataInicioUtc`, `DataFimUtc`). Um vínculo é ativo
quando `DataFimUtc` é nulo. "Vincular" e "Alterar vaga" são a mesma operação
(`VeiculoVagaServico.VincularAsync`): vincular a uma vaga diferente da atual encerra o vínculo
antigo (`DataFimUtc = agora`) e cria um novo — nunca sobrescreve uma linha existente. Isso
preserva o histórico completo de ocupação de cada vaga e cada veículo sem esforço extra, e prepara
vagas rotativas futuras (múltiplos períodos de ocupação por vaga) sem exigir redesenho do schema.

## Decisão 3 — Permissões Veiculares reaproveitam PontoAcesso (não um enum próprio)

A missão descreveu "Garagem Principal", "Garagem Secundária", "Visitantes", "Área Comercial" como
exemplos de onde um veículo pode ter permissão de acesso — a mesma noção de "lugar com acesso
controlado" que `PontoAcesso` (Sprint 7, ADR 0010) já resolve.

### Alternativas consideradas

- **Enum `AreaVeicular` próprio, independente**: mais simples de implementar isoladamente, mas
  duplica um conceito que já existe (`PontoAcesso`), e é inflexível — adicionar uma área nova
  exigiria mudança de código/migration em vez de um cadastro simples pelo usuário.
- **`PermissaoVeicular` aponta para `PontoAcesso`** (decisão adotada, confirmada com o usuário):
  reaproveita toda a infraestrutura de pontos de acesso por propriedade já existente. Para isso,
  `PontoAcesso` ganhou um campo novo `Tipo` (`TipoPontoAcesso`: Geral/Veicular) — só pontos
  `Veicular` podem ser referenciados por uma `PermissaoVeicular`. Pontos já cadastrados antes desta
  Sprint recebem `Tipo = Geral` no backfill da migration (comportamento correto — nenhum ponto
  existente era pensado como garagem).

### Decisão

`PermissaoVeicular` é um vínculo `Veiculo` ↔ `PontoAcesso` (mesmo formato de `PermissaoAcesso`,
Sprint 7), validando que o `PontoAcesso` pertence à mesma Propriedade do Veículo E tem
`Tipo = Veicular`. Nenhuma regra de "lugar com acesso controlado" existe duas vezes no sistema.

## Decisão 4 (menor, autônoma) — Unicidade de Placa é validada em código, não por índice único

Uma Placa é, na prática, um identificador único real — mas um veículo pode ser vendido/removido e
sua placa reutilizada por outro veículo depois. Um índice único no banco não distingue
automaticamente "excluído logicamente" de "ativo" (o `HasQueryFilter` só afeta consultas via EF,
não a constraint física). Por isso a unicidade é garantida em `VeiculoServico`
(`GetByPlacaAsync`, que já respeita o query filter — só enxerga veículos não excluídos) antes de
criar ou atualizar, e nenhum índice único físico foi adicionado à coluna `Placa`.

## Consequências

- Nenhum job/scheduler foi introduzido — mantém a regra permanente de simplicidade em MVPs.
- O domínio está pronto para receber uma integração real (OCR de placas, portão automático,
  Control iD, Intelbras, Hikvision, JFL) numa Sprint futura sem redesenho: a "inteligência" de
  quem pode estacionar onde, e com qual veículo, já existe — falta só o conector físico.
- `PontoAcesso.Tipo` é um campo aditivo — nenhuma tela/fluxo da Sprint 7 quebrou; a tela de Pontos
  de Acesso (mobile) ganhou um seletor Geral/Veicular para permitir cadastrar pontos veiculares.

## Impactos

`Domain/Entities/{Veiculo,TipoVeiculo,StatusVeiculo,Vaga,TipoVaga,StatusVaga,VinculoVeiculoVaga,
PermissaoVeicular,HistoricoVeiculo,TipoEventoHistoricoVeiculo,HistoricoVaga,
TipoEventoHistoricoVaga,TipoPontoAcesso}.cs` (e `PontoAcesso.cs` alterado); `Domain/Repositories/
{IVeiculoRepositorio,IVagaRepositorio,IVinculoVeiculoVagaRepositorio,IPermissaoVeicularRepositorio,
IHistoricoVeiculoRepositorio,IHistoricoVagaRepositorio}.cs`; `Infrastructure/Persistence/*`;
`Application/{Veiculos,Vagas,VinculosVeiculoVaga,PermissoesVeiculares}/*.cs` (incluindo
`VagaStatusCalculator`); `Application/Propriedades/PropriedadeServico.cs`, `Application/Unidades/
UnidadeServico.cs`, `Application/Moradores/MoradorServico.cs`, `Application/PontosAcesso/
PontoAcessoServico.cs` (cascade expandido); `Api/Controllers/{VeiculosController,VagasController,
VeiculoVagaController,PermissoesVeicularesController}.cs`.

## Como revisar futuramente

Ao implementar a integração real de hardware (fase futura já anunciada no `ROADMAP.md`),
reaproveitar `Veiculo`/`Vaga`/`VinculoVeiculoVaga`/`PermissaoVeicular` como estão. Se um leitor de
OCR de placas for integrado, ele deve alimentar a mesma `VeiculoVagaServico.VincularAsync` (ou uma
variante dela) para acionar o vínculo automaticamente — nunca um caminho paralelo de escrita.
