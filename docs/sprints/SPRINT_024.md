# Sprint 24 — Alertas confiáveis

> Fase 1 (planejamento, sem código) — 2026-11-08. Origem: [Plano de Ação de Produto](../roadmap/PLANO_ACAO_PRODUTO.md).
> Pré-requisito: Sprint 23 (base única) — o alerta crítico só existe no app nativo.

## 1. Objetivo

Nenhum evento crítico passa despercebido: disparo, pânico e queda do sistema chegam como alerta
impossível de ignorar e, se ninguém confirmar, sobem para outras pessoas.

## 2. Levantamento (estado real)

| Ponto | Situação |
|---|---|
| Push | Pipeline completo desde a Sprint 19 (`NotificationDispatcher` → `NotificationService` → `FirebaseNotificationProvider`), canais `alertas`/`atividades`/`geral`. Sem credenciais Firebase: envio é no-op. |
| Destinatários | Só os dispositivos do **dono** (`Propriedade.ProprietarioId`). Membros chegam na Sprint 25. |
| Disparo | `AlarmEventProcessor` notifica `AlarmeDisparado` ao criar a `Ocorrencia`. Sem confirmação de leitura nem escalonamento. |
| Códigos de alarme | `ContactIdCatalog` reconhece **só `1130`** (disparo de zona). Falta de energia, bateria baixa, violação e pânico vindos da central são descartados como "desconhecidos". |
| Queda de comunicação | `EquipamentoOffline` só é notificado quando um **comando** falha. O servidor JFL já emite `SessaoRemovida` quando a central desconecta, mas ninguém escuta. Queda silenciosa (sem fechar a conexão) não é detectada. |
| Timeline | Online/Offline não vira evento no histórico (dívida 24). |
| Debounce | Em memória (dívida 41) — perde-se ao reiniciar. |
| Agendamento | Não existe nenhum serviço de fundo além do servidor JFL e do observador de conexão. |

## 3. Escopo

### Backend
1. **Catálogo Contact ID ampliado** com os códigos do padrão SIA DC-05 (ver Decisão A):
   disparo, pânico, falta/retorno de energia, bateria baixa/restaurada, violação. Cada código novo
   marcado como "pendente de homologação física" até o teste na central JFL.
2. **Alerta crítico** (`AlertaCritico`): criado para disparo, pânico, violação e perda de
   comunicação; guarda origem, quem confirmou e quando, e o nível de escalonamento.
3. **Confirmação** "Estou ciente": `POST /api/alertas/{id}/confirmar` — registra quem e quando e
   encerra o escalonamento; aparece no histórico.
4. **Escalonamento**: serviço de fundo simples (a cada 30 s) que procura alertas sem confirmação
   vencidos e avança o nível: dono → demais membros (quando existirem, Sprint 25) → contatos de
   emergência (Decisão B). Tempo por propriedade, padrão 2 minutos.
5. **Contatos de emergência**: CRUD por propriedade (nome, telefone, ordem), soft delete.
6. **Botão de pânico**: `POST /api/propriedades/{id}/panico` — cria alerta crítico e dispara o
   mesmo fluxo; registrado no histórico com o autor.
7. **Saúde do sistema**:
   - `SessaoRemovida` → equipamento Offline + alerta "Central sem comunicação";
   - **vigia de keep-alive**: sessão sem atividade por 3× o intervalo combinado = queda silenciosa;
   - retorno da conexão → evento "Comunicação restabelecida" (sem push, só histórico).
   - Online/Offline registrado como evento na Central de Eventos (fecha dívida 24).
8. **Debounce persistido** no banco (fecha dívida 41).

### App (base única)
1. **Canal crítico Android**: importância máxima, som próprio, vibração longa, opção de ignorar o
   "Não perturbe" (só funciona se o usuário conceder a permissão — o app explica e leva à tela do
   sistema).
2. **Tela de alerta crítico** em tela cheia ao tocar na notificação (ou ao receber pelo tempo real
   com o app aberto): o que aconteceu, onde, quando, última imagem da câmera, **Estou ciente**,
   **Ligar para…** (contatos) e **Ver câmeras**.
3. **Pânico** no Início: botão que exige **segurar 3 s** (evita acionamento acidental), com
   confirmação visual.
4. **Contatos de emergência** no Menu: listar, adicionar, ordenar, remover.
5. **Ajustes**: tempo de escalonamento e atalho para a permissão de "Não perturbe".
6. Histórico mostra confirmações, escalonamentos e quedas/retornos de comunicação.

### Fora do escopo
- Credenciais Firebase reais (entram quando o usuário criar o projeto; até lá, no-op + logs).
- Membros com login próprio (Sprint 25) — o nível "membros" fica pronto e vazio.
- Tela cheia sobre a tela bloqueada (`USE_FULL_SCREEN_INTENT`): o Android 14+ restringe a apps de
  despertador/chamada; usamos notificação de prioridade máxima.

## 4. Decisões do usuário (2026-11-08)

- **A → adotar a tabela padrão Contact ID (SIA DC-05)**, marcada como pendente de homologação na central JFL.
- **B → SMS automático** por provedor, atrás de uma interface (`IEnvioSms`), em modo simulado até haver conta.

### Contexto das decisões

**A. Códigos de alarme.** A regra do projeto é nunca dar significado a um código sem fonte. O
padrão Contact ID (SIA DC-05) é público e é a fonte natural; a JFL o segue, mas não foi confirmado
em hardware. Proposta: adotar a tabela padrão, marcada como pendente de homologação física.

**B. Como avisar contatos de emergência** (eles não têm o app):
- **SMS** por um provedor (ex.: Zenvia, Twilio) atrás de uma interface, em modo simulado até haver
  conta — custo por mensagem;
- **WhatsApp** (API oficial do WhatsApp Business) — mais caro e burocrático de aprovar;
- **Só ligação manual**: a tela de alerta mostra "Ligar para…"; o escalonamento automático vai só
  para usuários do app.

## 5. Modelo de dados (migration aditiva, tabelas em pt-BR)

| Tabela | Campos principais |
|---|---|
| `AlertasCriticos` | Id, PropriedadeId, Tipo (Disparo/Panico/Violacao/SemComunicacao), OcorrenciaId?, EquipamentoId?, CriadoEmUtc, ConfirmadoEmUtc?, ConfirmadoPorUsuarioId?, NivelEscalonamento, ProximoEscalonamentoEmUtc? |
| `ContatosEmergencia` | Id, PropriedadeId, Nome, Telefone, Ordem, soft delete |
| `EventosConectividade` | Id, EquipamentoId, PropriedadeId, Tipo (Perdida/Restabelecida), OcorridoEmUtc |
| `ControlesEnvioNotificacao` | Chave, UltimoEnvioEmUtc (debounce) |
| `Propriedades` (+coluna) | `MinutosEscalonamento` (padrão 2) |

Nenhuma operação destrutiva. Resumo de revisão apresentado antes de aplicar.

## 6. Ordem de implementação

1. Catálogo Contact ID + testes.
2. Migration (resumo de revisão antes de aplicar).
3. Alerta crítico + confirmação + pânico (Application/Api) + testes.
4. Serviço de escalonamento + debounce persistido + testes.
5. Saúde do sistema: `SessaoRemovida`, vigia de keep-alive, eventos de conectividade na Central de Eventos.
6. Contatos de emergência (backend + tela) e canal escolhido na Decisão B.
7. App: canal crítico, tela de alerta, pânico, ajustes, histórico.
8. ADR 0040, documentação, APK + homologação em dispositivo, Reviewer.

## 7. Riscos

| Risco | Mitigação |
|---|---|
| Códigos JFL diferentes do padrão | Catálogo isolado e marcado como pendente; um código errado só deixa de notificar, não quebra o processamento. |
| Falso alarme de pânico | Segurar 3 s + autor registrado. |
| Escalonamento duplicado com várias instâncias do backend | Avanço de nível com atualização condicional no banco (só avança quem ainda está no nível esperado). |
| Push no-op até haver Firebase | Logs e histórico mostram o que seria enviado; homologação valida o fluxo pelo tempo real (SignalR). |
| Usuário não concede "Não perturbe" | App explica e mostra o estado da permissão em Ajustes. |

## 8. Testes

- Backend: catálogo, criação/confirmação de alerta, avanço de nível, confirmação parando o
  escalonamento, pânico, `SessaoRemovida`/vigia, debounce.
- App: jest da tela de alerta e do botão de pânico; typecheck; lint; expo-doctor.
- Simulador JFL enviando disparo, falta de energia e queda de conexão.
- Homologação no Android: alerta crítico com som, "Estou ciente", pânico, contatos, regressão obrigatória.

## 9. Critérios de aceite

1. Disparo simulado gera alerta crítico com som no canal de prioridade máxima.
2. Sem confirmação em 2 minutos, o próximo nível é acionado e isso aparece no histórico.
3. "Estou ciente" encerra o escalonamento e registra quem confirmou.
4. Pânico (segurar 3 s) gera o mesmo fluxo.
5. Desligar o simulador da central gera "Central sem comunicação"; religar registra o retorno.
6. Falta de energia e bateria baixa simuladas aparecem no histórico e notificam.
