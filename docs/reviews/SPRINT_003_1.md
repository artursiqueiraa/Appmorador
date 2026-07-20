# Relatório — Sprint 3.1 (Production Readiness, Homologação e Estabilização)

**Data de conclusão**: 2026-07-19
**Marco**: v0.3.0-alpha

## Contexto

Sprint exclusivamente de estabilização — sem funcionalidades novas, sem Sprint 4, sem alteração
de arquitetura ou de regra de negócio fora do necessário para estabilidade/homologação. Objetivo:
qualquer desenvolvedor consegue clonar, configurar, executar Backend/Mobile/Banco e validar os
fluxos principais sem depender de conhecimento prévio do projeto.

## Achado de escopo, resolvido com o usuário

O briefing da Sprint pressupunha um sistema de Perfis (Administrador/Supervisor/Operador/Morador)
e um CRUD completo de Usuários (Editar/Alterar senha/Desativar/Reativar/Excluir). Investigação
exaustiva do código-fonte (excluindo artefatos de build) confirmou que **nada disso existe** — só
`Register`/`Login`/`Refresh`/`Logout`. Apresentado ao usuário, que decidiu: **homologar só o que
existe hoje**, registrando o restante como dívida técnica/backlog em vez de simular
funcionalidade inexistente ou criá-la fora do escopo desta Sprint. Ver `docs/DIVIDA_TECNICA.md`
itens 6 e 7.

## Problemas encontrados e corrigidos

### 1. JFL Server podia derrubar a Api inteira (crítico, corrigido)

`JflTcpServer.Start()` não tratava exceção de bind (`SocketException`), e
`JflServerHostedService.StartAsync()` deixava a exceção se propagar. Como falhas em
`IHostedService.StartAsync` são fatais para o Generic Host do ASP.NET Core, qualquer conflito de
porta do JFL (ex.: instância zumbi já rodando) derrubava a Api inteira, não só o listener JFL —
violação direta do requisito da Sprint ("nenhuma exceção poderá impedir a inicialização da Api").

**Correção**: bind agora é protegido por try/catch em duas camadas (`JflTcpServer` loga com
contexto acionável e relança; `JflServerHostedService` captura e só loga, nunca relança). Testado
de verdade: duas instâncias reais do backend rodando em paralelo, a segunda com a porta JFL já
ocupada — logou o erro claro e subiu normalmente, confirmado via `curl` retornando 401 (não erro
de conexão) num endpoint autenticado.

### 2. Histórico de migrations divergente do banco real (investigado, reconciliado)

A pasta de migrations continha só um `InitialCreate` (squash de 5 migrations originais), enquanto
o banco de desenvolvimento real ainda registrava as 5 migrations originais como aplicadas em
`__EFMigrationsHistory`. Investigado a fundo antes de qualquer ação (dados reais confirmados
intactos: 8 Usuarios, 5 Propriedades, 6 Ocorrencias antes de qualquer mudança desta Sprint).
Apresentado ao usuário, que confirmou: squash intencional, apropriado para a v0.3.0-alpha.

**Ação**: validado que o `InitialCreate` squashed não diverge do modelo atual
(`dotnet ef migrations has-pending-model-changes`); documentado em ADR 0007
(`docs/adr/0007-squash-migrations-v030-alpha.md`) e `docs/ALTERACOES_BANCO.md`. Nenhum banco real
foi recriado ou alterado.

### 3. `PropriedadeRepositorio.ListByOwnerAsync` rastreava entidades desnecessariamente

Único problema de performance real encontrado na revisão (item 12 do escopo). Listagem
somente-leitura não precisava de tracking do EF Core. Corrigido com `.AsNoTracking()`. Nenhuma
outra otimização foi aplicada — nenhum outro problema real foi encontrado (sem N+1, sem
`Include` supérfluo, build limpo com 0 warnings).

## Entregas desta Sprint

- Startup da Api resiliente a falha do JFL Server (item 1 acima).
- `DevelopmentSeeder` (`AppMorador.Infrastructure/Persistence/Seed/`): seed idempotente rodando
  automaticamente em Development — 1 usuário de teste, 1 propriedade, 1 central, 2 zonas, 5
  ocorrências. Testado: primeira execução insere, segunda confirma idempotência (nenhuma
  duplicação).
- `docs/setup/SETUP_AMBIENTE.md`: guia do zero — banco, usuário MySQL escopado, segredos via
  `dotnet user-secrets`, migrations, seed, execução de Backend/Mobile, troubleshooting.
- `docs/TESTES_FUNCIONAIS.md`: checklist de homologação com todo cenário testado via requests
  reais (não só lido no código).
- Backend e Mobile executados lado a lado em janelas de terminal separadas na máquina do usuário
  (a pedido explícito), integração validada de ponta a ponta.

## Fluxos homologados (via requests reais, não inspeção de código isolada)

- **Autenticação**: login, claims do JWT, rota autenticada com/sem token, refresh com rotação
  (token antigo invalidado imediatamente), logout (revogação), lockout após 5 tentativas erradas,
  isolamento de ownership entre dois usuários diferentes.
- **Propriedades**: criar, listar (só do dono), editar (própria/inexistente/de outro dono — todas
  as combinações testadas), validação de entrada (400 em nome vazio).
- **Dashboard**: propriedade com dados reais (Health Score, último evento) e propriedade vazia
  (sem central/zona/câmera) — ambos sem erro.
- **Central de Eventos**: paginação (3 páginas de 5 itens com tamanho 2), busca por nome de zona,
  filtro de período (incluindo caso vazio), propriedade sem eventos.
- **Mobile**: `tsc --noEmit` limpo; bundle web do Metro compila integralmente (8.6 MB, sem stack
  trace de erro); preflight CORS simulado contra a origem real do Metro web retorna os headers
  corretos.

Detalhe completo cenário a cenário em `docs/TESTES_FUNCIONAIS.md`.

## Pendências remanescentes

Nenhuma pendência bloqueadora para o marco v0.3.0-alpha. Não foi possível interagir com um
navegador/dispositivo real nesta sessão (limitação de ambiente já registrada em Sprints
anteriores) — validação de mobile ficou limitada a `tsc`, build do bundle e aos contratos de API
que a UI consome, validados diretamente via `curl`.

## Dívidas técnicas registradas

- Item 6 (`DIVIDA_TECNICA.md`): CRUD de Usuários incompleto, sem Perfis/Papéis — decisão
  explícita de escopo desta Sprint, não uma omissão.
- Item 7 (`DIVIDA_TECNICA.md`): claim `securityStamp` emitido no JWT mas nunca validado — sem
  impacto hoje (não há endpoint de troca de senha), deve ser implementado junto quando esse
  endpoint existir.

## Atualizações de documentação

- `docs/CHANGELOG.md`: nova entrada Sprint 3.1.
- `docs/ROADMAP.md`: marco v0.3.0-alpha registrado; CRUD de Usuários/Perfis movido para backlog
  explícito; referências a ADR-003→ADR-004 corrigidas (numeração duplicada pré-existente — essas
  decisões foram renumeradas de novo, para 0005/0006, na consolidação de ADRs feita antes da
  Sprint 4; ver `docs/adr/README.md`).
- `docs/ALTERACOES_BANCO.md`: squash documentado, histórico original preservado como contexto.
- `docs/DECISOES_ARQUITETURA.md`: ADR-005 (squash de migrations) adicionado; duplicidade de
  numeração ADR-003 corrigida (Central de Eventos vira ADR-004). **Nota**: este arquivo foi
  descontinuado na consolidação pré-Sprint-4 — o conteúdo agora vive em `docs/adr/` (a decisão do
  squash é o ADR 0007).
- `.decisions/0004-squash-migrations-v0.3.0-alpha.md`: novo registro (movido para
  `docs/adr/0007-squash-migrations-v030-alpha.md` na consolidação pré-Sprint-4).
- `docs/DIVIDA_TECNICA.md`: itens 6 e 7 adicionados; referência a ADR-003→ADR-004 corrigida.
- `docs/setup/SETUP_AMBIENTE.md`, `docs/TESTES_FUNCIONAIS.md`: novos.

## Decisões arquiteturais adicionadas

- ADR 0007 (`docs/adr/0007-squash-migrations-v030-alpha.md`, numerado ADR-005 no momento desta
  Sprint, renumerado na consolidação de ADRs pré-Sprint-4):
  squash do histórico de migrations pré-v0.3.0-alpha.

## Parecer do Reviewer

Ver seção "Parecer do Reviewer" apresentada ao final da entrega desta Sprint na conversa (mesmo
formato usado na Sprint 3) — a Sprint só é considerada concluída após essa aprovação explícita.
