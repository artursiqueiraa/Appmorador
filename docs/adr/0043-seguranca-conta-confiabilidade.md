# ADR 0043 — Segurança da conta e confiabilidade

**Data**: 2026-11-08

## Contexto

Comandos que deixam a casa exposta (desarmar o alarme, acionar uma saída, abrir um portão) eram um
toque só: quem estivesse com o celular desbloqueado fazia tudo. Ninguém sabia quem tinha desarmado
o alarme: só as aberturas de portão eram registradas, desde a Sprint 24.5. Um toque duplo, ou uma
rede que reenvia a requisição, mandava o comando duas vezes.

A ADR 0033 (plataforma de comandos) estava aprovada só como especificação. No app web, os tokens de
sessão ficavam no `localStorage` (dívida 51). O backup do banco só rodava à mão (dívida 37). Uma
queda interna, como o listener JFL ou uma rotina em segundo plano, não avisava ninguém.

## Decisão

1. **Uma porta única para os comandos** (`ExecucaoComandoServico`). Armar, desarmar, PGM, inibir zona
   (JFL), armar/desarmar Intelbras e abrir acesso passam todos por ela. Ela:
   - confere o acesso (`NivelAcesso.Operar`);
   - exige a confirmação nos comandos sensíveis;
   - garante a idempotência;
   - executa;
   - registra em `ComandosExecutados`: autor, propriedade, equipamento ou ponto, tipo, parâmetro,
     forma de confirmação, se o equipamento obedeceu e a mensagem.

   É a parte da ADR 0033 que esta Sprint precisa. As capacidades declarativas e o catálogo completo
   ficam para depois. `AberturasPontoAcesso` foi copiada para a tabela nova e deixou de receber
   registros.
2. **Idempotência.** O app manda `Idempotency-Key` em cada toque. Antes de executar, a chave é
   reservada com um índice único (Usuário, Chave). Repetir a mesma chave em até 24 h devolve a
   resposta guardada sem falar com o equipamento. Se a primeira requisição ainda estiver em curso,
   a repetição recebe 409. Não há retry automático de comando destrutivo, como manda a ADR 0033.
3. **PIN validado no servidor** (decisão A).
   - Quem pede: desarmar, acionar PGM e abrir acesso. Armar não pede.
   - Formato: 4 a 6 dígitos, guardado como hash BCrypt. Repetidos e sequências são recusados.
   - Bloqueio: 5 erros seguidos bloqueiam por 15 minutos. Criar, trocar ou redefinir ("esqueci o
     PIN") exige a senha da conta e tira o bloqueio.
   - Quem ainda não criou PIN passa sem confirmação. O app convida a criar um na primeira vez, sem
     travar o uso.
   - Transporte: cabeçalhos `X-Confirmacao-Pin` e `X-Confirmacao-Biometria`. A falta ou o erro de
     confirmação responde 428, com `codigo` PIN_OBRIGATORIO, PIN_INCORRETO ou PIN_BLOQUEADO.
4. **Digital ou rosto do celular** (`expo-local-authentication`).
   - A biometria nunca vai ao servidor: ela só libera o PIN guardado no SecureStore, e o servidor
     confere o PIN como sempre.
   - Para ligar, o PIN é conferido antes (`/api/minha-conta/pin/verificar`).
   - "Bloquear o app" é opcional. Ele pede a biometria ao abrir o app e ao voltar depois de 5 minutos
     em segundo plano, e fica abaixo da tela de alerta crítico: um disparo continua visível.
5. **Sessão web em cookie HttpOnly** (fecha a dívida 51).
   - Com `X-Cliente: web`, o refresh token vai num cookie `HttpOnly; Secure; SameSite=Strict;
     Path=/api/auth` e sai vazio do corpo da resposta. O access token fica só em memória e é renovado
     pelo cookie ao recarregar a página.
   - O CORS passou a permitir credenciais, mas só das origens configuradas.
   - O logout deixou de exigir o access token, porque a sessão é provada pelo refresh token.
   - O celular não mudou.
6. **Saúde do sistema.** Os componentes verificados são:
   - o banco;
   - o listener JFL;
   - as rotinas em segundo plano, pelo batimento de cada rodada;
   - o backup: o último com sucesso precisa ter menos de 26 h, e o último teste de restauração não
     pode ter falhado.

   Para consultar:
   - `/health` é público e mínimo: 503 só quando algo está fora;
   - `/api/painel/saude` traz o detalhe, para a equipe interna;
   - o painel admin mostra um card de saúde.

   Um vigia roda a cada minuto. Se algo fica fora por mais de 2 minutos, ele manda e-mail aos Master
   (Resend) e avisa de novo quando volta (decisão C).
7. **Backup agendado** (decisão B, fecha a dívida 37).
   - `scripts/backup_agendado.ps1`, chamado pelo Agendador de Tarefas do Windows às 3h
     (`scripts/agendar_backup.ps1`), gera o dump compactado.
   - Guarda 7 diários e 4 semanais.
   - Depois de cada backup, restaura em `appmorador_restore_teste` e confere o número de tabelas e de
     usuários.
   - O resultado vai para `RegistrosBackup`.
   - A senha fica protegida por DPAPI, legível só pelo usuário do Windows.

## Consequências

- A Sprint 28 (relatórios de abertura e fechamento) já tem a base: `ComandosExecutados`, com autor e
  horário.
- A queda do **próprio backend** não tem como ser avisada de dentro dele. Isso fica para um monitor
  externo chamando `/health` (dívida 69).
- O painel admin ainda guarda os tokens no `localStorage` (dívida 68).
- O cookie `Secure` funciona em `http://localhost`. Fora dele, o app web precisa de HTTPS, o que já
  era pré-requisito de produção.
- Tocar no que é nativo (`expo-local-authentication`) exigiu recompilar o app de desenvolvimento do
  emulador e gerar um APK novo.
