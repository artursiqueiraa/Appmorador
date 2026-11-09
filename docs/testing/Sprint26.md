# Homologação — Sprint 26 (Facial ponta a ponta + LGPD)

APK: build `c98492b6-99e7-4c27-a1c2-0c90130dde9e` — https://expo.dev/artifacts/eas/SaB6vJn1jvOJXMVdTHkV_mG0yx6Rn-PckCNrR7YDcac.apk (inclui Sprints 24–26; sem Firebase)

## Validado pelo desenvolvimento (2026-11-08) — API real, migration aplicada, simuladores

| # | Cenário | Resultado |
|---|---|---|
| 1 | Foto sem consentimento | Recusada: "Registre o consentimento da pessoa antes de cadastrar o rosto." |
| 2 | Consentimento (dependente, responsável legal) | Registrado, versão 2026-11-08 |
| 3 | Foto com consentimento | Aceita; enviada ao simulador Control iD e ao Hikvision (`UserInfo/Record` + `FaceDataRecord`) |
| 4 | Leitor configurado mas fora do ar | Situação "Falhou", nova tentativa a cada minuto |
| 5 | Equipamento sem endereço/senha ou sem capacidade Face | Ignorado (não entra na fila) |
| 6 | Portão ligado a terminal Hikvision | Abriu (`RemoteControl/door/1 open`) |
| 7 | Revogar consentimento | Foto apagada do servidor (404) e dos leitores (`FDSearch/Delete`, `UserInfo/Delete`) |

## Checklist para o usuário

| # | Verificação | Resultado |
|---|---|---|
| 1 | Cadastrar rosto: aparece o termo antes da câmera; "Concordo" libera a câmera | |
| 2 | Dependente: só libera com "Sou o responsável legal" | |
| 3 | Pessoa com rosto mostra "Enviado ao …" para cada leitor | |
| 4 | "Revogar consentimento" apaga a foto | |
| 5 | Menu → "Privacidade e termos" abre a Política; Termos acessíveis | |
| 6 | Homologação física: Control iD e Hikvision reais recebem e apagam o rosto; terminal Hikvision abre a porta | |

## Resultado

_(pendente — APK)_
