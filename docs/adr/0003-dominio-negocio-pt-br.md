# ADR 0003 — Domínio de negócio em português (pt-BR), infraestrutura em inglês

**Data**: 2026-07-19

## Contexto

A Sprint 1 criou o domínio inicial (`User`, `Property`, `AlarmPanel`, `Zone`, `Camera`, `Dvr`,
`Occurrence`) em inglês. O objetivo do produto é 100% português, então uma Sprint exclusiva de
padronização rodou antes da Sprint 2 para corrigir isso definitivamente.

## Problema

Onde exatamente traçar a fronteira entre "domínio de negócio" (que deve virar português) e
"infraestrutura/protocolo/convenção" (que deve continuar em inglês), de forma que qualquer
funcionalidade nova saiba, sem reconsultar ninguém, qual lado da fronteira ela ocupa?

## Alternativas consideradas

- **Traduzir tudo, sem exceção** (incluindo `RefreshToken`, campos de ORM, nomes de protocolo
  JFL/CGI/ISAPI): mais consistente à primeira vista, mas força vocabulário estranho sobre
  convenções de ecossistema (JWT/OAuth, EF Core) e sobre protocolos de hardware externos que têm
  nome próprio fora do projeto.
- **Fronteira por camada** (regra adotada): só o que representa um conceito de **negócio**
  (entidades, enums, DTOs, propriedades, pastas de Application) vira português; o que é
  **infraestrutura, protocolo ou convenção .NET/HTTP** fica em inglês.

## Decisão

Regra de fronteira adotada:

- Entidades/enums/DTOs de negócio → português (`Usuario`, `Propriedade`, `Central`, `Zona`,
  `Gravador`, `Ocorrencia`, `VinculoZonaCamera`, `RegistroEventoAlarme`, `StatusResolucao`,
  `ResultadoProcessamentoEvento`, `FabricanteGravador`).
- FKs/navegações para entidades renomeadas acompanham o novo nome (`AlarmPanelId`→`CentralId`).
- Campos técnicos universais de ORM (`Id`, `CreatedAtUtc`, `RevokedAtUtc`, `ExpiresAtUtc`) ficam
  em inglês — convenção comum mesmo em domínios pt-BR.
- `RefreshToken` (classe e campos `TokenHash`/`ExpiresAtUtc`/`RevokedAtUtc`/`ReplacedByTokenHash`)
  fica em inglês (vocabulário JWT/OAuth), mas sua FK `UserId`→`UsuarioId` acompanha o rename por
  ser uma FK comum, não vocabulário de token.
- Protocolo JFL (`AppMorador.Jfl`) e integração CGI/ISAPI/Digest Auth
  (`AppMorador.Infrastructure/Snapshots`) ficam em inglês — nomes de protocolo/hardware externo,
  mesma categoria de exceção que JWT/OAuth/HTTP. As entidades que esse protocolo popula (Central,
  Zona, Ocorrencia) são domínio e são traduzidas; o parser de bytes não é.
- `ContactIdCatalog`/`ContactIdDefinition` ficam como estão — nome de um padrão externo nomeado
  (Ademco Contact ID), mesma categoria de exceção.
- Controllers, Middleware, `Program.cs`, `DbContext`, Entity Framework, rotas HTTP e chaves JSON
  de vocabulário JWT/OAuth (`accessToken`, `refreshToken`, `expiresInSeconds`) ficam em inglês —
  convenção de ecossistema .NET/HTTP, não domínio de negócio.

## Consequências

Qualquer funcionalidade nova deve seguir esta fronteira sem precisar reconsultar a decisão. Rotas
HTTP não mudam quando um recurso de negócio é renomeado — só o corpo (DTOs/JSON) muda.

## Impactos

Toda a camada `Domain`/`Application` (nomes de entidades, enums, DTOs, pastas); a camada `Api`
(rotas/controllers) e a infraestrutura de protocolo (JFL, CGI/ISAPI) ficam fora desta regra.
Qualquer Sprint futura que crie um novo conceito de negócio segue esta fronteira por padrão.

## Arquivos afetados

Rename em massa de `backend/src/AppMorador.Domain`, `AppMorador.Application`,
`AppMorador.Infrastructure/Persistence` (via migration `PadronizacaoDominioPtBr`, ver
`docs/ALTERACOES_BANCO.md`).

## Como revisar futuramente

Válida enquanto o produto continuar 100% em português para o usuário final. Se o produto expandir
para outro idioma de interface, esta fronteira precisa ser revisitada (mas o contrato JSON/rotas
HTTP, já em inglês, não seria afetado).
