# Sprint 17 — Refinamento da Experiência do Morador

## Missão

Corrigir 7 fricções reais encontradas numa validação em dispositivo físico ("Sprint 16.1") sobre
o Design System Mobile Oficial UX001 entregue na Sprint 16: erros técnicos expostos, cadastro
facial duplicado/sem foto, aba Acessos e tela de Detalhes do Equipamento ainda técnicas, Empty
States sem CTA, HeroCard sem conectividade útil. Diretriz: "se eu fosse um morador de 65 anos que
nunca usou um app de segurança, eu entenderia isso?" — o objetivo é remover frustrações, não
adicionar funcionalidades.

## Fase 0 — Auditoria (achados verificados por leitura direta de código, não assumidos)

| # | Achado | Severidade | Verificado em |
|---|---|---|---|
| 1 | Erros técnicos expostos (`ex.Message` cru, "partição"/"(offline)") | Crítico | `ArmCommandService.cs`, `ControlIdProvider.cs`, `IntelbrasProvider.cs` |
| 2 | Cadastro facial duplicado (sem checagem) | Crítico | `CredencialServico.CreateAsync` |
| 3 | Cadastro facial sem foto real | Crítico | `CredenciaisScreen.tsx` (Sprint 7) |
| 4 | Aba Acessos é CRUD, não painel de comando | Alto | `AccessScreen.tsx` (Sprint 16) |
| 5 | Tela de equipamento 100% técnica | Alto | `DetalhesEquipamentoScreen.tsx` |
| 6 | Empty States sem CTA | Médio | ~10 telas sem prop `cta` |
| 7 | HeroCard sem conectividade útil ("Online"/"Offline" cru) | Médio | `HeroCard.tsx` (Sprint 16) |

3 lacunas arquiteturais foram descobertas ao verificar os achados (não assumidas pela missão) e
resolvidas com decisão explícita do usuário antes de qualquer código — ver ADR 0020 Decisões 1-3:
não existe endpoint de upload de foto de morador; não existe campo de perfil em nenhuma resposta
de API (domínio sem RBAC); só centrais JFL têm PGM real (Intelbras/Control iD não).

## Fase 1 — Discovery (resumo)

- **Mapeamento de Perfis**: domínio sem RBAC completo (dívida técnica item 6) — perfil
  morador/técnico vira preferência de UI local (`auth/profilePreference.ts`), padrão `'morador'`.
- **Inventário de mensagens de erro**: `utils/errorMapper.ts` classifica por prioridade (sem
  internet → falha de servidor → dispositivo não responde → não encontrado → vazamento técnico →
  mensagem de domínio já amigável), com mensagens distintas para cada categoria.
- **Fluxo de Cadastro Facial**: anti-duplicação client-side + prioridade de captura (Control
  iD/dispositivo inexistente → `expo-image-picker` câmera → galeria → indisponível).
- **Painel de Controle**: mapeia PGM `permitida=true` de centrais JFL para comando amigável
  (ícone + nome + estado + ação + feedback) — backend já suporta acionamento desde a Sprint 12.

## Escopo entregue

1. **Tratamento de Erros**: `errorMapper.ts` + `client.ts` centralizado (toda tela já se
   beneficia via `err instanceof ApiError ? err.message`) + `ErrorBoundary` com texto específico +
   `Toast` (feedback de ação com "Tentar novamente").
2. **Cadastro Facial**: anti-duplicação (modal Atualizar foto/Remover/Cancelar) + captura real via
   `expo-image-picker` (câmera → galeria) + pré-visualização + credencial criada sem persistir a
   foto (disclosure explícito) + thumbnail/status/Atualizar foto/Remover na lista.
3. **Aba Acessos — Painel de Controle**: comandos reais (só JFL, `permitida=true`), rótulos
   amigáveis locais por equipamento, Empty State honesto quando vazio.
4. **Detalhes do Equipamento — RBAC de UI**: visão morador (ícone + nome + estado + "Ver
   histórico") vs. visão técnico (tela completa) via `perfil`; estendido a `MinhaPropriedadeScreen`
   (seção "Proteção" escondida do morador) para coerência.
5. **HeroCard — conectividade útil**: "Conectado"/"Última comunicação há X min"/"Sem comunicação
   desde HH:MM", derivado de campos já reais via SignalR (Sprint 14), sem mudança de backend.
6. **Empty States com CTA**: adicionado em Entregas, Permissões, Visitantes, Pontos de Acesso,
   Unidades, Veículos, Centrais JFL/Intelbras, Moradores, Credenciais, Equipamentos, Vagas,
   Autorizações, Saúde da Propriedade. Câmeras mantida sem CTA (exceção deliberada — nenhuma ação
   real existe, documentada no próprio arquivo).
7. **Ajustes**: reorganizado em Propriedade/Conta/Legal + toggle discreto "Modo técnico".
8. **Validação em dispositivo real**: build EAS gerado ao final — ver relatório de entrega para o
   link e status.

## Fora de Escopo (confirmado)

RBAC completo no domínio/backend, novos comandos no backend, qualquer alteração em backend/
domínio/contratos de API/integrações/SignalR/Providers/Snapshot/Timeline/Eventos, Analytics, Push
Notification, novos fabricantes, câmeras ao vivo, biometria facial real de verdade, automações,
regras de acesso.

## Processo Obrigatório

Executado em etapas pequenas, cada uma seguida de `dotnet build` (backend, sem regressão) +
`npm run typecheck` + `npm run lint`: Escopo 1 (erros/Toast) → Escopo 3 (Painel de Controle) →
Escopo 2 (facial) → Escopo 4 (RBAC de UI) → Escopo 5 (HeroCard) → Escopo 6 (Empty States) →
Escopo 7 (Ajustes) → `expo-doctor` → build EAS final → documentação.

## Critérios de Aceite

Todos atendidos — ver relatório de entrega (`docs/reviews/SPRINT_017.md`) para evidências
detalhadas e parecer do Reviewer nos 8 pilares desta Sprint.
