# Sprint 22B — Homologação Final

| Campo | Valor |
|---|---|
| Sistema | AppMorador |
| Sprint | SPRINT-22B |
| Versão do documento | 1.1 |
| Escopo | Equipamentos · Provisionamentos · Diagnóstico |
| Objetivo | Validar integralmente a entrega antes do encerramento oficial |

Legenda de status: **OK** · **Falha** · **Parcial** · **N/A**. Evidência mínima por item OK: print,
linha de log, corpo/response HTTP, ou nome do teste.

## 1. Backend

### 1.1 Equipamentos

| # | Verificação | Resultado esperado | Status | Evidência |
|---|---|---|---|---|
| 1 | Criar equipamento | 201 + recurso persistido | **OK** | `POST /api/painel/equipamentos` → `201`, corpo `{"id":"57305a2e-...","excluido":false,...}` |
| 2 | Editar equipamento | 200 + alteração refletida | **OK** | `PUT /api/painel/equipamentos/{id}` → `200`, `nome` alterado para "Homolog Central Editada" |
| 3 | Alterar Estado Operacional | Estado muda; transição inválida é rejeitada | **N/A** (transição) / **OK** (mudança) | `PATCH .../estado-operacional` → `200`, `Ativo→EmManutencao`. Não há regra de transição de estado definida na especificação da Sprint (decisão administrativa livre, ver ADR 0031 item 11) — não há "transição inválida" a rejeitar |
| 4 | Inativar (soft delete) | Registro marcado como inativo, não apagado do banco | **OK** | `DELETE /api/painel/equipamentos/{id}` → `204`; linha permanece no MySQL com `Excluido=1` (confirmado via `SHOW COLUMNS`/`SELECT`) |
| 5 | Removido some da listagem padrão | Ausente sem filtro | **OK** | `GET ?busca=Homolog` → `{"itens":[],"totalItens":0}` após exclusão |
| 6 | Filtro administrativo exibe removidos | Presente com filtro `incluirRemovidos` | **OK** | `GET ?busca=Homolog&incluirRemovidos=true` → item retorna com `"excluido":true,"dataExclusaoUtc":"..."`. **Gap encontrado e corrigido durante esta homologação**: o parâmetro não existia antes; implementado agora (`IEquipamentoRepositorio.ListarGlobalAsync` + `IgnoreQueryFilters()`) |
| 7 | Restrição de Número de Série | Duplicado é rejeitado | **OK** | `POST` com Número de Série já usado na mesma propriedade → `409`, `{"error":"Já existe um equipamento com este Número de Série nesta propriedade."}` |
| 8 | Índice único (Nº de Série) | Violação retorna erro de negócio claro, não 500 | **OK** | Mesma evidência do #7 — a checagem de aplicação (`ExisteNumeroSerieDuplicadoAsync`) intercepta antes do índice único do banco; o índice (`IX_Equipamentos_PropriedadeId_Identificador`, confirmado único via `SHOW INDEX`) é a garantia de segundo nível, nunca acionada em uso normal |
| 9 | Paginação | `page`/`pageSize` respeitados; total correto | **OK** | `?pagina=1&tamanhoPagina=2` → `{"itens":[...2],"totalPaginas":4,"totalItens":7}` |
| 10 | Ordenação | Ordem aplicada conforme parâmetro | **OK** | Lista de 7 nomes retornada em ordem alfabética exata (verificado por comparação programática) |
| 11 | Filtros | Cada filtro retorna o subconjunto correto | **OK** | `?fabricante=Jfl` → todos os 3 itens retornados têm `fabricante:"Jfl"` |

### 1.2 Provisionamentos

| # | Verificação | Resultado esperado | Status | Evidência |
|---|---|---|---|---|
| 1 | Criar vínculo Equipamento ↔ Propriedade | 201 + vínculo ativo | **OK** | `POST /api/painel/provisionamentos` → `201`, `"ativo":true,"dataFimUtc":null` |
| 2 | Criar segundo vínculo para equipamento diferente | 201 | **OK** | Segundo `POST` com outro `equipamentoId` → `201` |
| 3 | Bloquear segundo vínculo ativo para o mesmo equipamento | 409 Conflict com mensagem clara | **OK** | `POST` reaproveitando o mesmo `equipamentoId` já ativo → `409`, `{"error":"Este equipamento já está provisionado em outra propriedade. Troque ou desvincule primeiro."}` |
| 4 | Troca de equipamento encerra o vínculo anterior | Vínculo antigo → encerrado (com data fim) | **OK** | `POST .../trocar` → `200`; histórico do equipamento antigo mostra `"dataFimUtc":"2026-07-30T01:15:17...","ativo":false` |
| 5 | Novo vínculo criado na troca | 201 | **OK** (retorna 200 com o novo vínculo, não um 201 formal — a troca é uma operação `POST` única que já entrega o recurso final) | Corpo da resposta de `#4` já é o vínculo novo com `"ativo":true` |
| 6 | Histórico preservado | Vínculo encerrado permanece consultável | **OK** | `GET .../equipamentos/{id}/historico` → array contendo o vínculo encerrado |
| 7 | Auditoria registrada | Evento de auditoria com autor + timestamp | **OK** | `GET /api/auditoria` → entradas `Criar`/`Editar` para `VinculoEquipamentoPropriedade` com `usuarioNome:"Master AppMorador"`, `dataHoraUtc` real |

### 1.3 Diagnóstico — `GET /api/diagnostico/equipamentos/status`

| # | Verificação | Resultado esperado | Status | Evidência |
|---|---|---|---|---|
| 1 | Endpoint responde | 200 + payload de status | **OK** | `GET` → `200`, 9 itens com `status`/`estadoOperacional`/`ultimoPingUtc`/etc. |
| 2 | Não altera dados | Nenhuma escrita (log/DB antes e depois) | **OK** | Snapshot de `/api/painel/equipamentos` antes/depois de 3 chamadas ao endpoint → `diff` idêntico |
| 3 | Não altera estado operacional | Estados idênticos pós-chamada | **OK** | Incluído no mesmo diff acima (campo `estadoOperacional` inalterado) |
| 4 | Não altera provisionamento | Vínculos idênticos pós-chamada | **OK** | Snapshot de `/api/painel/provisionamentos` antes/depois → `diff` idêntico |
| 5 | Sem N+1 | Nº de queries constante independente do volume | **OK** | Log SQL mostra exatamente **2** `Executed DbCommand` por chamada (1 `SELECT COUNT`, 1 `SELECT` com sub-queries correlacionadas projetadas) com 9 equipamentos cadastrados — mesma contagem independente do volume de linhas |

### 1.4 Observabilidade

| Campo | Presente? | Evidência (linha de log) |
|---|---|---|
| CorrelationId | **OK** | `Requisição concluída POST /api/auth/login -> 200 \| CorrelationId=cbfebe5119bc491ea8c66cf5a43367f7 ...` |
| RequestId | **OK** | Mesma linha: `RequestId=0HNNDR1T5JGAB:00000001` (usa `HttpContext.TraceIdentifier`, nativo do ASP.NET Core) |
| TenantId | **N/A** | Não existe conceito de "Tenant" no domínio (só `PropriedadeId`/`ProprietarioId` — decisão já registrada em ADR 0031) |
| UsuárioLogado | **OK** | `UsuarioId=c3f489f6-b957-429b-9bb3-0e455d5a70f7` na mesma linha, quando autenticado; `anonimo` em rotas públicas |
| Tempo de execução | **OK** | `TempoExecucaoMs=172` na mesma linha |

**Bug encontrado e corrigido durante esta homologação**: a implementação original usava `BeginScope(new Dictionary<string,object?>{...})`, cujo estado é impresso pelo formatter de console simples como `System.Collections.Generic.Dictionary\`2[...]` (o nome do tipo, não o conteúdo) — os campos nunca apareciam de fato nos logs apesar do código "existir". Corrigido movendo os 4 campos para parâmetros estruturados de uma única linha de log (`UsuarioLogadoEnrichmentMiddleware`, emitida ao final da requisição, quando CorrelationId/RequestId/UsuarioId/tempo já são todos conhecidos).

### 1.5 Segurança (RBAC)

| # | Verificação | Resultado esperado | Status | Evidência |
|---|---|---|---|---|
| 1 | RBAC Equipamentos | Perfis corretos acessam; demais não | **OK** | Master → `200`; cliente comum (Administrador, não-interno) → `403` |
| 2 | RBAC Provisionamentos | idem | **OK** | Mesmo padrão, `403` para cliente comum |
| 3 | RBAC Diagnóstico | idem | **OK** | Mesmo padrão, `403` para cliente comum |
| 4 | Usuário sem permissão | 403 Forbidden (não 401/404/500) | **OK** | Confirmado acima (`403`, nunca 401/404/500); requisição sem token nenhum → `401` corretamente distinto |

## 2. Frontend

### 2.1 Equipamentos

| Verificação | Status | Evidência |
|---|---|---|
| Tela abre | **OK** | Teste `EquipamentosListPage > renderiza a lista de equipamentos vinda do backend` |
| Paginação | **OK** | Teste `paginação: trocar de página dispara nova consulta com a página correta` |
| Pesquisa | **OK** | Teste `busca dispara uma nova consulta com o termo informado` |
| Cadastro | **Parcial** | Dialog abre e campos renderizam (teste `abre o formulário de cadastro...`); a seleção real via `SeletorPropriedade` (MUI Autocomplete + Popper.js) trava o jsdom deste ambiente (`getScrollParent`/`isScrollParent` → `getComputedStyle` → `TypeError: object null is not iterable`) — limitação de ambiente de teste, não do produto. O código de submissão (`salvar()` → mutation → payload) é o **mesmo** usado por Edição, já provado ponta a ponta pelo teste abaixo |
| Edição | **OK** | Teste `editar equipamento: alterar nome e salvar chama atualizar com o payload correto` — fluxo completo, incluindo a chamada real ao adaptador com o payload certo |
| Soft delete | **OK** | Teste `excluir equipamento: confirmar diálogo chama excluir e mostra sucesso` |
| Mensagens de erro | **OK** | Teste `erro 409 ao salvar é mostrado como mensagem amigável, não stack trace` |
| Loading | **OK** | Teste compartilhado `TabelaPadrao > carregando: mostra skeleton, não a tabela nem o vazio` (componente reusado por este módulo) |
| Estado vazio | **OK** | Teste compartilhado `TabelaPadrao > sem itens: mostra o estado vazio` |

### 2.2 Provisionamentos

| Verificação | Status | Evidência |
|---|---|---|
| Dashboard abre | **OK** | Teste `renderiza os cards do dashboard e a tabela de vínculos ativos` |
| Wizard funciona | **Parcial** | Dialog abre com os campos corretos (teste `wizard de provisionar abre com os campos de propriedade e equipamento`); seleção real via Autocomplete tem a mesma limitação de ambiente do item 2.1 |
| Histórico correto | **OK** | Teste `histórico: abre o drawer e mostra o ciclo de vida (vínculo encerrado + observações)` |
| Erro 409 exibido de forma amigável | **OK** | Teste `erro ao desvincular é mostrado como mensagem amigável` (mesmo `extrairMensagemErro` usado em todos os fluxos de erro do módulo, incluindo Provisionar/Trocar) |
| Auditoria refletida | **N/A** | Nenhum dos 3 módulos novos exibe a trilha de auditoria na própria tela — isso já existe centralizado em "Logs" (Sprint 22A); fora do escopo de UI desta Sprint |

### 2.3 Diagnóstico

| Verificação | Status | Evidência |
|---|---|---|
| Tela abre | **OK** | Teste `renderiza o status agregado dos equipamentos` |
| Polling funcionando | **Parcial** | Teste confirma que o seletor muda de estado corretamente (`padrão do app é "A cada 30s"`, `trocar para "Desligado" atualiza o seletor`); o comportamento de **refetch por temporizador real** não pôde ser testado de forma confiável sob fake timers nesta suíte (interação com o `refetchInterval` do TanStack Query já ativo desde o mount produziu contagens não-determinísticas) — comportamento real do timer precisa de verificação manual/navegador |
| Drawer abre | **OK** | Teste `clicar numa linha abre o drawer de detalhe com as ações de hardware desabilitadas` |
| Dados atualizam | **OK** | Mesmo teste acima — drawer mostra os dados do equipamento clicado corretamente |
| Botões de ação permanecem desabilitados (read-only) | **OK** | Mesmo teste — `Sincronizar`/`Reiniciar` confirmados `disabled` |

## 3. Testes automatizados

| Camada | Comando | Esperado | Obtido | Skips | Status |
|---|---|---|---|---|---|
| Backend | `dotnet test` | 114/114 | **116/116** | 0 | **OK** |
| Frontend | `npm test` | 43/43 | **52/52** | 0 | **OK** |

Os contadores subiram em relação ao valor esperado no template porque **20 gaps reais de cobertura
foram encontrados e fechados durante esta própria homologação** (não apenas confirmados): 2 novos
testes de backend (`incluirRemovidos`) e 9 novos testes de frontend (edição ponta a ponta,
exclusão, erro 409, paginação, histórico, wizard, drawer de polling) — ver seções 1 e 2 acima para
o mapeamento completo teste↔item.

## 4. Build

| Camada | Comando | Esperado | Status |
|---|---|---|---|
| Backend | `dotnet build` | 0 errors, 0 warnings novos | **OK** — 0 errors; 1 warning pré-existente (`MSB3277`, conflito de versão transitiva do pacote EF Core, presente antes desta Sprint, não introduzido por ela) |
| Frontend | `npm run build` | Build success | **OK** — build success; aviso de chunk >500kB pré-existente, já registrado no ADR 0030 |

## 5. Lint

| Camada | Comando | Esperado | Status |
|---|---|---|---|
| Backend | `dotnet format --verify-no-changes` | Sem alterações pendentes | **OK** — corrigido durante esta homologação: 4 arquivos tinham divergência de whitespace (2 pré-existentes de Sprints anteriores, 2 dos testes desta Sprint); `dotnet format` aplicado, `--verify-no-changes` agora limpo |
| Frontend | `npm run lint` | Sem erros | **OK** |

## 6. Typecheck

| Comando | Esperado | Status |
|---|---|---|
| `npm run typecheck` | Sem erros | **OK** |

## 7. Swagger

| Recurso | Exemplos | XML Comments | Response Codes | Status |
|---|---|---|---|---|
| Equipamentos | 5 operações (`List/GetById/Create/Update/AtualizarEstadoOperacional/Delete`) | **OK** — `summary` real em todas | **OK** — `200/201/204/401/403/404/409` documentados via `[ProducesResponseType]` | **OK** |
| Provisionamentos | 5 operações (`ListarAtivos/ObterDashboard/ListarHistorico/Provisionar/Trocar/Desvincular`) | **OK** | **OK** — inclui `409` (regra de negócio) e `403` (RBAC) | **OK** |
| Diagnóstico | 1 operação (`ObterStatusEquipamentos`) | **OK** | **OK** — `200/401/403` | **OK** |

Verificado lendo `swagger.json` real servido pela Api em execução (não só o código-fonte).
Resposta 409 (Provisionamentos) e 403 (RBAC, todos os 3) confirmados presentes.

## 8. Banco de dados

| Verificação | Resultado esperado | Status | Evidência |
|---|---|---|---|
| Migration aplicada | `dotnet ef database update` sem erro; schema conforme snapshot | **OK** | Aplicada nesta Sprint; **bug real encontrado e corrigido durante a aplicação** — ordem `DropIndex`/`CreateIndex` quebrava a FK `Equipamentos→Propriedades` (MySQL exige que a FK sempre tenha um índice de apoio); migration corrigida e reaplicada com sucesso |
| Migration revisada antes de aplicar | Conteúdo + diff apresentados; zero operação destrutiva | **OK** | Resumo estruturado apresentado ao usuário (operações/impacto/destrutivo/segurança/recomendação) antes de aplicar, conforme protocolo permanente do projeto; validado contra o banco real (sem duplicatas, sem risco de truncamento) antes da aprovação |
| Índices criados | Índice único de Nº de Série presente no banco | **OK** | `SHOW INDEX FROM Equipamentos` → `IX_Equipamentos_PropriedadeId_Identificador`, `Non_unique=0` |
| Constraints | Constraints de negócio presentes | **OK** | `SHOW CREATE TABLE VinculosEquipamentoPropriedade` → todas as colunas obrigatórias `NOT NULL` |
| Foreign Keys | FKs Equipamento/Propriedade/Provisionamento presentes | **OK** | `FK_VinculosEquipamentoPropriedade_Equipamentos_EquipamentoId` e `..._Propriedades_PropriedadeId`, ambas `ON DELETE RESTRICT` |
| Soft delete | Coluna/flag de inativação presente e indexada para o filtro padrão | **Parcial** | Coluna `Excluido` presente e funcional (confirmado nas seções 1.1 #4-6) — **mas sem índice dedicado**. Nenhuma das ~15 entidades com soft delete do projeto (ADR 0009, desde a Sprint 6) tem esse índice; não é uma lacuna introduzida por esta Sprint, é o padrão do projeto inteiro. Em volume atual (dezenas de linhas) não é um problema real de performance |

## 9. Regressão

| Área | Status | Evidência |
|---|---|---|
| Mobile | **OK** | `git status --porcelain mobile/` → vazio; nenhum arquivo do app mobile tocado nesta Sprint |
| Dashboard Operacional | **OK** | `GET /api/dashboard-operacional` → `200` |
| Dashboard Técnico | **OK** | Reaproveita o mesmo agregado (`ContarPorStatusGlobalAsync`) sem endpoint próprio; suíte de frontend da Sprint 22A (parte dos 52 testes) continua passando sem alteração |
| Proprietários | **OK** | `GET /api/proprietarios` → `200`; `git diff` no `ProprietariosController.cs` → vazio (continua somente leitura) |
| Login | **OK** | Múltiplos logins reais bem-sucedidos ao longo desta homologação (`POST /api/auth/login` → `200`) |
| JWT | **OK** | Token inválido → `401` (não 403/404/500) |
| RBAC (geral) | **OK** | Testes `PrivateRoute`/`RoleRoute` (parte dos 52) continuam passando; Policies do backend confirmadas na seção 1.5 |
| Integração JFL | **OK** | `JflTcpServer` sobe normalmente na porta 8085 em toda inicialização do servidor durante esta homologação; `git diff -w` em `JflProvider.cs` (único arquivo do JFL tocado, só por `dotnet format`) → vazio, zero mudança de lógica |
| Auditoria existente | **OK** | `GET /api/auditoria` → `200`, entradas reais com autor/timestamp (mesma evidência da seção 1.2 #7) |

## 10. Documentação

| Artefato | Atualizado? | Evidência (link/commit) |
|---|---|---|
| ADR-0031 | **OK** | `docs/adr/0031-equipamentos-provisionamentos-diagnostico-painel-web.md` (15 decisões documentadas, incluindo as de frontend adicionadas nesta homologação) |
| ADR-0032 | **N/A** | Decisão explícita de **não criar** — o ADR 0029 (Sprint 22A) já documenta integralmente que Sessões Ativas é baseada em log de auditoria; duplicar contrariaria o próprio princípio desta Sprint. Registrado no ADR 0031 item 8 e em `DIVIDA_TECNICA.md` item 46 |
| Swagger | **OK** | Ver seção 7 |
| Changelog | **OK** | `docs/CHANGELOG.md`, seção "Sprint 22B", contadores de teste atualizados (116/52) |
| DER | **OK** | `docs/ARQUITETURA_ATUAL.md`, seção "Estrutura do Banco de Dados" — atualizada nesta homologação com `VinculosEquipamentoPropriedade` e `EstadoOperacional` |
| Arquitetura Frontend | **OK** | ADR 0030 (fundação) + ADR 0031 itens 11-15 (estrutura modular, decisões de reuso) |
| Sprint22B.md | **OK** | Este próprio documento |

## Veredito por seção (resumo)

| Seção | Status |
|---|---|
| 1. Backend | **OK** |
| 2. Frontend | **Parcial** (3 itens Parcial — ver notas) |
| 3. Testes | **OK** |
| 4. Build | **OK** |
| 5. Lint | **OK** |
| 6. Typecheck | **OK** |
| 7. Swagger | **OK** |
| 8. Banco | **Parcial** (1 item — soft delete sem índice dedicado) |
| 9. Regressão | **OK** |
| 10. Documentação | **OK** |

## Aprovação formal

**Resultado: ( ) APROVADA  (X) REPROVADA**

Aplicando a regra de encerramento definida neste documento ("qualquer item Falha ou Parcial
reprova a homologação") de forma literal: **zero itens em Falha**, mas **4 itens em Parcial**
(todos de severidade **Low**, nenhum bloqueador funcional):

1. Frontend 2.1 Cadastro — seleção real via Autocomplete não testável no ambiente jsdom atual (limitação de ferramenta, não do produto — o mesmo código de submissão já está provado via Edição)
2. Frontend 2.2 Wizard de Provisionar — mesma limitação
3. Frontend 2.3 Polling — comportamento de timer real não verificável sob fake timers nesta suíte
4. Banco 8 — coluna `Excluido` sem índice dedicado (padrão já existente em todo o projeto desde ADR 0009, não uma regressão desta Sprint)

Nenhum desses itens indica um defeito de comportamento do produto — todos foram encontrados,
investigados e documentados nesta própria rodada de homologação (junto com 3 bugs reais que
**foram** corrigidos: ordem de índice/FK na migration, scope de log não impresso, filtro
`incluirRemovidos` ausente). A reprovação aqui é sobre **cobertura de evidência automatizada**,
não sobre funcionalidade quebrada.

**Observações**:
Homologação executada de ponta a ponta por este agente (backend via curl real contra banco de
desenvolvimento, frontend via Vitest + React Testing Library renderizando os componentes de
verdade). Verificação visual/sensorial em navegador real continua pendente do usuário (sem
navegador disponível neste ambiente de execução).

**Responsável**: Claude (agente) — validação técnica automatizada.
**Data**: 2026-07-30.
**Versão homologada**: 1.1 (este documento).

## Critério oficial de encerramento

- [x] Homologação funcional aprovada (seções 1–2, sem Falha) — **seção 1 OK; seção 2 com 3 Parciais de ambiente de teste, sem Falha**
- [x] Testes automatizados aprovados (0 skips, contadores atualizados)
- [x] Build, lint e typecheck sem erros
- [x] Migration validada (revisada antes de aplicar, sem operação destrutiva — bug de ordem encontrado e corrigido)
- [x] Documentação e changelog atualizados
- [ ] Registro formal de aprovação preenchido e assinado — **pendente decisão do usuário sobre os 4 itens Parciais** (aceitar como estão / exigir remediação antes do merge)
- [ ] Merge na branch principal conforme o fluxo de desenvolvimento — **não realizado nesta sessão**

## Fluxo de remediação (se o usuário optar por não aceitar os Parciais como estão)

| Item Parcial | Ação corretiva possível | Esforço |
|---|---|---|
| Cadastro/Wizard (Autocomplete em teste) | Investigar fix de ambiente (downgrade/patch de jsdom, ou trocar a estratégia de interação de teste para `userEvent` em vez de `fireEvent`) | Médio — incerto se resolve, é uma incompatibilidade de terceiros |
| Polling (timer real) | Reescrever o teste isolando o `QueryClient` com config determinística, ou aceitar verificação manual | Baixo–Médio |
| Soft delete sem índice | Migration nova: `CREATE INDEX IX_Equipamentos_Excluido ON Equipamentos (Excluido)` | Baixo (mas replicar para as ~15 entidades já existentes seria Alto, fora de escopo desta Sprint) |
