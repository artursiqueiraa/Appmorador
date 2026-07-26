# Relatório — Sprint 7 (Controle de Acesso Inteligente: Domínio)

**Data de conclusão**: 2026-07-21

## Resumo executivo

Construído o domínio completo de Controle de Acesso do AppMorador: `Credencial` (Facial/Tag RFID/
QR Code/PIN/Biometria/Chave Virtual, status Ativa/Suspensa/Expirada/Revogada), `PermissaoAcesso`
(dia da semana/horário/data por Ponto de Acesso) e `PontoAcesso` (pertence direto à Propriedade),
com `HistoricoCredencial` como auditoria interna preparada (sem tela). CRUD completo
(Create/List/Update/Delete) nos 3 níveis, exclusão lógica cascateada em todo o agregado
(`Propriedade` → `Unidade` → `Morador` → `Credencial` → `PermissaoAcesso`, mais `PontoAcesso` →
`PermissaoAcesso`), Dashboard com 4 contadores reais novos, e 3 telas mobile novas seguindo o
Design System já estabelecido. Zero comunicação com hardware físico — só o domínio de negócio.
Fase 1 (planejamento) apresentada e ajustada com o usuário — 2 decisões confirmadas antes de
qualquer código (ver `docs/sprints/SPRINT_007.md`). Fase 2 implementada em etapas pequenas, cada
uma validada com build/curl antes de avançar.

## Arquivos criados

**Backend**:
- `Domain/Entities/{Credencial,TipoCredencial,StatusCredencial,PontoAcesso,PermissaoAcesso,
  DiaSemana,HistoricoCredencial,TipoEventoHistorico}.cs`
- `Domain/Repositories/{ICredencialRepositorio,IPontoAcessoRepositorio,
  IPermissaoAcessoRepositorio,IHistoricoCredencialRepositorio}.cs`
- `Infrastructure/Persistence/{CredencialRepositorio,PontoAcessoRepositorio,
  PermissaoAcessoRepositorio,HistoricoCredencialRepositorio}.cs`
- `Application/Credenciais/{Dtos,ICredencialServico,CredencialServico}.cs`
- `Application/PontosAcesso/{Dtos,IPontoAcessoServico,PontoAcessoServico}.cs`
- `Application/PermissoesAcesso/{Dtos,IPermissaoAcessoServico,PermissaoAcessoServico}.cs`
- `Api/Controllers/{CredenciaisController,PontosAcessoController,PermissoesAcessoController}.cs`
- Migration `20260721020503_AdicionarControleDeAcesso`

**Mobile**:
- `screens/credenciais/CredenciaisScreen.tsx`, `screens/pontosAcesso/PontosAcessoScreen.tsx`,
  `screens/permissoes/PermissoesScreen.tsx`
- `components/TipoCredencialSelector.tsx`
- `screens/dashboard/CardControleAcesso.tsx`

**Documentação**:
- `docs/adr/0010-dominio-controle-acesso.md`
- `docs/sprints/SPRINT_007.md`, `docs/reviews/SPRINT_007.md` (este relatório)

## Arquivos modificados

**Backend**: `Infrastructure/Persistence/AppDbContext.cs` (4 DbSets, relacionamentos, query
filters), `Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (DI dos 4 repositórios + 3
serviços novos), `Application/Propriedades/PropriedadeServico.cs` (cascade expandido para
Credencial/PermissaoAcesso/PontoAcesso), `Application/Unidades/UnidadeServico.cs` (cascade
expandido para Credencial/PermissaoAcesso), `Application/Moradores/MoradorServico.cs` (cascade
expandido para Credencial/PermissaoAcesso), `Application/Dashboard/{DashboardResponse,
DashboardServico}.cs` (4 contadores novos).

**Mobile**: `api/types.ts` (novos DTOs: `CredencialResponse`, `PontoAcessoResponse`,
`PermissaoAcessoResponse`, tipos `TipoCredencial`/`StatusCredencial`/`DiaSemanaToken`; 4 campos
novos em `DashboardResponse`), `navigation/{types,RootNavigator}.tsx` (rotas Credenciais/
Permissoes/PontosAcesso; `propriedadeId` passa a ser propagado por toda a cadeia Unidades→
Moradores→Credenciais→Permissoes), `screens/unidades/UnidadesScreen.tsx` e `screens/moradores/
MoradoresScreen.tsx` (navegação para o próximo nível: Moradores→Credenciais),
`screens/SelecionarPropriedadeScreen.tsx` (atalho para Pontos de Acesso),
`screens/dashboard/DashboardScreen.tsx` (novo `CardControleAcesso`).

**Documentação**: `docs/CHANGELOG.md`, `docs/roadmap/ROADMAP.md`, `docs/ALTERACOES_BANCO.md`,
`docs/DIVIDA_TECNICA.md` (itens 12, 13 e 14), `docs/adr/README.md`.

## Arquitetura

Nenhuma camada nova — mesma Clean Architecture de sempre (`Domain → Application → Infrastructure
→ Api`). Ownership resolvido subindo a cadeia até `Propriedade.ProprietarioId` (agora também a
partir de `Credencial`/`PermissaoAcesso`/`PontoAcesso`), mesmo padrão já usado desde a Sprint 6.
Exclusão lógica via query filter global do EF Core (ADR 0009) reaproveitada sem alteração — as 3
entidades operacionais novas (`Credencial`/`PontoAcesso`/`PermissaoAcesso`) herdam
`EntidadeComSoftDelete`; `HistoricoCredencial` deliberadamente não herda (auditoria pura, mesmo
padrão de `RegistroEventoAlarme`). Decisão estrutural nova documentada em ADR 0010: `Credencial.
Status` como kill-switch geral + `PermissaoAcesso` como entidade de vínculo por Ponto de Acesso.

## Fluxos homologados (via requests reais)

- Login → criar Credencial (Facial) → criar 2 Pontos de Acesso (Portão Principal, Piscina) →
  criar 2 Permissões (uma com dias/horário restritos, outra sem restrição) → Dashboard reflete
  `quantidadeCredenciais=1`, `quantidadeCredenciaisAtivas=1`, `quantidadePontosAcesso=2` — 200/201
  em todos.
- Transição de status: Ativa → Suspensa → Ativa; Dashboard reage corretamente
  (`quantidadeCredenciaisAtivas`/`quantidadeCredenciaisSuspensas` trocam de lado).
- Validação de propriedade cruzada: criada uma 2ª propriedade (outro usuário) com seu próprio
  Ponto de Acesso; tentativa de vincular uma Permissão da credencial do 1º usuário a esse ponto →
  **404 "Ponto de acesso não encontrado"** (mesma mensagem genérica de "não existe" e "não é meu",
  nunca revela a existência do dado de outro dono).
- **Cascade em todos os níveis**, cada um confirmado via API (registro deixa de responder/
  aparecer em listagem após a exclusão do nível acima):
  - Excluir um `PontoAcesso` → sua `PermissaoAcesso` some da lista da credencial.
  - Excluir uma `Credencial` → suas `PermissaoAcesso` ficam inacessíveis (404).
  - Excluir um `Morador` → sua(s) `Credencial` fica(m) inacessível(is) (404) — valida
    especificamente a correção do bug do `AsNoTracking()` em `ListByMoradorAsync` (ver Errata).
  - Excluir uma `Unidade` → Credenciais/Permissões de todos os Moradores da unidade ficam
    inacessíveis; Dashboard reflete `quantidadeUnidades=0`/`quantidadePessoas=0`/
    `quantidadeCredenciais=0`.
  - Excluir uma `Propriedade` → toda a árvore (Unidade, Morador, Credencial, PermissaoAcesso,
    PontoAcesso) marcada excluída na mesma operação; propriedade some da listagem.
- Regressão: Login, Propriedades, Unidades, Moradores, Dashboard, Eventos — contratos inalterados.

## Evidências dos testes

- `dotnet build` (solução completa): 0 erros, 0 warnings em cada etapa (entidades+migration,
  serviços+cascade expandido, DI+controllers, Dashboard).
- `npx tsc --noEmit` (mobile): 0 erros após todas as telas/tipos novos.
- `npx expo export --platform web`: bundle Metro gerado com sucesso (2621 módulos, sem erro),
  confirmando que `CredenciaisScreen`/`PontosAcessoScreen`/`PermissoesScreen`/
  `CardControleAcesso` compilam e empacotam corretamente. Sem browser disponível neste ambiente
  para verificação visual direta — mesma limitação já registrada em Sprints anteriores.
- Migration revisada (resumo técnico: 100% `CreateTable`/`CreateIndex`, sem operação destrutiva)
  antes de `dotnet ef database update` contra o banco real — protocolo permanente desde a
  Sprint 3.1.
- **Errata corrigida antes de qualquer teste reportado como "passou"**: `ICredencialRepositorio.
  ListByMoradorAsync` foi escrito inicialmente com `.AsNoTracking()` (pensado só para exibição),
  mas `MoradorServico.DeleteAsync` também precisa dele para cascatear — `.AsNoTracking()` foi
  removido antes da primeira validação de exclusão em cascata de Morador (mesma classe de bug já
  identificada e corrigida na Sprint 6, desta vez prevenida em 3 dos 4 repositórios novos e
  corrigida no 4º antes de chegar a produção de teste).

## Pendências

Nenhuma bloqueadora. Registradas como dívida técnica (não implementadas por decisão de escopo):

- Item 12 (`DIVIDA_TECNICA.md`): soft delete não retrofitado em `Credencial`↔`HistoricoCredencial`
  — warning benigno do EF Core no startup, sem impacto funcional hoje.
- Item 13 (`DIVIDA_TECNICA.md`): mobile não expõe editor de `DataInicial`/`DataFinal` (vigência)
  na tela de Permissões — domínio/API já suportam, falta um date-picker no Design System (mesma
  decisão de "sem dependência nova" já tomada para o filtro de período da Central de Eventos).
- Item 14 (`DIVIDA_TECNICA.md`): integração real de hardware (Control iD/Intelbras/Hikvision/JFL)
  ainda não existe — fase futura própria, conforme pedido explicitamente pela missão.

## Atualizações de documentação

`docs/CHANGELOG.md` (nova entrada), `docs/roadmap/ROADMAP.md` (Sprint 7 concluída; backlog de
Integração de Controle de Acesso atualizado — domínio pronto, falta só os conectores),
`docs/ALTERACOES_BANCO.md` (nova migration documentada), `docs/DIVIDA_TECNICA.md` (itens 12, 13 e
14), `docs/adr/0010-dominio-controle-acesso.md` + índice atualizado, `docs/sprints/SPRINT_007.md`
(especificação), este relatório.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Clean Architecture respeitada em todas as camadas; nenhuma solução paralela; ownership e soft delete reaproveitam o padrão já estabelecido (ADR 0009), não um mecanismo novo. A decisão estrutural real (modelo Credencial/PermissaoAcesso/PontoAcesso) foi documentada em ADR 0010 com alternativas consideradas, não implícita no código. |
| **Segurança** | ✅ Aprovado. Ownership testado explicitamente entre usuários diferentes (404 "Ponto de acesso não encontrado" ao tentar vincular permissão a ponto de outra propriedade, sem vazar existência) e em todos os níveis de cascade. `Credencial.Tipo` imutável reduz superfície de ambiguidade. |
| **Produto** | ✅ Aprovado. Fluxo completo pedido (propriedade → unidade → morador → credencial → permissões → pontos de acesso → Dashboard) homologado via requests reais, com dado real persistido. Nada de "Fora de Escopo" foi implementado (confirmado: sem TCP/HTTP com equipamento, sem SDK, sem facial/QR/tag/biometria reais). |
| **UX/UI** | ✅ Aprovado. Loading (Skeleton), Empty State (copy tranquilizadora), validação (botão desabilitado até campo obrigatório), feedback de erro (mensagem inline) presentes nas 3 telas novas. Confirmação antes de revogar credencial implementada via `Alert.alert` com a opção "Revogada" destacada como destrutiva — nunca uma transição de status silenciosa. Confirmação antes de excluir também presente em Credenciais/Permissões/Pontos de Acesso. |
| **Performance** | ✅ Aprovado. Contadores do Dashboard via `CountByPropriedadeAsync` dedicado (sem carregar listas inteiras); cascade de exclusão usa exatamente as consultas necessárias por nível, sem consulta redundante. |
| **Manutenibilidade** | ✅ Aprovado. Repositórios com métodos `ListByX` específicos por granularidade de cascade (Morador/Unidade/Propriedade/PontoAcesso) tornam explícito qual nível cada consulta serve; padrão de ownership e soft delete idêntico ao já usado, reduz curva de aprendizado. |
| **Documentação** | ✅ Aprovado. ADR 0010 documenta a decisão estrutural com alternativas consideradas; 3 itens novos de dívida técnica registram os recortes de escopo conscientes; CHANGELOG/ROADMAP refletem exatamente o que foi entregue, incluindo a atualização do backlog de integração de hardware. |
| **Regressões** | ✅ Nenhuma. Login, Propriedades, Unidades, Moradores, Dashboard e Eventos revalidados via requests reais após toda a implementação — resultados idênticos aos anteriores. Um bug real (mesma classe do já visto na Sprint 6 — `AsNoTracking()` em repositório de cascade) foi encontrado e corrigido durante a própria integração, antes de qualquer teste ser reportado como concluído. |

**Conclusão**: Sprint aprovada, sem pendência bloqueadora. Critérios de aceite da missão
atendidos integralmente — CRUD funcional nos 3 níveis, Dashboard com dado real, soft delete
funcionando em cascade completo, sem regressão, documentação completa.
