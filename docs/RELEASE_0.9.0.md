# Release 0.9.0 — AppMorador

**Data**: 2026-07-25
**Natureza**: versão congelada de rastreabilidade (Sprint 17.5) — nenhuma funcionalidade nova,
nenhuma alteração de domínio/API/integrações em relação à Sprint 17.

## Estado atual

Backend em Clean Architecture (.NET 8) + Mobile React Native/Expo, cobrindo o ciclo completo de
segurança residencial/pequeno comércio self-service: cadastro e autenticação, propriedades com
unidades e moradores, controle de acesso (credenciais/permissões/pontos de acesso), visitantes e
autorizações, veículos e garagens, entregas e correspondências, integração com 3 fabricantes de
equipamento (Control iD, JFL Active 100 Bus, Intelbras), camada operacional unificada com
atualização em tempo real (SignalR), Design System mobile oficial (UX001) com RBAC de UI local e
tratamento de erro amigável.

## Funcionalidades implementadas (por Sprint)

| Sprint | Entrega |
|---|---|
| 1 | Autenticação (JWT+refresh+BCrypt+lockout), CRUD de Propriedade, Dashboard, app mobile Expo nasce |
| Padronização | Domínio inteiro traduzido para português (pt-BR) |
| 2 | Dashboard Premium, Design System formalizado, Reanimated |
| 3 | Central de Eventos (timeline paginada, fontes plugáveis) |
| 3.1 | Homologação/estabilização — v0.3.0-alpha |
| 4 | Dashboard Operacional Inteligente |
| 5 | Alinhamento visual a protótipo |
| 6 | Domínio Propriedade > Unidade > Morador, soft delete (ADR 0009) |
| 7 | Controle de Acesso — Credencial/PermissaoAcesso/PontoAcesso (ADR 0010) |
| 8 | Visitantes e Autorizações (ADR 0011) |
| 9 | Veículos e Garagens (ADR 0012) |
| 10 | Entregas e Correspondências (ADR 0013) |
| 11 | Integração Control iD (ADR 0014) |
| 12 | Integração JFL Active 100 Bus (ADR 0015) |
| 13 | Camada Operacional Unificada (ADR 0016) |
| 14 | Tempo Real via SignalR (ADR 0017) |
| 15 | Integração Intelbras (ADR 0018) |
| 16 | Design System Mobile Oficial UX001 (ADR 0019) |
| 17 | Refinamento da Experiência do Morador — RBAC de UI, erros amigáveis, Painel de Controle (ADR 0020) |
| 17.5 | Release 0.9.0 — backup, portabilidade, disaster recovery (esta versão) |

## Limitações conhecidas (não bloqueantes)

- **Sem RBAC real no domínio** — qualquer usuário autenticado acessa igualmente as próprias
  Propriedades; `perfil` morador/técnico no Mobile é só preferência de UI local (ADR 0020).
- **Sem CRUD completo de Usuários** — só Cadastro/Login/Refresh/Logout.
- **Facial**: captura e pré-visualização reais (Sprint 17), mas a foto não é persistida no
  backend — falta endpoint de upload (`Morador.FotoPath` nunca preenchido).
- **Câmeras ao vivo**: não implementado — só captura de snapshot pontual no disparo de alarme.
- **PGM/comando remoto**: só centrais JFL têm suporte real; Intelbras e Control iD não.
- **Integrações validadas só contra simuladores** (`backend/tools/*Simulator`) — Control iD e JFL
  nunca foram validados contra hardware físico real.
- Lista completa (36 itens, com motivo/impacto/prioridade/sugestão de resolução para cada) em
  `docs/DIVIDA_TECNICA.md`.

## Backlog (próximas fases, não iniciadas)

Ver `docs/roadmap/ROADMAP.md` seção "Não iniciado/backlog": integração real de controle de acesso
para os fabricantes restantes, validação contra hardware físico (Control iD/JFL), unificação
`Central`/`Equipamento`, integração de Entregas/Veículos com conectores automáticos, upload real
de foto facial, visualização ao vivo de câmeras, compartilhamento de propriedade entre múltiplos
usuários, entre outros.

## O que esta versão NÃO é

Não é uma versão "1.0" pronta para produção com clientes reais — é um ponto de restauração
completo e reproduzível do estado atual do projeto, para garantir que o desenvolvimento possa
continuar (ou ser retomado do zero, em caso de desastre) sem depender de conhecimento tácito da
equipe. Ver `docs/DISASTER_RECOVERY.md`.

## Como instalar/rodar

Ver `docs/SETUP.md` (ponto de entrada) → `docs/setup/SETUP_AMBIENTE.md` (detalhe completo).

## Changelog completo

Ver `docs/CHANGELOG.md` para o histórico Sprint a Sprint.
