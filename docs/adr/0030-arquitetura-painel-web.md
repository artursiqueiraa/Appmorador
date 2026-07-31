# ADR 0030 — Arquitetura do Painel Web (Sprint 22A)

**Data**: 2026-07-28

## Contexto

A Sprint 22A pediu a fundação do Painel Web — projeto frontend separado (React 19 + Vite +
TypeScript) consumindo a Api já existente (Sprint 21), com autenticação, layout, Dashboard
Operacional/Técnico, gestão de Clientes e o módulo de Suporte (impersonation). O projeto nasce do
zero em `PainelWeb/`, ao lado de `backend/` e `mobile/` na raiz do monorepo.

## Problema

Como estruturar um projeto novo, com stack diferente do resto do repositório (React web em vez de
React Native, sem histórico de convenção própria), mantendo consistência de Design Tokens e
qualidade com o que já existe, sem duplicar lógica de negócio (que continua 100% no backend)?

## Decisão

**Stack**: React 19, Vite, TypeScript, React Router DOM v6, TanStack Query (cache/refetch de
servidor), Axios (HTTP + interceptors), Zustand (estado global — auth/tema/toast), Material UI v9
(tema claro/escuro construído a partir dos mesmos Design Tokens do app mobile, ver
`src/styles/tokens.ts`).

**Estrutura de diretórios**: `components/`, `pages/`, `layouts/`, `services/`, `hooks/`, `stores/`,
`routes/`, `styles/`, `types/` — a pasta `contexts/` da missão original não foi criada
separadamente: Zustand (stores) já cobre 100% do estado global sem precisar de Context Providers
próprios (ver Decisão 2).

### Decisão 1 — Vitest em vez de Jest para testes

A missão pede "Jest + React Testing Library". Como o projeto usa Vite, Vitest é o padrão natural
(integração nativa com o mesmo pipeline de build/transform do Vite, API compatível com Jest —
`describe`/`it`/`expect`/mocks — então o código de teste é virtualmente idêntico). Jest exigiria
configuração adicional (`ts-jest`/`babel-jest`) só para reproduzir o que o Vite já faz de graça.
React Testing Library continua exatamente como pedido. `npm test` (`vitest run`) mantém a mesma
interface esperada (`npm test -- --watchAll=false` não se aplica ao Vitest, mas `npm test` sozinho
já roda uma vez e sai, sem watch).

### Decisão 2 — Zustand substitui a pasta `contexts/`

A missão pede tanto stores (Zustand) quanto contexts (Auth/Permissao/Tema) para o mesmo estado.
Como Zustand já expõe hooks (`useAuthStore`, `useTemaStore`) sem precisar de nenhum Provider na
árvore, um Context adicional em cima disso seria uma camada puramente redundante. `usePermissao`/
`useAuth` (em `hooks/`) são a camada de conveniência que a missão esperava dos "contexts".

### Decisão 3 — Impersonation via troca de token, não header `X-Impersonar-Propriedade-Id`

A missão assumia um header customizado para carregar o contexto de impersonation. Na prática (ver
ADR 0021), o token de impersonation JÁ contém toda a identidade necessária (claims do usuário
alvo) — o interceptor do Axios só troca qual token é usado como Bearer enquanto
`authStore.impersonation` não é nulo (`startImpersonation`/`endImpersonation`). Nenhum header
extra existe nem é necessário.

### Decisão 4 — `RoleGlobal` sempre lido do JWT decodificado, nunca do corpo da resposta de login

A missão pede "decodificação JWT para extrair RoleGlobal, Nome, Email, Exp" — mas o JWT real
**não tem `Nome`** (ver ADR 0021/ARQUITETURA_ATUAL.md). `Nome`/`Email`/`Id` vêm do corpo de
`EntrarResponse`, persistidos separadamente (`authStore.user`); `RoleGlobal`/`Exp` vêm sempre do
JWT decodificado (`jwt-decode`, sem verificação de assinatura client-side — a Api já validou o
token antes de qualquer resposta chegar ao browser).

### Decisão 5 — MUI v9 exige `sx`, não mais props de sistema soltas

A versão instalada do Material UI (v9.2.0) removeu as props de atalho do `Box`/`Typography`
(`display`, `gap`, `p`, `mb`, `fontWeight`, `textAlign` como props diretas) — só `sx={{...}}`
continua tipado. Todo componente deste projeto usa `sx` para essas propriedades desde o início
(não é uma migração futura, é a única forma que compila).

## Consequências

- Nenhuma lógica de negócio vive no Painel Web — toda regra (RBAC, permissões, impersonation)
  continua nos Servicos do backend; o frontend só consome e apresenta.
- `usePermissao`/`useAuth` são o único ponto de leitura de papel/sessão — nenhuma tela decodifica
  o JWT por conta própria.
- Zero mudança na Api além do estritamente necessário (ver ADR 0029) — Painel Web e Mobile
  compartilham 100% da mesma Api, sem endpoint duplicado.

## Impactos

Sprint 22B (Propriedades/Equipamentos/Provisionamentos completos) e 22C (Logs/Ocorrências/
Configurações) constroem em cima desta mesma estrutura de pastas/convenções — nenhuma delas
precisa reconsiderar a stack escolhida aqui.

## Arquivos afetados

`PainelWeb/` (projeto inteiro, novo).

## Como revisar futuramente

Revisar quando o bundle de produção crescer o suficiente para justificar code-splitting por rota
(o build já emite um aviso de chunk >500kB, aceitável para a fundação, mas a observar conforme
Propriedades/Equipamentos/Provisionamentos completos forem adicionados nas próximas Sprints).
