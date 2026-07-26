# Relatório — Sprint 4 (Dashboard Operacional Inteligente)

**Data de conclusão**: 2026-07-20

## Resumo executivo

Dashboard evoluído para funcionar como central operacional: um usuário deve entender o estado da
propriedade em poucos segundos ao abrir o app. Toda a Sprint foi implementada evoluindo
componentes mobile já existentes (não criando um sistema paralelo) e **sem nenhuma mudança de
backend, contrato ou banco de dados** — 100% reuso do que as Sprints 1–3.1 já entregaram. A Fase
1 (planejamento) foi apresentada e ajustada com o usuário antes de qualquer código ser escrito;
a Fase 2 (implementação) seguiu em 6 etapas pequenas, cada uma validada com `tsc`/build antes de
avançar, exatamente como pedido.

## Ajustes feitos ao plano original (Fase 1 → aprovação)

O usuário pediu 3 mudanças em relação ao plano inicial, todas incorporadas antes de implementar:

1. **Não remover `CardResumoInstalacao`/`CardUltimaAtividade`** — evoluí os componentes
   existentes em vez de criar um sistema novo de 6 cards paralelos.
2. **Câmeras + Gravadores combinados** num só indicador ("Câmeras • N gravadores"), preservando o
   dado que o Dashboard já entregava.
3. **Objetivo de "compreensão em menos de 5 segundos"** orientando as decisões de UX — por isso os
   4 indicadores sem dado real (Portões/Controle de acesso/Entregas/Visitantes) foram tratados
   como uma seção discreta de peso visual bem menor, não como cards do mesmo tamanho/destaque que
   o dado real (evita competir pela atenção com informação de segurança de verdade).

## Arquivos modificados

- `mobile/src/screens/dashboard/CardResumoInstalacao.tsx` — Câmeras+Gravadores combinados;
  "Centrais"→"Alarmes".
- `mobile/src/screens/dashboard/CardUltimaAtividade.tsx` — de evento único para timeline (fetch
  próprio, 3 eventos recentes, link "Ver todos").
- `mobile/src/screens/dashboard/DashboardScreen.tsx` — integra os componentes evoluídos e a nova
  seção `RecursosFuturos`.
- `mobile/src/screens/dashboard/SkeletonDashboard.tsx` — bloco de skeleton do card de eventos
  mais alto, refletindo a timeline.
- `mobile/src/screens/eventos/ItemEvento.tsx` — variante `compacto` opcional, aditiva
  (comportamento default inalterado).

## Arquivos novos

- `mobile/src/screens/dashboard/RecursosFuturos.tsx` — seção discreta para Portões/Controle de
  acesso/Entregas/Visitantes.
- `docs/sprints/SPRINT_004.md` — especificação da Sprint (salva no início da Fase 1).

## Arquitetura

Nenhuma linha em `Domain`/`Application`/`Infrastructure`/`Api`. Nenhuma migration. Nenhum ADR
necessário — nenhuma decisão estrutural nova, só reuso de padrões já decididos (endpoint de
eventos da Sprint 3, Design System da Sprint 2). Único ponto novo de I/O é um segundo
`api.get()` no cliente mobile, mesmo padrão de autenticação já usado em toda a app.

## Fluxos homologados

- Login (conta Morador/Fernanda Oliveira) → 200, token emitido.
- `GET /api/properties` → 200, propriedade correta.
- `GET /api/properties/{id}/dashboard` → 200, contrato idêntico ao pré-Sprint (nenhuma mudança).
- `GET /api/properties/{id}/eventos?tamanhoPagina=3` → 200, exatamente o payload que
  `CardUltimaAtividade` consome.
- Swagger → 200.

## Evidências dos testes

- `dotnet build`: 0 erros, 0 warnings (após corrigir a corrupção de `.git/HEAD` — ver
  Pendências).
- `npx tsc --noEmit`: limpo a cada uma das 6 etapas da Fase 2.
- Bundle web do Metro: compila e serve integralmente (8,6 MB, sem stack trace de erro), com os
  novos componentes presentes no bundle gerado.
- `grep` confirma que `CardResumoInstalacao`/`CardUltimaAtividade`/`RecursosFuturos` só são
  consumidos por `DashboardScreen.tsx` (nenhum outro ponto de uso quebrado) e que `ItemEvento`
  continua sendo usado por `EventosScreen.tsx` sem a nova prop (comportamento anterior
  preservado).
- Backend e Mobile validados rodando lado a lado nas janelas visíveis de sempre.

## Pendências

- **Não bloqueadora, fora do escopo desta Sprint**: `AtalhoEventos` (atalho existente no
  Dashboard para a tela de Eventos) e o chevron "Ver todos" novo dentro de `CardUltimaAtividade`
  agora são duas portas de entrada para o mesmo lugar. Não removi nenhum dos dois (instrução
  explícita de não remover componentes existentes nesta Sprint) — registrar como possível
  simplificação de UX numa Sprint futura.
- **Achado operacional, não relacionado ao código desta Sprint**: durante a validação, `dotnet
  build` falhou com erro do SourceLink porque `.git/HEAD` estava corrompido (zerado) — muito
  provavelmente interferência de sincronização de nuvem (OneDrive ou similar) na pasta
  `Documents`, onde o repositório vive. Refs/objetos/tag estavam intactos; só `HEAD` precisou ser
  restaurado, sem perda de dado. Recomendação: excluir a pasta do projeto da sincronização ativa
  de nuvem, ou mover o repositório para um caminho fora de uma pasta sincronizada, para evitar
  recorrência.
- **Backlog registrado** (não implementado, conforme "Fora de Escopo" da missão): Portões,
  Controle de acesso, Entregas, Visitantes — ver `docs/roadmap/ROADMAP.md`.

## Atualizações de documentação

- `docs/CHANGELOG.md`: nova entrada Sprint 4.
- `docs/roadmap/ROADMAP.md`: Sprint 4 movida para "Concluído"; backlog atualizado (Portões
  associado à integração de controle de acesso já registrada; Entregas/Visitantes como novos
  itens).
- `docs/sprints/SPRINT_004.md`: especificação salva.
- `docs/reviews/SPRINT_004.md`: este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Zero mudança em Domain/Application/Infrastructure/Api; nenhum contrato alterado. |
| **Segurança** | ✅ Aprovado. Nenhuma superfície nova — mesmo cliente HTTP autenticado, mesmo padrão de ownership já usado em toda a app. |
| **Produto** | ✅ Aprovado. Escopo exatamente o combinado após os ajustes da Fase 1; nada de "Fora de Escopo" foi implementado (sem push/SignalR/WebSocket/MQTT/hardware real/analytics/controle de acesso real/visitantes/entregas). |
| **UX/UI** | ✅ Aprovado. Objetivo de "5 segundos" guiou a decisão mais importante da Sprint: separar dado real (peso visual alto) de recursos futuros (peso visual propositalmente baixo) — evita que 4 indicadores vazios dominem a leitura da tela. Copy de `RecursosFuturos` segue a régua de tom tranquilizador já estabelecida (nunca "não implementado"). |
| **Performance** | ✅ Aprovado. Segundo fetch (`eventos`) é independente e não bloqueia o carregamento do resto do Dashboard — falha/lentidão ali é isolada. |
| **Manutenibilidade** | ✅ Aprovado. Todos os arquivos tocados ficaram bem abaixo do limite de ~250 linhas do agente `mobile`; `ItemEvento` ganhou reuso aditivo (prop opcional) em vez de duplicação. |
| **Documentação** | ✅ Aprovado. CHANGELOG/ROADMAP/relatório refletem exatamente o que foi entregue, incluindo os ajustes pedidos na Fase 1 e o achado operacional do git. |
| **Regressões** | ✅ Nenhuma. Central de Eventos (`ItemEvento` sem a nova prop), Autenticação, Propriedades e o próprio Dashboard revalidados via requests reais — resultados idênticos aos anteriores. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente.
