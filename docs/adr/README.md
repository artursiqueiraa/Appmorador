# Decisões de Arquitetura (ADRs) — AppMorador

Esta pasta é o histórico permanente de toda decisão arquitetural relevante do projeto. Cada
arquivo é uma mini-ADR (Architecture Decision Record): um registro autocontido de uma decisão,
escrito para que alguém sem nenhum contexto da discussão original consiga entender o que foi
decidido, por quê, e o que isso afeta.

Nada aqui é temporário. Uma vez criado, um registro nunca é apagado ou reescrito para esconder o
que foi decidido antes — mesmo quando uma decisão é revertida ou substituída, o histórico
permanece intacto.

**Histórico**: até a Sprint 4, os registros viviam split entre `.decisions/` (numeração própria) e
`docs/adr/` (só `0001-conectividade-dvr.md`). Consolidados numa única pasta/sequência antes da
Sprint 4, preservando todo o conteúdo original — nenhuma decisão foi reescrita, só relocada e
renumerada. `docs/DECISOES_ARQUITETURA.md` (o antigo índice/mirror em prosa) foi descontinuado;
esta pasta é a única fonte de verdade agora.

## Índice

| # | Decisão | Data | Status |
|---|---|---|---|
| [0002](0002-conectividade-dvr.md) | Conectividade com o DVR na LAN do cliente | 2026-07-18 | Em aberto |
| [0003](0003-dominio-negocio-pt-br.md) | Domínio de negócio em português (pt-BR), infraestrutura em inglês | 2026-07-19 | Decidido |
| [0004](0004-rename-tabela-via-migration-manual.md) | Rename de tabela via migration manual, nunca Drop+Create | 2026-07-19 | Decidido |
| [0005](0005-serializacao-global-enums-negocio.md) | Enums de negócio serializam como texto via configuração global | 2026-07-19 | Decidido |
| [0006](0006-modelo-eventos-fontes-plugaveis.md) | Central de Eventos: modelo unificado por fontes plugáveis (IFonteEventos) | 2026-07-19 | Decidido |
| [0007](0007-squash-migrations-v030-alpha.md) | Squash do histórico de migrations num único InitialCreate (pré-v0.3.0-alpha) | 2026-07-19 | Decidido |
| [0008](0008-migrations-automaticas-no-startup.md) | Migrations aplicadas automaticamente no startup da Api | 2026-07-20 | Decidido |
| [0009](0009-soft-delete-dominio-principal.md) | Soft delete como padrão de exclusão no domínio principal (Propriedade/Unidade/Morador) | 2026-07-21 | Decidido |
| [0010](0010-dominio-controle-acesso.md) | Domínio de Controle de Acesso: Credencial + PermissaoAcesso + PontoAcesso | 2026-07-21 | Decidido |
| [0011](0011-dominio-visitantes-autorizacoes.md) | Domínio de Visitantes e Autorizações: Visitante na Propriedade + Status híbrido (computado/manual) | 2026-07-21 | Decidido |
| [0012](0012-dominio-veiculos-garagens.md) | Domínio de Veículos e Garagens: Vaga independente + Status híbrido + PermissaoVeicular reaproveita PontoAcesso | 2026-07-21 | Decidido |
| [0013](0013-dominio-entregas-correspondencias.md) | Domínio de Entregas e Correspondências: Status 100% manual + visão unificada por Propriedade | 2026-07-22 | Decidido |
| [0014](0014-provider-integracao-control-id.md) | Arquitetura oficial de integração de fabricante: Provider Control iD (Equipamento + IControlIdProvider) | 2026-07-22 | Decidido |
| [0015](0015-provider-integracao-jfl-active-100-bus.md) | Migração JFL Active 100 Bus: Provider para protocolo de conexão invertida (IJflProvider + StatusCentralJfl) | 2026-07-22 | Decidido |
| [0016](0016-camada-operacional-unificada.md) | Camada Operacional Unificada: Estado Bruto → Classificador Operacional → Snapshot Operacional | 2026-07-25 | Decidido |
| [0017](0017-comunicacao-operacional-tempo-real.md) | Comunicação Operacional em Tempo Real: SignalR como transporte, grupos por Propriedade | 2026-07-25 | Decidido |
| [0018](0018-prova-extensibilidade-arquitetura.md) | Prova de Extensibilidade da Arquitetura: integração Intelbras sem alterar camadas compartilhadas | 2026-07-25 | Decidido |
| [0019](0019-ux-first-design-system-mobile-ux001.md) | UX First: Design System Mobile Oficial UX001 — Bottom Nav, HeroCard, vocabulário do morador, onboarding persistente | 2026-07-25 | Decidido |
| [0020](0020-rbac-ui-local-e-refinamento-experiencia-morador.md) | RBAC de UI Local e Refinamento da Experiência do Morador: perfil local, captura facial sem persistência, Painel de Controle só JFL, erros mapeados centralmente | 2026-07-25 | Decidido |
| [0022](0022-experiencia-tempo-real.md) | Experiência em Tempo Real: RealtimeContext dividido em 3 (Regra 5), backoff exponencial customizado, Timeline com inserção real e scroll preservado, Painel de Controle com máquina de estados grounded na arquitetura síncrona real | 2026-07-26 | Decidido |
| [0023](0023-notificacoes-push.md) | Notificações Push: DispositivoPush + INotificationProvider (Firebase, modo sem-op documentado), NotificationDispatcher com hooks diretos na Aplicação, debounce em memória, canais Android, token nativo FCM, deep link com retry de prontidão | 2026-07-26 | Decidido |
| [0024](0024-visualizacao-cameras.md) | Visualização de Câmeras: evolução da entidade Camera (StatusCamera, dois timestamps), captura sob demanda via ISnapshotCaptureService, imagem servida autenticada com content-type sniffado, evento SignalR leve CameraStatusAlterado, seed com PNG gerado em memória | 2026-07-26 | Decidido |
| [0021](0021-rbac-master-role-global-vs-perfil-propriedade.md) | RBAC Master: Role Global (interno) vs. Perfil de Propriedade (cliente) — só RBAC de internos implementado, ProprietarioId continua fonte de verdade do cliente, RequireAssertion em vez de AuthorizationHandler, auditoria de falha centralizada | 2026-07-26 | Decidido |
| [0025](0025-permissoes-funcionais-granulares.md) | Permissões Funcionais Granulares: UsuarioPropriedadePermissao (N:N), Plano Básico automático na criação da Propriedade | 2026-07-26 | Decidido |
| [0026](0026-feature-flags-por-propriedade.md) | Feature Flags por Propriedade: PropriedadeFeatureFlag ortogonal a Permissão Funcional, consumo do mobile via GET /api/properties enriquecido | 2026-07-26 | Decidido |
| [0027](0027-modelo-equipamento-capacidades-dinamicas.md) | ModeloEquipamento + Capacidades Dinâmicas: catálogo real substitui Equipamento.Modelo (texto), migration com backfill de dados, resolução transparente sem mudar o contrato da Api | 2026-07-26 | Decidido |
| [0028](0028-provisionamento-metadados.md) | Provisionamento como Registro de Metadados: árvore completa de vínculos de hardware adiada, escopo reduzido a Nome/Template/Status | 2026-07-26 | Decidido |

## Quando criar uma decisão nova

Cria-se um registro sempre que a decisão for:

- **Estrutural**: muda a divisão de camadas, cria/remove um projeto ou módulo, altera a direção
  de dependência entre partes do sistema.
- **De tecnologia**: adota, troca ou remove uma biblioteca/framework/ferramenta usada de forma
  ampla no projeto.
- **De política permanente**: define uma regra de segurança, de dado, ou de processo que vai
  valer para todo o projeto daqui em diante (não só para uma entrega específica).
- **De difícil reversão**: uma vez implementada, desfazer custa caro (mudança de schema com dado
  real, mudança de contrato público, mudança de convenção de nomenclatura em massa).
- **De arbitragem**: resolve um conflito de responsabilidade entre duas áreas do projeto que, sem
  registro, provavelmente ressurgiria mais tarde.

**Não** se cria um registro para: escopo de uma entrega específica (isso vive no roteiro de
prioridades do projeto), detalhe de implementação sem impacto estrutural, ou qualquer escolha
facilmente reversível sem custo.

## Como criar uma decisão nova

1. Copiar `0001-template.md`.
2. Nomear o novo arquivo como `NNNN-titulo-curto-em-kebab-case.md`, usando o próximo número
   sequencial disponível (ver seção de numeração abaixo).
3. Preencher todas as seções do template por completo — nenhuma seção fica com placeholder
   ("a definir") no arquivo final.
4. Escrever para quem não participou da discussão: contexto suficiente para que a decisão faça
   sentido isoladamente, sem precisar perguntar a ninguém.

## Numeração

- `0001` é reservado para o próprio template (`0001-template.md`) e nunca é reutilizado para uma
  decisão real.
- A primeira decisão real começa em `0002`.
- A numeração é sequencial e nunca reaproveitada, mesmo que uma decisão antiga seja revertida —
  o número de um registro é permanente, assim como o próprio registro.

## Quando uma decisão é revertida ou substituída

O arquivo antigo nunca é apagado nem editado para remover o conteúdo original. Adiciona-se uma
nota curta no topo do arquivo antigo apontando para o novo registro (ex.: "Substituída pela
decisão 000X — ver motivo lá"), preservando a decisão original como ela foi tomada.

## Quem escreve

Qualquer pessoa (ou agente, ver `.agents/`) pode registrar uma decisão relevante do seu domínio.
Decisões que atravessam mais de uma área do projeto são de responsabilidade do agente
`arquiteto`, que atua como árbitro final quando há divergência.

## Padrão de qualidade

Um registro só está pronto quando alguém de fora da discussão original consegue lê-lo e entender,
sem ajuda externa: o que era o problema, o que foi decidido, por que essa alternativa venceu as
outras, e o que muda em consequência.
