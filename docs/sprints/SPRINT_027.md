# Sprint 27 — Segurança da conta e confiabilidade

> Fase 1 (planejamento, sem código) — 2026-11-08. Origem: [Plano de Ação de Produto](../roadmap/PLANO_ACAO_PRODUTO.md).

## 1. Objetivo

Comandos sensíveis protegidos e plataforma confiável: desarmar e abrir portão pedem confirmação,
todo comando fica registrado com autor, o banco tem backup diário testado e o sistema avisa quando
algo interno cai.

## 2. Levantamento (estado real)

| Ponto | Situação |
|---|---|
| Comandos JFL (armar/desarmar/inibir) | `JflComandoServico.ExecutarComandoAsync` executa e notifica. **Não registra quem fez**, sem idempotência. Intelbras tem o mesmo padrão. |
| Abrir portão/porta | `AberturaPontoAcesso` (Sprint 24.5) já registra quem, quando e resultado — **só para aberturas**. |
| ADR 0033 | Plataforma de comandos aprovada como especificação (capacidades, retry por tipo, idempotência, auditoria). **Nada implementado.** |
| Confirmação | Desarmar e abrir são um toque, sem PIN nem biometria. |
| Desbloqueio do app | Não existe; `expo-local-authentication` não está instalado. |
| Sessão web | Access e refresh token no `localStorage` (dívida 51, prioridade alta antes de publicar). |
| Backup | `scripts/backup_database.ps1` e `restore_database.ps1` existem, só manuais (dívida 37). |
| Health checks | Nenhum endpoint de saúde; queda do listener JFL ou dos serviços em segundo plano não avisa ninguém. |

## 3. Escopo

### Backend
1. **Auditoria de comandos** — tabela única `ComandosExecutados`, alimentada por armar, desarmar,
   inibir/desinibir zona, PGM, abrir portão (JFL, Intelbras, Control iD, Hikvision):
   quem, propriedade, equipamento/ponto, tipo, como confirmou, resultado, mensagem, quando.
   Base dos relatórios da Sprint 28. `AberturaPontoAcesso` mantém o histórico antigo (copiado para a
   tabela nova na migration) e deixa de receber registros.
2. **Idempotência** — cabeçalho `Idempotency-Key` nos endpoints de comando: repetição com a mesma
   chave em 24 h devolve o resultado já registrado, sem reenviar ao equipamento (toque duplo, rede
   que reenvia). Sem retry automático de comando destrutivo (ADR 0033 §retry).
3. **PIN de confirmação** — cada usuário define um PIN de 4 a 6 dígitos (hash BCrypt). Desarmar e
   abrir exigem o PIN no corpo da requisição; 5 erros bloqueiam a confirmação por 15 min. Armar e
   consultar não pedem.
4. **Sessão web segura (fecha dívida 51)** — no web, o refresh token vai para cookie
   `HttpOnly; Secure; SameSite=Strict` e o access token fica só em memória. O celular continua com o
   armazenamento seguro do aparelho.
5. **Health checks** — `/health` (banco, listener JFL, fila de eventos, serviços em segundo plano com
   batimento) e um vigia interno que gera aviso quando algo fica fora por mais de 2 min.
6. **Backup automático (fecha dívida 37)** — backup diário com retenção e teste de restauração
   automático num banco temporário, com resultado registrado.

### App
1. **Confirmar com PIN ou biometria** ao desarmar e abrir portão/porta (folha de confirmação única,
   reaproveitada nas duas telas). No celular, a digital/rosto do aparelho libera o PIN guardado no
   armazenamento seguro — o servidor valida sempre o PIN.
2. **Ajustes → Segurança**: criar/trocar PIN, ligar "Usar digital/rosto", ligar "Bloquear o app"
   (pede biometria ao abrir e ao voltar após 5 min em segundo plano).
3. Sem PIN cadastrado, a primeira tentativa de desarmar convida a criar um (sem travar o uso).

### Painel admin
- Card de **saúde do sistema** (componentes, último backup e último teste de restauração).

### Fora do escopo
- Relatórios de abertura/fechamento (Sprint 28 — usa a auditoria desta Sprint).
- Capacidades declarativas e catálogo de comandos completo da ADR 0033 (só idempotência e auditoria).
- 2FA no login.

## 4. Decisões do usuário (2026-11-08)

- **A → PIN validado no servidor**; a biometria do celular só libera o PIN guardado.
- **B → Agendador de Tarefas do Windows** (diário às 3h, 7 diários + 4 semanais, teste de restauração).
- **C → painel + e-mail ao Master** (Resend, quando a chave existir).

### Opções apresentadas

- **A. Como a confirmação é validada**
  - *Recomendado*: PIN validado no servidor; a biometria do celular só libera o PIN guardado.
    Funciona no web e não dá para pular chamando a API direto.
  - Alternativa: biometria só local no celular, sem PIN (mais simples, mas o web fica sem
    confirmação e a API aceita sem prova).
- **B. Onde roda o backup**
  - *Recomendado*: Agendador de Tarefas do Windows chamando o script (diário às 3h, guarda 7 diários +
    4 semanais) + script de teste de restauração. Fica fora do app, simples, troca por cron no Linux.
  - Alternativa: rotina dentro do backend (exige `mysqldump` no servidor da API).
- **C. Quem recebe o aviso de queda**
  - *Recomendado*: painel admin (card + destaque) e e-mail ao Master pelo Resend quando a chave existir.
  - Alternativa: só painel.

## 5. Modelo de dados (migration aditiva)

| Tabela | Campos |
|---|---|
| `ComandosExecutados` (nova) | Id, PropriedadeId, UsuarioId, EquipamentoId?, PontoAcessoId?, Tipo, Parametros?, Confirmacao (Nenhuma/Pin/Biometria), Sucesso, Mensagem?, ChaveIdempotencia?, OcorridoEmUtc |
| `Usuarios` (colunas novas) | PinConfirmacaoHash?, TentativasPinFalhas, PinBloqueadoAteUtc? |
| `RegistrosBackup` (nova) | Id, Tipo (Backup/TesteRestauracao), Sucesso, Arquivo?, TamanhoBytes?, Mensagem?, OcorridoEmUtc |
| `AberturasPontoAcesso` | sem mudança de estrutura; linhas copiadas para `ComandosExecutados` |

## 6. Ordem de implementação

1. Auditoria + idempotência nos comandos existentes.
2. PIN (backend) + folha de confirmação (app) no desarmar e no abrir.
3. Biometria do aparelho e bloqueio do app (`expo-local-authentication` — exige novo dev build do emulador).
4. Sessão web em cookie HttpOnly.
5. Health checks + vigia + card no painel.
6. Backup agendado + teste de restauração.
7. ADR 0043, documentação, emulador → APK → homologação, Reviewer.

## 7. Riscos

| Risco | Mitigação |
|---|---|
| Esquecer o PIN trava o desarmar | "Esqueci o PIN" redefine com a senha da conta. |
| Cookie entre portas diferentes (8081 → 5027) | Mesma origem de site em localhost; CORS com credenciais só para as origens configuradas. |
| Mudar o login quebra o celular | Fluxo atual do celular intocado; cookie só quando o cliente é web. |
| Backup que não restaura | Teste de restauração automático após cada backup, com resultado no painel. |

## 8. Critérios de aceite

1. Desarmar e abrir portão pedem PIN (ou biometria no celular); PIN errado 5× bloqueia por 15 min.
2. Cada comando aparece em `ComandosExecutados` com autor e forma de confirmação.
3. Toque duplo com a mesma chave não envia o comando duas vezes.
4. No web, nenhum token no `localStorage`.
5. `/health` mostra o estado; derrubar o listener JFL gera aviso no painel.
6. Backup diário gerado e restaurado com sucesso num banco de teste.
