# Homologação — Sprint 24 (Alertas confiáveis)

## Build

| Campo | Valor |
|---|---|
| Build ID | `8b6411dd-dfca-48d0-a772-09babd704def` |
| APK Preview | https://expo.dev/artifacts/eas/szqxl0qZthnQj10QrcunIFBr1a_ysAb1pWcK1KHWrPg.apk |
| Link do EAS Build | https://expo.dev/accounts/ostresmosqueteiros/projects/appmorador/builds/8b6411dd-dfca-48d0-a772-09babd704def |
| Perfil | `preview` (`webapp/eas.json`), API em `http://192.168.100.2:5027` |

## Release Notes

```
Release Notes — Sprint 24

Novidades
• Alerta em tela cheia quando o alarme dispara, alguém aciona o pânico ou a central para de responder
• Botão "Estou ciente": confirma para todos e para de avisar outras pessoas
• Se ninguém confirmar em 2 minutos, avisamos as próximas pessoas — por último, seus contatos de emergência
• Botão de pânico no Início (segure 3 segundos)
• Nova tela "Alertas e emergência" no menu: contatos, tempo de aviso e "Não perturbe"
• Avisos de falta de energia e bateria fraca da central

Observação técnica
• Alteração no banco: 4 tabelas novas e 1 coluna nova, nada removido
• SMS dos contatos em modo simulado até haver provedor contratado
```

## Antes de testar

1. Migration `Sprint24AlertasConfiaveis` aplicada em 2026-11-08 (backend reiniciado; fluxo pânico → escalonamento → SMS simulado → confirmação validado via API).
2. Simulador JFL disponível para disparo, falta de energia e queda de conexão.
3. Sem Firebase configurado, o push não chega ao celular: a homologação valida o alerta pelo tempo
   real (app aberto) e os logs mostram o que seria enviado.

## Checklist da Sprint

| # | Verificação | Resultado |
|---|---|---|
| 1 | Pânico: segurar menos de 3 s não faz nada; segurar 3 s abre a tela de alerta | |
| 2 | Tela de alerta mostra o que aconteceu, horário e situação ("Se ninguém confirmar até…") | |
| 3 | "Estou ciente" fecha a tela; o histórico mostra "Confirmado por …" | |
| 4 | X fecha só a tela; o alerta continua aberto e reaparece ao reabrir o app | |
| 5 | Sem confirmar por 2 min: histórico mostra contatos avisados; log do backend mostra o SMS simulado | |
| 6 | Alertas e emergência: adicionar, reordenar e remover contato; telefone sem DDD é recusado | |
| 7 | Alterar o tempo para 1 min e repetir o item 5 | |
| 8 | Ligar para contato / Ligar 190 abre o discador | |
| 9 | Disparo simulado (1130) abre a tela de alerta com o nome da zona | |
| 10 | Falta de energia simulada (1301) aparece no histórico como Atenção, sem tela cheia | |
| 11 | Desligar o simulador: em até 15 min + 30 s aparece "Central sem comunicação"; religar registra o retorno | |
| 12 | Canal "Alertas críticos" existe nos ajustes de notificação do app | |

## Regressão obrigatória

| # | Fluxo | Resultado |
|---|---|---|
| R1 | Login | |
| R2 | Logout | |
| R3 | Troca de propriedade (alerta da propriedade anterior não aparece) | |
| R4 | Início (Dashboard) | |
| R5 | Acessos / Moradores | |
| R6 | Push: permissão pedida e dispositivo registrado | |
| R7 | Tempo real (SignalR) | |

## Resultado

### Rodada 1 — build `8b6411dd` (2026-11-08)

Reprovada (ajustes antes de seguir).

| Severidade | Achado | Ação |
|---|---|---|
| High | Botões do fim das telas (Portões, Visitantes, formulários) atrás da barra de navegação do Android | `ColunaConteudo` reserva também o espaço de baixo (as abas continuam usando a própria barra); 12 formulários embaixo de lista passaram a rolar e subir com o teclado (`FormularioRolavel`) |
| Medium | Não havia como gerar alerta de teste sem central física | Simulador de eventos no painel admin (cliente → propriedade → "Simular evento"), só Master, marcado "Teste:" e auditado |
| — | "Portões e portas" não mostra o que abre de verdade | Fora do escopo desta Sprint: planejamento em [SPRINT_024_5.md](../sprints/SPRINT_024_5.md) aguardando aprovação |

### Rodada 2 — build `eaff9280-191e-4514-95a3-356e65124901`

APK: https://expo.dev/artifacts/eas/rBzQPGKFqbnmPZtey1Z3WFbC0s2njwn2IFTNaYFklN0.apk

**Aprovada** pelo usuário em 2026-11-08: alerta de pânico simulado pelo painel apareceu no celular
com o app aberto; telas sem corte embaixo.

| Severidade | Achado | Ação |
|---|---|---|
| High | Horários 3 h adiantados no app inteiro (API devolvia datas sem "Z") | Corrigido: datas `...Utc` voltam do banco como UTC (`AppDbContext.MarcarDatasUtc`) |
| High | Depois de o servidor reiniciar, o tempo real desistia e o alerta demorava | Corrigido: reconexão automática a cada 30 s e ao voltar ao app; consulta de alertas a cada 15 s sem tempo real (medido no emulador: 141 ms com tempo real) |
| Medium | Alerta crítico sem som com o app aberto | Corrigido: som de alarme (canal crítico) + vibração enquanto a tela de alerta estiver aberta; validado no emulador |
| Pendente | Nada chega com o app fechado | Depende do Firebase (credenciais do usuário) — dívida 65, próximo APK |

As correções acima foram validadas no emulador (development build); chegam ao celular no próximo
APK, junto com o Firebase.

