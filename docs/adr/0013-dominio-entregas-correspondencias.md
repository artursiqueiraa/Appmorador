# ADR 0013 — Domínio de Entregas e Correspondências: máquina de estados manual e visão unificada por Propriedade

**Data**: 2026-07-22

## Contexto

A Sprint 10 pediu o domínio completo de Entregas e Correspondências: registrar, acompanhar e
finalizar entregas destinadas a moradores, sem nenhuma integração física (QR Code, assinatura
digital, foto, transportadoras). Diferente das Sprints 8 e 9, a própria missão já resolveu a
principal fonte de ambiguidade das Sprints anteriores — "Não utilizar jobs automáticos. Todo o
fluxo será comandado por ações do usuário" — eliminando de saída a pergunta "o status é
computado ou manual?" que motivou perguntas explícitas nas Sprints 8 (ADR 0011) e 9 (ADR 0012).

## Decisão 1 — Status da Entrega é 100% manual (sem calculadora híbrida)

Diferente de `Autorizacao` (ADR 0011) e `Vaga` (ADR 0012), `Entrega` **não** tem um
`StatusManual`/calculadora híbrida — `Status` é um campo simples, gravado diretamente a cada
transição, porque a própria missão fecha essa porta explicitamente ("todo o fluxo será comandado
por ações do usuário"). Não há necessidade de computar nada em tempo de leitura.

`DataRecebimentoUtc`/`DataRetiradaUtc` começam nulas na criação — a Diretriz de Produto descreve
3 passos distintos ("Registrar uma entrega ↓ Marcar como disponível para retirada ↓ Registrar a
retirada"), o que resolve, pelo próprio texto da missão, se a data de recebimento é conhecida no
cadastro (não é) ou só na ação "marcar disponível" (é). `RecebidoPor` (texto livre — sem cadastro
de porteiro/funcionário neste domínio) é capturado junto dessa mesma ação.

`AtualizarStatusAsync` valida uma máquina de estados explícita:

| De ↓ / Para → | DisponivelParaRetirada | Retirada | Cancelada |
|---|---|---|---|
| AguardandoRecebimento | ✅ | ❌ | ✅ |
| DisponivelParaRetirada | — | ✅ | ✅ |
| Retirada / Cancelada | ❌ (terminal) | ❌ | ❌ |

Qualquer transição fora dessa tabela é rejeitada com uma mensagem clara ("Não é possível mudar de
X para Y"). `Retirada`/`Cancelada` são estados terminais — nem o Status nem os demais campos
(`UpdateAsync`) podem ser alterados depois.

## Decisão 2 — Entregas são consultadas/criadas no nível da Propriedade, não do Morador

Diferente de `Credencial`/`Veiculo` (aninhados sob o Morador, ver ADR 0010/0012), `Entrega` é
criada e listada no nível da **Propriedade** (`api/properties/{id}/entregas`), com
`UnidadeId`/`MoradorDestinatarioId` como campos do corpo da requisição (validados consistentes
entre si — o Morador precisa pertencer à Unidade informada, mesmo padrão de `Autorizacao`).

### Motivo

O caso de uso natural de uma "central de entregas" é ver **todas** as entregas da propriedade em
um só lugar (uma "portaria" que recebe encomendas para moradores diferentes ao longo do dia), não
navegar morador por morador para descobrir o que chegou. Aninhar sob o Morador replicaria o
padrão de Credencial/Veículo sem necessidade real — o valor de produto está na visão consolidada.

### Consequência

A tela mobile de cadastro usa seleção em cascata Unidade → Morador (mesmo padrão de
`AutorizacoesScreen`, Sprint 8) dentro do formulário de criação, em vez de a tela já nascer no
contexto de um morador específico.

## Consequências gerais

- Nenhum job/scheduler foi introduzido — mantém a regra permanente de simplicidade em MVPs.
- `HistoricoEntrega` é auditoria pura (sem soft delete, mesmo padrão de `HistoricoCredencial`) —
  registra as 6 transições/ações (criada, alterada, recebida, retirada, cancelada, excluída).
- O domínio está pronto para receber uma integração real (portaria virtual, notificações,
  automações) numa Sprint futura sem redesenho: a máquina de estados e o histórico já existem —
  falta só o gatilho automático (ex.: webhook de transportadora chamando
  `AtualizarStatusAsync` em vez de um toque manual na tela).

## Impactos

`Domain/Entities/{Entrega,TipoEntrega,StatusEntrega,HistoricoEntrega,
TipoEventoHistoricoEntrega}.cs`; `Domain/Repositories/{IEntregaRepositorio,
IHistoricoEntregaRepositorio}.cs`; `Infrastructure/Persistence/{EntregaRepositorio,
HistoricoEntregaRepositorio,AppDbContext}.cs`; `Application/Entregas/*.cs`; `Application/
Propriedades/PropriedadeServico.cs`, `Application/Unidades/UnidadeServico.cs`, `Application/
Moradores/MoradorServico.cs` (cascade expandido); `Api/Controllers/EntregasController.cs`;
`Application/Dashboard/{DashboardResponse,DashboardServico}.cs`.

## Como revisar futuramente

Ao implementar uma integração real (portaria virtual, notificação por push, automação de
transportadora), reaproveitar `Entrega`/`AtualizarStatusAsync` como estão — a máquina de estados
já valida transições, então um gatilho automático só precisa chamar o mesmo método que a tela
mobile chama hoje, nunca um caminho de escrita paralelo.
