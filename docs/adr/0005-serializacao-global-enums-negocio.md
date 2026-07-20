# ADR 0005 — Serialização de enums de negócio via configuração global, não por-DTO

**Data**: 2026-07-19

## Contexto

Durante a Sprint 2, `POST /api/properties` retornava 400 ao receber `"tipo":"Comercial"` no
corpo da requisição. O enum de negócio `TipoPropriedade` era o primeiro enum do projeto a chegar
como *entrada* do cliente (os anteriores só eram serializados na saída/resposta). O bug só se
manifestou ao executar o fluxo real via `curl` — o `dotnet build` e o `npx tsc --noEmit` não o
detectaram, porque é um comportamento de runtime da desserialização JSON.

## Problema

Como configurar a Api para aceitar e devolver enums de negócio como texto legível
(`"Comercial"`), de forma que a solução valha para qualquer enum exposto — presente ou futuro —
sem depender de lembrar de repetir uma configuração a cada DTO novo?

## Alternativas consideradas

- **Atributo `[JsonConverter(typeof(JsonStringEnumConverter))]` em cada propriedade de enum de
  cada DTO**: resolve o caso pontual, mas exige disciplina de lembrar o atributo em todo DTO novo
  que expuser um enum — o próprio bug que motivou este ADR é evidência de que essa disciplina
  falha na prática.
- **Registro global do conversor em `Program.cs`** (via `AddControllers().AddJsonOptions(...)`):
  um único ponto de configuração na composition root, aplicado automaticamente a qualquer enum
  de qualquer DTO da Api.

## Decisão

Registrar `JsonStringEnumConverter` globalmente em `Program.cs`, na configuração de
`AddControllers()`. Nenhum DTO precisa de atributo individual para um enum de negócio ser
serializado/desserializado como texto.

## Consequências

- Elimina, por construção, a classe inteira de bug que gerou este ADR — qualquer enum de negócio
  novo funciona automaticamente, sem passo manual a lembrar.
- A configuração fica centralizada na composition root, não junto ao DTO — quem procurar "por que
  esse enum vira texto" precisa saber que é uma configuração global do projeto, não uma decisão
  local do DTO.
- Reforça a convenção já registrada em `.rules/backend.md`: "Enums de negócio expostos pela API
  são configurados para (de)serializar como texto legível, nunca como número interno."

## Impactos

`AppMorador.Api` (configuração de serialização em `Program.cs`). Todo caso de uso de
`AppMorador.Application` que expõe um DTO com enum de negócio se beneficia automaticamente, sem
alteração própria. Não afeta contrato de rota, autenticação, ou qualquer outra camada.

## Arquivos afetados

- `backend/src/AppMorador.Api/Program.cs`

## Como revisar futuramente

Se um enum específico precisar de uma serialização diferente da convenção (caso hoje não
previsto), a exceção deve ser explícita e documentada diretamente no DTO daquele enum — o padrão
default continua sendo o registro global, que não deve ser removido nem duplicado por engano em
uma refatoração futura de `Program.cs`.
