# Relatório — Sprint 2 (Dashboard Premium / Primeira Sprint de Produto)

**Data de conclusão**: 2026-07-19 (Sprint 2.1 — hotfix final e encerramento)

## Escopo entregue

### Backend
- Enum `TipoPropriedade` (Residencial/Comercial/Condominio/Rural/Outro) e campo `Propriedade.Tipo`.
- `CriarPropriedadeRequest`/`AtualizarPropriedadeRequest`/`PropriedadeResponse` com `Tipo`.
- `DashboardResponse` enriquecido: `Nome`, `Tipo`, `QuantidadeCentrais`, `QuantidadeGravadores`
  (o Dashboard nunca contava `Gravadores` antes desta Sprint).
- Migration `AdicionarTipoPropriedade` (coluna nova, backfill `"Outro"`, sem operação destrutiva).

### Mobile
- Dashboard componentizado em `src/screens/dashboard/`: `HeaderDashboard`, `CardSaude` (Health
  Score animado com rótulo por faixa), `CardResumoInstalacao`, `CardUltimaAtividade`,
  `AcoesRapidas` (Armar/Desarmar visual, preparado para integração real futura),
  `EstadoVazioDashboard`, `SkeletonDashboard`.
- Design System formalizado: `theme/tokens.ts` como fonte única de verdade; `theme.ts` importa e
  reexporta.
- `ServicoFeedbackTatil` centralizando `expo-haptics`; `react-native-reanimated` adotado como
  biblioteca padrão de animação do projeto.
- `TipoPropriedadeSelector` na tela de criar propriedade.

### Hotfix (Sprint 2.1)
- Corrigida a desserialização de `TipoPropriedade`: `POST /api/properties` retornava 400 ao
  receber `"tipo":"Comercial"` porque nenhum `JsonStringEnumConverter` estava registrado.
  Corrigido com registro global em `Program.cs`, cobrindo qualquer enum de negócio exposto pela
  Api daqui em diante — não uma correção pontual só para este DTO. Ver ADR 0005
  (`docs/adr/0005-serializacao-global-enums-negocio.md`, numerado ADR-003 originalmente).

## Validação executada

Fluxo completo via `curl` contra o servidor real, depois do hotfix:
cadastro → login → criar propriedade com `tipo` → listar propriedades → dashboard. Todos os
passos responderam com sucesso (201/200) e os campos novos (`tipo`, `quantidadeCentrais`,
`quantidadeGravadores`) retornaram corretamente. `dotnet build` limpo (0 erros, 0 avisos).

## Escopo explicitamente fora desta Sprint

- Comando real de Armar/Desarmar à central JFL (handlers já isolados/nomeados no mobile,
  prontos para receber a chamada real numa Sprint futura, sem reescrever a tela).
- Módulo de Clips/vídeo.
- Compartilhamento de propriedade entre múltiplos usuários.

## Dívida técnica gerada

Ver `docs/DIVIDA_TECNICA.md`: (1) decisões técnicas desta Sprint ainda não formalizadas como ADR
individuais (Reanimated como padrão, Design System, `TipoPropriedade`) — prioridade baixa; (2)
bridge de compatibilidade `theme.ts`→`tokens.ts` para não quebrar telas antigas — prioridade
baixa.

## Status

**Sprint 2 — Status: ✅ Concluída.**
