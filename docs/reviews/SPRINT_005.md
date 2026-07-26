# Relatório — Sprint 5 (Alinhamento Visual ao Protótipo)

**Data de conclusão**: 2026-07-20

## Resumo executivo

Interface do Dashboard aproximada de um protótipo visual anexado pelo usuário, preservando
integralmente arquitetura, lógica de negócio, navegação e contratos existentes. Achado inicial
relevante: a paleta de cores do protótipo é idêntica, valor por valor, a `mobile/src/theme/tokens.ts`
— o Design System do AppMorador já nasceu deste protótipo (ou de um ancestral idêntico) em
Sprints anteriores. Por isso, todo o trabalho desta Sprint foi composição/estilo de componentes
já existentes — **nenhuma mudança de backend, schema, contrato ou navegação**.

## Processo

Fase 1 (mapeamento protótipo × implementação real, sem código) apresentada com uma tabela
elemento-a-elemento classificando cada peça do protótipo como "polimento visual seguro" ou "fora
de escopo" (implica dado/contrato/navegação nova). Duas decisões perguntadas explicitamente ao
usuário antes de codar: adicionar o 3º estado "Noturno" (visual-only) e como tratar a seção de
câmeras "AO VIVO" do protótipo (virou item discreto "em breve", não um card de dado real). Fase 2
implementada em 7 etapas pequenas, cada uma validada com `tsc` antes de avançar.

## Arquivos modificados

- `mobile/src/screens/dashboard/CardSaude.tsx` — hero reestilizado (anel pulsando + glow).
- `mobile/src/screens/eventos/ItemEvento.tsx` — ícone círculo → caixa arredondada (afeta também
  a timeline do Dashboard, que reusa este componente desde a Sprint 4).
- `mobile/src/screens/dashboard/HeaderDashboard.tsx` — ícone de localização adicionado.
- `mobile/src/screens/dashboard/CardResumoInstalacao.tsx` — ícones em caixa (consistência visual).
- `mobile/src/screens/dashboard/AcoesRapidas.tsx` — 3º estado "Noturno"; `armado: boolean` →
  `modo: ModoArme`.
- `mobile/src/screens/dashboard/DashboardScreen.tsx` — adaptado ao novo tipo `ModoArme`.
- `mobile/src/screens/dashboard/RecursosFuturos.tsx` — "Visualização ao vivo" como 5º item.
- `mobile/src/theme/tokens.ts` — `colors.warnLine` e `motion.duration.ambient` (gaps reais no
  padrão já existente, não valores soltos em componente).

## Arquivos novos

- `docs/sprints/SPRINT_005.md` — especificação (mapeamento protótipo × real + decisões).

## Arquitetura

Nenhuma linha em `Domain`/`Application`/`Infrastructure`/`Api`. Nenhuma migration. Nenhum ADR —
nenhuma decisão estrutural nova, só estilo. Único ponto de mudança de "lógica" foi local ao mobile
(`ModoArme` de 2 para 3 estados), sem tocar em nenhum comando real a central — mesmo padrão
non-funcional já documentado desde a Sprint 1 para Armar/Desarmar.

## Fluxos homologados

- Login (Morador/Fernanda Oliveira) → 200.
- `GET /api/properties` → 200.
- `GET /api/properties/{id}/dashboard` → 200, contrato idêntico.
- `GET /api/properties/{id}/eventos?tamanhoPagina=3` → 200, contrato idêntico.
- Swagger → 200.

## Evidências dos testes

- `dotnet build`: 0 erros, 0 warnings.
- `npx tsc --noEmit`: limpo a cada uma das 7 etapas da Fase 2.
- Bundle web do Metro: compila e serve integralmente (8,7 MB, sem stack trace de erro), com todos
  os componentes evoluídos presentes no bundle gerado.
- Backend e Mobile validados rodando lado a lado nas janelas visíveis de sempre.

## Pendências

Nenhuma bloqueadora. Elementos do protótipo deliberadamente não implementados (registrados como
backlog em `docs/roadmap/ROADMAP.md`): grade de câmeras "AO VIVO" com vídeo real, tela de Acessos
(facial/QR), overlay de disparo com timeline de vídeo, navegação por abas fixas.

## Atualizações de documentação

- `docs/CHANGELOG.md`: nova entrada Sprint 5.
- `docs/roadmap/ROADMAP.md`: Sprint 5 movida para "Concluído"; novo item de backlog
  ("Visualização ao vivo de câmeras").
- `docs/sprints/SPRINT_005.md`, `docs/reviews/SPRINT_005.md`: espec + relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Zero mudança em Domain/Application/Infrastructure/Api; nenhuma migration; nenhum contrato alterado. |
| **Segurança** | ✅ Aprovado. Nenhuma superfície nova. |
| **Produto** | ✅ Aprovado. Fase 1 separou explicitamente "visual" de "funcionalidade nova disfarçada de estilo" antes de codar — nenhum elemento do protótipo que implicaria dado/contrato/navegação novos foi implementado, exatamente como pedido ("preservar arquitetura, lógica, navegação e contratos"). |
| **UX/UI** | ✅ Aprovado. Hero de status mais expressivo sem perder legibilidade; "Noturno" segue a mesma semântica visual-only já estabelecida (não finge um comando real); seção "em breve" mantém o tom tranquilizador já usado desde a Sprint 4, nunca "não implementado". |
| **Performance** | ✅ Aprovado. Animações novas (anel pulsando) usam Reanimated (thread de UI nativa), mesma biblioteca/padrão já adotado desde a Sprint 2 — nenhum re-render em JS por frame. |
| **Manutenibilidade** | ✅ Aprovado. Todos os arquivos tocados bem abaixo do limite de ~250 linhas; `ItemEvento` ganhou o novo tratamento de ícone num único ponto compartilhado (Dashboard + Central de Eventos), sem duplicar estilo. Dois tokens novos (`warnLine`, `motion.duration.ambient`) preenchem gaps reais do padrão já existente, não valores soltos. |
| **Documentação** | ✅ Aprovado. CHANGELOG/ROADMAP/relatório refletem exatamente o que foi feito e o que foi deliberadamente deixado de fora. |
| **Regressões** | ✅ Nenhuma. Dashboard, Central de Eventos, Autenticação e Propriedades revalidados via requests reais — resultados idênticos aos anteriores. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite atendidos
integralmente — interface aproximada do protótipo sem reescrever o app nem substituir a
implementação atual.
