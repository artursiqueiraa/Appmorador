# ADR 0040 — Alertas confiáveis: alerta crítico, escalonamento e vigia de conexão

**Data**: 2026-11-08

## Contexto

O app avisava o dono de um disparo, mas sem nenhuma garantia de que alguém viu. O levantamento da
[Sprint 24](../sprints/SPRINT_024.md) achou cinco falhas:

- **Pouco código reconhecido**: o catálogo Contact ID só conhecia o código `1130`. Falta de
  energia, bateria baixa, violação e pânico vindos da central eram descartados.
- **Queda da central invisível**: só era notada quando um comando falhava. Uma central que parava
  de mandar keep-alive continuava "Online".
- **Sem confirmação nem escalonamento**: ninguém confirmava a leitura e o aviso não subia para
  mais ninguém.
- **Debounce perdido no reinício**: o debounce de notificações ficava em memória (dívida 41).
- **Conectividade fora do histórico**: queda e retorno de comunicação não apareciam (dívida 24).

## Decisão

1. **Catálogo Contact ID por categoria.**
   - Cada código ganhou `Categoria` (Disparo, Pânico, Violação, Falha de energia, Bateria baixa,
     Restauração), `UsaZona` e `PendenteHomologacao`.
   - Os códigos novos vêm do padrão público SIA DC-05 (decisão A do usuário). Ficam marcados como
     pendentes até o teste numa central JFL física; só o `1130` tem fonte própria.
   - Destino de cada categoria:
     - disparo, pânico e violação viram **alerta crítico**;
     - energia e bateria viram notificação comum;
     - restauração fica só no histórico.
   - A restauração de zona de intrusão nem gera Ocorrência: aconteceria a cada vez que um sensor
     volta ao repouso.
2. **Alerta crítico (`AlertasCriticos`).**
   - O alerta fica aberto até alguém tocar em "Estou ciente", que pode ser o dono ou um membro
     ativo.
   - Há um alerta aberto por propriedade e tipo: disparos repetidos se juntam ao alerta já aberto.
   - O push é crítico: canal Android próprio (`alertas_criticos`), som de alarme, importância
     máxima e pedido para ignorar o "Não perturbe". Ele ignora as preferências de canal, porque
     desligar "alertas" nunca pode silenciar um pânico.
   - O app aberto recebe o alerta pelo SignalR (`AlertaCritico`) e mostra uma tela cheia.
3. **Escalonamento.**
   - Um `BackgroundService` roda a cada 30 s. Sem confirmação dentro de
     `Propriedade.MinutosEscalonamento` (padrão 2, faixa 1–30), o aviso sobe: Dono → Membros
     (push) → Contatos de emergência (SMS).
   - Nível vazio é pulado. Depois do último nível o escalonamento para; o alerta continua aberto.
   - O avanço é uma atualização condicional (`UPDATE … WHERE nivel = esperado AND não confirmado`):
     duas execuções ou uma confirmação no meio nunca enviam em dobro.
4. **SMS atrás de uma porta (`IEnvioSms`, decisão B).**
   - A única implementação hoje é `EnvioSmsSimulado`, que registra no log com o telefone
     mascarado.
   - Ligar um provedor real (Zenvia, Twilio) é registrar outra implementação.
   - Limite de 5 contatos por propriedade, para o SMS não virar disparo em massa.
5. **Vigia de conectividade periódico, não reação ao evento.**
   - `VigiaConectividadeJfl` roda a cada 30 s. Central sem sessão, ou sem atividade há mais de
     3× o keep-alive, vira Offline, ganha um `EventosConectividade` "Perdida" e abre um alerta
     "Central sem comunicação".
   - Quando volta, registra "Restabelecida" e o alerta para de escalonar.
   - Não reage a `SessionManager.SessaoRemovida`: numa reconexão, a sessão antiga é removida um
     instante antes de a nova ser registrada. Reagir ao evento daria alarme falso a cada
     reconexão.
   - Há também uma carência de 3× o keep-alive após o servidor subir, e equipamento em manutenção
     ou inativo nunca alerta.
6. **Debounce persistido** em `ControlesEnvioNotificacao` (fecha a dívida 41). A chave é o
   alerta, quando houver: dois alertas diferentes nunca se suprimem.

## Alternativas consideradas

- **Fila e worker dedicados para escalonamento.** Recusada pela regra de simplicidade dos MVPs: um
  timer dentro da Api basta enquanto há uma instância, e a atualização condicional já cobre duas.
- **Tela cheia sobre a tela bloqueada (`USE_FULL_SCREEN_INTENT`).** Fica fora: o Android 14+
  restringe a apps de despertador e chamada. Usamos notificação de prioridade máxima com som de
  alarme.
- **WhatsApp ou só ligação manual para contatos.** O usuário escolheu SMS. A tela de alerta também
  oferece "Ligar para…" e "Ligar 190".

## Consequências

- **Novos no banco:** 4 tabelas e 1 coluna, todas aditivas (detalhe em
  [ALTERACOES_BANCO.md](../ALTERACOES_BANCO.md)).
- **Novos endpoints:**
  - `GET /api/properties/{id}/alertas`
  - `GET /api/alertas/{id}`
  - `POST /api/alertas/{id}/confirmar`
  - `POST /api/properties/{id}/panico`
  - `GET|PUT /api/properties/{id}/alertas/configuracao`
  - CRUD de `/api/properties/{id}/contatos-emergencia`
- **Central de Eventos:** ganhou as fontes `AlertasFonteEventos` e `ConectividadeFonteEventos`.
  A gravidade dos eventos da central agora vem da categoria do código.
- **App:**
  - `TelaAlertaCritico`, montada uma vez acima da navegação;
  - botão de pânico (segurar 3 s) no Início;
  - tela "Alertas e emergência";
  - canal crítico com o som `assets/sons/alerta_critico.wav`.
  - O `AlertaDisparo` de demonstração da Sprint 16 foi removido.
- **Ainda pendente:**
  - Push real depende de credenciais Firebase.
  - SMS real depende de um provedor.
  - Os códigos SIA DC-05 dependem de homologação na central JFL.
  - Membros com login próprio chegam na Sprint 25; até lá o nível "Membros" existe e fica vazio.
