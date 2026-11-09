# Homologação — Sprint 27 (Segurança da conta e confiabilidade)

APK: build `a765af77-7e64-45e4-9c74-6bdaf03ae952` — https://expo.dev/artifacts/eas/HqIk-1vVV1BugJXZDgEx1j56JD1rtWsGzjie4LiYjr8.apk (inclui Sprints 24–27; sem Firebase)

## Validado pelo desenvolvimento (2026-11-08) — API real, migration aplicada

| # | Cenário | Resultado |
|---|---|---|
| 1 | Sem PIN criado: abrir acesso passa sem confirmação | OK |
| 2 | Criar PIN com senha errada / PIN óbvio (1234) | Recusados |
| 3 | Com PIN: abrir sem PIN → 428 `PIN_OBRIGATORIO`; PIN errado → `PIN_INCORRETO`, "Restam 4" | OK |
| 4 | PIN certo abre; com biometria registra "Biometria" | OK |
| 5 | Mesma `Idempotency-Key` repetida: mesma resposta, nenhum registro/comando novo | OK |
| 6 | Auditoria com autor e forma de confirmação; 7 aberturas antigas migradas | OK |
| 7 | 5 erros bloqueiam (nem o PIN certo passa); "esqueci o PIN" com a senha desbloqueia | OK |
| 8 | Web: refresh token fora do corpo, cookie HttpOnly + SameSite=Strict + Path=/api/auth; refresh só com cookie; cookie rotacionado não reaproveita; celular segue com token no corpo | OK |
| 9 | `/health` público e mínimo; `/api/painel/saude` só para equipe interna (cliente 403) | OK |
| 10 | Backup + restauração: 51 tabelas, 21 usuários; tarefa agendada rodou sozinha (resultado 0); saúde "Ok" | OK |
| 11 | Navegador: só `currentUser` no localStorage; recarregar mantém a sessão; criar PIN pela tela; folha de PIN no "Abrir" com erro e acerto | OK |

Testes automatizados: backend 255 (21 novos), app 95 (7 novos), painel 62; typecheck e lint limpos.

## Checklist para o usuário (celular)

| # | Verificação | Resultado |
|---|---|---|
| 1 | Menu → "Segurança e PIN": criar PIN com a senha da conta | |
| 2 | Desarmar (aba Alarme) e abrir portão pedem o PIN; PIN errado mostra tentativas restantes | |
| 3 | Ligar "Confirmar com digital/rosto": desarmar/abrir pede a digital em vez do PIN | |
| 4 | Ligar "Bloquear o app": fechar e abrir o app pede a digital; voltar após 5 min também | |
| 5 | Sem PIN (outra conta): primeiro desarmar mostra o convite "Proteja com um PIN" | |
| 6 | "Últimos comandos" mostra quem fez e como confirmou | |
| 7 | Painel admin → Visão geral: card "Saúde do sistema" com último backup | |
| 8 | Regressão: login/logout, troca de propriedade, Dashboard, Acessos, Push, tempo real | |

PIN de teste já criado: Fernanda (`fernanda.oliveira@appmorador.local`) e Administrador (`admin@appmorador.local`): **2580**.

## Resultado

_(pendente — APK)_
