# Homologação — Sprint 22A (Fundação do Painel Web)

## Como rodar localmente

```bash
# Backend (a partir de backend/src/AppMorador.Api)
dotnet run

# Painel Web (a partir de PainelWeb/)
npm install
npm run dev
```

Acesse `http://localhost:5173`. O backend precisa estar rodando em `http://localhost:5027` (valor
padrão em `PainelWeb/.env`) e ter `http://localhost:5173` na lista `Cors:AllowedOrigins` do
`appsettings.Development.json` (já configurado nesta Sprint).

**Contas de teste** (seed de desenvolvimento, ver `docs/setup/SETUP_AMBIENTE.md`):

| Papel | E-mail | Senha |
|---|---|---|
| Master | `master@appmorador.local` | `Master@123` |

Não há conta de seed para Técnico/Suporte — crie uma via `POST /api/usuarios-internos` (logado
como Master) se precisar validar esses papéis especificamente.

## Checklist de Validação Manual

### 1. Autenticação (Fase 2)
- [ ] Login com credenciais corretas funciona e redireciona ao Dashboard
- [ ] Login com credenciais erradas mostra erro amigável (nunca a mensagem técnica crua)
- [ ] Logout funciona e volta para a tela de login
- [ ] Acessar uma URL protegida sem estar logado redireciona para `/login`
- [ ] Após logar, é redirecionado de volta para a URL que tentou acessar originalmente
- [ ] Alternar tema claro/escuro funciona em todas as telas

### 2. Dashboard Operacional (Fase 3) — logado como Master
- [ ] Os 4 cards carregam com números reais (Total de Clientes/Propriedades/Equipamentos/
      Equipamentos Offline)
- [ ] Clicar em "Total de Clientes" navega para a lista de Clientes
- [ ] Os 3 gráficos renderizam (Novos Clientes por Mês, Propriedades por Tipo, Equipamentos por
      Status)
- [ ] "Atividade Recente" mostra os últimos registros de auditoria

### 3. Dashboard Técnico (Fase 4) — logado como Técnico
- [ ] Landing page do Técnico é `/dashboard-tecnico`, não o Dashboard Operacional
- [ ] Card "Equipamentos Offline" carrega
- [ ] Mensagem honesta sobre "Minhas Instalações" aparece (não finge dado que não existe)

### 4. Clientes (Fase 5) — logado como Master ou Suporte
- [ ] Lista carrega paginada
- [ ] Busca por nome/e-mail funciona (aguardar ~300ms após parar de digitar)
- [ ] Clicar num cliente abre o detalhe com propriedades vinculadas
- [ ] Detalhe mostra dados cadastrais corretos

### 5. Suporte / Impersonation (Fase 6) — logado como Master ou Suporte
- [ ] "Entrar como Cliente" (dentro do detalhe de um cliente, numa propriedade específica) funciona
- [ ] Banner LARANJA aparece fixo no topo, em qualquer tela, com o timer regressivo correndo
- [ ] Diagnóstico da Propriedade carrega (câmeras, heartbeat, último evento) da propriedade certa
- [ ] "Encerrar Sessão do Cliente" no banner funciona e volta para Selecionar Cliente
- [ ] Sessões Ativas mostra a sessão em andamento enquanto durar a impersonation
- [ ] Logs mostra os registros de auditoria, filtro por tipo funciona

### 6. Responsividade / UX (Fase 1/7)
- [ ] Redimensionar a janela para menos de 600px mostra a tela "use um computador ou tablet"
- [ ] Nenhuma tela fica em branco durante carregamento (skeleton ou spinner sempre visível)
- [ ] Listas vazias mostram estado amigável, nunca uma tabela em branco sem explicação
- [ ] Erros de rede mostram toast vermelho com mensagem amigável

### 7. Regressão (não deve ter quebrado nada no backend/mobile)
- [ ] `dotnet test` no backend: 96/96 passando
- [ ] Login/logout no app mobile continuam funcionando normalmente
- [ ] Impersonation testada via curl contra o backend real (ver `docs/painel/mapeamento-api.md`)

## Resultado dos testes automatizados

- Backend: **96/96 passando** (`dotnet test`), 0 erros de build.
- Painel Web: **28/28 passando** (`npx vitest run`), `npm run build` limpo (1 warning de tamanho
  de chunk, aceitável para esta fundação), `npm run lint` e `npx tsc -b --noEmit` sem erros.

## Limitação conhecida desta validação

Sem navegador disponível neste ambiente de execução para capturar screenshots/gravação de tela —
mesma limitação já registrada em Sprints anteriores (ver `docs/reviews/`). A verificação nesta
Sprint foi feita via: `dotnet test`/`npx vitest run` (automatizado), `curl` direto contra os
endpoints reais do backend (impersonation ponta a ponta, `/api/proprietarios`,
`/api/dashboard-operacional` — todos confirmados com dado real do banco de desenvolvimento),
`npm run build`/`typecheck`/`lint` (sem erros). Validação visual/sensorial (layout, responsividade
real, fluxo de impersonation navegando de fato) fica pendente da homologação manual do usuário.

## Resultado da Homologação

| Campo | Valor |
|---|---|
| Resultado | ☐ Aprovada / ☐ Reprovada — **pendente, aguardando execução pelo usuário** |
| Quem homologou | — |
| Data da homologação | — |
| Tempo gasto | — |

### Se Reprovada

| Severidade | Bugs Encontrados | Ações Corretivas |
|---|---|---|
| Blocker | — | — |
| High | — | — |
| Medium | — | — |
| Low | — | — |
