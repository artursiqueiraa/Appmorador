# Plano de Ação — Produto Akteck Seg

> Origem: parecer técnico de 2026-11-08 (levantamento de controllers, telas, painel, `ROADMAP.md`
> e `DIVIDA_TECNICA.md`). Escopo aprovado pelo usuário: **tudo o que o parecer recomendou**, com
> uma única pendência declarada — a homologação oficial em hardware físico (Control iD, Hikvision
> e central JFL), que segue em aberto e é pré-requisito de venda, não de desenvolvimento.

## Princípio orientador

Todo item deste plano serve a uma das cinco necessidades do dono do imóvel ou da loja:

1. **Ser avisado na hora** — de forma impossível de ignorar.
2. **Entender o que aconteceu** — o quê, onde, quem, com imagem.
3. **Agir rápido** — armar, desarmar, abrir, pedir ajuda.
4. **Delegar** — família, funcionários, prestadores, com limites.
5. **Ter tranquilidade e histórico** — "está tudo bem", "a loja abriu às 8h02 pela Ana".

Funcionalidade que não serve a nenhuma das cinco fica escondida para residências e lojas.

## Regras de execução (valem para todas as Sprints)

- Cada Sprint segue o processo em duas fases: **Fase 1** (planejamento sem código, entregue para
  aprovação) e **Fase 2** (implementação em etapas pequenas, com build/testes a cada etapa).
- Migrations sempre com o resumo de revisão (operações, impacto, destrutivo?, segurança,
  recomendação) antes de aplicar. Tabelas e colunas novas em pt-BR.
- Sprint que alterar o app segue a Regra de Validação em Dispositivo (APK preview + homologação
  manual + regressão), registrada em `docs/testing/SprintXX.md`.
- Integrações de hardware novas são validadas contra simulador e marcadas como **"pendente de
  homologação física"** em `DIVIDA_TECNICA.md` até o teste oficial.
- Uma ADR por decisão arquitetural nova; `CHANGELOG.md` e `ROADMAP.md` atualizados ao fim de cada
  Sprint; Reviewer em todos os pilares no encerramento.

## Visão geral

| Sprint | Tema | Necessidade | Resultado para o cliente |
|---|---|---|---|
| 23 | Base única (web + Android + iOS) | — | Um só app, sem divergência entre celular e navegador |
| 24 | Alertas confiáveis | 1, 3 | Disparo, queda de sistema e pânico chegam e escalonam |
| 25 | Contas e multiusuário | 4 | Família e funcionários com login e permissões próprias |
| 26 | Facial ponta a ponta + LGPD | 3, 4 | Rosto cadastrado no app libera a porta; biometria em conformidade |
| 27 | Segurança da conta e confiabilidade | 1, 5 | Comandos sensíveis confirmados, auditados; backend observável |
| 28 | Rotina e relatórios | 1, 5 | "Esqueceu de armar", abertura/fechamento, resumo diário |
| 29 | Acessos temporários e modo férias | 3, 4 | Link/QR para diarista e prestador; modo férias |
| 30 | Câmeras: ao vivo e clipe do disparo | 2 | Vídeo ao vivo e o clipe do momento do alarme |
| 31 | Mobilidade | 1, 3 | Lembrete por localização, widgets, atalhos de voz, iOS |
| 32 | Produto comercial | — | Planos Essencial/Plus; suporte com chamados |

Ordem pensada para que cada Sprint aproveite a anterior: a base única (23) vem primeiro porque
alerta crítico, biometria, localização e widgets só existem no app nativo; alertas (24) vêm
antes do multiusuário por serem o item mais crítico — o escalonamento já funciona com contatos de
emergência e passa a incluir os demais membros quando a Sprint 25 entrar; acessos temporários (29) e relatórios (28) precisam de multiusuário (25) e
auditoria (27).

---

## Sprint 23 — Base única (web + Android + iOS)

**Objetivo:** um só código de app, que gera a versão de navegador e a de celular (decisão do
usuário em 2026-11-08: unificar, em vez de liberar a cópia mobile ou ficar só no navegador).

- O `webapp/` (identidade Akteck Seg, ADRs 0037/0038) vira a **base oficial** e passa a gerar
  também o Android (EAS) e, mais adiante, o iOS.
- Recursos nativos que hoje são "passthrough" no navegador (push, câmera, armazenamento seguro)
  voltam a funcionar no celular por arquivos específicos de plataforma (`.native.ts`/`.web.ts`).
- Levantamento do que existe só no `mobile/` e ainda não está no `webapp/`, e migração do que
  faltar.
- `mobile/` é **arquivado** (não apagado) só depois que o APK da base única for homologado no
  celular; fecha itens 52 e 59.

**Aceite:** o mesmo código abre no navegador e gera um APK que passa na regressão obrigatória
(login, logout, troca de propriedade, Início, Acessos, push, tempo real).

## Sprint 24 — Alertas confiáveis

**Objetivo:** nenhum evento crítico passa despercebido.

- Push real via Firebase (fecha `DIVIDA_TECNICA` item 38). O usuário já tem conta no Firebase; o
  projeto e as credenciais entram depois — até lá o envio roda em modo de simulação, sem bloquear
  a Sprint.
- **Alerta crítico** de disparo: canal Android de alta prioridade, som próprio, tela cheia.
- **Confirmação de leitura** ("Estou ciente") registrada no histórico com quem e quando.
- **Escalonamento:** sem confirmação em N minutos (configurável) → demais membros da propriedade →
  contatos de emergência.
- **Contatos de emergência** por propriedade (nome, telefone, ordem).
- **Botão de pânico** no Início: dispara o mesmo fluxo de escalonamento.
- **Saúde do sistema:** eventos de conectividade na Timeline (fecha item 24) e alertas de
  central sem comunicação, falta de energia, bateria baixa e violação — usando os códigos Contact
  ID do catálogo JFL (conferir quais já estão confirmados antes de mapear).
- Debounce de notificações persistido no banco em vez de memória (fecha item 41).

**Aceite:** um disparo simulado gera alerta crítico; sem confirmação, o segundo membro recebe;
queda da central aparece no histórico e notifica.

## Sprint 25 — Contas e multiusuário

**Objetivo:** o dono divide o acesso com quem precisa, com controle.

- Gestão da própria conta: editar dados, trocar senha, excluir conta (fecha item 47; atende o
  direito de exclusão da LGPD).
- **Convite** por link/e-mail para a propriedade, com aceite e criação de login.
- Papéis prontos: **Proprietário**, **Gerente** (tudo menos gestão de usuários), **Membro**
  (armar/desarmar/abrir), **Visualizador** (só ver) — sobre o RBAC já existente
  (`UsuarioPropriedadePermissoes`), sem sistema paralelo.
- Remover/suspender acesso de um membro, com efeito imediato (revogação de sessão).
- Substituir o perfil local morador/técnico pelo perfil real vindo da API (fecha item 34).
- **Simplificação por tipo de propriedade:** Vagas e Entregas escondidas para Residência e
  Comércio (usando `TipoPropriedade` + feature flags por propriedade); telas sem funcionalidade
  real removidas da navegação (fecha item 58).

**Aceite:** dono convida um funcionário como Membro; o funcionário arma/desarma mas não vê
gestão de usuários; ao ser removido, perde o acesso na hora.

## Sprint 26 — Facial ponta a ponta + LGPD

**Objetivo:** o rosto cadastrado no app libera a porta, dentro da lei.

- Envio da foto ao leitor **Control iD** (cadastro/troca/remoção do rosto no equipamento) — fecha
  item 57.
- Provider de controle de acesso **Hikvision** (terminais faciais via ISAPI) seguindo o padrão
  oficial de fabricantes (ADR 0014) — hoje o Hikvision só existe para snapshot de câmera.
- **Consentimento biométrico** explícito na tela de captura, persistido (quem, quando, versão do
  termo); para menores, consentimento do responsável.
- Remoção da foto do disco e do equipamento ao remover a pessoa; política de retenção documentada.
- Termos de Uso e Política de Privacidade da Akteck Seg publicados e linkados (fecha item 55).

**Aceite:** cadastrar rosto no app → simulador Control iD e simulador Hikvision recebem a imagem;
remover a pessoa → imagem apagada nos dois e no servidor; sem consentimento, a captura não abre.

## Sprint 27 — Segurança da conta e confiabilidade

**Objetivo:** comandos sensíveis protegidos e plataforma confiável.

- Execução de comandos de hardware sobre a plataforma já aprovada na ADR 0033 (antiga Sprint 22C):
  idempotência e **auditoria de comandos** (quem armou, desarmou, abriu — base dos relatórios da
  Sprint 28).
- **Confirmação para desarmar e abrir portão** (PIN do app ou biometria do aparelho, configurável).
- Desbloqueio do app por digital/rosto (celular).
- Sessão do app web fora do `localStorage` (fecha item 51).
- **Backup automático** do banco com rotina agendada e teste de restauração (fecha item 37).
- Health checks e alerta interno quando o backend ou a fila de eventos cair.

**Aceite:** desarmar pede confirmação; o comando aparece na auditoria com autor; backup diário
gerado e restaurado com sucesso em ambiente de teste.

## Sprint 28 — Rotina e relatórios

**Objetivo:** o sistema cuida da rotina pelo dono.

- **Horário de funcionamento** por propriedade (dias e horários).
- **"Você esqueceu de armar"**: fora do horário e desarmado → push com ação "Armar agora".
- **Relatório de abertura e fechamento**: quem desarmou/armou e quando, por dia — tela e
  exportação.
- **Resumo diário/semanal** opcional (push e/ou e-mail): disparos, quedas, acessos, aberturas.
- Alerta de abertura/fechamento fora do horário ("a loja foi desarmada às 23h40").

**Aceite:** propriedade com fechamento às 19h e alarme desarmado às 19h15 recebe o lembrete e
arma pela notificação; relatório da semana mostra os horários e autores corretos.

## Sprint 29 — Acessos temporários e modo férias

**Objetivo:** liberar quem precisa, só quando precisa.

- **Acesso temporário** (já existe `Autorizacao` com janela de horário): geração de **link, QR
  ou PIN** e compartilhamento direto (WhatsApp etc.); recorrência ("terças, 8h–12h").
- Notificação ao dono quando o visitante entra e quando o acesso expira sem uso.
- **Modo férias**: alertas mais rigorosos, resumo diário obrigatório, contato de confiança
  incluído no escalonamento, lembrete ao voltar.

**Aceite:** dono cria acesso recorrente para a diarista e envia por WhatsApp; o primeiro uso gera
notificação; modo férias ligado inclui o contato de confiança no escalonamento.

## Sprint 30 — Câmeras: ao vivo e clipe do disparo

**Objetivo:** ver o que está acontecendo e o que aconteceu.

- ADR de arquitetura de vídeo (relay de mídia RTSP → HLS/WebRTC; onde roda; custo de banda).
- **Ao vivo** no app e no web, começando por Hikvision (provider ISAPI já existe para snapshot).
- **Clipe do disparo**: alguns segundos antes/depois do evento, anexado ao alerta (fecha item 31).
- Wording sempre honesto sobre latência e disponibilidade.

**Aceite:** câmera do simulador/gravador abre ao vivo em até X segundos; disparo gera clipe
acessível a partir da notificação.

## Sprint 31 — Mobilidade

**Objetivo:** o app trabalha mesmo quando o dono não abre o app.

- **Lembrete por localização**: saiu de casa com alarme desarmado → "Armar?".
- Widgets de tela inicial (status + armar) e atalhos de voz (Google Assistente / Siri).
- **iOS** (fecha item 39).

**Aceite:** sair da área configurada com o alarme desarmado gera o lembrete; widget arma a
propriedade; build iOS homologado.

## Sprint 32 — Produto comercial

**Objetivo:** transformar o sistema em oferta vendável.

- **Planos** Essencial / Plus via feature flags por propriedade (já existentes); painel admin
  atribui o plano. Plano Monitorado (central 24h humana) fora por ora — decisão do usuário em
  2026-11-08; o escalonamento de alertas vai só para membros e contatos de emergência.
- **Suporte:** chamados por cliente e histórico de atendimento no painel.
- Leitura de placa e conectores de entrega ficam restritos a condomínios (backlog).

**Aceite:** cliente no plano Essencial não vê câmeras ao vivo; atendimento de suporte
tem o chamado de suporte registrado com histórico no painel.

---

## Pendência declarada (fora deste plano)

| Item | Situação | Bloqueia |
|---|---|---|
| Homologação física Control iD | Validado só em simulador (item 20) | Venda / release de produção |
| Homologação física Hikvision | Snapshot e (Sprint 26) facial só em simulador | Venda / release de produção |
| Homologação física central JFL | Validado só em simulador (itens 22, 26, 49) | Venda / release de produção |

Roteiro de teste: `docs/testing/` (checklist de hardware real já existente).

## Riscos principais

- **Tudo de hardware construído sobre simulador** até a homologação física — divergências de
  protocolo podem aparecer tarde. Mitigação: providers isolados (ADR 0014), testes de contrato,
  homologação física o quanto antes.
- **Unificação (Sprint 23)** mexe na base do app inteiro — regressão completa em dispositivo antes
  de arquivar o `mobile/`.
- **Vídeo (Sprint 30)** tem custo de infraestrutura e banda que precisa de decisão antes.
- **Biometria (Sprint 26)** é dado sensível — erro de conformidade tem custo jurídico.
