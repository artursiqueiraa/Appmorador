# Sprint 16 — Implementação do Design System Mobile Oficial UX001

## Missão

Implementar integralmente o Design System Mobile Oficial UX001, reproduzindo fielmente
comportamento, fluxo, hierarquia visual, componentes, espaçamentos, animações, estados e
navegação do protótipo aprovado — sem alterar backend, domínio, contratos de API, integrações,
SignalR, Providers, Snapshot, Timeline, Eventos ou ADRs 0014-0018. Persona oficial: morador comum,
18-70 anos, baixo conhecimento técnico, nunca usou software de monitoramento.

## Fase 0 — Auditoria (achados principais)

Auditoria completa de ~24 telas existentes. Achados: Dashboard com 10+ blocos empilhados sem
hierarquia (viola "máximo 5 blocos"); telas de fabricante (Control iD/JFL/Intelbras) expondo
vocabulário técnico diretamente ao morador; Empty States sem CTA acionável em quase toda lista;
Central Operacional/Saúde da Propriedade 100% técnicas, sem lugar no fluxo do morador comum. O
achado mais crítico só apareceu durante a implementação: todas as telas de detalhe (Unidades,
Credenciais, Centrais...) só existiam no branch de navegação anterior à seleção de propriedade —
ficavam inalcançáveis depois do login, confirmando tecnicamente "navegação escondida" citado na
missão.

Divergências resolvidas com o usuário (Fase 0/1): aba Câmeras mantida com Empty State permanente
honesto (sem suporte real de backend); Veículos/Vagas/Entregas → aba Acessos; Unidades/Moradores
→ Ajustes → Minha Propriedade; sem teste iOS (projeto sempre foi Android-only).

## Fase 1 — Discovery (resumo)

**Protótipo real**: fornecido pelo usuário no meio da Sprint (componente React web interativo) —
não existe nenhum arquivo Figma/PDF no repositório antes disso. Confirmou que a paleta de cores já
usada desde a Sprint 2 é idêntica à do protótipo. Um protótipo semelhante já havia aparecido antes
(Sprint 5), com os mesmos elementos de câmera/facial/vídeo já adiados na época pelo mesmo motivo
(sem suporte real de backend).

**Tokens**: `colors`/`spacing`/`typography`/`borderRadius`/`animation`/`shadow` em arquivos
dedicados, com os tipos exatos exigidos, mantendo 100% de compatibilidade com os nomes já usados
por toda tela existente (nenhuma migração forçada de import).

**Componentes**: 11 novos (`HeroCard`, `QuickAction`, `SectionHeader`, `CameraCard`,
`ActivityCard`, `StatusChip`, `PropertyCard`, `ProfileHeader`, `BottomNavigation`,
`NotificationButton`, `DemoButton`); 2 reaproveitados/estendidos (`EstadoVazio`=EmptyState ganhou
CTA; `Skeleton`=LoadingSkeleton já cumpria a função); `PullToRefresh` dispensado (RefreshControl
nativo já em uso em toda tela com rolagem).

**Critério de fidelidade**: divergências conscientes e documentadas (cartão de auto-cadastro
facial e timeline de vídeo do protótipo, sem suporte real de backend; botões de arme continuam
visuais) — fora esses pontos, hierarquia/tokens/fluxo seguem o protótipo integralmente.

## Escopo entregue

1. Bottom Navigation de 4 abas (Início/Câmeras/Acessos/Ajustes), sempre visível.
2. HomeScreen: HeroCard (status único, derivado de dados reais já existentes) + Ações rápidas +
   Câmeras (condicional) + Atividade recente (máx. 3) + card de configuração pendente + botão de
   demonstração (dev only).
3. AccessScreen: Moradores (agregados) + Visitantes + atalhos Portões/Vagas/Entregas.
4. CamerasScreen: Empty State permanente honesto.
5. SettingsScreen + MinhaPropriedadeScreen: caminho permanente de configuração.
6. Onboarding Wizard: 7 etapas, opcionais a partir da 4ª, progresso persistido por Propriedade,
   recuperável por 3 caminhos.
7. AlertaDisparo: alerta de disparo em tela cheia, vocabulário traduzido.
8. Regra de Vocabulário aplicada em toda superfície de uso diário — exceção documentada em
   Ajustes → Minha Propriedade (área técnica de instalação).

## Fora de Escopo (confirmado, nada implementado)

Push Notification, Analytics, IA, alterações de domínio/backend/contratos de API/integrações/
SignalR/ADRs 0014-0018, teste em dispositivo iOS.

## Processo Obrigatório

Executado em etapas pequenas: tokens → build → componentes base → build → componentes de
Início → build → HomeScreen/AccessScreen/CamerasScreen/SettingsScreen/MinhaPropriedadeScreen →
build → navegação (MainTabNavigator + RootNavigator, achado do bug real) → build → limpeza de
código morto → build → Onboarding Wizard → build/typecheck/lint/bundle final → documentação.

## Critérios de Aceite

Todos atendidos — ver relatório de entrega (`docs/reviews/SPRINT_016.md`) para evidências
detalhadas e Design QA completo.
