# Homologação — Sprint 24.5 (Portões e portas)

## Ambiente

| Campo | Valor |
|---|---|
| Migration | `Sprint24_5PortoesEPortas`, aplicada em 2026-11-08 (resumo aprovado pelo usuário) |
| Teste | Navegador (web) e emulador Android (development build); simulador Control iD em `localhost:9500` |
| APK | build `b22e4160-e0f8-4c88-838a-63d8929a477e` — https://expo.dev/artifacts/eas/b3Df2D0Z8ZnOlpYu2nU_kjnRW75PklRQQy76GC0GUDM.apk (sem Firebase; inclui Sprint 24 rodada 3) |

## Validado pelo desenvolvimento (2026-11-08)

| # | Verificação | Resultado |
|---|---|---|
| 1 | Leitor Control iD cadastrado aparece em "O que abre esta porta?" | OK (API `acionadores`) |
| 2 | Ligar "Recepção" ao leitor, porta 1 | OK — lista mostra "Leitor facial da recepção (simulador) · porta 1" |
| 3 | Tocar no acesso abre a tela com câmera/Abrir/Agora não | OK (web) |
| 4 | Abrir envia o comando ao leitor | OK — simulador recebeu `{"action":"door","parameters":"door=1"}` |
| 5 | Abertura registrada | OK — `AberturasPontoAcesso` (sucesso, usuário, horário) |
| 6 | Início lista os portões que abrem pelo app | OK — "Recepção · Toque para ver e abrir" |
| 7 | Validações: equipamento de outra propriedade, saída ausente, quem não é dono | OK (testes automatizados) |
| 8 | Emulador Android: Início → Portões → "Portão principal" → Abrir | OK — "comando de abrir enviado", simulador recebeu o comando |

## Checklist para o usuário

| # | Verificação | Resultado |
|---|---|---|
| 1 | Em uma propriedade com câmera, ligar o portão à câmera e ver a imagem antes de abrir | |
| 2 | "Atualizar imagem" traz uma captura nova | |
| 3 | "Agora não" volta sem abrir nada | |
| 4 | Portão ligado à central JFL (PGM) abre com a central conectada | |
| 5 | Equipamento sem comunicação: aviso claro, sem travar a tela | |

## Regressão obrigatória

R1–R7 (login, logout, troca de propriedade, Início, Acessos/Moradores, push, tempo real) — no
próximo APK.

## Resultado

_(pendente)_
