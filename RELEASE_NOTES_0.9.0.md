# AppMorador 0.9.0

Versão congelada de rastreabilidade (Sprint 17.5) — ponto de restauração completo do projeto após
a Sprint 17 (Refinamento da Experiência do Morador). **Nenhuma funcionalidade nova** em relação à
Sprint 17 — esta Release existe para garantir que qualquer pessoa consiga clonar, restaurar o
banco, configurar o ambiente e rodar Backend + Mobile do zero, sem depender de conhecimento tácito
da equipe.

## O que está incluído

- **Backup do banco**: `appmorador_full_20260725.sql` anexado a esta Release (dump completo,
  schema + dados de desenvolvimento). `schema.sql`/`seed_data.sql` ficam versionados em
  `database/`. Ver `database/README.md`.
- **Scripts de automação** (`scripts/`): `setup_project.ps1`, `backup_database.ps1`,
  `restore_database.ps1`, `clean_project.ps1`, `start_backend.ps1`, `start_mobile.ps1`,
  `start_frontend.ps1` (documenta a ausência de um frontend web separado).
- **Documentação nova**: `docs/SETUP.md`, `docs/ARCHITECTURE.md`, `docs/AUDITORIA_AMBIENTE.md`,
  `docs/ENVIRONMENT.md`, `docs/STORAGE.md`, `docs/DISASTER_RECOVERY.md`, `docs/RELEASE_0.9.0.md`.
- **`.env.example`** para Backend (novo) e Mobile (já existia).
- Consolida no histórico do git todo o trabalho das Sprints 4-17 (nunca commitado desde a tag
  `v0.3.0-alpha`) — ver `docs/CHANGELOG.md` para o histórico completo Sprint a Sprint.

## Como instalar

Ver [`docs/SETUP.md`](docs/SETUP.md) (ponto de entrada) e
[`docs/DISASTER_RECOVERY.md`](docs/DISASTER_RECOVERY.md) (recuperação completa de desastre).

## Changelog completo

Ver [`docs/CHANGELOG.md`](docs/CHANGELOG.md).

## Limitações conhecidas

Ver [`docs/RELEASE_0.9.0.md`](docs/RELEASE_0.9.0.md) e [`docs/DIVIDA_TECNICA.md`](docs/DIVIDA_TECNICA.md)
(36 itens registrados, com motivo/impacto/prioridade/sugestão de resolução para cada).
