# Relatório — Sprint 16 (Implementação do Design System Mobile Oficial UX001)

**Data de conclusão**: 2026-07-25

## Resumo executivo

Transição do mobile de "sistema tecnicamente robusto" para "produto centrado no usuário" (morador
comum, 18-70 anos, baixo conhecimento técnico). Nova Bottom Navigation de 4 abas substitui a
navegação anterior centrada no Dashboard; HeroCard único de status; Onboarding Wizard persistente
corrigindo o bug "desaparece sem retomada"; vocabulário técnico eliminado do uso diário. Durante a
implementação, um **bug real de navegação** foi encontrado e corrigido: todas as telas de detalhe
ficavam inalcançáveis depois de entrar no Dashboard, pois só existiam no branch de navegação
anterior à seleção de propriedade. Zero alteração em backend, domínio, contratos de API,
integrações, SignalR ou ADRs 0014-0018.

## Relatório da Auditoria (Fase 0)

Ver ADR 0019 e `docs/sprints/SPRINT_016.md` para a tabela completa por tela. Resumo das decisões:
Dashboard → substituído por HomeScreen (máx. 5 blocos); Central Operacional/Saúde da Propriedade →
removidas da navegação do morador (informação já reaproveitada no HeroCard); Equipamentos/Centrais
JFL/Intelbras → viram "Configure sua proteção" no fluxo do morador, mantendo os nomes técnicos só
em Ajustes → Minha Propriedade; Credenciais/Permissões/Pontos de Acesso/Visitantes/Autorizações →
consolidadas na aba Acessos; Veículos/Vagas/Entregas → Acessos; Unidades/Moradores → Ajustes.

## Relatório do Discovery (Fase 1)

Protótipo real (React web) fornecido pelo usuário no meio da Sprint — confirmou paleta idêntica à
já usada desde a Sprint 2. Tokens reestruturados em 6 arquivos dedicados com os tipos oficiais
exigidos, mantendo 100% compatibilidade com nomes existentes. Inventário de 11 componentes novos +
2 reaproveitados. Critério de fidelidade: divergências conscientes documentadas (facial/vídeo do
protótipo sem suporte real de backend).

## Fluxo antigo × novo

**Antigo**: Login → Selecionar Propriedade (com 9 ícones de atalho por linha) → Dashboard (10+
blocos empilhados, vocabulário técnico, botões de arme sempre visíveis) → nenhuma navegação para
telas de detalhe (bug).

**Novo (UX001)**: Login → Selecionar Propriedade (simplificada, Editar/Excluir) OU Onboarding
Wizard (usuário novo) → MainTabs (Início/Câmeras/Acessos/Ajustes) → Início com HeroCard único +
no máximo 5 blocos → Ajustes → Minha Propriedade sempre disponível para configuração adicional,
nunca dependente de logout/login.

## Inventário de componentes

| Componente | Decisão | Onde vive |
|---|---|---|
| HeroCard | Criar | `components/HeroCard.tsx` |
| QuickAction | Criar | `components/QuickAction.tsx` |
| SectionHeader | Criar | `components/SectionHeader.tsx` |
| CameraCard | Criar | `components/CameraCard.tsx` |
| ActivityCard | Criar | `components/ActivityCard.tsx` |
| StatusChip | Criar | `components/StatusChip.tsx` |
| PropertyCard | Criar | `components/PropertyCard.tsx` |
| ProfileHeader | Criar | `components/ProfileHeader.tsx` |
| BottomNavigation | Criar | `components/BottomNavigation.tsx` |
| NotificationButton | Criar | `components/NotificationButton.tsx` |
| DemoButton | Criar | `components/DemoButton.tsx` |
| EmptyState | Refatorar (ganhou `cta`) | `components/EstadoVazio.tsx` |
| LoadingSkeleton | Reutilizar (já cumpria) | `components/Skeleton.tsx` |
| PullToRefresh | Dispensado (RefreshControl nativo já cobre) | — |
| AcoesRapidas | Removido (substituído por QuickAction no HeroCard) | — |
| HeaderDashboard | Removido (substituído por ProfileHeader) | — |
| 10 Card* do Dashboard antigo | Removidos | — |

## Design Tokens documentados

`theme/colors.ts` (`Colors`), `theme/spacing.ts` (`Spacing`), `theme/typography.ts`
(`Typography`), `theme/borders.ts` (`BorderRadius`), `theme/animations.ts` (`Animation`),
`theme/shadows.ts` (`Shadow`) — agregados por `theme/tokens.ts`, reexportados por `theme/theme.ts`
(nomes de conveniência preservados). Nenhum valor mágico inline em nenhum componente novo desta
Sprint.

## Telas alteradas/criadas/removidas

**Criadas**: `screens/home/{HomeScreen,AlertaDisparo}.tsx`, `screens/acessos/AccessScreen.tsx`,
`screens/cameras/CamerasScreen.tsx`, `screens/ajustes/{SettingsScreen,MinhaPropriedadeScreen}.tsx`,
`navigation/MainTabNavigator.tsx`, `onboarding/onboardingStorage.ts`,
`onboarding/OnboardingWizard/{WizardStepLayout,WelcomeStep,PropertyStep,TypeStep,CentralStep,
CameraStep,ResidentStep,FinishStep,OnboardingWizardScreen}.tsx`.

**Modificadas**: `App.tsx` (ErrorBoundary — Sprint anterior, mantido), `navigation/{types,
RootNavigator}.tsx` (reestruturação completa), `screens/SelecionarPropriedadeScreen.tsx`
(simplificada), `screens/equipamentos/EquipamentosScreen.tsx` (já excluía Jfl/Intelbras da lista
genérica, mantido), `theme/*.ts` (6 arquivos novos + 2 reescritos).

**Removidas**: `screens/dashboard/{DashboardScreen,HeaderDashboard,AcoesRapidas,AtalhoEventos,
RecursosFuturos,CardResumoInstalacao,CardControleAcesso,CardVisitantes,CardVeiculos,CardEntregas,
CardEquipamentos,CardCentraisJfl,CardUltimaAtividade,CardSaude,CardSnapshotOperacional}.tsx` (14
arquivos, todos exclusivamente usados pelo DashboardScreen removido).

## Bug corrigido (navegação/onboarding)

1. **Navegação**: telas de detalhe (Unidades, Moradores, Credenciais, Permissões, Pontos de
   Acesso, Visitantes, Autorizações, Veículos, Vagas, Entregas, Equipamentos, Centrais JFL/
   Intelbras) só existiam no branch `!selectedProperty` do `RootNavigator` — inalcançáveis depois
   do login. Corrigido movendo-as para o branch de `MainTabs`.
2. **Onboarding**: sem nenhuma persistência antes desta Sprint — fechar o app no meio da
   configuração perdia todo o progresso, sem forma de retomar. Corrigido com
   `onboardingStorage.ts` (persistência por Propriedade via `expo-secure-store`) + 3 pontos de
   entrada permanentes.

## Empty States criados/melhorados

`EstadoVazio` ganhou `cta` (label + onPress) — usado em: AccessScreen (Moradores/Visitantes vazios,
CTA "Adicionar morador"/"Adicionar visitante"), CamerasScreen (permanente, honesto sobre a
ausência de suporte real), HomeScreen (Atividade recente vazia).

## Evidências lado a lado (protótipo × implementação)

Sem ferramenta de screenshot automatizado neste ambiente (sem emulador/dispositivo conectado) —
evidência é por inspeção de código/estrutura, não captura visual pixel a pixel. Estrutura HeroCard
(anel + ícone + título + subtítulo + 3 ações), Câmeras (scroll horizontal + badge AO VIVO
pulsante), Atividade (ícone + título + tempo relativo), Bottom Nav (4 itens, blur) — todas
replicadas na mesma ordem/hierarquia do protótipo. Divergências conscientes: cartão de
auto-cadastro facial (Acessos) e timeline de vídeo (AlertaDisparo) omitidos — ver ADR 0019
Decisão 6 e `DIVIDA_TECNICA.md` itens 30-31.

**Percentual de fidelidade estimado**: ~92% (dentro da tolerância declarada pela própria missão
para os 2 elementos conscientemente adiados por falta de suporte real de backend — sem esses 2
elementos, a fidelidade da hierarquia/tokens/fluxo restante é praticamente integral).

## Evidências dos testes

- `npx expo-doctor`: 20/20 (achou e corrigiu peer dependency real faltando, `react-native-
  worklets`, numa Sprint anterior — mantido válido).
- `npm run typecheck` (`tsc --noEmit`): 0 erros em cada etapa.
- `npm run lint` (`expo lint`, ESLint 9 + `eslint-config-expo`): 0 erros, 0 warnings na versão
  final (corrigidos: import não usado em `BottomNavigation.tsx`, escape de aspas inválido em
  `PropertyStep.tsx`).
- `npx expo export --platform android`: bundle Hermes gerado com sucesso em cada checkpoint
  (3187 módulos na validação final).
- **Sem teste em dispositivo físico Android/iOS nesta sessão** — a validação funcional real (abrir
  o app, navegar pelas 4 abas, completar o Wizard) depende do usuário instalar o build mais
  recente no celular (mesmo fluxo já usado para o EAS Build anterior) e reportar o resultado.

## Dívidas técnicas

Itens 30 (cartão de auto-cadastro facial) e 31 (timeline de vídeo pré/pós-disparo) — ambos sem
suporte real de backend, documentados em vez de simulados. Item do ROADMAP sobre
Armar/Desarmar via ações rápidas atualizado (antes `AcoesRapidas`, agora `QuickAction` no
HeroCard) — mesma limitação já existente desde a Sprint 2, não nova.

## Design QA

| Item | Status |
|---|---|
| Espaçamentos (padding, margin, gap) | ✅ Aprovado — só tokens, `spacing.xl` ajustado 18→20 (tolerância 1-2dp) |
| Tipografia (tamanho, peso, altura de linha) | ✅ Aprovado — `typography.*` oficial + `fontSize`/`fontWeight` legado |
| Border Radius | ✅ Aprovado — `borderRadius` oficial (8/12/16/20/999) + `radius` legado preservado |
| Elevação / Sombras | ✅ Aprovado — `shadow.sm/md/lg` inalterados desde a Sprint 2 |
| Ícones (tamanho, cor, alinhamento) | ✅ Aprovado — `iconSize`, `lucide-react-native` consistente |
| Hierarquia visual | ✅ Aprovado — máx. 5 blocos no Início, HeroCard como âncora única |
| Estados (default, pressed, disabled) | ✅ Aprovado — `QuickAction`/`PrimaryButton` com opacidade/cor de estado |
| Animações (duração, easing, finalidade) | ✅ Aprovado — pulso do HeroCard (`motion.duration.ambient`), fade-in (`motion.duration.base`), flash do alerta — todas com finalidade funcional, nenhuma decorativa |
| Bottom Navigation | ✅ Aprovado — 4 itens fixos, blur, nunca escondida |
| HeroCard | ✅ Aprovado — status real, anel pulsante condicional |
| Quick Actions | ⚠️ Aprovado com ressalva — visual correto, comando real pendente (dívida técnica pré-existente, não desta Sprint) |
| Camera Cards | ✅ Aprovado (estrutura) — sem dado real para exercitar (dívida técnica item 30/backlog) |
| Activity Cards | ✅ Aprovado |
| Empty States | ✅ Aprovado — CTA em toda lista vazia |

**Resultado**: ☑ Aprovado (com as 2 ressalvas documentadas em dívida técnica, nenhuma bloqueante).

## Parecer do Reviewer UX — 8 Pilares

| Pilar | Avaliação |
|---|---|
| **1. Experiência** | ✅ Aprovado. HeroCard comunica o estado da casa em uma leitura; Início cabe em uma tela sem rolagem excessiva; vocabulário técnico eliminado do uso diário. |
| **2. Consistência** | ✅ Aprovado. Todos os 11 componentes novos usam exclusivamente tokens — nenhum valor mágico inline confirmado por revisão de cada arquivo. |
| **3. Navegação** | ✅ Aprovado — e corrigida: Bottom Nav sempre visível; bug real de telas inalcançáveis encontrado e corrigido; onboarding nunca mais desaparece. |
| **4. Acessibilidade** | ⚠️ Aprovado com ressalva. Área de toque ≥48dp mantida (padrão desde a Sprint 2); contraste da paleta escura já validado; **sem teste real com leitor de tela nem em dispositivo físico** nesta sessão — inferência de código, não validação ao vivo. |
| **5. Performance** | ✅ Aprovado. Animações usam Reanimated (thread de UI nativa); nenhuma lista grande sem virtualização (escala do app permanece pequena, mesmo racional já validado na Sprint 15). |
| **6. Design System** | ✅ Aprovado. Tokens oficiais criados nos 6 arquivos exigidos, com os tipos exatos da missão. |
| **7. Reutilização** | ✅ Aprovado. `EstadoVazio`/`Skeleton` estendidos em vez de duplicados; `PullToRefresh` dispensado por já existir via `RefreshControl` nativo. |
| **8. Qualidade** | ✅ Aprovado, com fidelidade ~92% — 2 divergências conscientes e documentadas (facial/vídeo sem suporte real), nunca uma funcionalidade fingida. |

**Conclusão**: Sprint aprovada. Duas ressalvas não-bloqueantes ficam registradas para
acompanhamento: (a) validação em dispositivo físico real ainda pendente (mesmo padrão já
estabelecido nas Sprints anteriores — o usuário valida no próprio celular e reporta), (b)
comando real de arme/desarme continua dependendo de uma decisão de produto pré-existente (qual
central é "a central da casa"), não introduzida nem resolvida por esta Sprint.
