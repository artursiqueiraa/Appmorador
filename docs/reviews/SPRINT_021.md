# Sprint 21 — RBAC Master (Base de Permissões da Plataforma)

**Data**: 2026-07-26

## Resumo

Base de autorização da plataforma: papéis internos (Master/Técnico/Suporte), impersonation,
auditoria centralizada, Permissões Funcionais + Feature Flags por Propriedade, Capacidades
Dinâmicas por Equipamento (via `ModeloEquipamento`, entidade nova) e Provisionamento (registro de
metadados). Cliente (Administrador) mantém `ProprietarioId` como fonte de verdade nesta Sprint —
`UsuarioPropriedade` nasce como abstração preparatória para o Multiusuário (Sprint futura
dedicada), decisão explícita do usuário após a Fase 0 (auditoria, `docs/audits/AUDIT_RBAC_021.md`)
confirmar que a missão original assumia uma arquitetura de multiusuário que não existia de forma
nenhuma no projeto real. Painel Web fica fora de escopo (Sprint 22), também por decisão explícita.
Ver ADRs 0021/0025/0026/0027/0028.

## Entregas — Backend

| Item | Onde | O que faz |
|---|---|---|
| `RoleSistema`/`UsuarioPropriedade`/`PerfilPropriedade` | `Domain/Entities/` | Papel global (interno) vs. perfil de propriedade (cliente) |
| `PermissaoFuncionalidade`/`UsuarioPropriedadePermissao` | `Domain/Entities/` | 12 permissões granulares, concessão por vínculo |
| `FeatureFlag`/`PropriedadeFeatureFlag` | `Domain/Entities/` | 8 features, o que a propriedade contratou |
| `ModeloEquipamento`/`ModeloEquipamentoCapacidade`/`EquipamentoCapacidade` | `Domain/Entities/` | Catálogo real substituindo `Equipamento.Modelo` (texto) |
| `Provisionamento` | `Domain/Entities/` | Registro de metadados do pacote de instalação |
| `AuditoriaMaster` | `Domain/Entities/` | Trilha de auditoria sem FK (snapshot desnormalizado) |
| `Policies`/`ClaimsPrincipalExtensions` | `Api/Auth/` | 7 Policies via `RequireAssertion`, claims de papel/impersonation |
| `AuditoriaAuthorizationMiddlewareResultHandler` | `Api/Auth/` | Auditoria de falha de autorização centralizada |
| `IPermissaoService`/`PermissaoService` | `Application/Rbac/` | Permissão funcional, feature flag, capacidade — ponto único de consulta |
| `IImpersonationServico`/`ImpersonationServico` | `Application/Rbac/` | Token de 15min sem refresh, 100% auditado |
| `IUsuarioInternoServico` | `Application/Rbac/` | CRUD parcial de contas Master/Técnico/Suporte |
| `IModeloEquipamentoServico` | `Application/Equipamentos/` | Get-or-create de modelo + gestão de capacidades |
| `IProvisionamentoServico` | `Application/Provisionamentos/` | Criar/listar/arquivar |
| `IPropriedadeFeatureFlagServico`/`IUsuarioPropriedadePermissaoServico` | `Application/Propriedades/`, `Application/Rbac/` | Gestão de feature/permissão por vínculo |
| 7 Controllers novos + 1 endpoint em `EquipamentosController` | `Api/Controllers/` | Todos protegidos por Policy |
| Migration `RbacMaster` | `Infrastructure/Persistence/Migrations/` | Com backfill de dados escrito manualmente — ver `docs/ALTERACOES_BANCO.md` |
| Seed | `Infrastructure/Persistence/Seed/DevelopmentSeeder.cs` | Conta Master + backfill de `UsuarioPropriedade`/Permissões para propriedades pré-existentes |

## Entregas — Mobile

| Item | Onde | O que faz |
|---|---|---|
| `usePermissao` | `mobile/src/auth/` | Única fonte de verdade — lê `selectedProperty`, nunca o `perfil` local |
| Tipos `PerfilPropriedade`/`PermissaoFuncionalidade`/`FeatureFlag` | `mobile/src/api/types.ts` | Espelham os enums do backend |
| `GET /api/properties` enriquecido | `PropriedadeServico.ToDtoEnriquecidoAsync` | `perfil`/`permissoes`/`features` por propriedade, ownership-checked |
| UI condicional (5 telas) | `MoradoresScreen`/`VisitantesScreen`/`AccessScreen`/`CamerasScreen`/`CredenciaisScreen`+`TipoCredencialSelector` | Ver detalhe abaixo |

**Detalhe da UI condicional**: `MoradoresScreen` esconde "Adicionar morador" sem `CadastrarMorador`;
`VisitantesScreen` esconde "Adicionar visitante" sem `CriarVisitante`; `AccessScreen` esconde o
Painel de Controle (comandos PGM) sem `AbrirPortao`; `CamerasScreen` mostra um estado honesto
("Câmeras não contratadas") sem `FeatureFlag.Cameras` — **a aba continua sempre visível**,
preservando a Navegação Previsível (ADR 0019, 4 abas fixas) em vez de escondê-la, reconciliando um
pedido da missão com uma decisão de UX já estabelecida; `CredenciaisScreen` esconde os chips
"Facial"/"Tag RFID" (via novo prop `permitidos` em `TipoCredencialSelector`) sem
`CadastrarFacial`/`CadastrarTag`.

## Evidências dos testes

- `dotnet build` (solução inteira): 0 erros, 0 avisos novos.
- `dotnet test` (`AppMorador.Tests`): **87/87 passando** — 43 novos desta Sprint
  (`ClaimsPrincipalExtensionsTests`, `PermissaoServiceTests`, `AuditoriaServiceTests`,
  `ImpersonationServicoTests` — inclui verificação explícita de que nenhum refresh token é
  gerado —, `UsuarioInternoServicoTests`, `ModeloEquipamentoServicoTests`,
  `ProvisionamentoServicoTests`, `PropriedadeFeatureFlagServicoTests`,
  `UsuarioPropriedadePermissaoServicoTests`).
- **Migration `RbacMaster` verificada contra o banco real** (não só revisão de código): backfill
  de `Equipamento.Modelo` → `ModeloEquipamento` confirmado (9 equipamentos, 5 com modelo
  preenchido → 4 modelos distintos criados, todos os 5 corretamente resolvidos, 0 perda de dado);
  achado corrigido antes de aplicar (default de `Usuarios.Ativo` desativaria as 14 contas
  existentes — corrigido com backfill próprio, confirmado 14/14 ativas após aplicar); seed de
  backfill de `UsuarioPropriedade` confirmado (12 propriedades pré-existentes, 72 permissões do
  Plano Básico concedidas).
- `npm run typecheck`/`npm run lint` (Mobile): 0 erros, 0 avisos.
- `npx expo-doctor`: 20/20 checks aprovados.
- `npm test` (Mobile): **57/57 passando** (16 novos: `usePermissao` — fail-closed sem propriedade
  selecionada, leitura correta de permissões/features com múltiplos valores, troca de propriedade
  nunca mistura dado da anterior).
- **APK Preview (EAS)**/**Validação em dispositivo físico**: pendente — ver seção "Pendências"
  abaixo.

## Achados corrigidos durante a implementação (não estavam no plano original)

1. **Migration gerada automaticamente dropava `Equipamento.Modelo` sem backfill** — corrigido
   escrevendo manualmente o `INSERT`/`UPDATE` de backfill antes do `DropColumn` (ver ADR 0027).
2. **Default de `Usuarios.Ativo` desativaria toda conta pré-existente** — corrigido com um
   `UPDATE` explícito logo após o `AddColumn`, verificado contra o banco real.
3. **`PropriedadeFeatureFlagServico`/`IPermissaoService` não fazem ownership check** (desenhados
   para consumo só por internos) — resolvido enriquecendo `GET /api/properties` (já
   ownership-checked) em vez de expor as rotas internas diretamente ao mobile (ver ADR 0026).
4. **Namespace `AppMorador.Application.Autorizacao` colidia conceitualmente com o já existente
   `Autorizacoes`** (Sprint 8, visitantes) — corrigido antes de causar ambiguidade real, movido
   para `AppMorador.Application.Rbac`.

## Dívidas técnicas registradas

- Capacidades reais de equipamento não foram cadastradas para os modelos já existentes (sem fonte
  verificável — mesmo princípio da Sprint 15) — ver ADR 0027.
- Árvore de vínculos de hardware do `Provisionamento` (Centrais/Gravadores/Câmeras/Leitores/PGMs/
  Zonas) foi deliberadamente adiada — ver ADR 0028.
- Gestão de Permissões/Features pelo próprio Administrador (autoatendimento) fica para quando o
  Painel Web existir — hoje é exclusiva de Técnico/Master.
- `perfil` local (`profilePreference.ts`) e `perfil` real (API, via `usePermissao`) coexistem sem
  o primeiro ter sido removido — ver ROADMAP.md, item "CRUD completo de Usuários (cliente)".

## Pendências

- **EAS Build Preview + homologação manual em dispositivo físico Android** — regra permanente
  desde a Sprint 20 (CLAUDE.md). Ainda não executada nesta sessão.
- **Painel Web (Sprint 22)** — fora de escopo por decisão explícita do usuário, não uma omissão.

## Parecer do Reviewer — 9 Pilares (nomenclatura da própria missão)

| Pilar | Avaliação |
|---|---|
| **1. Arquitetura** | ✅ Aprovado. Coexistência `ProprietarioId`/`UsuarioPropriedade` documentada e deliberada (ADR 0021); nenhuma `Servico` pré-existente foi alterada para depender do vínculo novo; `RequireAssertion` centralizado em vez de Handlers dispersos; `ModeloEquipamento` resolvido transparentemente sem quebrar o contrato da Api (ADR 0027). |
| **2. Segurança** | ✅ Aprovado. Impersonation nunca gera refresh token (testado explicitamente); token de impersonation expira em 15min fixo, não configurável; toda ação de impersonation é auditada com o `UsuarioId` do Master, nunca do cliente; falha de autorização é auditada centralmente (nenhum Controller decide isso sozinho); `AuditoriaMaster` sobrevive à exclusão da conta de origem (sem FK, por design). |
| **3. Domínio** | ✅ Aprovado. Papel global (interno) e perfil de propriedade (cliente) nunca se confundem — claim `role` só existe para internos; Permissão Funcional (ADR 0025) e Feature Flag (ADR 0026) mantidos como eixos ortogonais, nunca fundidos. |
| **4. Escalabilidade** | ✅ Aprovado. Concessão de permissões via "replace-all" indexado (mesmo padrão já usado no projeto); `UsuarioPropriedade` único por `(UsuarioId, PropriedadeId)` — adicionar uma propriedade nova não exige alterar nenhuma tabela existente. |
| **5. UX** | ✅ Aprovado. `CamerasScreen` preserva a Navegação Previsível (ADR 0019) em vez de esconder a aba inteira — reconciliação deliberada entre o pedido da missão e uma decisão de UX já estabelecida, não uma contradição silenciosa. Nenhuma tela trava ou quebra sem a permissão/feature — sempre esconde a ação ou mostra um estado honesto. |
| **6. Testes** | ✅ Aprovado. 87/87 backend (43 novos), 57/57 mobile (16 novos) — meta de 30+/15+ da missão superada em ambos. Cobertura inclui os cenários explicitamente pedidos (fail-closed sem vínculo, impersonation sem refresh, auditoria de login/falha, permissão/feature bloqueando corretamente). |
| **7. Documentação** | ✅ Aprovado. 5 ADRs (0021/0025/0026/0027/0028) + auditoria (`AUDIT_RBAC_021.md`) + `ALTERACOES_BANCO.md` com a revisão completa da migration destrutiva + CHANGELOG/ROADMAP atualizados. |
| **8. Performance** | ✅ Aprovado (por inspeção — sem carga real medida). `IPermissaoService` resolve por índice único; `GET /api/properties` enriquecido faz N+1 consultas (uma por propriedade) — aceitável na escala atual (poucas propriedades por usuário), sinalizado para revisão se o volume crescer. |
| **9. Observabilidade** | ⚠️ Aprovado com ressalva. Auditoria cobre login/impersonation/falha de autorização, mas ainda não há um endpoint/tela consumindo `GET /api/auditoria` na prática (existe, mas não foi exercitado por nenhum cliente real) — fica para o Painel Web (Sprint 22) tornar isso visível de fato. |

**Conclusão**: Sprint aprovada nos 9 pilares por código/teste automatizado — nenhum bloqueio em
Segurança ou Arquitetura (os dois com poder de veto automático da própria missão). Ressalva
não-bloqueante em Observabilidade (auditoria existe e é populada corretamente, mas ainda não tem
um consumidor real) e pendência conhecida (EAS Build/homologação manual em dispositivo físico,
regra permanente desde a Sprint 20) ficam registradas, não escondidas.
