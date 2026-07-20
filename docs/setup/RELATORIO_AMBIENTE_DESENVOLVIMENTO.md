# Relatório — Preparar Ambiente de Desenvolvimento Completo

**Data**: 2026-07-20
**Natureza**: atividade de infraestrutura para desenvolvimento — não faz parte da Sprint 4.

## Resumo executivo

Um desenvolvedor sem contexto prévio agora consegue: clonar o projeto, criar o banco vazio (único
passo manual restante, por decisão de segurança preexistente), rodar `dotnet run` e ter migrations
+ seed aplicados automaticamente, com logs claros em cada etapa, e navegar pelo sistema logando
com qualquer uma de 4 contas de desenvolvimento. Nenhuma migration existente foi apagada; o banco
de desenvolvimento real não foi recriado; nenhuma regressão foi introduzida (autenticação,
propriedades, dashboard e eventos foram revalidados via requests reais após as mudanças).

## Banco utilizado

MySQL 8.0+, via Pomelo.EntityFrameworkCore.MySql. Confirmado como o único provider configurado
(`ServerVersion.AutoDetect` em `Program.cs`) — não há suporte a SQL Server neste projeto.

## Migrations

Nenhuma migration nova foi criada — o schema já vivia em um único `InitialCreate` desde a
consolidação da Sprint 3.1 (ADR 0007). O que mudou foi **quando** essa migration é aplicada:
antes exigia `dotnet ef database update` manual; agora `Program.cs` chama
`Database.MigrateAsync()` automaticamente no startup, em qualquer ambiente, logo após
`builder.Build()`. Validado sem divergência: `dotnet ef migrations has-pending-model-changes`
continua respondendo "No changes have been made to the model since the last migration." depois
de todas as mudanças desta tarefa.

## Seed criada

`DevelopmentSeeder` (existente desde a Sprint 3.1) foi reescrito para criar 4 contas em vez de 1,
cada uma verificada por e-mail antes de inserir (idempotente por conta, não só por seed
completo). Só a conta Morador tem propriedade/central/zonas/ocorrências vinculadas — ver
justificativa na seção seguinte.

## Usuários de desenvolvimento e credenciais de acesso

| Persona | Nome | E-mail | Senha |
|---|---|---|---|
| Administrador | Administrador | `admin@appmorador.local` | `Admin@123` |
| Supervisor | Carlos Henrique | `carlos.henrique@appmorador.local` | `Supervisor@123` |
| Operador | Juliana Souza | `juliana.souza@appmorador.local` | `Operador@123` |
| Morador | Fernanda Oliveira | `fernanda.oliveira@appmorador.local` | `Morador@123` |

**As 4 contas são funcionalmente idênticas hoje** — o domínio não tem sistema de Papel/Perfil
(`docs/DIVIDA_TECNICA.md` item 6). Isso foi levantado com o usuário antes da implementação (mesmo
padrão da Sprint 3.1: não fabricar uma hierarquia de acesso que não existe) e a decisão foi criar
as 4 como contas simples, com a propriedade de exemplo ("Residencial Jardim das Flores")
vinculada só à conta Morador (Fernanda), por ser a única persona com sentido de "dona de
propriedade" no modelo atual (produto B2C self-service).

O usuário de teste da Sprint 3.1 (`teste@appmorador.com.br` / "Residência Modelo") permanece
intacto no banco de desenvolvimento real — o seed é aditivo, nunca remove dado existente.

## Arquivos modificados

- `backend/src/AppMorador.Api/Program.cs` — bloco de verificação/aplicação automática de
  migrations no startup; log "Sistema pronto." no fim da inicialização.
- `backend/src/AppMorador.Infrastructure/Persistence/Seed/DevelopmentSeeder.cs` — reescrito para
  as 4 personas (antes: 1 usuário genérico).
- `docs/setup/SETUP_AMBIENTE.md` — banco utilizado, migrations automáticas, sequência de log
  esperada, credenciais novas, seções novas "Como remover o banco local" e "Como recriar o
  ambiente do zero", troubleshooting atualizado.
- `docs/DIVIDA_TECNICA.md` — item 8 (limitação: criação do banco em si continua manual).
- `docs/adr/README.md` — índice atualizado com ADR 0008.

## Arquivos novos

- `docs/adr/0008-migrations-automaticas-no-startup.md` — decisão de aplicar migrations
  automaticamente no startup, incluindo a limitação conhecida (criação do banco em si continua
  manual) e por quê.
- `docs/setup/RELATORIO_AMBIENTE_DESENVOLVIMENTO.md` — este relatório.

## Fluxos testados (requests reais, não inspeção de código)

- Build completo (`dotnet build`): 0 erros, 0 warnings.
- Startup contra banco já migrado: log `Banco localizado, nenhuma migration pendente.` — nenhuma
  migration reaplicada.
- Startup contra banco vazio (schema removido e recriado por teste): log `Migration(ns)
  pendente(s) encontrada(s)... Aplicando... aplicada(s) com sucesso.` — schema criado do zero
  corretamente.
- Startup apontando para um banco **inexistente** (nome novo, mesmo usuário restrito): falha
  rápida e clara (`Access denied for user 'appmorador'@'localhost' to database '<nome>'`),
  interrompendo a inicialização — comportamento correto e esperado (dependência dura), documentado
  no troubleshooting.
- Seed: primeira execução cria as 4 contas + propriedade; encontrado e corrigido um bug real
  (colisão de `Central.NumeroSerie` com dado remanescente da Sprint 3.1 — corrigido trocando de
  `000001` para `000002`); reexecução confirma idempotência total (`"todas as contas ja existem —
  nada a fazer"`).
- Swagger: `GET /swagger/index.html` → 200.
- Login: as 4 personas autenticam corretamente (200, claims corretos, nome/e-mail retornados).
- Usuários: `POST /api/auth/register` → 201 (fluxo existente, sem regressão).
- Propriedades: `GET /api/properties` (conta Fernanda) → 200, retorna "Residencial Jardim das
  Flores".
- Dashboard: `GET /api/properties/{id}/dashboard` → 200, Health Score e último evento corretos.
- Eventos: `GET /api/properties/{id}/eventos` → 200, 5 ocorrências paginadas corretamente.
- Mobile: `npx tsc --noEmit` limpo (sem mudança de código mobile nesta tarefa, mas revalidado).
- Backend e Mobile confirmados rodando lado a lado em janelas de terminal separadas na máquina do
  usuário.

## Pendências encontradas

Nenhuma bloqueadora. Uma limitação conhecida e documentada (não uma pendência a corrigir):

- **Criação do banco em si continua manual** — o usuário MySQL de runtime (`appmorador`) não tem
  `CREATE DATABASE` por decisão de segurança preexistente, e conceder esse privilégio ampliaria a
  superfície de acesso do usuário de runtime sem necessidade real (o atrito é um único comando,
  uma única vez por ambiente). Ver ADR 0008 e `docs/DIVIDA_TECNICA.md` item 8.

## Parecer do Reviewer

| Pilar | Avaliação |
|---|---|
| **Arquitetura** | ✅ Aprovado. Mudança confinada à composition root (`Program.cs`) e à camada de seed (Infrastructure); nenhum limite de camada violado. |
| **Segurança** | ✅ Aprovado. A restrição de privilégio do usuário MySQL foi preservada deliberadamente (não contornada só para satisfazer o pedido literal de "criar banco automaticamente") — decisão explicada, não escondida. Falha de migration continua fatal, como toda dependência dura. |
| **Produto** | ✅ Aprovado. Divergência real entre o pedido (papéis) e o domínio (sem RBAC) foi levantada com o usuário antes de implementar, evitando tanto simular uma hierarquia falsa quanto expandir escopo de domínio numa tarefa declarada como "infraestrutura". |
| **UX/UI** | ➖ N/A — nenhuma tela mobile foi alterada nesta tarefa. |
| **Performance** | ✅ Aprovado. `MigrateAsync`/verificação de pendências é idempotente e rápida (no-op quando não há nada pendente); nenhum custo adicional perceptível no startup normal. |
| **Manutenibilidade** | ✅ Aprovado. Logs claros e sequenciais tornam o comportamento de startup auto-explicativo; ADR 0008 documenta o porquê para quem não participou da decisão. |
| **Documentação** | ✅ Aprovado. `SETUP_AMBIENTE.md` cobre banco utilizado, criação/remoção/recriação do ambiente, credenciais novas; `DIVIDA_TECNICA.md` e ADR 0008 registram a limitação conhecida. |
| **Regressões** | ✅ Nenhuma encontrada. Autenticação, Propriedades, Dashboard e Central de Eventos revalidados via requests reais após as mudanças, com resultado idêntico ao da Sprint 3.1. Um bug real foi introduzido e corrigido durante a própria validação desta tarefa (colisão de `NumeroSerie` no seed), antes da entrega — não chegou a afetar nenhum fluxo revalidado. |

**Conclusão**: tarefa concluída, sem pendência bloqueadora.
