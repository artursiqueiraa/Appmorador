# Homologação — Sprint 25 (Contas e multiusuário)

APK: build `3f56ed76-d75b-44d1-8c5c-2107831d69b3` — https://expo.dev/artifacts/eas/Axlb10dw3OQ53LnZuDBx-PzabhSjWcjzQWJNmr7Qw-E.apk (sem Firebase)

## Validado pelo desenvolvimento (2026-11-08) — API real, migration aplicada

| # | Cenário | Resultado |
|---|---|---|
| 1 | Sem convite, outra conta não vê a propriedade | OK (404) |
| 2 | Dono convida como Membro; tela pública mostra quem convidou, propriedade e papel | OK |
| 3 | Aceitar; mesmo link de novo é recusado (uso único) | OK |
| 4 | Membro vê a propriedade com permissões AbrirPortao/CriarVisitante/VerCameras | OK |
| 5 | Membro abre portão; não cadastra portão; não vê Pessoas | OK |
| 6 | Dono muda para Visualizador: vê, mas não abre | OK |
| 7 | Dono remove: o mesmo token perde acesso na hora; propriedade some da lista | OK |
| 8 | Dono continua com acesso total | OK |
| 9 | Tela "Pessoas com acesso" no navegador | OK |

## Checklist para o usuário

| # | Verificação | Resultado |
|---|---|---|
| 1 | Convidar alguém pelo app e compartilhar o link no WhatsApp | |
| 2 | Abrir o link, criar conta e aceitar | |
| 3 | A pessoa convidada (Membro) arma/abre portão e não vê "Pessoas com acesso" | |
| 4 | Remover e conferir que ela perde o acesso | |
| 5 | Minha conta: trocar senha e excluir uma conta de teste | |

## Resultado

_(pendente — APK)_
