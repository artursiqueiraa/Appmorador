# Relatório — Sprint 6 (Domínio do Produto: Propriedades, Unidades e Moradores)

**Data de conclusão**: 2026-07-21

## Resumo executivo

Estabelecido o agregado principal do AppMorador: `Propriedade` → `Unidade` → `Morador`, com CRUD
completo nos 3 níveis (Create/List/Update/Delete), exclusão lógica em vez de física (soft delete,
ADR 0009), Dashboard passando a exibir contadores reais (antes `quantidadePessoas` era sempre
`1`, hardcoded), e telas mobile novas seguindo o Design System já estabelecido. Fase 1
(planejamento) apresentada e ajustada com o usuário — 4 decisões de arquitetura confirmadas antes
de qualquer código (ver `docs/sprints/SPRINT_006.md`). Fase 2 implementada em 12 etapas pequenas,
cada uma validada com build/curl antes de avançar.

## Arquivos criados

**Backend**:
- `Domain/Common/EntidadeComSoftDelete.cs`, `Domain/Entities/{Unidade,TipoUnidade,Morador,StatusMorador}.cs`
- `Domain/Repositories/{IUnidadeRepositorio,IMoradorRepositorio}.cs`
- `Infrastructure/Persistence/{UnidadeRepositorio,MoradorRepositorio}.cs`
- `Application/Unidades/{Dtos,IUnidadeServico,UnidadeServico}.cs`
- `Application/Moradores/{Dtos,IMoradorServico,MoradorServico}.cs`
- `Api/Controllers/{UnidadesController,MoradoresController}.cs`
- Migration `20260721013056_AdicionarUnidadesEMoradoresESoftDelete`

**Mobile**:
- `screens/unidades/UnidadesScreen.tsx`, `screens/moradores/MoradoresScreen.tsx`
- `components/TipoUnidadeSelector.tsx`

**Documentação**:
- `docs/adr/0009-soft-delete-dominio-principal.md`
- `docs/sprints/SPRINT_006.md`, `docs/reviews/SPRINT_006.md` (este relatório)

## Arquivos modificados

**Backend**: `Domain/Entities/Propriedade.cs` (herda soft delete), `Infrastructure/Persistence/AppDbContext.cs`
(DbSets, relacionamentos, query filters), `Infrastructure/Identity/AuthServiceCollectionExtensions.cs`
(DI), `Infrastructure/Persistence/Seed/DevelopmentSeeder.cs` (1 Unidade + 2 Moradores de exemplo),
`Application/Common/Result.cs` (variante não-genérica para operações sem retorno), `Application/Propriedades/{IPropriedadeServico,PropriedadeServico}.cs`
(Delete + cascade), `Application/Dashboard/{DashboardResponse,DashboardServico}.cs` (contadores
reais), `Api/Controllers/PropertiesController.cs` (Delete).

**Mobile**: `api/{types,client}.ts` (novos DTOs + método `delete`), `navigation/{types,RootNavigator}.tsx`
(rotas Unidades/Moradores), `screens/SelecionarPropriedadeScreen.tsx` (editar/excluir/total/atalho
Unidades), `screens/dashboard/{DashboardScreen,CardResumoInstalacao}.tsx` (contadores reais +
correção do cálculo de "instalação vazia").

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (itens 10 e 11), `docs/adr/README.md`.

## Arquitetura

Nenhuma camada nova — mesma Clean Architecture de sempre (`Domain → Application → Infrastructure
→ Api`). Ownership resolvido subindo a cadeia até `Propriedade.ProprietarioId`, mesmo padrão já
usado por `DashboardServico`/`EventosServico`. Único mecanismo novo: exclusão lógica via query
filter global do EF Core (ADR 0009) — decisão estrutural documentada, não implícita.

## Fluxos homologados (via requests reais)

- Login → criar Unidade → listar Unidades → criar Morador → listar Moradores → 200/201 em todos.
- Atualizar Morador (nome + status) → 200, dado persistido corretamente.
- Excluir Morador → 204; some da listagem (200, lista vazia); **confirmado direto no banco**:
  linha continua existindo, `Excluido=1`, `DataExclusaoUtc`/`ExcluidoPorUsuarioId` preenchidos.
- Excluir Unidade → 204; some da listagem; mesma verificação de persistência no banco.
- **Cascade completo**: criar Propriedade → Unidade → Morador descartáveis; excluir a
  Propriedade; confirmado no banco que Propriedade **e** Unidade **e** Morador ficaram todos
  `Excluido=1` na mesma operação.
- Dashboard antes/depois de criar Unidade+2 Moradores: `quantidadeUnidades` 0→1,
  `quantidadePessoas` 0→2, confirmado via `curl`.
- Ownership entre usuários: usuário B tentando acessar Unidades da propriedade do usuário A →
  404 "Propriedade não encontrada" (sem vazar existência).
- Swagger, Eventos, Propriedades (regressão) → todos 200, contratos inalterados.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada uma das 12 etapas.
- `npx tsc --noEmit`: limpo em cada etapa mobile.
- Migration revisada (resumo técnico: 100% `AddColumn`/`CreateTable`/`CreateIndex`, sem operação
  destrutiva) antes de `dotnet ef database update` contra o banco real — protocolo permanente
  desde a Sprint 3.1.
- Bundle web do Metro: compila e serve integralmente (8,7 MB, sem stack trace de erro), com
  `UnidadesScreen`/`MoradoresScreen` presentes no bundle gerado.
- Achado durante a implementação (corrigido antes da entrega, não chegou a afetar nenhum teste
  reportado como "passou"): repositórios de listagem usados também para cascade de exclusão
  precisaram ficar sem `AsNoTracking()` (senão `SaveChangesAsync` não persistiria as mudanças) —
  identificado e corrigido antes da primeira validação de exclusão em cascata.

## Pendências

Nenhuma bloqueadora. Registradas como dívida técnica (não implementadas por decisão de escopo):

- Item 10 (`DIVIDA_TECNICA.md`): soft delete não retrofitado em `Central`/`Zona`/`Camera`/
  `Gravador`/`Ocorrencia` — gera um warning benigno do EF Core no startup, sem impacto funcional
  hoje (confirmado: nenhum código faz `.Include(x => x.Propriedade)` nessas entidades).
- Item 11 (`DIVIDA_TECNICA.md`): Lixeira/Restauração de registros excluídos — arquitetura
  preparada (query filters + `.IgnoreQueryFilters()` disponível), tela/endpoint não implementados,
  conforme pedido explicitamente.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 6 concluída; backlog de
Visitantes/Entregas/Veículos/Reconhecimento facial atualizado com o vínculo natural a Unidade/
Morador agora disponível), `docs/ALTERACOES_BANCO.md` (nova migration documentada),
`docs/DIVIDA_TECNICA.md` (itens 10 e 11), `docs/adr/0009-soft-delete-dominio-principal.md` +
índice atualizado, `docs/sprints/SPRINT_006.md` (especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Clean Architecture respeitada em todas as camadas; nenhuma solução paralela; ownership resolvido reaproveitando o padrão já estabelecido, não um mecanismo novo. Decisão estrutural real (soft delete) documentada em ADR, não implícita no código. |
| **Segurança** | ✅ Aprovado. Ownership testado explicitamente entre usuários diferentes (404, sem vazamento de existência) em todos os 3 níveis novos; nenhuma superfície de ataque nova; exclusão lógica reduz risco de perda de dado por engano do usuário. |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (criar propriedade → unidade → moradores → ver no Dashboard) homologado via requests reais, com dado real persistido, sem dado fictício na interface. Nada de "Fora de Escopo" foi implementado. |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State (copy tranquilizadora, mesmo padrão já estabelecido), validação (botão desabilitado até campo obrigatório preenchido), feedback de erro (mensagem inline) e confirmação antes de excluir (`Alert.alert`, nunca exclusão silenciosa) presentes em Unidades e Moradores. |
| **Performance** | ✅ Aprovado. Contagens do Dashboard via `CountAsync` dedicado (sem carregar listas inteiras); nenhum N+1 introduzido; cascade de exclusão usa exatamente as consultas necessárias (unidades/moradores da propriedade), sem consulta redundante. |
| **Manutenibilidade** | ✅ Aprovado. `EntidadeComSoftDelete` evita repetir 3 campos em 3 entidades; query filter global evita que todo repositório novo precise lembrar de filtrar `Excluido` manualmente; padrão de ownership idêntico ao já usado, reduz curva de aprendizado para quem for ler o código depois. |
| **Documentação** | ✅ Aprovado. ADR 0009 documenta a decisão estrutural com alternativas consideradas; 2 itens novos de dívida técnica registram os 2 recortes de escopo conscientes (sem soft delete retrofitado, sem Lixeira ainda); CHANGELOG/ROADMAP refletem exatamente o que foi entregue. |
| **Regressões** | ✅ Nenhuma. Login, Propriedades, Dashboard e Eventos revalidados via requests reais após toda a implementação — resultados idênticos aos anteriores. Um bug real foi encontrado e corrigido durante a própria integração (condição de "instalação vazia" do Dashboard não considerava Unidades/Moradores) — não chegou a ser reportado como concluído antes da correção. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — CRUD funcional nos 3 níveis, Dashboard com dado real, sem regressão,
documentação completa.
