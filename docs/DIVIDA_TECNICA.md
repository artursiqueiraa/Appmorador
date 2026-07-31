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

## 10. Soft delete não retrofitado em Central/Zona/Camera/Gravador/Ocorrencia

**Descrição**: a Sprint 6 introduziu soft delete (ADR 0009) só para `Propriedade`/`Unidade`/
`Morador`. As entidades do pipeline de alarme/câmera (`Central`, `Zona`, `Camera`, `Gravador`,
`Ocorrencia`) continuam sem exclusão lógica (e sem endpoint de exclusão nenhum, físico ou
lógico). Como `Propriedade` agora tem um query filter global e `Camera`/`Central`/`Gravador` têm
FK obrigatória para `Propriedade` sem filtro correspondente, o EF Core emite um warning de
validação no startup (`Model.Validation[10622]`) sobre a navegação `Propriedade` nessas 3
entidades poder ficar nula inesperadamente se algum código futuro fizer
`.Include(x => x.Propriedade)` nelas.

**Motivo**: retrofitar soft delete em 5 entidades adicionais estava fora do escopo da Sprint 6
(que era especificamente sobre o agregado Propriedade/Unidade/Morador) — expandir o escopo
aumentaria o risco de regressão na v0.3.0-alpha sem necessidade real hoje.

**Impacto**: nenhum hoje — confirmado por busca no repositório que nenhum código atual usa
`.Include(x => x.Propriedade)` em `Camera`/`Central`/`Gravador`. Vira relevante se/quando algum
código futuro precisar navegar de uma câmera/central para sua propriedade via Include.

**Prioridade**: baixa hoje.

**Sugestão de resolução**: ao implementar exclusão de Central/Câmera/Gravador (ou qualquer
funcionalidade que navegue `.Include(x => x.Propriedade)` nessas entidades), decidir então se
elas também ganham soft delete (seguindo o padrão do ADR 0009) ou se o warning é resolvido de
outra forma (ex.: `.IsRequired(false)` na relação).

## 11. Lixeira/restauração de registros excluídos ainda não existe

**Descrição**: o ADR 0009 estabeleceu soft delete para preparar uma futura tela de Lixeira/
Restauração, mas essa tela/funcionalidade não foi implementada na Sprint 6 — hoje um registro
excluído fica marcado no banco (`Excluido=true`) mas não há nenhuma forma de visualizá-lo ou
restaurá-lo pela interface.

**Motivo**: decisão explícita do usuário na Sprint 6 — "preparar a arquitetura... sem
implementá-la nesta Sprint".

**Impacto**: nenhum hoje — uma exclusão feita pelo usuário parece (e funciona como) definitiva do
ponto de vista da experiência, mesmo o dado estando preservado no banco.

**Prioridade**: baixa até haver uma necessidade de negócio concreta de restauração.

**Sugestão de resolução**: quando implementada, a consulta de itens excluídos usa
`.IgnoreQueryFilters()` explicitamente (nunca por acidente) filtrando por `Excluido=true`, e a
restauração é só marcar `Excluido=false`/limpar `DataExclusaoUtc`/`ExcluidoPorUsuarioId`.

## 12. Soft delete não retrofitado em Credencial↔HistoricoCredencial (warning benigno)

**Descrição**: mesma situação do item 10, agora entre `Credencial` (query filter) e
`HistoricoCredencial` (sem soft delete, propositalmente — é auditoria pura, nunca excluída, ver
ADR 0010). O EF Core emite o mesmo warning de validação de modelo (`Model.Validation[10622]`) no
startup sobre a navegação `Credencial` em `HistoricoCredencial` poder ficar nula inesperadamente.

**Motivo**: `HistoricoCredencial` não deve ganhar soft delete — um registro de auditoria não devia
poder "sumir" mesmo que sua Credencial seja excluída, e essa Sprint não implementa nenhuma tela
que leia o histórico (`.Include(x => x.Credencial)` não é usado em código nenhum hoje).

**Impacto**: nenhum hoje — confirmado por busca no repositório.

**Prioridade**: baixa hoje.

**Sugestão de resolução**: ao implementar uma tela/endpoint de auditoria que precise navegar de um
registro de histórico até sua Credencial, decidir então se o warning é resolvido via
`.IsRequired(false)` na relação, ou se passa a valer a pena manter `CredencialId` sozinho (sem
navegação) já que o histórico deve sobreviver à exclusão lógica da credencial de qualquer forma.

## 13. Mobile: `PermissaoAcesso.DataInicial`/`DataFinal` sem editor na tela de Permissões

**Descrição**: o domínio (`PermissaoAcesso`) e a API já suportam vigência por data
(`DataInicial`/`DataFinal`, ex.: "esta credencial só funciona entre 01/08 e 15/08"), mas a tela
mobile `PermissoesScreen` só expõe edição de dias da semana e horário — não há campo para as
datas de vigência.

**Motivo**: um seletor de intervalo de datas não existe no Design System hoje, e adicionar um
introduziria uma dependência nova só para esse campo — mesma decisão já tomada para o filtro de
período da Central de Eventos (Sprint 3, ADR 0006: chips pré-definidos em vez de date-picker).
Como `DataInicial`/`DataFinal` são opcionais e a API já os aceita, isso é um recorte de
apresentação, não do domínio.

**Impacto**: baixo — nenhuma credencial cadastrada pelo mobile hoje pode ter vigência por data
(sempre `null`, ou seja, "sem limite de data"), mas o campo já existe e funciona via API/curl.

**Prioridade**: baixa até haver um pedido de produto concreto para vigência temporária de acesso
(ex.: acesso de prestador de serviço por um período).

**Sugestão de resolução**: quando necessário, avaliar um date-picker nativo (`@react-native-
community/datetimepicker`, já comum no ecossistema Expo) ou reaproveitar os chips de período já
usados na Central de Eventos, adaptados para selecionar um intervalo em vez de "desde agora".

## 14. Integração real de Controle de Acesso com hardware ainda não existe

**Descrição**: a Sprint 7 construiu todo o domínio de Controle de Acesso (Credencial,
PermissaoAcesso, PontoAcesso — ver ADR 0010), mas nenhuma comunicação com equipamento físico foi
implementada: sem TCP/HTTP com equipamentos, sem SDK de fabricante (Control iD, Intelbras,
Hikvision, JFL), sem reconhecimento facial real, sem QR Code funcional, sem leitura de Tag física,
sem leitor biométrico real.

**Motivo**: decisão explícita do usuário na missão da Sprint 7 — "construir o cérebro do Controle
de Acesso, não os conectores"; a integração física fica para uma Sprint futura própria.

**Impacto**: nenhum hoje para o fluxo de cadastro/gestão (100% funcional via API/mobile). O
domínio nunca reflete o estado real de um equipamento físico — é só a intenção de negócio
(quem deveria ter acesso a onde, quando).

**Prioridade**: média — é a lacuna mais visível do módulo de Controle de Acesso, mas depende de
decisão de produto sobre qual fabricante integrar primeiro.

**Sugestão de resolução**: ver `docs/roadmap/ROADMAP.md`, item "Integração de Controle de Acesso e
Portões" — reaproveitar as entidades desta Sprint como estão (ADR 0010, seção "Como revisar
futuramente"), nunca criar um modelo de domínio paralelo para a integração.

## 15. Evento "Autorização expirada" não é registrado automaticamente no histórico

**Descrição**: `TipoEventoHistoricoVisitante.AutorizacaoExpirada` existe no enum (domínio
preparado), mas nada o produz hoje. O Status efetivo Pendente/Ativa/Expirada é computado em tempo
de leitura (ver ADR 0011) — nunca há um momento de "gravação" natural da transição para Expirada
sem um job/scheduler, que esta Sprint deliberadamente não implementa.

**Motivo**: decisão explícita de arquitetura (ADR 0011) — evitar um job/scheduler (regra
permanente de simplicidade em MVPs) e evitar gravar o evento como efeito colateral de uma leitura
(GET), o que produziria duplicação/spam de histórico ou exigiria lógica de dedupe frágil.

**Impacto**: nenhum no fluxo funcional hoje — o Status efetivo aparece correto em toda consulta
(API e Dashboard), só o registro histórico do momento exato da expiração não existe.

**Prioridade**: baixa hoje; sobe se uma necessidade de auditoria completa (ex.: "quando exatamente
esta autorização expirou") surgir.

**Sugestão de resolução**: se um job de expiração automática for implementado no futuro (por
exemplo, para notificar o morador quando uma autorização vence), ele deve gravar
`AutorizacaoExpirada` no momento real da transição — não antes, para não registrar um evento que
o sistema não presenciou de fato.

## 16. Soft delete não retrofitado em Visitante↔HistoricoVisitante (warning benigno)

**Descrição**: mesma situação dos itens 10 e 12, agora entre `Visitante` (query filter) e
`HistoricoVisitante` (sem soft delete, propositalmente — auditoria pura, nunca excluída, ver ADR
0011). O EF Core emite o mesmo warning de validação de modelo (`Model.Validation[10622]`) no
startup sobre a navegação `Visitante` em `HistoricoVisitante` poder ficar nula inesperadamente.

**Motivo**: `HistoricoVisitante` não deve ganhar soft delete — um registro de auditoria não devia
poder "sumir" mesmo que seu Visitante seja excluído, e essa Sprint não implementa nenhuma tela que
leia o histórico (`.Include(x => x.Visitante)` não é usado em código nenhum hoje).

**Impacto**: nenhum hoje — confirmado por busca no repositório.

**Prioridade**: baixa hoje.

## 17. Soft delete não retrofitado em Veiculo↔HistoricoVeiculo e Vaga↔HistoricoVaga (warning benigno)

**Descrição**: mesma situação dos itens 10, 12 e 16, agora entre `Veiculo`/`Vaga` (query filter) e
`HistoricoVeiculo`/`HistoricoVaga` (sem soft delete, propositalmente — auditoria pura, nunca
excluída, ver ADR 0012). O EF Core emite o mesmo warning de validação de modelo
(`Model.Validation[10622]`) no startup para as duas navegações.

**Motivo**: os dois históricos não devem ganhar soft delete pelo mesmo motivo dos itens anteriores
— um registro de auditoria não devia poder "sumir" mesmo que o Veículo/Vaga seja excluído, e essa
Sprint não implementa nenhuma tela que leia esses históricos.

**Impacto**: nenhum hoje — confirmado por busca no repositório.

**Prioridade**: baixa hoje.

**Sugestão de resolução**: mesma dos itens anteriores — resolver via `.IsRequired(false)` na
relação quando uma tela de auditoria real precisar navegar até o Veículo/Vaga.

## 18. Placa de Veículo sem índice único físico no banco

**Descrição**: a unicidade de `Veiculo.Placa` é garantida em código de aplicação
(`VeiculoServico`, via `GetByPlacaAsync`), não por um índice único na coluna do banco — decisão
deliberada (ver ADR 0012) para permitir que uma placa seja recadastrada depois que o veículo
antigo é excluído logicamente, já que um índice único físico não distingue automaticamente
"excluído" de "ativo".

**Motivo**: soft delete (ADR 0009) e unicidade física de coluna são difíceis de conciliar sem um
índice filtrado — não suportado de forma portável pelo Pomelo/MySQL nesta versão do projeto.

**Impacto**: baixo — em teoria, uma escrita concorrente (duas requisições simultâneas cadastrando
a mesma placa) poderia escapar da checagem em código antes do primeiro `SaveChangesAsync`
confirmar. Nenhuma evidência de que isso tenha ocorrido; volume de escrita esperado (cadastro
manual por um morador/proprietário) torna a janela de corrida extremamente improvável.

**Prioridade**: baixa hoje.

**Sugestão de resolução**: se o volume de cadastro concorrente crescer, avaliar um índice único
filtrado (`WHERE Excluido = 0`) — suportado nativamente pelo MySQL 8 via índice funcional/generated
column, mas não mapeado pelo EF Core Fluent API hoje sem SQL bruto na migration.

## 19. Soft delete não retrofitado em Entrega↔HistoricoEntrega (warning benigno)

**Descrição**: mesma situação dos itens 10, 12, 16 e 17, agora entre `Entrega` (query filter) e
`HistoricoEntrega` (sem soft delete, propositalmente — auditoria pura, nunca excluída, ver ADR
0013). O EF Core emite o mesmo warning de validação de modelo (`Model.Validation[10622]`) no
startup sobre a navegação `Entrega` em `HistoricoEntrega` poder ficar nula inesperadamente.

**Motivo**: `HistoricoEntrega` não deve ganhar soft delete — um registro de auditoria não devia
poder "sumir" mesmo que sua Entrega seja excluída, e essa Sprint não implementa nenhuma tela que
leia o histórico (`.Include(x => x.Entrega)` não é usado em código nenhum hoje).

**Impacto**: nenhum hoje — confirmado por busca no repositório.

**Prioridade**: baixa hoje.

**Sugestão de resolução**: mesma dos itens anteriores — resolver via `.IsRequired(false)` na
relação quando uma tela de auditoria real precisar navegar até a Entrega.

**Sugestão de resolução**: ao implementar uma tela/endpoint de auditoria que precise navegar de um
registro de histórico até seu Visitante, decidir então se o warning é resolvido via
`.IsRequired(false)` na relação, ou se passa a valer a pena manter `VisitanteId` sozinho (sem
navegação) já que o histórico deve sobreviver à exclusão lógica do visitante de qualquer forma.

## 20. Integração Control iD nunca validada contra hardware físico real (Sprint 11)

**Descrição**: `ControlIdProvider` foi validado via comunicação HTTP real contra um simulador
local (`backend/tools/ControlIdSimulator`), não contra um equipamento Control iD físico. Toda a
comunicação de rede/protocolo/tratamento de erro foi exercitada de verdade (requisições HTTP
genuínas, não um mock em memória do `IControlIdProvider`), mas o comportamento exato de um
dispositivo real (timing, formato de resposta em casos de borda, comportamento de autenticação)
permanece não confirmado.

**Motivo**: nenhum equipamento Control iD real estava acessível neste ambiente de
desenvolvimento — confirmado com o usuário na Fase 1 da Sprint 11, que optou explicitamente por
validar com simulador em vez de bloquear a Sprint.

**Impacto**: médio — a arquitetura (Provider, DTOs, mapper, criptografia de senha) está correta e
testada estruturalmente, mas um equipamento real pode expor formatos de resposta, códigos de
evento (`access_logs`) ou particularidades de autenticação que o simulador (deliberadamente
simplificado) não replica.

**Prioridade**: alta antes de qualquer uso em produção com hardware real; não bloqueia a Sprint
em si (arquitetura e código-fonte já preparados para receber a validação real sem redesenho).

**Sugestão de resolução**: assim que um equipamento Control iD físico estiver disponível,
repetir a mesma bateria de testes desta Sprint (testar conexão, consultar informações, sincronizar
os 3 domínios, importar eventos) apontando `Equipamento.Ip`/`Porta` para o dispositivo real, e
ajustar `ControlIdMapper`/`ControlIdWireDtos` conforme qualquer divergência de formato encontrada.

## 21. Sincronização de Credencial não envia um valor físico real (tag/PIN)

**Descrição**: `EquipamentoIntegracaoServico.SincronizarCredenciaisAsync` monta
`CredencialParaSincronizar` com `Valor: null` para toda credencial — o domínio de `Credencial`
(Sprint 7, ADR 0010) modela só `Tipo`/`Status`, nunca um valor físico real (número de tag RFID,
código PIN, template biométrico).

**Motivo**: a Sprint 7 deliberadamente não modelou o valor físico da credencial (fora do escopo
daquela Sprint — "o cérebro do Controle de Acesso, não os conectores"). A Sprint 11 sincroniza o
que existe hoje (tipo/vínculo com o Morador), sem inventar um campo novo fora do próprio escopo
desta Sprint (que é migração de integração, não expansão do domínio de Credencial).

**Impacto**: baixo hoje (nenhuma tela usa esse valor), mas significa que a sincronização real com
um equipamento físico não conseguiria efetivamente autorizar uma tag/PIN específico até que
`Credencial` ganhe esse campo.

**Prioridade**: média — necessária antes de uma sincronização real ter efeito prático em um
controlador de acesso físico.

**Sugestão de resolução**: numa Sprint futura de evolução do domínio de Controle de Acesso,
adicionar um campo de valor físico (`Credencial.Valor` ou equivalente, provavelmente cifrado como
`Equipamento.SenhaCriptografada`) e atualizar `CredencialParaSincronizar`/`ControlIdMapper` para
usá-lo.

## 22. Integração JFL Active 100 Bus (comandos de superusuário) nunca validada contra hardware físico real (Sprint 12)

**Descrição**: `JflProvider` foi validado via comunicação TCP real contra um simulador local
(`backend/tools/JflSimulator`), não contra uma central Active 100 Bus física. Toda a comunicação
de protocolo (handshake, framing, checksum, correlação SEQ, comandos de armar/desarmar/PGM/
inibir zona) foi exercitada de verdade via `TcpClient`, com o simulador mantendo estado em
memória e refletindo corretamente cada comando no próximo status — mas o comportamento exato de
uma central real (timing, particularidades de firmware, códigos de problema em cenários reais)
permanece não confirmado. Mesma classe de pendência do item 20 (Control iD, Sprint 11).

**Motivo**: nenhuma central JFL Active 100 Bus física estava acessível neste ambiente de
desenvolvimento — confirmado com o usuário na Fase 1 da Sprint 12, que optou explicitamente por
um simulador simplificado em vez de bloquear a Sprint.

**Impacto**: médio — a arquitetura (Provider, parsers de protocolo, correlação de comandos) está
correta e testada estruturalmente (inclusive contra os achados documentados do protocolo, como a
convenção de bits invertida do campo de inibição de zonas), mas um equipamento real pode expor
particularidades (timing de resposta, valores de campo fora do documentado) que o simulador
(deliberadamente simplificado) não replica.

**Prioridade**: alta antes de qualquer uso em produção com hardware real; não bloqueia a Sprint
em si (arquitetura e código já preparados para receber a validação real sem redesenho).

**Sugestão de resolução**: assim que uma central Active 100 Bus física estiver disponível,
configurá-la para discar para o AppMorador (mesmo IP/porta usados pelo simulador) e repetir a
mesma bateria de testes desta Sprint (handshake, keep-alive, status, armar/desarmar/PGM/zonas)
contra o equipamento real, ajustando os parsers (`CentralStatusResponse` e sub-parsers) conforme
qualquer divergência de formato encontrada — em especial a ordem dos nibbles do campo ZONA, que o
próprio manual JFL não exemplifica numericamente (comentário já presente no código portado).

## 23. Central (pipeline de eventos) e Equipamento (Fabricante=Jfl, comandos) não são unificados

**Descrição**: uma central JFL física precisa ser cadastrada duas vezes no AppMorador hoje — uma
vez como `Central` (Fase 1, obrigatória para o pipeline de eventos existente, resolver
`Ocorrencia`/`Zona`) e outra como `Equipamento` (Fabricante=Jfl, Sprint 12, para os comandos de
superusuário) — cada cadastro usando o mesmo Número de Série manualmente, sem nenhuma validação
cruzada além de um vínculo de leitura exibido na tela de detalhes (`CentralJflResponse.
CentralVinculadaId/Nome`).

**Motivo**: unificar as duas entidades exigiria migrar `Ocorrencia.CentralId`/`Zona.CentralId`
para apontar a `Equipamento` — uma mudança de schema invasiva no pipeline de eventos já em
produção (o mais sensível do projeto, por lidar com alarmes reais), fora do escopo desta Sprint
de migração de comandos. Decisão confirmada com o usuário na Fase 1: cadastros separados com
vínculo de leitura, não unificação de schema.

**Impacto**: baixo a médio — nenhuma falha funcional (os dois cadastros funcionam
independentemente), mas é uma experiência de cadastro duplicada e potencialmente confusa para o
usuário final, que precisa saber que os dois números de série devem ser idênticos manualmente.
Além disso, `Central` ainda não tem nenhuma tela de cadastro via API (só é criada
manualmente/via seed) — o que hoje limita a ocorrência prática deste problema.

**Prioridade**: média — vale revisitar quando `Central` ganhar CRUD via API (ainda não existe,
ver histórico do projeto) ou quando o volume de centrais JFL cadastradas justificar a unificação.

**Sugestão de resolução**: numa Sprint futura, avaliar unificar `Central` e `Equipamento`
(migrando as FKs de `Ocorrencia`/`Zona`) ou, alternativamente, criar CRUD de `Central` que
auto-cria o `Equipamento` correspondente (e vice-versa) no mesmo fluxo de cadastro, eliminando a
duplicação sem tocar no schema de eventos.

## 24. Eventos de transição de conectividade não são registrados na Timeline Operacional (Sprint 13)

**Descrição**: a missão da Sprint 13 exemplificou a "Timeline Operacional" incluindo eventos como
"Equipamento Offline"/"Equipamento Reconectado". Hoje essas transições não geram nenhuma linha de
`Ocorrencia`/`EventoEquipamento` — são só uma sobrescrita do campo `Equipamento.Status`, sem
nenhum registro auditável do momento em que a mudança aconteceu.

**Motivo**: implementar isso exigiria uma nova fonte de eventos (ou uma tabela de transições de
status), o que se aproxima do "novo domínio de eventos" que a missão da Sprint 13 proibiu
explicitamente ("não criar novos protocolos... não alterar regras de negócio já homologadas" —
Timeline Operacional foi construída deliberadamente como reaproveitamento puro da Central de
Eventos já existente, ver ADR 0016 Decisão 5).

**Impacto**: baixo hoje — o estado atual de cada equipamento (`Status`) e o Snapshot consolidado
(`SnapshotOperacional`) continuam corretos em toda leitura; só falta o histórico de "quando"
uma transição específica ocorreu.

**Prioridade**: baixa hoje; sobe se um caso de uso concreto de auditoria de conectividade
(ex.: "há quanto tempo este equipamento está offline", SLA de disponibilidade) for pedido.

**Sugestão de resolução**: quando necessário, criar uma fonte de eventos dedicada (`Origem`
específica, ex. `Conectividade`) que registre uma linha toda vez que
`EquipamentoIntegracaoServico`/`JflComandoServico` mudar `Equipamento.Status`, reaproveitando o
`IFonteEventos` já existente — sem duplicar a Central de Eventos.

## 25. Grupos SignalR por Perfil de usuário não implementados (Sprint 14)

**Descrição**: a missão da Sprint 14 pediu que as conexões do `OperacionalHub` fossem organizadas
também por Perfil do usuário (Administrador/Operador/Morador/Técnico), além de por Propriedade.
Só o agrupamento por Propriedade foi implementado.

**Motivo**: o domínio não tem nenhum sistema de Perfil/Papel/RBAC (dívida técnica item 6) — cada
Propriedade tem um único dono (`Propriedade.ProprietarioId`). Implementar grupos por Perfil
exigiria inventar um conceito de RBAC que nenhuma Sprint de produto decidiu ainda, o que violaria
a proibição explícita da Sprint 14 de "não alterar o domínio". Decisão confirmada com o usuário na
Fase 1: implementar só o agrupamento real (Propriedade), registrar o resto aqui.

**Impacto**: nenhum hoje — o produto é B2C self-service de dono único; não há usuário adicional
por Propriedade para diferenciar por perfil.

**Prioridade**: baixa hoje; sobe junto com uma futura Sprint de RBAC/múltiplos usuários por
Propriedade.

**Sugestão de resolução**: quando um sistema de Perfil/Papel for implementado, adicionar um
segundo agrupamento no `OperacionalHub` (ex.: `propriedade:{id}:perfil:{perfil}`) sem alterar o
fluxo Provider→Snapshot→Publicador já existente — só uma nova dimensão de agrupamento.

## 26. Disparo assíncrono do alarme (AlarmEventProcessor) não validado de ponta a ponta via TCP real (Sprint 14)

**Descrição**: `AlarmEventProcessor` é o único gatilho verdadeiramente assíncrono da Camada
Operacional em Tempo Real (Sprint 14) — uma central pode discar um evento a qualquer momento, fora
de uma requisição HTTP. A publicação em tempo real disparada por ele (`PublicarNovoEventoAsync` +
`RegenerarEPublicarAsync`) foi validada por revisão de código e por reaproveitar o mesmo mecanismo
já comprovado ao vivo pelos outros 2 pontos de mutação (Control iD, comandos JFL), mas não foi
disparada de ponta a ponta enviando um evento Contact ID real via TCP neste ambiente.

**Motivo**: validar isso exigiria uma `Central` (pipeline de eventos, Fase 1) cadastrada com o
mesmo Número de Série do Equipamento de teste — `Central` não tem CRUD via API (item 23) e não
havia cliente MySQL disponível neste ambiente para inserir uma diretamente.

**Impacto**: baixo — o código que publica é idêntico (mesma chamada de método) ao já validado ao
vivo nos outros 2 pontos; o único risco real não coberto é algo específico do contexto de execução
do `AlarmEventProcessor` (Scoped, disparado pelo `JflTcpServer` fora de um request HTTP).

**Prioridade**: média — validar antes de depender desta notificação para um cenário de produção
real de disparo de alarme.

**Sugestão de resolução**: assim que `Central` tiver CRUD via API (ou um cliente MySQL estiver
disponível), repetir o teste desta Sprint enviando um evento Contact ID real (ex.: código 1130)
via TCP para uma Central cadastrada, com um cliente SignalR conectado, e confirmar o recebimento
de `NovoEventoOperacional` + `OperacionalAtualizado` (motivo `AlarmeDisparado`).

## 27. Integração Intelbras (AMT 8000) nunca validada contra hardware físico real, nem contra um protocolo real documentado (Sprint 15)

**Descrição**: `IntelbrasProvider` foi validado via comunicação HTTP real contra um simulador
local (`backend/tools/IntelbrasSimulator`), não contra uma central Intelbras física. Diferente de
Control iD (Sprint 11) e JFL (Sprint 12), que tinham repositórios de referência legados com
protocolo real documentado para portar, este projeto não tem nenhuma documentação oficial pública
nem uma referência já investigada para um protocolo TCP proprietário Intelbras — a Sprint 15
modelou a central como uma API HTTP (decisão consciente, ver ADR 0018), o que é uma simplificação
arquitetural deliberada, não uma tentativa de replicar o protocolo real da AMT 8000.

**Motivo**: nenhuma central Intelbras física nem documentação de protocolo verificável estava
disponível neste ambiente — inventar um protocolo binário sem fonte real arriscaria apresentar
como "validado" algo que não foi.

**Impacto**: alto antes de qualquer uso em produção — diferente dos itens 20/22 (onde a
arquitetura de protocolo já é real, só falta o hardware), aqui o próprio protocolo de transporte
(HTTP) é uma simplificação; uma central AMT 8000 real provavelmente fala um protocolo TCP
proprietário diferente do modelado.

**Prioridade**: alta antes de produção; não bloqueia a Sprint (o objetivo era provar a
arquitetura de integração, não entregar uma integração pronta para uso real).

**Sugestão de resolução**: antes de qualquer uso real, investigar a documentação oficial
Intelbras (ISECNet2/protocolo de central AMT) ou obter acesso a hardware físico para engenharia
reversa documentada, seguindo o mesmo rigor já aplicado a JFL (ADR 0015) — nunca assumir que o
protocolo HTTP desta Sprint reflete o protocolo real do equipamento.

## 28. PGM e Inibição de Zona não implementados para Intelbras (Sprint 15)

**Descrição**: a missão da Sprint 15 marcou PGM e Inibição de Zona como "se suportado" — não
foram implementados no `IIntelbrasProvider`/`IntelbrasComandoServico`/`IntelbrasSimulator`.

**Motivo**: escopo mínimo da Sprint era Arme/Desarme/Status/Eventos; adicionar PGM/Zona sem um
propósito de teste concreto adicional seria escopo extra não pedido.

**Impacto**: nenhum hoje — a tela de Detalhes da central Intelbras não oferece essas ações.

**Prioridade**: baixa — sobe só se um caso de uso de produto concreto pedir PGM/Inibição de Zona
para centrais Intelbras.

**Sugestão de resolução**: quando necessário, seguir o mesmo padrão já usado por JFL
(`ZoneInhibitCommandService`/`PgmCommandService`) adaptado ao vocabulário HTTP desta integração.

## 29. EquipamentoIntegracaoServico.ResolverProvider continua hardcoded a um único fabricante dial-out (achado da Sprint 15, não corrigido)

**Descrição**: `EquipamentoIntegracaoServico.ResolverProvider` devolve `IControlIdProvider?`
especificamente — só resolve Control iD. A Sprint 15 identificou isso como uma limitação real
durante a Auditoria (Fase 0), mas não precisou corrigi-la porque Intelbras usa um serviço próprio
(`IntelbrasComandoServico`), nunca `EquipamentoIntegracaoServico`.

**Motivo**: corrigir isso exigiria generalizar `ResolverProvider` para resolver por
`IEnumerable<I...Provider>` (mesmo padrão já usado por `IFonteEventos`), o que só se justifica
quando um **segundo** fabricante precisar reaproveitar de verdade o fluxo de sincronização de
Moradores/Credenciais/Permissões/Importação de Eventos do Control iD — generalizar antes disso
seria especulativo (mesma regra já aplicada em "Como revisar futuramente" da ADR 0014).

**Impacto**: nenhum hoje — Intelbras e JFL já provam que fabricantes podem ter serviços de
orquestração inteiramente próprios sem tocar `EquipamentoIntegracaoServico`.

**Prioridade**: baixa — só sobe quando um fabricante genuinamente precisar do mesmo fluxo de
sincronização do Control iD (não apenas do vocabulário de comando de alarme).

**Sugestão de resolução**: quando esse fabricante existir de fato, generalizar
`ResolverProvider` para `IEnumerable<IEquipamentoProvider>` resolvido por `Fabricante`, seguindo
o mesmo padrão de `IFonteEventos`.

## 30. Cartão de auto-cadastro facial (câmera + upload) do protótipo UX001 não implementado (Sprint 16)

**Descrição**: o protótipo UX001 fornecido nesta Sprint mostra um cartão "Cadastrar meu rosto" na
aba Acessos (captura via câmera do celular, upload direto para as faciais da casa). Não
implementado — não existe `expo-camera` no projeto, nem nenhum endpoint de upload de foto no
backend (`Morador.FotoPath` existe no domínio desde a Sprint 6, mas nunca foi preenchido por
nenhuma Sprint). Interessante notar que este mesmo elemento já havia aparecido num protótipo
anterior (Sprint 5) e foi conscientemente adiado na época pelo mesmo motivo — a lacuna é
conhecida e consistente ao longo do projeto, não uma surpresa desta Sprint.

**Motivo**: implementar o visual sem a capacidade real (câmera + upload + reconhecimento) seria
fingir uma funcionalidade que não existe — viola o próprio princípio de Progressive Disclosure da
missão desta Sprint.

**Impacto**: nenhum hoje — a aba Acessos funciona plenamente sem esse cartão (Moradores/
Visitantes via API real).

**Prioridade**: média — é o elemento mais visível do protótipo ainda faltando, mas depende de uma
Sprint própria de integração de câmera/reconhecimento facial (fora do escopo de UX).

**Sugestão de resolução**: quando uma Sprint de reconhecimento facial acontecer, adicionar
`expo-camera` + um endpoint de upload de foto (`POST /api/moradores/{id}/foto`, populando
`FotoPath`), e só então reativar este cartão na aba Acessos.

## 31. Timeline de vídeo pré/pós-disparo do protótipo UX001 não implementada (Sprint 16)

**Descrição**: o protótipo UX001 mostra, no alerta de disparo em tela cheia, uma linha do tempo
de vídeo com "scrubbing" entre pré-disparo/disparo/pós-disparo (indicando um buffer contínuo de
vídeo). O `AlertaDisparo` implementado nesta Sprint mostra só um espaço reservado para a imagem
única do momento do disparo — porque é só isso que o backend captura (decisão de MVP da Fase 2,
ver item "Módulo de Clips/vídeo" no ROADMAP), nunca um buffer contínuo.

**Motivo**: mostrar uma barra de "scrubbing" fingindo esse buffer seria simular uma capacidade que
não existe.

**Impacto**: nenhum hoje — o alerta de disparo continua funcional (mostra o alarme, permite ligar
para emergência, permite desarmar como falso positivo), só sem o elemento de vídeo do protótipo.

**Prioridade**: baixa — depende de uma Sprint própria de buffer de vídeo contínuo (streaming/
gravação), uma mudança de infraestrutura significativa, não uma tela nova.

**Sugestão de resolução**: quando um buffer de vídeo contínuo existir (fora do escopo desta
Sprint e da Fase 2), reativar a timeline de scrubbing no `AlertaDisparo`.

## 32. Foto de credencial facial não é persistida no backend (Sprint 17)

**Descrição**: `Morador.FotoPath` existe no domínio desde a Sprint 6, mas nunca ganhou rota de
escrita (dívida técnica item 30, pré-existente). A Sprint 17 implementou captura real via
`expo-image-picker` (câmera/galeria) com pré-visualização real, mas a credencial `Facial` é criada
sem persistir a foto em lugar nenhum do backend — a imagem fica só localmente
(`credenciais/fotoFacialLocal.ts`, `expo-secure-store`), associada ao id da credencial. Se o app
for reinstalado ou o armazenamento local for limpo, a miniatura desaparece — a credencial em si
(no backend) continua intacta.

**Motivo**: implementar upload real exigiria um endpoint novo (`POST /api/moradores/{id}/foto` ou
equivalente) e uma decisão de armazenamento (disco local do servidor, blob storage), fora do
escopo desta Sprint de UX (que não altera contratos de API). O usuário confirmou explicitamente
essa abordagem (capturar/pré-visualizar com aviso, nunca fingir persistência) em vez de esconder o
cadastro facial.

**Impacto**: baixo hoje — o cadastro facial funciona (credencial real criada, permissões
funcionam), só a miniatura de foto é efêmera ao nível do dispositivo.

**Prioridade**: média — sobe quando uma Sprint de integração de reconhecimento facial de verdade
acontecer (mesmo item do ROADMAP "Reconhecimento facial de Moradores").

**Sugestão de resolução**: endpoint de upload de foto + `Morador.FotoPath` preenchido de verdade,
substituindo `fotoFacialLocal.ts` por uma URL real vinda do backend.

## 33. `expo-camera` deliberadamente não instalado — captura cobre câmera e galeria só via `expo-image-picker` (Sprint 17)

**Descrição**: a prioridade de captura definida na Discovery da Sprint 17 previa 4 níveis (fluxo
do equipamento/Control iD → câmera → galeria → indisponível). `expo-image-picker` (já dependência
do projeto) cobre tanto câmera (`launchCameraAsync`) quanto galeria (`launchImageLibraryAsync`)
sozinho — instalar `expo-camera` também para a mesma captura de câmera seria uma dependência
nativa nova sem ganho real.

**Motivo**: reduzir risco de engenharia (nova dependência nativa = novo ponto de falha em builds
EAS) sem perder nenhuma capacidade real — o resultado funcional (uma foto capturada pela câmera)
é idêntico.

**Impacto**: nenhum — nenhuma funcionalidade da missão ficou sem cobertura.

**Prioridade**: baixa — só sobe se uma Sprint futura precisar de recursos que só `expo-camera`
oferece (preview customizado em tempo real, filtros, detecção de rosto ao vivo).

**Sugestão de resolução**: instalar `expo-camera` só quando um caso de uso concreto exigir algo
que `expo-image-picker` não ofereça.

## 34. Perfil (morador/técnico) é preferência de UI local, sem RBAC real no domínio (Sprint 17)

**Descrição**: `auth/profilePreference.ts` guarda `perfil` (`'morador'`/`'tecnico'`) só em
`expo-secure-store`, sem nenhum campo correspondente em `EntrarResponse`/`StoredUser` ou no
backend. Qualquer pessoa com acesso ao próprio celular pode alternar para "técnico" livremente.

**Motivo**: o domínio não tem RBAC completo (dívida técnica item 6, CRUD de Usuários/Perfis).
Implementar RBAC real seria uma Sprint própria de backend — fora do escopo desta Sprint de UX, que
usa a preferência local só para organizar a interface (esconder telas técnicas por padrão), nunca
como proteção de dado real.

**Impacto**: nenhum na segurança real — o backend continua validando posse/autenticação
normalmente, sem relação com este campo. O único efeito é cosmético: quais telas aparecem.

**Prioridade**: média — sobe junto com o item 6 (RBAC completo), quando uma Sprint de backend
introduzir perfis de verdade (Administrador/Supervisor/Operador/Morador/Síndico/Técnico) — nesse
momento, `perfil` local deveria ser substituído pelo perfil real vindo da API.

**Sugestão de resolução**: quando o RBAC real existir, substituir `profilePreference.ts` por um
campo vindo de `EntrarResponse`, mantendo a mesma interface (`useAuth().perfil`) para não exigir
mudança nas telas que já o consomem.

## 35. Rótulos amigáveis de PGM são só locais, um por dispositivo (Sprint 17)

**Descrição**: `acessos/pgmLabels.ts` guarda o mapeamento "PGM 3" → "Abrir portão" só localmente
(`expo-secure-store`, chave por `equipamentoId`) — o backend (`PgmStatusInfo`) não tem campo de
nome amigável. Trocar de dispositivo (reinstalar o app, usar outro celular) perde a configuração
de rótulos, voltando ao padrão `Comando N`.

**Motivo**: adicionar uma coluna de rótulo amigável ao backend seria mudança de contrato de API,
fora do escopo desta Sprint de UX.

**Impacto**: baixo — o comando continua funcionando (aciona a PGM real), só o nome mostrado volta
ao genérico até ser configurado de novo no novo dispositivo.

**Prioridade**: baixa — sobe se usuários relatarem perder a configuração com frequência (múltiplos
dispositivos por propriedade, troca frequente de celular).

**Sugestão de resolução**: mover o rótulo para o backend (nova coluna/tabela associada a
`Equipamento` + número de PGM), com endpoint de leitura/escrita, substituindo o
`expo-secure-store` local.

## 36. Painel de Controle da aba Acessos só funciona para centrais JFL (Sprint 17)

**Descrição**: o Painel de Controle criado nesta Sprint busca PGMs `permitida=true` só de
equipamentos `Fabricante === 'Jfl'`. Intelbras nunca implementou PGM (item 28) e Control iD é um
leitor de controle de acesso, sem conceito de PGM — uma propriedade só com esses dois fabricantes
nunca vê nenhum comando no painel (Empty State honesto aparece nesse caso).

**Motivo**: nunca inventar um comando que não existe de verdade para um fabricante sem essa
capacidade — decisão explícita do usuário nesta Sprint (ver ADR 0020 Decisão 3).

**Impacto**: nenhum hoje — reflete a capacidade real do sistema, não uma limitação artificial da
Sprint.

**Prioridade**: baixa — sobe só se uma Sprint futura implementar PGM/comando equivalente para
Intelbras (mesmo item 28) ou um conceito de acionamento para Control iD.

**Sugestão de resolução**: quando essa capacidade existir para outro fabricante, estender a busca
de `carregarComandosJfl` (`screens/acessos/AccessScreen.tsx`) para incluir também esses
equipamentos, generalizando o nome da função.

## 37. Sem rotina de backup de banco agendada/automática (Sprint 17.5)

**Descrição**: `scripts/backup_database.ps1` gera um dump completo do banco sob demanda — não há
nenhum agendamento (Task Scheduler do Windows ou equivalente) rodando isso periodicamente.

**Motivo**: esta Sprint documenta e automatiza o que já existe localmente (ambiente de
desenvolvimento) — introduzir infraestrutura de agendamento de produção seria escopo novo, fora
do que foi pedido (backup/portabilidade/disaster recovery de documentação e scripts, não operação
de produção).

**Impacto**: nenhum hoje (ambiente de desenvolvimento local) — seria relevante para um ambiente de
produção real com dado de cliente de verdade.

**Prioridade**: média — sobe quando o projeto tiver um ambiente de produção real rodando.

**Sugestão de resolução**: agendar `scripts/backup_database.ps1` via Task Scheduler (Windows) ou
`cron`/systemd timer (Linux, se o backend for hospedado lá no futuro), com rotação/retenção de
backups antigos.

## 38. Push real ainda não validado ponta a ponta — sem credenciais Firebase reais (Sprint 19)

**Descrição**: `FirebaseNotificationProvider` opera em modo "sem-op documentado"
(`FirebaseOptions.Configurado == false`) — loga o que enviaria e retorna sucesso simulado, nunca
chama a API real do FCM. O Mobile obtém um token nativo (`getDevicePushTokenAsync`) mesmo sem
`google-services.json` real, mas esse token não corresponde a nenhum projeto Firebase de verdade.

**Motivo**: criar um projeto Firebase real e obter as credenciais (conta de serviço para o backend,
`google-services.json` para o app) exige acesso ao Firebase Console, indisponível nesta sessão —
decisão explícita do usuário (perguntado antes de qualquer código) foi construir a arquitetura real
completa com esse modo sem-op, em vez de não entregar nada.

**Impacto**: nenhuma notificação chega de verdade a um dispositivo físico hoje. Toda a lógica
(decisão de notificar, mensagem, canal, debounce, ciclo de vida do token) está implementada e
testada, só o envio real depende de configuração externa.

**Prioridade**: alta — sobe assim que o usuário tiver acesso a um projeto Firebase real.

**Sugestão de resolução**: criar o projeto no Firebase Console, gerar a credencial de conta de
serviço (colocar o caminho em `Firebase:CredenciaisPath`, `appsettings.json`), adicionar o app
Android e baixar `google-services.json` para o build do Expo (`android.googleServicesFile` no
`app.json`), gerar um novo build EAS e validar em um dispositivo físico.

## 39. iOS não implementado/validado (Fase 3, Sprint 19)

**Descrição**: só Android foi implementado e (parcialmente, ver item 38) preparado para validação
nesta Sprint. `DispositivoPush.Plataforma` já suporta `Ios`, mas nenhum certificado APNs foi
configurado e não há dispositivo iOS físico neste ambiente para testar.

**Motivo**: previsto explicitamente pela própria missão da Sprint 19 ("se não houver dispositivo
iOS físico, documentar como dívida técnica e validar apenas Android").

**Impacto**: nenhum morador com iPhone recebe push nesta versão.

**Prioridade**: média — sobe quando houver um dispositivo iOS físico disponível para validação.

**Sugestão de resolução**: registrar App ID com push capability, gerar certificado/chave APNs,
configurar no Firebase (FCM pode usar APNs como transporte) e validar em dispositivo físico.

## 40. Agrupamento de notificações (Fase 8.1) não implementado (Sprint 19)

**Descrição**: só o limite de frequência (debounce, Fase 8.2) foi implementado. Múltiplas
notificações do mesmo tipo em sequência não são agrupadas numa única mensagem ("3 novas atividades
em Casa Serra").

**Motivo**: o debounce de 60s já evita a maior parte do ruído prático (mesmo evento/equipamento
repetido); implementar agregação por janela de tempo sem um caso de uso real pressionando por isso
seria abstração especulativa.

**Impacto**: baixo — em uso normal, eventos de tipos diferentes chegam como notificações
separadas, o que já é aceitável.

**Prioridade**: baixa — sobe se o volume real de notificações em produção se mostrar incômodo.

**Sugestão de resolução**: agregar por `(EventoNotificacaoTipo, PropriedadeId)` numa janela curta
(ex.: 2 minutos) antes de enviar, com uma mensagem consolidada quando há mais de uma ocorrência.

## 41. Debounce em memória não sobrevive a múltiplas instâncias de backend (Sprint 19)

**Descrição**: `DebounceNotificacaoEmMemoria` usa um `ConcurrentDictionary` local ao processo — se
o backend rodar mais de uma instância (load balancer, múltiplos containers), cada instância tem seu
próprio estado de debounce, permitindo notificações duplicadas.

**Motivo**: continuação deliberada da filosofia de simplicidade do projeto — sem Redis/fila
antecipados sem necessidade real (mesmo racional de Sprints anteriores). Hoje o backend roda como
processo único.

**Impacto**: nenhum hoje. Só se manifesta se/quando o backend escalar horizontalmente.

**Prioridade**: baixa — sobe junto com qualquer decisão futura de escalar o backend
horizontalmente.

**Sugestão de resolução**: substituir por um cache distribuído (Redis) com a mesma interface
`IDebounceNotificacao`, sem mudar `NotificationDispatcher`.

## 42. Preferências de canal de notificação espelhadas localmente, sem `GET` no backend (Sprint 19)

**Descrição**: a tela Ajustes → Notificações lê o estado inicial dos 3 toggles de um espelho local
(`pushDeviceStorage`), não de um `GET /api/dispositivos-push/{id}` (que não existe — não foi pedido
pela missão). Se o mesmo dispositivo tiver suas preferências alteradas por outro caminho (ex.:
diretamente no banco), o Mobile não perceberia até a próxima alteração feita pela própria tela.

**Motivo**: criar um endpoint `GET` novo não solicitado seria escopo além do pedido; o servidor
continua sendo a fonte de verdade para o ENVIO (o espelho local é só para renderizar o toggle).

**Impacto**: baixo — cenário de divergência exigiria uma alteração fora do fluxo normal do app.

**Prioridade**: baixa.

**Sugestão de resolução**: adicionar `GET /api/dispositivos-push/{id}` quando houver necessidade
real de sincronizar preferências entre múltiplos pontos de escrita.

## 43. Detecção de movimento não implementada — nenhum gravador emite esse sinal (Sprint 20)

**Descrição**: a missão da Sprint 20 pedia um badge "Movimento detectado" (Fase 5/7) quando o
gravador suportasse. Nenhum `ISnapshotProvider` (Dahua/Intelbras CGI, Hikvision ISAPI) hoje expõe
webhook, polling ou qualquer sinal de detecção de movimento — só captura de snapshot sob demanda.

**Motivo**: implementar o badge exigiria fabricar um evento que não existe de verdade —
contrariaria a mesma disciplina já aplicada em Sprints anteriores (nunca simular uma capacidade de
hardware sem uma fonte real por trás, ver Sprint 3/ADR 0006 sobre a decisão equivalente para
controle de acesso).

**Impacto**: nenhum hoje — o recurso simplesmente não existe em nenhum lugar do sistema.

**Prioridade**: baixa — sobe se um gravador com essa capacidade real (webhook de movimento) for
integrado no futuro.

**Sugestão de resolução**: quando um fabricante com suporte real a movimento for integrado,
adicionar um novo `IMotionEventProvider` (ou estender `ISnapshotProvider`) e publicar via
`IOperacionalEventoPublicador`, mesmo padrão de `CameraStatusAlterado`.

## 44. Arquivo de snapshot do seed salvo com extensão `.jpg` contendo bytes PNG (Sprint 20)

**Descrição**: `SnapshotStorage.SaveAsync` sempre nomeia o arquivo como `{guid}.jpg` (convenção
usada desde a Fase 2 do projeto, pré-Sprint 1) — o seed de desenvolvimento (`DevelopmentSeeder`)
grava bytes PNG reais (`PlaceholderImageGenerator`) nesse mesmo caminho, resultando num arquivo
com extensão `.jpg` mas conteúdo PNG.

**Motivo**: mudar a assinatura de `ISnapshotStorage.SaveAsync` para aceitar uma extensão/
content-type customizado afetaria o caminho de captura real (sempre JPEG) sem necessidade —
inofensivo porque `GET /api/cameras/{id}/imagem` sempre sniffa o Content-Type pela assinatura do
arquivo (primeiros bytes), nunca pela extensão do nome (ver ADR 0024 Decisão 3).

**Impacto**: nenhum funcional — só uma inconsistência cosmética visível a quem inspecionar a
pasta `snapshots/` manualmente.

**Prioridade**: baixa.

**Sugestão de resolução**: se algum dia importar, estender `ISnapshotStorage.SaveAsync` para
aceitar uma extensão explícita, ou gerar o placeholder do seed como JPEG de verdade.

## 45. `Provisionamento` sem vínculo a um Técnico responsável (Sprint 22A)

**Descrição**: O Dashboard Técnico do Painel Web pediria "Minhas Instalações" (provisionamentos
atribuídos ao técnico logado), mas `Provisionamento` (ADR 0028) não tem nenhum campo de
responsável — só `PropriedadeId/Nome/Template/Status`. Não há também nenhum endpoint que liste
Provisionamentos cross-propriedade (só por propriedade específica).

**Motivo**: adicionar essa atribuição exigiria uma migration nova + endpoint de listagem global,
além do "mínimo necessário" combinado para a Sprint 22A (ver ADR 0029).

**Impacto**: Dashboard Técnico mostra um estado honesto explicando a limitação em vez de dado
fabricado — nenhuma funcionalidade quebrada, só incompleta.

**Prioridade**: média — sobe quando o produto realmente precisar de atribuição de técnico por
instalação (provavelmente junto do wizard de provisionamento com templates, Sprint 22B+).

**Sugestão de resolução**: adicionar `TecnicoResponsavelId` (nullable) a `Provisionamento` +
endpoint `GET /api/provisionamentos?tecnicoId=` (ou similar) cross-propriedade.

**Reavaliado na Sprint 22B (ADR 0031)**: continua em aberto para a entidade `Provisionamento`
(ADR 0028) — não foi alterada nesta Sprint. A Sprint 22B criou uma entidade diferente
(`VinculoEquipamentoPropriedade`, o vínculo Equipamento↔Propriedade) que já nasce com
`CriadoPorUsuarioId` obrigatório e auditável, então não repete esta lacuna para o conceito novo;
nenhuma tabela associativa adicional foi necessária para isso.

## 46. Sessões de impersonation não são revogáveis (Sprint 22A)

**Descrição**: Impersonation é 100% stateless (JWT auto-contido, ver ADR 0021) — não existe
nenhum registro de "sessão ativa" persistido nem mecanismo de revogação de um token já emitido. A
tela "Sessões Ativas" do Painel Web infere sessões em andamento a partir do log de Auditoria
(Início sem Fim correspondente, dentro dos 15min de vida do token), mas **não tem** botão de
"forçar logout" funcional.

**Motivo**: implementar revogação real exigiria uma blocklist de token (ou trocar para sessões
persistidas no servidor) — mudança arquitetural maior que o escopo combinado para esta Sprint (ver
ADR 0029).

**Impacto**: um Master/Suporte não consegue encerrar à força a sessão de impersonation de OUTRO
Master/Suporte. Na prática, o token expira sozinho em 15 minutos de qualquer forma.

**Prioridade**: baixa — o dano potencial já é limitado pelo teto de 15 minutos.

**Sugestão de resolução**: se o produto precisar disso de verdade, avaliar uma blocklist de
`jti` (Guid do token) com TTL igual ao tempo de vida restante do token, verificada no pipeline de
autenticação.

**Reavaliado na Sprint 22B (ADR 0031)**: a missão desta Sprint pedia documentar num "ADR 0032"
que Sessões Ativas é baseada em log de auditoria — o ADR 0029 (Sprint 22A) já documenta isso
integralmente, então nenhum ADR novo foi criado (evitando duplicação). Este item continua a
referência definitiva do assunto junto com o ADR 0029.

## 47. Sem CRUD de gestão de conta de cliente (Sprint 22A)

**Descrição**: A Fase 5 do Painel Web (Sprint 22A) pedia Criar/Editar/Desativar cliente, mas não
existe nenhum endpoint para isso — um cliente só nasce via autocadastro (`POST /api/auth/register`).
O Painel Web implementou só leitura (lista + busca + detalhe).

**Motivo**: construir esse CRUD (validação de documento CPF/CNPJ, edição de plano, desativação)
seria uma Sprint própria — fora do "mínimo necessário" combinado para a fundação do Painel Web.

**Impacto**: Master/Suporte não conseguem editar/desativar uma conta de cliente pelo Painel Web
ainda.

**Prioridade**: média — natural para uma Sprint 22B/22C, junto da gestão completa de Propriedades/
Equipamentos.

**Sugestão de resolução**: endpoints `PUT /api/proprietarios/{id}` e
`POST /api/proprietarios/{id}/desativar` (Master-only), seguindo o mesmo padrão já usado por
`UsuariosInternosController`.

**Reavaliado na Sprint 22B (ADR 0031)**: confirmado que `ProprietariosController` continua só
com `GET` (lista + detalhe) — nenhum endpoint de criação/edição foi adicionado. Fora de escopo
desta Sprint (que tratou Equipamentos/Provisionamentos/Diagnóstico, não gestão de conta de
cliente); a interface do Painel Web permanece Somente Leitura com a justificativa já registrada
acima.

## 48. Regra de "1 vínculo ativo por equipamento" garantida só em código, não no banco (Sprint 22B)

**Descrição**: `VinculoEquipamentoPropriedade` não tem nenhuma constraint de banco impedindo dois
registros com o mesmo `EquipamentoId` e `DataFimUtc IS NULL` simultaneamente — a regra é garantida
inteiramente por `VinculoEquipamentoServico` (checagem antes de criar/trocar).

**Motivo**: MySQL/InnoDB não suporta índice único parcial/condicional (ex.: `WHERE DataFimUtc IS
NULL`) sem uma coluna gerada adicional — implementar isso seria desproporcional ao escopo desta
Sprint (mesma classe de simplificação já registrada em decisões anteriores do projeto, ex.:
debounce em memória em vez de Redis).

**Impacto**: um bug futuro no Servico (ou uma escrita direta no banco fora do Servico) poderia, em
teoria, violar essa regra sem o banco rejeitar. Os testes automatizados
(`VinculoEquipamentoServicoTests`) cobrem os caminhos de rejeição conhecidos, mas isso não
substitui uma garantia de banco.

**Prioridade**: baixa — toda escrita nesta tabela hoje passa exclusivamente pelo Servico.

**Sugestão de resolução**: se o volume de escrita justificar, migrar para uma coluna gerada
(`AtivoFlag` calculada a partir de `DataFimUtc`) + índice único parcial em cima dela.

## 49. `EquipamentoJflConexaoObserver` validado só via teste unitário, nunca contra um handshake JFL real ponta a ponta (Sprint 22C.2)

**Descrição**: o hook que marca um Equipamento JFL como Online ao conectar (`EquipamentoJflConexaoObserver`,
`Infrastructure/Jfl/`) foi validado via testes unitários diretos (`ProcessarAsync` chamado
diretamente, sem passar pelo protocolo TCP real) e via subida real da aplicação (confirma que a
inscrição no evento `SessionManager.SessaoRegistrada` não quebra o startup). Não foi construído um
pacote JFL bruto (0x21/0x2A) de handshake para simular uma central real conectando na porta 8085
durante esta Sprint.

**Motivo**: não existe hardware JFL real disponível neste ambiente de desenvolvimento (mesma
limitação já registrada para outras integrações do projeto), e montar um cliente de protocolo
binário só para este teste seria desproporcional ao escopo desta Sprint.

**Impacto**: baixo — a lógica de negócio do observador (localizar Equipamento, marcar Online,
persistir descobertas, nunca lançar) está coberta por teste unitário; o único risco não coberto é
uma incompatibilidade na integração com o `SessionManager` real sob um handshake genuíno.

**Prioridade**: baixa — o padrão de assinatura de evento é idêntico ao já usado (e validado em
produção) por `EventoCommandHandler`.

**Sugestão de resolução**: quando uma central JFL física estiver disponível para testes (mesma
dependência já registrada para a homologação original do protocolo JFL), validar o fluxo completo:
cadastrar o Equipamento pelo Número de Série, conectar a central física, confirmar Status=Online.

## 50. Sem reconexão automática ao editar um equipamento Control iD/Intelbras (Sprint 22C.2)

**Descrição**: `EquipamentoAdminServico.AtualizarAsync` atualiza Ip/Porta/Usuário/Senha sem tentar
reconectar/redescobrir informações — só o cadastro (`CriarAsync`) dispara a conexão automática.

**Motivo**: decisão de escopo explícita — reconectar é responsabilidade do fluxo de sincronização
manual já existente (`EquipamentoIntegracaoServico`, mobile), não deste formulário administrativo.

**Impacto**: baixo — se o Técnico corrigir um IP errado via edição, o equipamento continua
Offline/com dados desatualizados até uma sincronização manual ou um novo cadastro.

**Prioridade**: baixa.

**Sugestão de resolução**: se o padrão de uso mostrar que Técnicos esperam reconexão automática ao
editar, replicar a mesma chamada de `ConectarEDescobrirControlIdAsync`/`ConectarIntelbrasAsync` já
usada em `CriarAsync`.
