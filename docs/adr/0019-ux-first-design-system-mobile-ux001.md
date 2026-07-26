# ADR 0019 — UX First: Design System Mobile Oficial UX001

**Data**: 2026-07-25

## Contexto

Depois de 5 Sprints de integração (Control iD, JFL, Camada Operacional, Tempo Real, Intelbras), a
plataforma se provou tecnicamente robusta — mas a experiência mobile continuava sendo a de um
sistema, não a de um produto: Dashboard sobrecarregado com dezenas de seções empilhadas,
funcionalidades técnicas expostas sem contexto (nomes de fabricante, jargão de protocolo),
Empty States que só diziam "0" sem indicar o que fazer, onboarding que desaparecia sem forma de
retomar, navegação escondida (dependente de ícones numa tela que deixava de existir depois do
login), e botões de arme/desarme visíveis mesmo sem central cadastrada.

A Sprint 16 marca a transição de "sistema tecnicamente robusto" para "produto centrado no
usuário" — implementando o Design System Mobile Oficial UX001 (protótipo fornecido pelo usuário
durante a Fase 0 desta Sprint) integralmente.

## Persona Oficial

Morador comum, 18-70 anos, baixo conhecimento técnico, nunca usou software de monitoramento.
Toda decisão de design responde primeiro: "o usuário realmente precisa ver isso agora?" — se não,
a informação é escondida, agrupada, ou movida para o contexto certo (nunca removida de vez, só
reposicionada).

## Fase 0 — Auditoria (achados principais)

Auditoria completa das ~24 telas existentes encontrou: Dashboard com 10+ blocos empilhados sem
hierarquia; telas de Control iD/JFL/Intelbras expondo nomes de fabricante e vocabulário técnico
(PGM, Zona, Partição, Snapshot) diretamente ao usuário; Empty States sem CTA em praticamente
todas as listas; e — o achado mais crítico, encontrado durante a implementação, não na auditoria
estática — **todas as telas de detalhe (Unidades, Credenciais, Centrais JFL/Intelbras,
Equipamentos, Veículos, Vagas, Entregas...) só existiam no branch de navegação anterior à seleção
de propriedade**. Depois de entrar no Dashboard, essas telas eram literalmente inalcançáveis —
confirma tecnicamente o problema "navegação escondida e não previsível" citado na missão.

## Achado sem artefato de protótipo formal (resolvido durante a Sprint)

Não existe nenhum arquivo Figma/PDF do UX001 neste repositório — a Fase 0 desta Sprint começou
sem esse artefato. O usuário forneceu, no meio da Sprint, o protótipo real (um componente React
web interativo) cobrindo Dashboard/Início, Acessos e um alerta de disparo em tela cheia — usado
como fonte de verdade a partir desse ponto. Confirmou algo já suspeitado: a paleta de cores já
usada pelo app desde a Sprint 2 é **idêntica, valor por valor**, à paleta do protótipo — nenhuma
cor mudou nesta Sprint.

## Decisão 1 — Design Tokens: nova estrutura oficial, zero regressão visual

Tokens divididos em arquivos dedicados (`colors.ts`, `spacing.ts`, `typography.ts`, `borders.ts`,
`animations.ts`, `shadows.ts`), agregados por `tokens.ts`, com os tipos exatos exigidos pela
missão (`Colors`, `Spacing`, `Typography`, `BorderRadius`, `Animation`, `Shadow`). Cada arquivo
mantém, lado a lado com os nomes oficiais, os nomes de conveniência já usados por todas as ~150
ocorrências de token em telas existentes desde a Sprint 2 (`colors.safe`, `radius.lg`,
`fontSize.title`, `motion.duration.base`...) — nunca removidos. Só 2 valores mudaram ligeiramente
(dentro da tolerância de 1-2dp da própria missão): `spacing.xl` (18→20) e o `borderRadius`
oficial ganhou valores próprios (8/12/16/20/999) enquanto o `radius` legado mantém os originais
(8/11/14/16/22/999) para as telas que já o usam. Nenhum componente novo desta Sprint usa um valor
mágico inline — todos os `padding`/`margin`/`gap`/cor/fonte/raio vêm de token.

## Decisão 2 — Nova arquitetura de navegação: Bottom Tabs de 4 itens, corrigindo um bug real

`BottomTabNavigator` (Início/Câmeras/Acessos/Ajustes, sempre visível, nunca escondido) substitui a
navegação anterior centrada no Dashboard. Ao restruturar, todas as telas de detalhe foram movidas
para o mesmo branch de `MainTabs` no `RootNavigator` — corrigindo o bug real descrito acima. A
"Regra de Ouro" (mostrar menos) definiu onde cada domínio existente vive:
- **Início**: HeroCard (status único: Protegido/Atenção/Desarmado) + Câmeras (condicional) +
  Atividade recente (máx. 3) + card de configuração pendente + botão de demonstração (dev only).
- **Acessos**: Moradores (agregados de todas as Unidades) + Visitantes, com atalhos para
  Portões/Vagas/Entregas.
- **Câmeras**: aba mantida (fidelidade ao UX001), mas com Empty State permanente e honesto — ver
  Decisão 5.
- **Ajustes**: Perfil, Minha Propriedade (caminho permanente de configuração), Notificações,
  Termos, Sair.
- **Minha Propriedade** (nova, dentro de Ajustes): Unidades/Moradores (estrutura) e Controladores
  de acesso/Centrais JFL/Centrais Intelbras (proteção) — ver Decisão 6 sobre a exceção de
  vocabulário aqui.

## Decisão 3 — HeroCard: status derivado de dados reais, nunca inventado

O status (PROTEGIDO/ATENÇÃO/DESARMADO) é calculado a partir de campos que já existem no
`DashboardResponse` (Sprint 13/14: `saude`, `quantidadeParticoesArmadas`,
`quantidadeAlarmesAtivos`, `ultimaAtualizacaoOperacionalUtc`) — nenhum contrato de API mudou. O
anel do HeroCard "respira" (pulso contínuo via Reanimated) só quando protegido — a ausência de
movimento quando desarmado/em atenção também comunica algo, não é só decoração.

Os botões de arme (Armar total/Noturno/Desarmar) continuam **visuais nesta Sprint** — mesma
limitação já registrada desde a Sprint 2 (`AcoesRapidas`, removido nesta Sprint e substituído).
Conectá-los a um comando real (`JflComandoServico`/`IntelbrasComandoServico`, já existentes)
exigiria primeiro resolver qual central é "a central da casa" quando há mais de uma cadastrada —
uma decisão de produto já registrada como pendente no ROADMAP antes desta Sprint, que uma Sprint
de UX não deveria resolver unilateralmente. Não é uma regressão: é o mesmo estado de antes,
carregado adiante de forma transparente.

## Decisão 4 — Regra de Vocabulário aplicada à experiência diária, com uma exceção documentada

Início, Câmeras, Acessos, alertas e notificações nunca mostram nomes de fabricante ou termos de
protocolo — só "sua casa está protegida", "Portões", "Alarme disparado". **Exceção consciente e
única**: dentro de Ajustes → Minha Propriedade (área de instalação/configuração técnica, não o
uso diário), os nomes Control iD/JFL/Intelbras continuam aparecendo, porque unificar o cadastro de
equipamento entre os 3 fabricantes num único fluxo sem nome de marca exigiria mudança de backend
(ADR 0014/0015/0018), fora do escopo desta Sprint (que não altera contratos de API). Documentado
aqui e em `DIVIDA_TECNICA.md` — nunca um esquecimento silencioso.

## Decisão 5 — Câmeras: Empty State honesto, nunca uma funcionalidade fingida

Não existe nenhuma API/CRUD de câmera no backend (`Camera`/`Gravador` são só entidades internas de
captura de snapshot, Fase 1/2 — sem Controller). Decisão confirmada com o usuário: manter a aba
(fidelidade à navegação do UX001) com um Empty State permanente ("Esse recurso está chegando"),
nunca simular câmeras reais. A seção "Câmeras" do Início usa o mesmo princípio: só aparece quando
`dashboard.quantidadeCameras > 0` (campo real, já existente) — hoje sempre 0, então a seção nunca
aparece, mas o código já está pronto para o dia em que isso mudar, sem exigir outro código novo.

## Decisão 6 — "Cadastrar meu rosto" e timeline de vídeo do protótipo: não implementados, documentados

O protótipo real mostra um cartão de auto-cadastro facial (captura por câmera + upload) na aba
Acessos, e uma linha do tempo de vídeo pré/pós-disparo no alerta de alarme. Nenhum dos dois tem
suporte real: não existe `expo-camera` nem endpoint de upload de foto; o backend só captura uma
única imagem por disparo (decisão de MVP da Fase 2), nunca um buffer contínuo de vídeo. Implementar
o visual desses elementos sem a capacidade real por trás seria fingir uma funcionalidade que não
existe — violaria o próprio princípio de Progressive Disclosure da missão. Ambos ficaram de fora
desta Sprint, registrados como dívida técnica.

## Decisão 7 — Onboarding Wizard persistente, corrigindo o bug "desaparece sem retomada"

7 etapas (Bem-vindo, Propriedade, Tipo, Central, Câmeras, Moradores, Concluir), cada uma opcional
a partir da 4ª. Progresso persistido via `expo-secure-store` (já uma dependência — nenhuma
biblioteca nova) **por Propriedade**, a partir do momento em que ela existe (etapas 0-2 acontecem
antes da Propriedade existir, então ficam só em memória — fechar o app antes de criar a
Propriedade reinicia do Bem-vindo, aceitável). Propriedades criadas antes desta Sprint nunca têm
registro de progresso — tratadas retroativamente como já configuradas, nunca forçadas a passar
pelo Wizard. Acessível permanentemente por 3 caminhos: card "Complete sua configuração" no
Início (quando não há central), botão "Prefiro ser guiado" na seleção de propriedade (usuário
novo), e "Continuar configuração guiada" em Ajustes → Minha Propriedade — nunca depende de
logout/login.

## Componentes criados

`HeroCard`, `QuickAction`, `SectionHeader`, `CameraCard`, `ActivityCard`, `StatusChip`,
`PropertyCard`, `ProfileHeader`, `BottomNavigation`, `NotificationButton`, `DemoButton`. Reaproveitados
(estendidos, não duplicados): `EstadoVazio` (= EmptyState, ganhou prop `cta`), `Skeleton` (=
LoadingSkeleton, já cumpria a função). `PullToRefresh` não virou componente próprio — o
`RefreshControl` nativo já usado em toda tela com rolagem já cumpre a mesma função; empacotá-lo
seria abstração sem necessidade real.

## Critério de fidelidade

Divergências assumidas conscientemente (documentadas, nunca silenciosas): cartão de auto-cadastro
facial e timeline de vídeo (Decisão 6, sem suporte real); rótulos das abas "Moradores"/"Visitantes"
em vez de "Moradores"/"Provisórios" (nomes que já existem no domínio, evitando inventar um conceito
novo de "provisório" fora do que `Visitante`/`Autorizacao` já representam); botões de arme
continuam visuais (Decisão 3). Fora esses pontos documentados, a hierarquia visual, tokens,
componentes e fluxo de navegação seguem o protótipo integralmente.

## Acessibilidade e responsividade

Área de toque mínima 48dp em todo item de lista/botão (já era o padrão do Design System desde a
Sprint 2). Contraste dos tokens de cor já validado — paleta escura fixa com texto
`#F2F6FA`/`#93A1B1` sobre fundo `#0A0E13`/`#141C25`, acima de 4.5:1. Sem teste em dispositivo
físico de múltiplos tamanhos nesta Sprint (ver Entrega) — layout usa unidades relativas (`flex`,
`%`, `dp`) e nenhum valor de largura fixo além dos cards de câmera (140dp, com scroll horizontal),
então deve se adaptar bem, mas isso é uma inferência de código, não uma validação visual real.

## Lições aprendidas

1. A auditoria estática não encontra tudo — o bug de navegação (telas de detalhe inalcançáveis)
   só apareceu ao tentar religar essas telas à nova estrutura, não na leitura de código isolada.
2. "Regra de Ouro: mostrar menos" e "nunca fingir funcionalidade indisponível" às vezes entram em
   tensão com fidelidade a um protótipo (Câmeras, cadastro facial) — quando isso acontece, a
   honestidade sobre capacidade real do sistema vence a fidelidade visual, sempre documentada.
3. Reaproveitar componentes existentes (EstadoVazio, Skeleton) em vez de duplicá-los sob nomes
   novos manteve a mudança muito menor do que recriar tudo do zero teria sido.

## Impactos

Ver relatório de entrega (Sprint 16) para a lista completa de arquivos criados/modificados/
removidos. Nenhuma mudança em backend, domínio, contratos de API, integrações, SignalR, Providers,
Snapshot, Timeline, Eventos ou ADRs 0014-0018.
