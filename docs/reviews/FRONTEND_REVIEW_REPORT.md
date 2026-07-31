# Frontend Review Report — Painel Web (Sprint 22C.1)

**Data**: 2026-07-30
**Escopo**: Sprint 22C.1 — Web UI Review, Stabilization and Visual Validation. Nenhuma
funcionalidade nova foi implementada; nenhum redesign, troca de layout, modernização de
componente, mudança de cor ou de identidade visual foi feita. Este documento é uma auditoria do
estado real do frontend existente (PainelWeb), validado com o backend rodando de verdade (não
mocks) e navegado num navegador Chromium real via Playwright.

## Resultado resumido

**O frontend já estava funcional.** `npm install`, `npm run dev`, `npm run build`,
`npm run typecheck` e `npm run lint` rodaram todos sem nenhum erro antes de qualquer intervenção
— consequência direta do rigor de validação já aplicado na Sprint 22B (build/lint/typecheck/testes
como critério de encerramento daquela Sprint). **Nenhum import quebrado, nenhuma rota inválida e
nenhum componente que impedisse a aplicação de abrir foram encontrados.** Por isso, esta Sprint
não teve nenhuma correção de compilação para fazer — o trabalho real foi navegar a aplicação de
ponta a ponta, com um usuário real, contra o backend real, e documentar o que existe.

Isso **não** significa que não há problemas: a navegação real encontrou 2 bugs de comportamento
(não de compilação) que passam despercebidos em `npm run build`/testes automatizados porque só
aparecem ao logar com o papel errado — ver seção "Problemas encontrados".

## Checklist obrigatório

| Item | Status |
|---|---|
| `npm install` sem erros | ✅ |
| `npm run dev` funcionando | ✅ |
| Aplicação abrindo no navegador | ✅ |
| Login funcionando (mock ou real) | ✅ real, contra o backend rodando em `localhost:5027` |
| Dashboard carregando | ✅ |
| Menu lateral funcionando | ✅ (com uma ressalva de destaque visual, ver Problema #3) |
| Navegação funcionando | ✅ — todas as rotas testadas |
| Páginas existentes renderizando | ✅ — 11/11 rotas + 404 |
| Nenhum erro fatal no console | ✅ — só avisos de depreciação do React Router (não-fatais) |
| Nenhum erro fatal de React | ✅ |
| Nenhum erro fatal de Vite | ✅ |
| Nenhuma tela branca | ✅ |

## Como foi validado

Backend (`dotnet run`, porta 5027) e frontend (`npm run dev`, porta 5173) rodando simultaneamente,
navegados com Chromium real via Playwright (não `curl`, não teste unitário) — login de verdade,
clique real em cada item do menu, screenshot de tela cheia de cada página, leitura do console do
navegador (`console.error`, `pageerror`, `requestfailed`, respostas HTTP ≥ 400).

## Páginas existentes

| # | Página | Rota | Arquivo |
|---|---|---|---|
| 1 | Login | `/login` | `src/pages/LoginPage.tsx` |
| 2 | Dashboard Operacional | `/dashboard` | `src/pages/DashboardOperacionalPage.tsx` |
| 3 | Dashboard Técnico | `/dashboard-tecnico` | `src/pages/DashboardTecnicoPage.tsx` |
| 4 | Clientes (lista) | `/clientes` (e `/suporte/selecionar-cliente`) | `src/pages/ClientesListPage.tsx` |
| 5 | Cliente (detalhe) | `/clientes/:id` | `src/pages/ClienteDetalhePage.tsx` |
| 6 | Suporte — Diagnóstico da Propriedade | `/suporte/diagnostico` | `src/pages/SuporteDiagnosticoPage.tsx` |
| 7 | Suporte — Sessões Ativas | `/suporte/sessoes-ativas` | `src/pages/SuporteSessoesAtivasPage.tsx` |
| 8 | Suporte — Logs | `/suporte/logs` | `src/pages/SuporteLogsPage.tsx` |
| 9 | Equipamentos | `/equipamentos` | `src/modulos/equipamentos/EquipamentosListPage.tsx` |
| 10 | Provisionamentos | `/provisionamentos` | `src/modulos/provisionamentos/ProvisionamentosPage.tsx` |
| 11 | Diagnóstico de Equipamentos | `/diagnostico-equipamentos` | `src/modulos/diagnostico/DiagnosticoEquipamentosPage.tsx` |
| — | Não Suportado (mobile) | condicional, não é rota própria | `src/pages/MobileNotSupportedPage.tsx` |
| — | 404 | `*` | `src/pages/NotFoundPage.tsx` |

Todas as 11 páginas roteáveis estão referenciadas em `src/routes/AppRoutes.tsx` — nenhum arquivo de
página órfão (sem rota) foi encontrado em `src/pages/` ou `src/modulos/`.

## Páginas funcionais

Todas as 11 páginas + 404 renderizam com dado real do backend, sem tela branca, sem erro fatal:

- **Login** — formulário limpo, validação HTML5, erro amigável em credenciais inválidas.
- **Dashboard Operacional** — 4 cards + 3 gráficos (Recharts) + atividade recente, todos com dado
  real (14 clientes, 12 propriedades, 9 equipamentos, 3 offline no momento do teste).
- **Clientes** (lista + detalhe) — paginação, busca, detalhe com propriedades vinculadas e "Entrar
  como Cliente" visível.
- **Suporte / Sessões Ativas / Logs** — estados vazios corretos e honestos quando não há sessão de
  impersonation ativa; Logs mostra histórico de auditoria real e filtrável.
- **Equipamentos** — listagem paginada, busca, filtros por Fabricante/Estado, botão de exclusão com
  confirmação, todos os dados vindos de `/api/painel/equipamentos`.
- **Provisionamentos** — dashboard de alocação (3 cards) + tabela de vínculos ativos com dado real.
- **Diagnóstico de Equipamentos** — grid de status com seletor de polling, dado real de
  conectividade/eventos recentes.
- **Dashboard Técnico** — só acessível/visível corretamente quando alcançada diretamente por um
  usuário com papel `Tecnico` (ver Problema #1) — quando alcançada, é honesta e funcional (mostra
  "Minhas Instalações ainda não é rastreável" em vez de fingir um dado que não existe, decisão já
  documentada desde a Sprint 22A).
- **404** — funcional, com botão de volta ao Dashboard.

## Páginas parcialmente funcionais

- **Dashboard Operacional**, quando alcançada por um usuário sem papel Master/Suporte (um Técnico,
  ou até um cliente comum não-interno), dispara chamadas para endpoints que retornam `403` e
  renderiza os cards zerados sem nenhuma explicação ao usuário — ver Problema #1/#2. A página em si
  não está quebrada; o problema é que usuários sem o papel certo conseguem chegar nela.
- **Suporte / Diagnóstico da Propriedade / Sessões Ativas** dependem inteiramente de uma sessão de
  impersonation ativa para mostrar dado real — isso é comportamento pretendido (documentado desde a
  Sprint 22A), não uma falha, mas half do conteúdo da tela só aparece nesse contexto específico.

## Páginas quebradas

Nenhuma. Nenhuma tela em branco, nenhum crash de React, nenhum erro fatal de Vite foi encontrado em
nenhuma das 11 rotas + 404 + estado mobile, com login válido.

## Componentes reutilizados

`TabelaPadrao`, `BadgeStatus`/`SeletorStatus`, `CabecalhoPagina`, `BarraPesquisa`,
`PaginacaoPadrao`, `SeletorPropriedade` (compartilhados entre os 3 módulos mais recentes —
Equipamentos/Provisionamentos/Diagnóstico) e `ConfirmDialog`/`EmptyState`/`StatCard`/`Breadcrumbs`
(compartilhados desde a Sprint 22A, reusados sem duplicação pelos módulos novos — decisão já
documentada no ADR 0031, que optou por reaproveitar `ConfirmDialog` em vez de criar um
`DialogoConfirmacao` novo).

## Componentes duplicados

Nenhum componente duplicado (duas implementações do mesmo conceito) foi encontrado. Uma reutilização
que **não** é duplicação, mas vale registrar: `ClientesListPage` é o mesmo componente por trás de
duas rotas (`/clientes` e `/suporte/selecionar-cliente`) — intencional, mas o título/breadcrumb da
página não muda para refletir o contexto de onde veio (ver Sugestão de UX).

## Telas antigas que deveriam ser removidas

Nenhuma. Todo arquivo em `src/pages/` e `src/modulos/*/  *Page.tsx` está referenciado em
`AppRoutes.tsx` — não há tela morta/órfã no código atual.

## Problemas encontrados

### Crítico

Nenhum.

### Alto

**#1 — Login não redireciona por papel; usuário Técnico cai no Dashboard errado.**
`LoginPage.tsx` sempre navega para `urlRetorno ?? '/dashboard'` após o login, sem checar
`roleGlobal`. Um usuário com papel `Tecnico` (criado e testado nesta revisão) loga com sucesso mas
é levado à Dashboard Operacional (visão de Master/Suporte) em vez da Dashboard Técnico — a própria
Sidebar já sabe calcular a rota certa (`isTecnico && !isMaster && !isSuporte ? '/dashboard-tecnico'
: '/dashboard'`, ver `Sidebar.tsx`), mas essa lógica não é reaproveitada no redirecionamento
pós-login. Consequência observada: 2 requisições `403 Forbidden` no console (a página tenta
carregar dado de `/api/proprietarios`/auditoria, que o papel Técnico não tem permissão para ver) e
uma Dashboard Técnico praticamente inacessível por fluxo normal (só se chega lá digitando a URL à
mão ou clicando no link certo da Sidebar depois de já estar em outra tela).
*Evidência*: testado com conta `tecnico.review@appmorador.local` criada nesta revisão via
`POST /api/usuarios-internos`. Reproduzível em qualquer ambiente com uma conta Técnico.

**#2 — `/dashboard` não tem nenhuma guarda de papel; até um cliente comum consegue "entrar" no
Painel Administrativo.** A rota `/dashboard` em `AppRoutes.tsx` fica fora de qualquer `RoleRoute` —
só exige `PrivateRoute` (estar autenticado, sem checar se é um usuário interno). Como o
Painel Web usa o mesmo `POST /api/auth/login` do app mobile, uma conta de cliente comum (testado
com `admin@appmorador.local`, papel de Administrador de propriedade, não-interno) consegue logar
com sucesso no Painel Administrativo e cai numa Dashboard Operacional com todos os cards zerados e
4 requisições `403` no console, sem nenhuma mensagem explicando por quê. O RBAC do backend
funcionou corretamente (nenhum dado vazou — todas as chamadas que exigiam papel interno foram
bloqueadas), mas a experiência no frontend para esse caso é uma tela confusa e sem saída, não uma
mensagem clara de "esta conta não tem acesso ao Painel Administrativo".
*Evidência*: `docs/reviews/frontend/cliente-comum-no-painel.png`.
*Sugestão*: `PrivateRoute` (ou uma nova guarda) deveria checar `EhInterno()`/`roleGlobal !== null`
e, se a conta não for interna, mostrar uma mensagem de acesso negado com logout — nunca deixar cair
numa tela de dado real quebrado.

### Médio

**#3 — Destaque da Sidebar acende mais de um item ao mesmo tempo.** A lógica de "item ativo" em
`Sidebar.tsx` (`location.pathname.startsWith(item.rota.split('/').slice(0,2).join('/'))`) reduz
qualquer rota `/suporte/*` ao mesmo prefixo `/suporte` — então, estando em `/suporte/diagnostico`
ou em `/suporte/sessoes-ativas`, os 3 itens "Suporte", "Sessões Ativas" e "Logs" acendem em verde
ao mesmo tempo (ver `docs/reviews/frontend/suporte-diagnostico.png` e `sessoes-ativas.png`),
mesmo só um estando de fato aberto. Não impede o uso, mas é visualmente confuso.

**#4 — Tabelas não têm tratamento de overflow em largura de tablet.** Em 768px (tablet retrato — a
própria aplicação afirma suportar tablet, ver `MobileNotSupportedPage`: "Use um computador ou
tablet"), a tabela de Equipamentos corta colunas (`Fabricante`, `Número de Série`, `Conectividade`,
`Estado`) para fora da tela, sem nenhuma barra de rolagem horizontal visível. O mesmo componente
(`TabelaPadrao`) é usado por Provisionamentos e Diagnóstico — mesmo problema esperado lá.
*Evidência*: `docs/reviews/frontend/tablet-768-equipamentos.png`.

### Baixo

**#5 — Logs mostra nomes internos crus.** A tela de Logs exibe valores como
`VinculoEquipamentoPropriedade`, `ImpersonationInicio`, `FalhaAutorizacao` diretamente (nome da
entidade/enum em C#), em vez de um rótulo traduzido/humanizado. Correto tecnicamente, pouco legível
para quem não conhece o código.

**#6 — Avisos de depreciação do React Router em toda página.** `v7_startTransition` e
`v7_relativeSplatPath` aparecem no console em 100% das páginas testadas. Não quebram nada hoje;
acumulam dívida de migração para quando a v7 for adotada.

## Sugestões de UX

- Diferenciar o título/contexto de `ClientesListPage` quando acessada via `/suporte/selecionar-cliente`
  (hoje mostra "Clientes" igual à rota principal — um rótulo como "Selecionar Cliente para Suporte"
  deixaria a intenção mais clara nesse fluxo).
- Mostrar uma mensagem explícita quando o Dashboard Operacional não consegue carregar um widget por
  falta de permissão (em vez de silenciosamente mostrar "0"), tanto para diagnosticar o Problema #1
  quanto para qualquer 403 futuro que passe despercebido pela tela.
- Humanizar os valores de `TipoAcaoAuditoria`/`Entidade` na tela de Logs (Problema #5).

## Sugestões de Design

Nenhuma — fora do escopo desta Sprint por instrução explícita (não redesenhar, não trocar layout,
não modernizar componente, não alterar cor/identidade visual). O único ponto registrado
(overflow de tabela em tablet, Problema #4) é tratado aqui como bug de responsividade, não como
sugestão estética.

## Dívida técnica

- Redirecionamento pós-login deve considerar `roleGlobal` (reaproveitar a mesma lógica já existente
  em `Sidebar.tsx`) — ver Problema #1.
- `/dashboard` (e possivelmente toda a árvore do Painel Web) precisa de uma guarda de "é usuário
  interno" na entrada, não confiar em cada endpoint individual devolver 403 — ver Problema #2. Isso
  é além do RBAC de permissão específica (`RoleRoute`) já existente por página; é uma guarda de
  "este login nem deveria estar aqui".
- Lógica de item-ativo da Sidebar precisa comparar o pathname completo (ou o primeiro segmento
  exato), não um prefixo reduzido por `.slice(0,2)` — ver Problema #3.
- `TabelaPadrao` (compartilhado por Equipamentos/Provisionamentos/Diagnóstico) precisa de um
  contêiner com `overflow-x: auto` para larguras de tablet — ver Problema #4.
- Mapa de rótulos humanizados para `TipoAcaoAuditoria`/nomes de entidade na tela de Logs — ver
  Problema #5.
- Adotar as duas future flags do React Router v6 (`v7_startTransition`, `v7_relativeSplatPath`)
  antes de uma eventual migração para v7 — ver Problema #6.

## Execução — confirmação final

1. **Aplicação executada**: backend (`dotnet run`, porta 5027) e frontend (`npm run dev`, porta
   5173) rodando simultaneamente durante toda a validação.
2. **Abriu sem erros**: confirmado via Playwright — 0 erros fatais de React/Vite, 0 telas brancas,
   em 11 rotas + estado mobile + estado de acesso negado (cliente comum) + estado de papel Técnico.
3. **Informações de acesso**:
   - **URL local**: `http://localhost:5173`
   - **Usuário de teste (acesso total)**: `master@appmorador.local`
   - **Senha de teste**: `Master@123`
   - **Conta Técnico** (criada nesta revisão, para reproduzir o Problema #1):
     `tecnico.review@appmorador.local` / `Tecnico@123`
   - **Páginas disponíveis**: as 11 listadas em "Páginas existentes", todas navegáveis a partir da
     Sidebar após login como Master.
   - **Limitações conhecidas**: Dashboard Técnico só é alcançada corretamente por URL direta (ver
     Problema #1); Diagnóstico da Propriedade/Sessões Ativas mostram estado vazio honesto sem uma
     sessão de impersonation ativa (comportamento esperado, não um bug); tabelas cortam colunas em
     largura de tablet (Problema #4).

## Screenshots

Salvos em `docs/reviews/frontend/`:

| Arquivo | Conteúdo |
|---|---|
| `login.png` | Tela de login |
| `dashboard.png` | Dashboard Operacional (Master, pós-login) |
| `clientes.png` | Lista de Clientes |
| `cliente-detalhe.png` | Detalhe de um cliente |
| `suporte-diagnostico.png` | Diagnóstico da Propriedade (estado vazio, sem impersonation) |
| `sessoes-ativas.png` | Sessões Ativas (estado vazio) |
| `logs.png` | Logs de auditoria |
| `equipamentos.png` | Lista de Equipamentos |
| `provisionamentos.png` | Dashboard de Provisionamentos |
| `diagnostico-equipamentos.png` | Grid de Diagnóstico de Equipamentos |
| `not-found.png` | Página 404 |
| `dashboard-tecnico.png` | Dashboard Técnico (alcançada por URL direta) |
| `cliente-comum-no-painel.png` | Evidência do Problema #2 — cliente comum logado no Painel |
| `mobile-viewport.png` | Estado "use um computador ou tablet" em viewport de celular (375px) |
| `tablet-768.png` / `tablet-768-equipamentos.png` | Evidência do Problema #4 — tablet 768px |
