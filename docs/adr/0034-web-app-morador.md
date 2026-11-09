# ADR 0034 — Web App do morador como cópia do Mobile via react-native-web

**Data**: 2026-11-02

## Contexto

O usuário pediu um web app **idêntico** ao app mobile do morador, **responsivo**, sem nenhuma
alteração em `mobile/`. Já existia um `PainelWeb/` (ADR 0030), mas ele é outro produto: painel
administrativo em React DOM + MUI, com telas e navegação próprias. O Mobile já tinha
`react-native-web` instalado e `Platform.OS === 'web'` previsto nos storages, mas nunca rodou de
verdade no navegador.

## Problema

Como entregar no navegador as mesmas telas, componentes e tokens do Mobile sem reescrever a
interface (o que faria o visual divergir) e sem tocar no Mobile?

## Alternativas consideradas

1. **Reescrever em React DOM/MUI (como o PainelWeb)**: visual diferente por construção, e cerca
   de 40 telas reescritas. Descartada.
2. **Ligar o alvo web dentro de `mobile/`**: zero duplicação, mas exige alterar o Mobile, o que o
   usuário proibiu explicitamente. Descartada.
3. **Pasta nova `webapp/` copiando o Mobile e rodando via Expo Web (react-native-web)**: mesmos
   componentes e tokens, logo o mesmo visual. As adaptações de navegador ficam isoladas na
   cópia. **Escolhida.**
4. **Redesenho com a skill Hallmark**: ela sorteia um tema visual novo, o que contraria o
   requisito "idêntico". Por decisão do usuário, a Hallmark foi usada só como **audit**
   (`sloplint`).

## Decisão

`webapp/` é uma cópia de `mobile/` (src, App.tsx, assets e configs; sem eas.json e sem config
nativa no app.json), com estas adaptações, todas exclusivas do web app:

- **Responsivo** (`theme/layout.ts`, `layout/`): abaixo de 1024px o app fica igual ao mobile
  (largura total + navegação inferior). A partir de 1024px, as mesmas 4 abas viram barra lateral
  (`tabBarPosition: 'left'`) e o conteúdo fica numa coluna de 760px (formulários: 480px), via
  `screenLayout` do React Navigation 7.
- **`Alert.alert` → `components/DialogoAlerta`**: no react-native-web, `Alert.alert` é uma
  implementação vazia. Sem isso, as 24 confirmações (sair, excluir, revogar, comandos) nunca
  apareceriam e a ação nunca rodaria. A assinatura é a mesma, então as 15 telas só trocaram o
  import.
- **Push indisponível na web**: o backend só registra `Ios`/`Android`. `pushService` devolve
  `'indisponivel'`, `PushNotificationProvider` só repassa os filhos na web (as APIs de
  `expo-notifications` lançam `UnavailabilityError` e derrubavam o app inteiro no ErrorBoundary),
  e a tela de Notificações explica que, no navegador, os avisos chegam com a página aberta, via
  SignalR.
- **SignalR com `withCredentials: false`**: o cliente de navegador envia cookies por padrão, e o
  CORS da Api (sem `AllowCredentials`, de propósito) bloqueava a negociação. A autenticação é só
  pelo JWT, então cookies não são necessários. Nenhuma mudança no backend.
- **Feedback tátil**: falhas de `expo-haptics` são silenciadas, porque desktop não tem vibração.
- **Foto facial**: no navegador vem como data URI e pode exceder a cota do `localStorage`. Nesse
  caso vale só para a sessão, a mesma regra de "nunca finge que salvou" do ADR 0020.
- **Foco por teclado** (`layout/estilosGlobaisWeb.ts`): anel em `:focus-visible` com
  `colors.safe`. É um achado do audit Hallmark (contraste de 1.02:1) e não muda o visual em
  clique ou toque.

## Consequências

- O visual fica igual ao do Mobile por construção: os mesmos componentes renderizam via
  react-native-web.
- **Duplicação consciente**: correções feitas em `mobile/src` não chegam ao `webapp/src`
  sozinhas. Cada Sprint que mexer no Mobile precisa decidir se replica no web app.
- **Sessão no navegador**: os tokens ficam em `localStorage` (fallback que já existia em
  `secureStorage.ts`), o que é mais exposto a XSS que o Keychain/Keystore. Isso viola a
  regra `.rules/mobile.md` ("sessão sempre no armazenamento seguro da plataforma"), que não
  tem equivalente seguro no navegador sem mudar o backend para cookie `HttpOnly`. Fica
  registrado em dívida técnica, não resolvido aqui.
- A seleção de propriedade continua só em memória (como no Mobile): um F5 volta para "Suas
  propriedades".

## Impactos

- Backend: nenhum. A origem `http://localhost:8081` já estava liberada em
  `appsettings.Development.json`. Em produção, a origem do web app precisa entrar em
  `Cors:AllowedOrigins`.
- Mobile: nenhum arquivo alterado.

## Arquivos afetados

`webapp/` (novo). Diferenças em relação a `mobile/`: `App.tsx`, `app.json`, `package.json`,
`src/theme/{layout,tokens,theme}.ts`, `src/layout/*`, `src/components/{BottomNavigation,DialogoAlerta}.tsx`,
`src/navigation/{RootNavigator,MainTabNavigator}.tsx`, `src/notifications/{pushService,PushNotificationProvider}.ts(x)`,
`src/realtime/RealtimeContext.tsx`, `src/services/ServicoFeedbackTatil.ts`,
`src/credenciais/fotoFacialLocal.ts`, `src/screens/ajustes/NotificacoesScreen.tsx`, o import de
`Alert` em 15 telas e 2 testes novos.

## Como revisar futuramente

Rever se o web app deve deixar de ser cópia quando:
- a divergência entre `mobile/src` e `webapp/src` passar a custar mais que unificar (monorepo com
  pacote `shared/` ou alvo web dentro do próprio Mobile); ou
- o backend ganhar sessão por cookie `HttpOnly`, o que permite tirar os tokens do `localStorage`.
