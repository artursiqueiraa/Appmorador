# Dívida Técnica

Registro de toda dívida técnica identificada durante o desenvolvimento: descrição, motivo,
impacto, prioridade e sugestão de resolução. Nenhum atalho ou compromisso temporário fica só na
cabeça de quem o aceitou.

## 1. Decisões técnicas da Sprint 2 não formalizadas como ADR

**Descrição**: a Sprint 2 tomou decisões permanentes (adoção do `react-native-reanimated` como
biblioteca padrão de animação do projeto; criação do enum `TipoPropriedade`; formalização do
Design System via `theme/tokens.ts`) que não foram registradas em `docs/adr/` no momento em que
foram tomadas.

**Motivo**: a sessão em que a Sprint 2 foi implementada foi encerrada antes da etapa de
documentação ser concluída.

**Impacto**: baixo a médio — as decisões já estão implementadas e funcionando, mas alguém lendo só
`docs/adr/` no futuro não encontraria o porquê dessas escolhas.

**Prioridade**: baixa (não bloqueia nada em produção).

**Sugestão de resolução**: registrar como ADR(s) numa próxima Sprint de manutenção/documentação,
usando o histórico já existente em `docs/reviews/SPRINT_002.md` como fonte.

## 2. Bridge de compatibilidade `theme.ts` → `tokens.ts`

**Descrição**: `theme.ts` reexporta nomes de conveniência antigos (`colors`, `spacing`, `radius`,
`fontSize`, `fontWeight`) para não quebrar `LoginScreen`/`CadastroScreen`/`SplashScreen`/
`PrimaryButton`/`TextField`, que ainda importam de `theme.ts` em vez de `tokens.ts` diretamente.

**Motivo**: evitar um refactor de import em telas fora do escopo da Sprint 2 (que tratava do
Dashboard).

**Impacto**: nenhum funcional — é só uma camada de compatibilidade. Duas formas de importar o
mesmo valor coexistem (`theme.ts` vs. `tokens.ts` direto).

**Prioridade**: baixa.

**Sugestão de resolução**: numa Sprint futura que já esteja mexendo nessas telas antigas, migrar o
import para `tokens.ts` diretamente e simplificar `theme.ts`.

## 3. Filtro por zona cortado do v1 da Central de Eventos

**Descrição**: a Central de Eventos (Sprint 3) não tem filtro por zona — só período (chips) e
busca de texto livre.

**Motivo**: não existe hoje nenhum endpoint que lista as zonas de uma propriedade; criar um só
para popular um seletor de filtro seria escopo extra não pedido nesta Sprint. A busca de texto
livre cobre parcialmente a necessidade (casa contra o nome da zona).

**Impacto**: baixo — usuários com poucas zonas conseguem usar a busca; propriedades com muitas
zonas sentem mais falta de um filtro dedicado.

**Prioridade**: baixa.

**Sugestão de resolução**: quando um endpoint de listagem de zonas existir (ou for pedido por
outra Sprint), adicionar um filtro por zona à `FiltroEventos` e à tela `FiltrosEventos.tsx`.

## 4. Agregação entre múltiplas fontes de eventos ainda não implementada

**Descrição**: `IEventosServico` hoje chama diretamente a única fonte registrada
(`JflFonteEventos`) e repassa sua paginação. Não existe lógica de mesclar+reordenar+paginar
resultados de duas ou mais fontes de `IFonteEventos` ao mesmo tempo.

**Motivo**: decisão deliberada (ADR 0006) — não há uma segunda fonte real hoje para validar essa
lógica contra, e escrevê-la sem um caso real seria código especulativo.

**Impacto**: nenhum hoje. Quando uma segunda fonte real existir (ex.: controle de acesso), vira
bloqueador para a Sprint que a implementar.

**Prioridade**: baixa (não bloqueia nada hoje; sobe de prioridade junto com a Sprint que adicionar
a segunda fonte).

**Sugestão de resolução**: decidir a estratégia (paginação por fonte com merge em memória vs.
paginação unificada por uma view/consulta cross-fonte) no momento em que a segunda fonte for
implementada, não antes.

## 5. Integração real de controle de acesso é uma fase futura própria

**Descrição**: o AppMorador não tem nenhuma entidade/cadastro/integração de controle de acesso
(catraca, fechadura, leitor facial) hoje. A referência indicada pelo usuário
(`Teste-portaria-main1`) tem uma integração Control iD real (cliente HTTP contra a API REST
documentada) nunca validada contra hardware real (seed com IPs/credenciais fake), e uma
integração Intelbras (protocolo CGI) que, ao ser testada contra hardware real naquele projeto,
revelou-se um leitor de controle de acesso da família ASI — com ajustes ainda documentados como
pendentes lá (`AccessUser.cgi`, ordem de chamada do `FaceInfoManager`).

**Motivo**: implementar isso exigiria uma entidade de dispositivo nova, cadastro por propriedade
e um cliente HTTP novo — dimensão comparável à Fase 2 (Snapshot) do projeto, não um item dentro
de uma Sprint de UI de eventos.

**Impacto**: a Central de Eventos hoje só mostra eventos de central de alarme (`Origem.Jfl`). A
arquitetura já está preparada (`IFonteEventos`) para receber essa fonte sem redesenho, mas o dado
real não existe.

**Prioridade**: média — é a funcionalidade mais visível ainda faltando da visão original de
"Central de Eventos", mas depende de uma decisão de produto sobre qual hardware suportar
primeiro (Control iD vs. Intelbras ASI/CGI) antes de começar.

**Sugestão de resolução**: tratar como fase própria no Roadmap (ver `docs/roadmap/ROADMAP.md`), reaproveitando
os achados técnicos desta investigação como ponto de partida — não recomeçar a investigação do
zero.

## 6. CRUD de Usuários incompleto — sem Perfis/Papéis

**Descrição**: a Sprint 3.1 partiu do pressuposto de que existiam Editar/Alterar senha/
Desativar/Reativar/Excluir usuário e um sistema de Perfis (Administrador/Supervisor/Operador/
Morador). Investigação confirmou que **nada disso existe** — só `Register`/`Login`/`Refresh`/
`Logout` (`AutenticacaoServico`). Não há entidade de Papel/Permissão em lugar nenhum do domínio.

**Motivo**: decisão explícita do usuário na Sprint 3.1 ("Homologar só o que existe") — em vez de
forçar a homologação de algo inexistente ou criar essas funcionalidades fora de escopo (a Sprint
era só de estabilização, não de features novas), o que existe hoje foi homologado de verdade e
esta lacuna foi registrada para decisão de escopo de uma Sprint futura.

**Impacto**: nenhum hoje — nenhuma tela/fluxo do produto depende de Perfis ou de editar/desativar
usuário. Vira bloqueador só se um caso de uso futuro (ex.: múltiplos membros por propriedade,
Sprint mencionada em `docs/roadmap/ROADMAP.md`) precisar de controle de acesso diferenciado.

**Prioridade**: baixa hoje; sobe quando houver decisão de produto para múltiplos papéis/usuários
por propriedade.

**Sugestão de resolução**: decidir modelo de Papéis (RBAC simples vs. ownership-only) numa Sprint
de produto futura, antes de implementar qualquer endpoint de CRUD de usuário além do que já
existe.

## 7. Claim `securityStamp` emitido no JWT mas nunca validado

**Descrição**: `JwtTokenService.GenerateAccessToken` embute `usuario.SecurityStamp` como claim,
mas nenhum middleware/handler compara esse valor contra o `SecurityStamp` atual do usuário no
banco a cada request. O próprio comentário em `Usuario.cs` já registrava isso como
"ainda não implementado, mas o campo já existe".

**Motivo**: o campo foi adicionado prevendo um mecanismo futuro de invalidação de sessão (ex.: ao
trocar a senha, todos os access tokens já emitidos deveriam parar de funcionar antes de
expirarem naturalmente), mas esse mecanismo nunca foi implementado porque não existe hoje nenhum
fluxo que troque `SecurityStamp` (não há endpoint de troca de senha — ver item 6).

**Impacto**: nenhum hoje, porque nada troca `SecurityStamp` em produção. Vira relevante no dia em
que "Alterar senha" for implementado — sem essa validação, um access token emitido antes da troca
continuaria válido até expirar (até `Jwt:AccessTokenMinutes`, hoje 20 minutos por padrão).

**Prioridade**: baixa hoje; obrigatório implementar junto com o endpoint de troca de senha (item
6), nunca depois.

**Sugestão de resolução**: ao implementar "Alterar senha", adicionar validação do claim
`securityStamp` num middleware/handler de autenticação (comparando contra o valor atual em
banco) antes de liberar o endpoint de troca de senha como "concluído".

## 8. Criação automática do banco de dados em si não é possível com o usuário atual

**Descrição**: `Program.cs` aplica migrations automaticamente no startup (`Database.MigrateAsync()`,
ver ADR 0008), o que cria tabelas/schema pendente automaticamente — mas **não** consegue criar o
banco de dados em si (`CREATE DATABASE appmorador`) quando ele ainda não existe, porque o usuário
MySQL `appmorador` usado pela aplicação é deliberadamente restrito ao próprio banco (sem
privilégio `CREATE DATABASE` — decisão de segurança de uma sessão anterior).

**Motivo**: conceder `CREATE DATABASE` ao usuário de runtime da aplicação ampliaria a superfície
de acesso desse usuário além do necessário para operação normal — uma regressão de segurança que
não deveria ser feita apenas para eliminar um único comando manual de setup.

**Impacto**: baixo — um desenvolvedor configurando o ambiente do zero precisa rodar um único
`CREATE DATABASE appmorador;` manualmente (com um usuário privilegiado, ex.: `root`) antes do
primeiro `dotnet run`. Documentado em `docs/setup/SETUP_AMBIENTE.md`. Depois desse passo único,
tudo mais (tabelas, seed) é automático.

**Prioridade**: baixa (o atrito real é um único comando, uma única vez por ambiente).

**Sugestão de resolução**: se o projeto adotar um usuário de provisionamento separado (distinto do
usuário de runtime da aplicação, usado só em scripts de setup/CI), esse usuário poderia ter
`CREATE DATABASE` sem tocar no privilégio do usuário de runtime — reavaliar se isso for
implementado.

## 9. Licença do projeto ainda não definida

**Descrição**: na preparação para publicação da `v0.3.0-alpha`, o repositório não tem um arquivo
`LICENSE` na raiz. `mobile/LICENSE` (a licença MIT do próprio Expo/650 Industries, resquício do
template `create-expo-app`) foi removido por estar mal-atribuído ao projeto — não substituído por
nenhuma licença própria ainda.

**Motivo**: decidir entre licença proprietária (todos os direitos reservados, apropriado para um
produto comercial B2C/B2B) ou uma licença de código aberto é uma decisão de negócio, não técnica —
não deve ser assumida silenciosamente. Perguntado ao usuário durante a preparação da `v0.3.0-alpha`;
decisão adiada por ele.

**Impacto**: baixo enquanto o repositório for privado/não publicado externamente. Se o repositório
for tornado público sem um `LICENSE`, o padrão legal implícito (em várias jurisdições, incluindo a
ausência de licença explícita no GitHub) é "todos os direitos reservados" — ou seja, a ausência do
arquivo não é uma brecha de segurança, mas deixa a intenção do autor implícita em vez de explícita.

**Prioridade**: baixa até a decisão de modelo de negócio ser tomada; média se o repositório for
tornado público antes disso.

**Sugestão de resolução**: criar `LICENSE` na raiz assim que o modelo de negócio for decidido —
ver README.md (seção "Licença") para o estado atual.
