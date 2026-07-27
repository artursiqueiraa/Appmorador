# Auditoria — Sprint 21 (RBAC Master)

**Data**: 2026-07-26
**Método**: leitura direta de código (não suposição) — cada afirmação abaixo tem `arquivo:linha`.

Esta auditoria é o Fase 0 obrigatório da missão da Sprint 21. Objetivo: confirmar, campo a
campo, o que já existe versus o que a missão assume que existe, antes de qualquer linha de
código ser escrita.

## 1. Auth existente (JWT/Claims/Refresh)

`JwtTokenService.GenerateAccessToken` (`backend/src/AppMorador.Infrastructure/Identity/JwtTokenService.cs:26-32`)
emite exatamente 4 claims: `sub` (Usuario.Id), `email`, `securityStamp` (custom, `Usuario.SecurityStamp`),
`jti` (aleatório por token). **Nenhuma claim de `role` é emitida hoje.**
`ClaimsPrincipalExtensions.GetUsuarioId()` (`backend/src/AppMorador.Api/Auth/ClaimsPrincipalExtensions.cs:8-15`)
só lê `sub`/`NameIdentifier` — é o único método de extensão do arquivo, não existe `GetRole()`.

Refresh token existe (`RefreshToken` entity, fluxo já documentado em Sprints anteriores) — sem
mudança relevante para esta auditoria.

## 2. Middleware

Pipeline completo (`Program.cs:216-254`): `ExceptionHandlingMiddleware` → `SecurityHeadersMiddleware`
→ (Swagger/seed só em Dev) → `UseHttpsRedirection` → `UseCors` → `UseRateLimiter` →
`UseAuthentication` → `UseAuthorization` → `MapControllers` → `MapHub<OperacionalHub>`.

**Não existe middleware de tenant/propriedade.** Nenhum header `X-Propriedade-Id` é lido em
lugar nenhum. `builder.Services.AddAuthorization()` (`Program.cs:94`) é chamado sem nenhum
`.AddPolicy(...)` — zero Policy registrada, zero `AuthorizationHandler`/`IAuthorizationRequirement`
implementado em qualquer lugar do backend.

**Como o "isolamento de tenant" funciona hoje**: manualmente, dentro de cada Application Servico,
sempre o mesmo padrão — comparar `propriedade.ProprietarioId` (ou a cadeia de navegação até lá)
com o `usuarioId` extraído do token. Exemplos confirmados:
- `EquipamentoServico.cs:31` — `if (propriedade is null || propriedade.ProprietarioId != proprietarioId)`
- `CameraServico.cs:48` e `CameraServico.cs:194` — mesmo padrão
- `PropriedadeServico.cs:86,102` — mesmo padrão
- `MoradorServico.cs:50` — cadeia Morador→Unidade→Propriedade
- `PermissaoVeicularServico.cs:36` — cadeia Veiculo→Morador→Unidade→Propriedade

Esse padrão se repete em **todos** os Application Servicos do domínio (dezenas de ocorrências) —
não há uma única exceção nem um ponto central. Isso confirma exatamente o problema que a missão
quer resolver (checagem de posse espalhada), mas também significa que centralizar via Policy
exigiria tocar em praticamente todo Servico existente.

## 3. App mobile — como decide o que mostrar/ocultar hoje

`EntrarResponse` (`mobile/src/api/types.ts:3-10`): `{ accessToken, refreshToken, expiresInSeconds,
usuarioId, nome, email }` — **nenhum campo de papel/perfil no contrato da Api.**

`AuthContext.login()` (`mobile/src/auth/AuthContext.tsx:47-53`) guarda só
`{ id, nome, email }` como `StoredUser`. O único controle de UI relacionado a "papel" hoje é
`perfil` (`morador`/`tecnico`), carregado de `expo-secure-store` via `profilePreference.ts`,
**totalmente independente da resposta de login** — é uma preferência local, opt-in, sem nenhuma
validação no servidor. A própria ADR 0020 (Decisão 2) documenta isso explicitamente: "a
preferência organiza a UI, nunca protege dado algum". Ou seja: **hoje, todo usuário autenticado
vê exatamente a mesma UI no nível de segurança real** — só 2 telas (`DetalhesEquipamentoScreen`,
seção "Proteção" de `MinhaPropriedadeScreen`) somem com o toggle local, sem relação com
autorização de verdade.

## 4. Banco — tabelas de usuário/propriedade que existem hoje

```
Usuario (Id, Nome, Email, SenhaHash, TentativasFalhas, BloqueadoAteUtc, SecurityStamp, CreatedAtUtc)
   │ 1
   │
   │ N  (Propriedade.ProprietarioId — UM dono só, sem tabela de vínculo)
   ▼
Propriedade (Id, Nome, Tipo, Endereco, ProprietarioId, ...)
   │ 1
   │
   │ N
   ▼
Unidade (Id, PropriedadeId, Tipo, Identificacao, ...)
   │ 1
   │
   │ N
   ▼
Morador (Id, UnidadeId, Nome, FotoPath, Telefone, Email, Documento, Status, Observacoes, CreatedAtUtc)
   — SEM SenhaHash, SEM UsuarioId, SEM qualquer vínculo com Usuario.
```

**Não existe nenhuma tabela `UsuarioPropriedade` (nem com qualquer nome parecido) hoje.** O
modelo é confirmado 1:N Usuario→Propriedade (dono único), exatamente como
`docs/DIVIDA_TECNICA.md` item 6 já registrava: "não há entidade de Papel/Permissão em lugar
nenhum do domínio" e o produto é B2C self-service de dono único (reforçado pelos itens 25 e 34
do mesmo documento).

## 5. Endpoints com `[Authorize]`

22 ocorrências em `backend/src/AppMorador.Api/Controllers/*.cs` — **todas** `[Authorize]` puro,
sem `Policy=` nem `Roles=`. Nenhum endpoint usa policy hoje (não existe nenhuma policy para usar).

## 6. Gap adicional não previsto pela missão — `Equipamento.Modelo` é texto livre

`Equipamento.Modelo` (`backend/src/AppMorador.Domain/Entities/Equipamento.cs:30`) é uma `string?`
livre, preenchida por texto (ex.: "Active 100 Bus"), não uma entidade/FK. **Não existe
`ModeloEquipamento` como entidade em lugar nenhum do código.** Diferenças de capacidade por
fabricante são tratadas hoje por `if (Fabricante == ...)` espalhado (ex.:
`docs/DIVIDA_TECNICA.md` item 36 — Painel de Controle só busca PGM para `Fabricante == Jfl`).

## 7. Gap adicional — nenhum log de auditoria genérico existe

O único registro parecido com auditoria é `RegistroEventoAlarme` — estritamente do pipeline
JFL/Contact-ID (payload, número de série, código de evento), sem `UsuarioId`/`Acao`/`Entidade`
genéricos. Não existe nenhum `AuditoriaMaster`/log de ação genérico hoje.

## 8. ADRs — numeração livre real

Arquivos existentes em `docs/adr/`: 0001 a 0020, depois um salto — **0021 não existe**, seguido
de 0022, 0023, 0024. A missão da Sprint 21 pede ADRs 0026-0030, presumindo que 0021-0025 já
estariam ocupados — **não estão**: 0021 e 0025 estão livres, junto com 0026+.

## Resumo — o que a missão assume vs. o que existe

| Assumido pela missão | Existe hoje? |
|---|---|
| `UsuarioPropriedade` (vínculo N:N Usuario↔Propriedade com Perfil) | ❌ Não existe — hoje é `Propriedade.ProprietarioId`, dono único |
| `Morador` como cliente autenticável (login, senha) | ❌ Não existe — `Morador` não tem `SenhaHash` nem vínculo com `Usuario` |
| Claim `role` no JWT | ❌ Não existe — só `sub`/`email`/`securityStamp`/`jti` |
| Policies de autorização | ❌ Não existe nenhuma — `AddAuthorization()` sem nenhuma policy registrada |
| Middleware de tenant/propriedade | ❌ Não existe — isolamento é 100% manual, espalhado em cada Servico |
| `ModeloEquipamento` (entidade separada) | ❌ Não existe — `Equipamento.Modelo` é texto livre |
| Log de auditoria genérico | ❌ Não existe — só `RegistroEventoAlarme` (JFL-específico) |
| App mobile já filtra UI por papel/permissão vindo do servidor | ❌ Não existe — `perfil` é 100% local, sem relação com o backend |

Todos os 8 pontos centrais da missão partem de uma base que ainda não existe — isso não é uma
lacuna pequena a ajustar durante a implementação, é a fundação inteira do domínio de autorização
que esta Sprint precisa criar do zero, decidindo antes como ela se encaixa no modelo atual
(dono único por propriedade).
