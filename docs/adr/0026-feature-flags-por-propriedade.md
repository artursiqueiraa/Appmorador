# ADR 0026 — Feature Flags por Propriedade (Sprint 21)

**Data**: 2026-07-26

## Contexto

Distinto de Permissão Funcional (ADR 0025 — "o que este usuário pode fazer"), a missão pede um
eixo ortogonal: "o que esta propriedade contratou" — ex.: uma propriedade sem câmeras contratadas
não deveria mostrar a aba Câmeras como se fosse só uma permissão negada, é a funcionalidade
inteira que não existe para aquele cliente.

## Problema

Como representar "o que foi contratado" de um jeito que não se confunda com "o que o usuário pode
fazer", já que os dois eixos combinados decidem o que a UI mostra?

## Alternativas consideradas

- **Reaproveitar `PermissaoFuncionalidade`** para isso também: rejeitada — misturaria dois
  conceitos com ciclos de vida diferentes (permissão muda por usuário/vínculo, feature muda pela
  propriedade inteira, é decisão comercial/de instalação).
- **`PropriedadeFeatureFlag`** (escolhida): tabela N:N Propriedade↔`FeatureFlag` (enum, 8
  valores: Facial/Cameras/Pgm/Push/Snapshot/InterfoneSip/StreamingAoVivo/Ia), com `Ativo` +
  `AtivadoEmUtc`.

## Decisão

`FeatureFlag` + `PropriedadeFeatureFlag`, gerenciado por Técnico/Master (é decisão comercial/de
instalação, não do cliente) via `PUT /api/properties/{id}/features/{feature}`. `IPermissaoService.
PropriedadeTemFeatureAsync`/`ListarFeaturesAsync` são os únicos pontos de consulta.

**Achado durante a implementação, corrigido antes de expor ao mobile**: o serviço
(`PropriedadeFeatureFlagServico`) não faz ownership check por `ProprietarioId` — foi desenhado
para ser chamado só por internos (que legitimamente acessam qualquer propriedade). Consumo pelo
cliente (mobile) foi resolvido enriquecendo `GET /api/properties` (já ownership-checked via
`IPropriedadeServico.ListByOwnerAsync`) com os campos `perfil`/`permissoes`/`features` por
propriedade, em vez de expor a rota interna diretamente ao app — ver `PropriedadeServico.
ToDtoEnriquecidoAsync`.

## Consequências

- Nenhuma propriedade nasce com nenhuma feature ativa — precisa ser explicitamente ligada por
  Técnico/Master no momento da instalação/venda.
- O app mobile lê `features` diretamente da resposta de `GET /api/properties` (não de um endpoint
  próprio) — mantém o padrão de "toda leitura do cliente passa por um ponto já ownership-checked".
- `CamerasScreen` usa isso para mostrar um estado honesto ("Câmeras não contratadas") em vez de
  esconder a aba inteira — preserva a Navegação Previsível (ADR 0019: 4 abas sempre visíveis),
  reconciliando um pedido da missão com uma decisão de UX já estabelecida.

## Impactos

Painel Web (Sprint 22) e qualquer tela de "o que esta propriedade contratou" dependem deste
modelo. Futuras features (streaming ao vivo, IA) já têm o flag correspondente reservado no enum,
mesmo sem implementação ainda.

## Arquivos afetados

`AppMorador.Domain/Entities/{FeatureFlag,PropriedadeFeatureFlag}.cs`,
`AppMorador.Domain/Repositories/IPropriedadeFeatureFlagRepositorio.cs`,
`AppMorador.Application/Propriedades/{IPropriedadeFeatureFlagServico,PropriedadeFeatureFlagServico,PropriedadeServico}.cs`,
`AppMorador.Api/Controllers/PropriedadeFeatureFlagsController.cs`, `mobile/src/api/types.ts`,
`mobile/src/auth/usePermissao.ts`, `mobile/src/screens/cameras/CamerasScreen.tsx`.

## Como revisar futuramente

Revisar quando o Painel Web (Sprint 22) precisar de uma UI de gestão de features por propriedade —
confirmar que o endpoint interno atual (`PUT /api/properties/{id}/features/{feature}`) atende ou
precisa evoluir para operação em lote.
