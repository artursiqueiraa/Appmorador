# ADR 0035 — Identidade Akteck Seg, redesign do Painel e "Nova instalação"

**Data**: 2026-11-02

## Contexto

O produto passou a se chamar **Akteck Seg**. O usuário pediu:
- uma análise do Painel Web;
- um dashboard novo com funcionalidades novas;
- um redesign total do Painel e do Web App do morador, porque o visual estava "muito feio";
- nenhuma alteração no app mobile;
- nomes em pt-BR em qualquer mudança de banco.

Foram usadas as skills `ui-ux-pro-max` (regras de UX) e Hallmark (regras "app screens" e o audit `sloplint`), e os agentes `produto`, `design-system`, `ux`, `banco` e `reviewer`.

## Problema

A análise (Fase 1) encontrou:
- **Status com cor errada:** o gráfico "Equipamentos por status" pintava as fatias pela ordem do array. Offline saía verde e Online, laranja.
- **Conteúdo deslocado 480px:** o `<main>` recebia `ml: 240px`, além dos 240px que o Drawer permanente já ocupa no fluxo flex.
- **Menu e cabeçalho:** o menu era uma lista plana sem marca. O cabeçalho não tinha título, e os três itens de Suporte acendiam juntos (o item ativo era detectado por prefixo).
- **Modo claro:** os gráficos usavam as cores do tema escuro.
- **Atividade recente:** mostrava enums crus ("FalhaAutorizacao", "Endpoint").
- **Telas que faltavam:** vários endpoints existentes não tinham interface (equipe interna, catálogo de modelos, feature flags), e não havia como criar um cliente pelo Painel (dívida técnica 47).
- **Cor da marca e cor de status eram a mesma:** o verde `#25C98D` era marca e também "Protegido".

## Alternativas consideradas

1. **Deixar a Hallmark sortear um tema visual:** descartado. A identidade precisa ser única entre Painel e Web App, e o usuário delegou a criação da marca.
2. **Manter o verde como cor da marca:** descartado. Botões, abas e "Protegido" continuariam com a mesma cor, e o status deixaria de se destacar.
3. **Criar um cliente pelo Painel com um endpoint de CRUD de Usuário genérico:** descartado. Seria maior que o necessário e duplicaria a regra de cadastro.

## Decisão

### Identidade
- **Marca:** "Akteck Seg". O símbolo é um escudo com um "A" em chevron vazado, em SVG `currentColor` (Painel: `components/marca/MarcaAkteck.tsx`; Web App: a mesma forma em `react-native-svg`).
- **Cor de marca:** azul cobalto (`#4C86FF` no escuro, `#1F5FE0` no claro). Marca ação principal, seleção e navegação ativa, e nada mais.
- **Cores de status:** verde, âmbar e vermelho voltam a significar só estado.
- **Pares de cor:** todos os pares texto/superfície foram medidos (WCAG AA ≥ 4.5:1). Bordas de campo usam `borderStrong`, com ≥ 3:1.
- **Tipografia:** Geist (Google Fonts) nos dois. Escala fixa de razão ~1.2 e algarismos tabulares no Painel.
- **Web App:** tokens `marca*` novos. Os usos de marca (botões, abas, seletores, progresso, login/splash, interruptores) migraram de `safe` para `marca`; os usos de status continuam em `safe`. A borda do `TextField` passou para `borderStrong`, o que resolve o achado de 1.47:1 do ADR 0034. O mobile não foi tocado (ADR 0019 continua valendo lá).

### Painel
- **Navegação:** fonte única em `routes/navegacao.ts`, com 5 grupos e o item ativo por rota exata ou sub-rota. O cabeçalho mostra "Grupo / Página". Abaixo de 960px o menu vira gaveta temporária.
- **Layout:** largura máxima de 1360px, o que corrige o deslocamento de 480px. Abaixo de 600px o aviso de "não suportado" cobre o Painel sem desmontá-lo, para que estreitar a janela não apague formulários.
- **Dashboard (`modulos/dashboard/`):**
  - indicadores sem ícone decorativo, cada um levando à tela correspondente;
  - a fila **"Clientes que precisam de atenção"**, que agrupa por propriedade os equipamentos offline, com defeito ou com alerta, a partir do Diagnóstico (Master/Técnico);
  - distribuições em barras com número e %, e cor semântica fixa por status;
  - gráfico de novos clientes com as cores do tema;
  - atividade em linguagem humana.
- **Telas novas** sobre endpoints que já existiam:
  - **Equipe interna** (`api/usuarios-internos`, só Master);
  - **Catálogo de modelos** (`api/modelos-equipamento`, filtro de fabricante na URL);
  - **Recursos contratados** por propriedade, no detalhe do cliente (`api/properties/{id}/features`).
- **Nova instalação (endpoint novo):**
  - **Rota:** `POST api/painel/novas-instalacoes`, só Master.
  - **Caso de uso:** `NovaInstalacaoServico` orquestra os casos de uso que já existem (`IAutenticacaoServico.RegisterAsync`, `IPropriedadeServico.CreateAsync`, `IVinculoEquipamentoServico.ProvisionarAsync`), sem duplicar regra. E-mail em uso e equipamento inexistente ou já provisionado são verificados antes de qualquer gravação.
  - **Validação e auditoria:** a senha inicial segue a mesma regex do cadastro público, e a ação é auditada.
  - **Banco:** sem migration e sem tabela nova.
  - **Tela:** o assistente tem 4 etapas (Cliente, Propriedade, Equipamento, Revisão), e nada é gravado antes da revisão.

### Banco
Nenhuma migration nesta Sprint. Conforme a decisão do usuário, os nomes em inglês já existentes (`RefreshTokens`, `CreatedAtUtc` etc.) não foram renomeados. A regra vale para o que for criado daqui para frente: nomes em pt-BR, como no endpoint e nos DTOs novos.

## Consequências

- O Painel e o Web App compartilham a identidade. O app mobile fica visualmente diferente até uma Sprint própria, que precisará de APK e homologação em dispositivo.
- A Nova instalação não é transacional. Os passos 1 e 2 (cliente e propriedade) dependem de validações feitas antes deles. O passo 3 (vínculo) só falharia numa corrida com outro provisionamento simultâneo; nesse caso, a resposta indica `vinculoEquipamentoId: null` e a tela orienta refazer em Provisionamentos.
- A lista de equipamentos livres e a fila de atenção usam amostras de até 100 itens (o limite de paginação do backend). As telas dizem isso quando a base é maior.

## Impactos

- **Backend:** 1 controller, 1 caso de uso, DTOs, registro de DI e 5 testes unitários. A dívida técnica 47 fica parcialmente resolvida: criar cliente agora é possível; editar e desativar, ainda não.
- **Audit Hallmark (Painel, `/login`):** 1 FAIL na regra 40, que é falso positivo. A borda do campo MUI fica no `fieldset` (`borderStrong`, 3.16:1), não no `<input>` que o sloplint mede.

## Arquivos afetados

- **PainelWeb:**
  - `index.html`, `public/favicon.svg`;
  - `src/styles/{tokens,muiTheme}.ts`;
  - `src/routes/{navegacao.ts,AppRoutes.tsx}`;
  - `src/components/{Sidebar,Header,ImpersonationBanner}.tsx`, `src/components/marca/`;
  - `src/layouts/*`;
  - `src/pages/{DashboardOperacionalPage,ClienteDetalhePage,SuporteLogsPage}.tsx`;
  - `src/modulos/{dashboard,equipe,modelos,recursos,novaInstalacao}/`;
  - `src/compartilhado/tipos/propriedade.ts`.
- **Backend:**
  - `Application/Painel/NovasInstalacoes/`;
  - `Api/Controllers/NovasInstalacoesController.cs`;
  - `Infrastructure/Identity/AuthServiceCollectionExtensions.cs`;
  - `tests/.../NovaInstalacaoServicoTests.cs`.
- **webapp:**
  - `app.json`, `assets/favicon.png`;
  - `src/theme/colors.ts`;
  - `src/components/marca/`, `src/components/{TextField,BottomNavigation}.tsx`;
  - `src/layout/estilosGlobaisWeb.ts`;
  - troca `safe`→`marca` em 20 arquivos de marca.

## Como revisar futuramente

- Levar a identidade para o mobile numa Sprint própria, com a Regra de Validação em Dispositivo.
- Se a base passar de 100 equipamentos, criar no backend um filtro "precisa de atenção" e um filtro "livres", em vez das amostras no cliente.
- Tornar a Nova instalação transacional se aparecer falha parcial real.
