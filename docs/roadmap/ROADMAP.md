# Roadmap — Segurança Conectada

## Concluído

- **Fase 0** — Pivot de portaria virtual para app self-service de segurança (B2C/B2B).
- **Fases 1–2.2** — Pipeline de eventos JFL: ACK imediato, `Occurrence`, `AlarmEventLog` de
  auditoria, catálogo Contact ID extensível, captura síncrona de snapshot no disparo
  (Dahua/Intelbras via CGI, Hikvision via ISAPI).
- **Sprint 1** — Autenticação (JWT + refresh + BCrypt + lockout), CRUD de Propriedade, Dashboard
  com Health Score. App mobile Expo (Splash, Login, Cadastro, Selecionar Propriedade, Dashboard).
- **Sprint de Padronização** — Domínio de negócio inteiro traduzido para português (pt-BR),
  preservando arquitetura e dados existentes. Ver `docs/reviews/SPRINT_001_1.md`.
- **Sprint 2 — Dashboard Premium / primeira Sprint de Produto** — Dashboard enriquecido
  (tipo de propriedade, resumo da instalação, Health Score com rótulo amigável), mobile
  componentizado e com Design System formalizado (`theme/tokens.ts`), `react-native-reanimated`
  adotado como padrão. Concluída na Sprint 2.1 após hotfix de serialização de enum. Ver
  `docs/reviews/SPRINT_002.md`.
- **Sprint 3 — Central de Eventos Inteligente** — timeline de eventos paginada (período/busca),
  modelo unificado por fontes plugáveis (`IFonteEventos`/`EventoTimeline`), única fonte real hoje
  (`JflFonteEventos`, central de alarme). Ver `docs/reviews/SPRINT_003.md` e ADR 0006.
- **Sprint 3.1 — Homologação/Estabilização** — JFL Server nunca mais derruba a Api (falha de bind
  vira log, não exceção fatal); histórico de migrations squashado num único `InitialCreate`
  validado sem divergência (ADR 0007); seed de desenvolvimento idempotente; autenticação, CRUD de
  Propriedades, Dashboard e Central de Eventos homologados fim a fim via requests reais; único
  problema de performance real encontrado corrigido (`AsNoTracking` em listagem). Ver
  `docs/reviews/SPRINT_003_1.md` e `docs/TESTES_FUNCIONAIS.md`.

## Próximo

- **v0.3.0-alpha**: marco de estabilização atingido na Sprint 3.1 — base executável, previsível e
  documentada para homologação manual.
- **Sprint 4**: aguardando definição de escopo — não iniciar sem autorização explícita.

## Não iniciado / backlog

- **CRUD completo de Usuários** — hoje só existe Cadastro/Login/Refresh/Logout. Editar dados,
  alterar senha, desativar/reativar conta, excluir conta e qualquer sistema de Perfis/Papéis
  (Administrador/Supervisor/Operador/Morador) não existem — decisão explícita da Sprint 3.1 foi
  homologar só o que existe hoje e registrar o restante aqui, não simular funcionalidade
  inexistente. Ver `docs/DIVIDA_TECNICA.md`.

- **Integração de Controle de Acesso** (Control iD ou Intelbras ASI/CGI) — fase própria, dimensão
  comparável à Fase 2 (Snapshot). Investigação já feita na Sprint 3 (ver `DIVIDA_TECNICA.md` item
  5): referência `Teste-portaria-main1` tem integração Control iD nunca validada contra hardware
  real, e uma integração Intelbras/CGI que na prática é um leitor de acesso família ASI, com
  ajustes documentados como pendentes lá. A Central de Eventos já está preparada
  (`IFonteEventos`) para receber essa fonte sem redesenho quando essa fase acontecer.
- Filtro por zona na Central de Eventos (hoje só período + busca de texto) — depende de um
  endpoint de listagem de zonas que ainda não existe.
- Módulo de Clips/vídeo (substituído pelo MVP de snapshot por ora).
- Armar/Desarmar funcional (hoje é só visual no mobile — handlers já preparados para receber a
  chamada real, ver `AcoesRapidas` no mobile).
- Compartilhamento de propriedade entre múltiplos usuários.
- Homologação de novos códigos Contact ID / novos fabricantes de DVR.
- Registrar formalmente como ADR as decisões técnicas da Sprint 2 (adoção do Reanimated como
  padrão, `TipoPropriedade`) — ver `DIVIDA_TECNICA.md`.
