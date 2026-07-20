# Testes Funcionais — Sprint 3.1 (Homologação/Estabilização)

Checklist executado manualmente (via `curl` contra uma instância real da Api rodando localmente,
mais inspeção de código para os pontos que exigem revisão estática) durante a Sprint 3.1. Todo
item abaixo foi de fato exercitado, não apenas lido no código — request e resposta reais.

## Critérios de aceite da Sprint (todos ✅)

| Critério | Resultado |
|---|---|
| Executar Backend | ✅ `dotnet run` sobe limpo, sem exceção, Swagger disponível |
| Executar Mobile | ✅ `npx expo start`, Metro sobe, bundle web compila sem erro |
| Executar Banco | ✅ `dotnet ef database update` a partir do `InitialCreate` único (ver `docs/setup/SETUP_AMBIENTE.md`) |
| Abrir Swagger | ✅ `GET /swagger/index.html` → 200 |
| Login | ✅ credenciais corretas emitem tokens; erradas retornam 401 genérico |
| Criar usuário | ✅ `POST /api/auth/register` → 201 |
| Editar usuário | ⬜ N/A — não existe endpoint de edição de usuário hoje (ver `docs/DIVIDA_TECNICA.md`) |
| Criar propriedade | ✅ `POST /api/properties` → 201 |
| Navegar Dashboard | ✅ `GET /api/properties/{id}/dashboard` → 200, dados corretos e estado vazio corretos |
| Navegar Central de Eventos | ✅ paginação/busca/filtro de período validados |
| Fluxo completo sem erro crítico | ✅ |
| Rodar em ambiente limpo só com a documentação | ✅ `docs/setup/SETUP_AMBIENTE.md` cobre banco/segredos/migrations/seed/execução |

## 1. Startup da Api

- `dotnet build` na solução inteira: **0 Warnings, 0 Errors**.
- `dotnet run`: sobe em Development, aplica seed automaticamente (idempotente), expõe Swagger,
  loga `Servidor JFL escutando na porta 8085`, `Now listening on: http://localhost:5027`,
  `Application started`.
- **Teste de resiliência do JFL Server**: subiu uma segunda instância do backend com a porta JFL
  já ocupada pela primeira. Resultado: a segunda instância logou um erro claro
  (`Nao foi possivel iniciar o servidor JFL: a porta 8085 ja esta em uso...`) e **continuou
  subindo normalmente** (`Now listening on:`, `Application started`) — confirmado também via
  `curl` retornando 401 (não erro de conexão) num endpoint autenticado da segunda instância.
  Corrigido nesta Sprint (ver `docs/CHANGELOG.md`); antes desta correção, esse cenário derrubava
  a Api inteira.

## 2. Banco de dados

- `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model
  since the last migration." (sem divergência entre o `InitialCreate` squashed e o `AppDbContext`
  atual).
- Banco real (`appmorador`) inspecionado diretamente: schema, `__EFMigrationsHistory` e dados
  reais íntegros antes e depois de todas as alterações desta Sprint (ver `ADR 0007` em
  `docs/adr/0007-squash-migrations-v030-alpha.md`).

## 3. Seed de desenvolvimento

- Primeira execução: cria 1 usuário (`teste@appmorador.com.br`), 1 propriedade, 1 central, 2
  zonas, 5 ocorrências — log confirma.
- Segunda execução (idempotência): log confirma `"usuario de teste ja existe — nada a fazer"`,
  nenhuma linha duplicada (verificado por contagem antes/depois).

## 4. Autenticação

| Cenário | Resultado |
|---|---|
| Login com credenciais corretas | 200, `accessToken`/`refreshToken`/`expiresInSeconds` emitidos |
| Login com senha errada | 401, mensagem genérica ("E-mail ou senha inválidos"), sem revelar qual campo |
| Claims do access token | `sub` (usuarioId), `email`, `securityStamp`, `jti`, `exp`, `iss`, `aud` — decodificado e conferido |
| Acesso a rota autenticada sem token | 401 |
| Acesso a rota autenticada com token válido | 200 |
| Refresh (rotação) | novo par de tokens emitido; token antigo imediatamente inválido (401 "Sessão expirada") ao tentar reusar |
| Logout | revoga o refresh token corrente (204); tentativa de refresh após logout → 401 |
| Lockout (5 tentativas erradas) | 6ª tentativa, mesmo com senha correta, retorna 401 "Conta temporariamente bloqueada" |
| Ownership entre usuários | usuário B autenticado tentando acessar propriedade do usuário A → 404 "Propriedade não encontrada" (não revela existência) |

**Limitação pré-existente registrada (não é bug novo, já documentada no código)**: o claim
`securityStamp` é emitido no token mas nunca validado contra o valor atual do usuário a cada
request — troca de senha não invalidaria tokens de acesso já emitidos antes de expirarem
naturalmente. Sem impacto prático hoje porque não existe endpoint de troca de senha. Ver
`docs/DIVIDA_TECNICA.md`.

## 5. CRUD de Propriedades

| Cenário | Resultado |
|---|---|
| Criar propriedade | 201, dados corretos |
| Criar propriedade sem nome | 400, erro de validação claro |
| Listar propriedades do usuário | 200, só propriedades do dono autenticado |
| Editar propriedade própria | 200, dados atualizados |
| Editar propriedade inexistente | 404 |
| Editar propriedade de outro usuário | 404 (mesma mensagem de "não encontrada", sem vazar existência) |

## 6. Dashboard

| Cenário | Resultado |
|---|---|
| Propriedade com central/zonas/ocorrências | `statusSeguranca: "Protegido"`, `pontuacaoSaude` calculada corretamente, `ultimoEvento` com texto amigável |
| Propriedade sem nenhum equipamento (estado vazio) | `statusSeguranca: "Configuração pendente"`, `pontuacaoSaude: 0`, contagens zeradas — sem erro |
| Propriedade de outro usuário | 404 |

## 7. Central de Eventos

| Cenário | Resultado |
|---|---|
| Listagem paginada (tamanho 2, 5 itens) | 3 páginas, itens corretos, ordenados por data desc |
| Busca por nome de zona (`busca=Garagem`) | filtra corretamente, só itens da zona buscada |
| Filtro de período (`desdeUtc` no futuro) | lista vazia, `totalPaginas: 0`, sem erro |
| Propriedade sem ocorrências (estado vazio) | lista vazia, sem erro |

## 8. Performance (revisão real, não especulativa)

- `ConsultaDashboardServico`/`JflFonteEventos`: consultas projetadas (`.Select` para tipos
  anônimos), paginação/contagem feitas no SQL (`Skip`/`Take`/`CountAsync`), nenhum N+1
  encontrado.
- Único ponto real encontrado: `PropriedadeRepositorio.ListByOwnerAsync` rastreava entidades
  desnecessariamente numa listagem somente-leitura — corrigido com `.AsNoTracking()`.
- Nenhuma outra otimização aplicada — nenhum outro problema real encontrado.

## 9. Mobile + integração

- `npx tsc --noEmit`: sem erros.
- Bundle web (`Metro`, `platform=web`) compila e serve integralmente (8.6 MB, sem stack trace de
  erro), incluindo as telas da Central de Eventos.
- Preflight CORS simulado (`OPTIONS` com `Origin: http://localhost:8081`, a origem real do Metro
  web) contra `/api/auth/login`: `204`, headers `Access-Control-Allow-Origin`/`-Methods`/`-Headers`
  corretos.
- **Limitação de ambiente**: não há navegador/dispositivo disponível para clique-a-clique real
  nesta sessão (mesma limitação já registrada em Sprints anteriores) — a validação de UI ficou
  limitada a `tsc`, build do bundle e aos contratos de API que a UI consome (validados via
  `curl` diretamente).

## 10. Logs

Todos os logs de erro relevantes (JFL, seed) carregam contexto suficiente para diagnóstico:
porta, motivo, se a Api segue disponível. Nenhum log genérico tipo "ocorreu um erro" sem
contexto foi introduzido ou encontrado nos caminhos revisados nesta Sprint.
