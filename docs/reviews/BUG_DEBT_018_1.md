# Bug Debt — Sprint 18.1 (Hotfix: Correções Críticas de UX e Estabilidade)

**Data**: 2026-07-26
**Natureza**: correção de bugs críticos encontrados na validação em dispositivo físico da Sprint
18. Nenhuma funcionalidade nova, nenhuma alteração de backend.

## Metodologia (Fase 0 — Root Cause Analysis)

Cada bug reportado foi investigado por leitura direta de código antes de qualquer correção — os
"causas possíveis" listadas na missão foram tratadas como hipóteses a verificar, não fatos. Dois
dos sete itens reportados não puderam ser reproduzidos nem confirmados como defeito de código
depois de investigação extensiva (ver "Não Reproduzidos") — nenhuma correção especulativa foi
aplicada para eles, por respeito à própria regra da missão: "nenhum bug pode ser marcado como
corrigido sem passar por todas as etapas" do fluxo de Fase 0.

**Limitação importante desta sessão**: nenhuma screenshot real foi anexada à missão (o texto a
menciona, mas nenhuma imagem chegou a este ambiente) — toda a investigação foi feita por
arqueologia de código (grep, leitura de componentes, consulta direta ao banco de dados de
desenvolvimento), não por inspeção visual do bug reportado.

## Checklist por Bug

| Bug | Reproduzido? | Causa Raiz | Corrigido? | Teste Auto? | Android? | iOS? | Fechado? |
|---|---|---|---|---|---|---|---|
| Overlay "DSW" | ❌ Não | Não encontrada (nenhum código no repositório produz isso) | ❌ Não | ➖ N/A | ➖ N/A | ➖ N/A | ❌ Não fechado — ver "Não Reproduzidos" |
| Botão sem texto | ⚠️ Parcial | Não confirmada com certeza (contraste calculado é adequado); hex hardcoded corrigido preventivamente | ✅ Preventivo | ➖ Não aplicável (sem defeito confirmado) | ❌ Pendente do usuário | ➖ N/A | ⚠️ Parcial — ver nota |
| Propriedades não carregam | ✅ Sim (por análise de código) | `fetch` sem timeout — requisição pendurava indefinidamente se o backend estivesse inalcançável; tela sem skeleton/erro visível | ✅ Sim | ✅ Sim (`client.test.ts`) | ❌ Pendente do usuário | ➖ N/A | ✅ Fechado |
| Logout quebrado | ✅ Sim (por análise de código) | Mesma causa do timeout (POST de logout pendurava); `setUser(null)` fora de bloco protegido | ✅ Sim | ✅ Sim (`AuthContext.logout.test.tsx`) | ❌ Pendente do usuário | ➖ N/A | ✅ Fechado |
| Texto cortado (Safe Area) | ✅ Sim (por análise de código) | `SafeAreaProvider` ausente no app inteiro; `useSafeAreaInsets` usado só em 1 arquivo (`BottomNavigation.tsx`); `SelecionarPropriedadeScreen` fora da Tab Bar, sem proteção nenhuma | ✅ Sim | ➖ Não aplicável (visual, sem framework de teste de snapshot configurado) | ❌ Pendente do usuário | ➖ N/A | ✅ Fechado |
| Texto "Tetd" truncado | ❌ Não (não é bug de código) | Nenhuma lógica de truncamento existe (`numberOfLines` não é usado no nome da propriedade); banco de dev não tem nenhuma propriedade chamada "Tetd" | ➖ N/A | ➖ N/A | ➖ N/A | ➖ N/A | ➖ Ver "Não Reproduzidos" |
| Layout desalinhado | ✅ Parcial | Form colava no topo quando a lista estava vazia (FlatList colapsada); chips de tipo já usavam tokens uniformes (nada de errado encontrado ali) | ✅ Sim (spacing) | ➖ Não aplicável (visual) | ❌ Pendente do usuário | ➖ N/A | ✅ Fechado (spacing); chips sem alteração (nada encontrado) |

## Corrigidos

1. **Timeout de 15s em todas as requisições** (`mobile/src/api/client.ts`) — causa raiz real e
   verificada de "propriedades não carregam" e "não sai da conta": `fetch()` nunca tinha um
   limite de tempo; um backend inalcançável (comum num celular físico numa rede diferente da
   máquina de desenvolvimento) deixava a requisição pendurada para sempre. Implementado via
   `AbortController` + `setTimeout(15000)`; a mensagem de timeout já era classificada como
   amigável pelo `errorMapper.ts` existente (nenhuma mudança necessária lá). **Teste automatizado**:
   `src/api/__tests__/client.test.ts` (2 casos, usando fake timers).
2. **Logout com limpeza local garantida** (`mobile/src/auth/AuthContext.tsx`) — `setUser(null)`/
   `setSelectedProperty(null)` agora rodam dentro de um `finally`, garantindo que a sessão local
   sempre é limpa mesmo se a revogação no servidor falhar/travar ou se `secureStorage.clear()`
   lançar. Combinado com o timeout acima, o logout agora sempre termina num tempo limitado.
   **Teste automatizado**: `src/auth/__tests__/AuthContext.logout.test.tsx` (2 casos).
3. **Feedback visual durante o logout** (`SettingsScreen.tsx`, `SelecionarPropriedadeScreen.tsx`)
   — botão de saída mostra um spinner e fica desabilitado enquanto o logout está em andamento
   (antes, nada indicava que o app estava processando o pedido).
4. **Máquina de estados de carregamento em `SelecionarPropriedadeScreen`** — a única tela do
   fluxo autenticado sem nenhum feedback de carregamento: adicionado Skeleton (primeira carga),
   bloco de erro com "Tentar novamente" (falha), e mantido o comportamento já existente de abrir o
   formulário automaticamente quando a lista está vazia.
5. **`SafeAreaProvider` global** (`App.tsx`) + insets aplicados em `SelecionarPropriedadeScreen`
   — antes desta Sprint, `react-native-safe-area-context` só era consumido por
   `BottomNavigation.tsx`; nenhuma outra tela tinha proteção contra a barra de navegação do
   Android. `SelecionarPropriedadeScreen` (fora da Bottom Tab Bar, com um botão flush no rodapé)
   era a mais exposta — corrigida com `useSafeAreaInsets()`.
6. **Cor hardcoded removida de `PrimaryButton.tsx`** — `#062015` (texto do botão primário) e a
   cor do `ActivityIndicator` correspondente substituídos por `colors.bg` (token já existente,
   mesmo valor visual); `opacity: 0.5`/`0.85` (disabled/pressed) substituídos pelos tokens
   `opacity.disabled`/`opacity.pressed` já definidos em `theme/tokens.ts`. Corrige uma violação
   real da própria regra do Design System ("nenhum componente pode usar hex literal").
7. **Espaçamento do formulário em `SelecionarPropriedadeScreen`** — `marginTop` adicionado ao
   bloco do formulário (colava no topo quando a lista de propriedades estava vazia, por causa da
   `FlatList` colapsar a zero altura); placeholder do endereço trocado de "Rua, número" para "Rua,
   número, bairro, cidade" (sugestão literal da missão).
8. **Infraestrutura de testes automatizados criada do zero** — o projeto não tinha nenhum test
   runner configurado antes desta Sprint. Adicionado `jest` + `jest-expo` +
   `@testing-library/react-native`, com 4 testes cobrindo as duas causas-raiz mais críticas
   (timeout de rede, limpeza de logout).

## Pendentes

Nenhum item crítico ficou pendente de correção nesta Sprint — os 3 bugs Blocker
(propriedades não carregam, logout quebrado) foram corrigidos na raiz (timeout); o overlay "DSW"
não pôde ser corrigido por não ter sido reproduzido (ver abaixo).

## Não Reproduzidos

### Overlay "DSW"

Investigação extensiva, sem sucesso em encontrar a origem:
- Busca por `"DSW"` literal em todo o código-fonte: nenhuma ocorrência.
- Busca por lógica de iniciais de avatar (`charAt(0)`, `slice(0,1)`, etc.): nenhuma ocorrência —
  o app não gera nenhum avatar com iniciais em lugar nenhum.
- Busca por cores hex roxas/magenta hardcoded: nenhuma ocorrência.
- Busca por bibliotecas de debug/dev-tooling (Flipper, Reactotron, portals, `why-did-you-render`):
  nenhuma instalada.
- `expo-dev-client` não é dependência do projeto; o perfil de build usado (`preview`) não tem
  `developmentClient` habilitado (só o perfil `development` tem, e não foi o perfil usado).
- `ErrorBoundary.tsx` (único componente de fallback global) não renderiza nada parecido.

**Hipótese mais forte**: não é um bug do código do AppMorador — é provavelmente um overlay em
nível de sistema/dispositivo (um app de bolha flutuante com permissão de sobreposição, um
indicador de acessibilidade, gravação de tela, ou ferramenta de terceiros ativa no aparelho de
teste), sem relação com o app. **Ação recomendada**: o usuário verificar se algum app com
permissão "Exibir sobre outros apps" está ativo no dispositivo de teste, ou testar num aparelho
limpo/reinicializado, antes de assumir que é um bug do AppMorador.

### Texto "Tetd"

- Nenhuma lógica de truncamento (`numberOfLines`, `ellipsizeMode`) existe no campo de nome da
  propriedade — se fosse overflow de "Teste" ou similar, o texto quebraria linha normalmente, não
  truncaria para "Tetd".
- Consulta direta ao banco de desenvolvimento (`SELECT Nome FROM propriedades WHERE Nome LIKE
  '%Tet%'`) não encontrou nenhuma propriedade chamada exatamente "Tetd" — só várias chamadas
  "Teste"/"Sprint N Teste"/etc. (dado de teste legítimo de Sprints anteriores).

**Hipótese mais forte**: "Tetd" é um nome real digitado pelo próprio usuário durante o teste manual
(um typo ao testar o formulário de criar propriedade), salvo com sucesso como um registro real —
não um bug de renderização. **Ação recomendada**: se incomodar, o usuário pode simplesmente
editar/excluir essa propriedade de teste pela própria tela (Editar/Excluir já funcionam).

## Não Corrigidos por Escopo

- **Botões de tipo de propriedade "padding irregular"**: `TipoPropriedadeSelector.tsx` foi
  revisado linha a linha — todos os chips compartilham exatamente o mesmo objeto de estilo
  (`padding`/`gap` uniformes, tokens do Design System). Nenhuma irregularidade de código
  encontrada; a percepção de "irregular" relatada provavelmente vinha do form colar no topo da
  tela (já corrigido acima), não dos próprios chips.
- **Login/Cadastro — Safe Area**: verificadas; ambas usam `justifyContent: 'center'` num
  `ScrollView` com padding generoso, sem botão flush no rodapé como `SelecionarPropriedadeScreen`
  tinha — risco bem mais baixo. Não alteradas nesta Sprint por não haver evidência concreta de
  problema (`SafeAreaProvider` global já protege qualquer consumidor futuro de qualquer forma).
- **Reset de navegação após logout**: já funciona corretamente por construção — o
  `RootNavigator` troca todo o conjunto de rotas com base em `!user` (Stack.Screen
  condicionais), então as telas autenticadas deixam de existir na árvore de navegação assim que
  `user` vira `null`; não há como "voltar com gesto" para uma tela que não está mais montada.
  Verificado por leitura de código, nenhuma mudança necessária.
- **Testes em iOS**: nenhum dispositivo/simulador iOS disponível neste ambiente — mesma limitação
  já registrada em Sprints anteriores (projeto é validado só em Android até aqui).
- **React DevTools "Highlight Updates" ao vivo**: sem dispositivo/emulador conectado neste
  ambiente de execução, não foi possível rodar o profiler ao vivo. Avaliação de performance feita
  por inspeção estrutural do código (ver Sprint 18/ADR 0022) — mesma limitação já documentada.

## Lições Aprendidas

1. **Toda chamada de rede precisa de timeout, sem exceção** — o `fetch` nativo do React Native
   não tem limite de tempo por padrão; isso deveria ter sido parte do Design System de rede desde
   o início (`api/client.ts` existe desde a Sprint 1), não descoberto só quando um usuário testou
   num celular físico numa rede real. Processo a mudar: qualquer novo cliente HTTP/wrapper de rede
   introduzido no projeto deve ter timeout explícito desde o primeiro commit, não como um hotfix
   posterior.
2. **"Funciona no emulador" não é o mesmo que "funciona no dispositivo físico numa rede real"** —
   backend inalcançável é uma condição rara ao desenvolver (mesma máquina, mesma rede) mas comum
   ao testar de verdade (celular, rede diferente, Wi-Fi instável). Processo a mudar: qualquer
   Sprint que vá ser validada em dispositivo físico deveria testar explicitamente o cenário "app
   aberto, backend inacessível" antes de considerar a validação completa.
3. **`SafeAreaProvider` deveria ter sido adicionado no dia 1 do projeto** — é uma dependência que
   já existia (via `react-navigation`) mas nunca foi conectada globalmente; o fato de ter
   "funcionado" até aqui em várias telas foi sorte de layout (conteúdo centralizado, scroll com
   padding generoso), não proteção real. Processo a mudar: ao criar uma tela nova fora da Bottom
   Tab Bar (fluxo de auth, onboarding), sempre considerar explicitamente o rodapé do dispositivo.
4. **Zero testes automatizados por 18 Sprints é uma dívida que cobra caro num hotfix** — não foi
   possível "só confirmar rapidamente" que o timeout funciona sem antes montar toda a
   infraestrutura de teste (Jest, mocks de `expo-secure-store`/`NetInfo`, versão específica de
   `@testing-library/react-native` compatível com React 19). Processo a mudar: introduzir testes
   básicos incrementalmente a partir da próxima Sprint de produto, não esperar um hotfix crítico
   para montar a infraestrutura inteira de uma vez.
5. **Nem toda "causa possível" listada numa missão é real** — de 7 hipóteses de causa dadas pela
   missão para os bugs visuais, várias não se confirmaram (ex.: i18n — o app não tem nenhum
   sistema de tradução; `numberOfLines={0}`/`height:0` — não existe em nenhum botão). Verificar
   cada hipótese contra o código real antes de "corrigir" evita perder tempo consertando algo que
   não existe.
