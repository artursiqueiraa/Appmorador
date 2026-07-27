# Homologação — Sprint 20 (Visualização de Câmeras)

## Build

**Build 1** (`cefa1886`) foi substituído pelo Build 2 abaixo — durante a tentativa de homologação
com o Build 1, o usuário reportou "servidor indisponível" (IP da máquina havia mudado de
`192.168.100.5` para `192.168.100.2` desde o build anterior — só descoberto/corrigível
rebuildando, já que `EXPO_PUBLIC_API_URL` é valor de build-time) e um bug de layout ("Olá, Carlos"
sobreposto à barra de status por falta de `paddingTop` com o inset seguro em
`SelecionarPropriedadeScreen`). Os 2 problemas foram corrigidos e um novo build gerado — ver
"Problemas encontrados/corrigidos" abaixo.

| Campo | Valor |
|---|---|
| Build ID | `21aa743b-f91b-439b-b177-564167a938eb` |
| Link do EAS Build | https://expo.dev/accounts/ostresmosqueteiros/projects/appmorador/builds/21aa743b-f91b-439b-b177-564167a938eb |
| APK Preview | https://expo.dev/artifacts/eas/YHkNNLwH4RfFIq365_OBvJxVUqPccDSEqpn3Rf0UMGw.apk |
| Versão do app | 1.0.0 (`app.json`) |
| Perfil | `preview` (`eas.json`) |

## Release Notes

```
Release Notes — Sprint 20

Novidades
• Aba Câmeras funcional (lista real, status online/offline, miniatura)
• Tela de detalhe com imagem ampliada
• Botão "Atualizar imagem" (captura sob demanda)
• Cache de imagens (expo-image)
• Pull-to-Refresh na lista de câmeras
• Status online/offline em tempo real (SignalR)

Correções
• Nenhuma (Sprint de funcionalidade nova, sem hotfix)

Performance
• Cache de imagem em disco (expo-image, cachePolicy="disk") — segunda visualização
  da mesma câmera é instantânea
```

## Checklist de Validação Manual

### 1. Itens específicos desta Sprint
- [ ] Aba Câmeras carrega lista
- [ ] Câmeras exibem nome, imagem, status (comparar com as 3 câmeras do seed: Entrada/Sala/Fundos)
- [ ] Toque abre detalhe com imagem ampliada
- [ ] Câmera offline (Sala) mostra status correto ("Sem imagem" na lista, "Offline — nenhuma
      imagem disponível ainda" no detalhe)
- [ ] Botão "Atualizar imagem" funciona (loading visível, timeout/mensagem amigável se falhar —
      esperado falhar, já que o gravador de exemplo (`192.168.1.100`) não existe de verdade)
- [ ] Pull-to-refresh atualiza a lista
- [ ] Skeleton loading visível durante o carregamento
- [ ] Cache de imagens funciona (sair e voltar para a mesma câmera exibe a imagem instantaneamente)
- [ ] SignalR atualiza status em tempo real (difícil de gerar manualmente sem um gravador real —
      verificar ao menos que a conexão SignalR permanece estável com a aba Câmeras aberta)

### 2. Testes de Regressão Obrigatórios (todas as Sprints)
- [ ] Login funciona
- [ ] Logout funciona
- [ ] Troca de propriedade funciona
- [ ] Dashboard carrega (HeroCard, Snapshot, Timeline)
- [ ] Aba Acessos funciona (Painel de Controle, Moradores, Visitantes)
- [ ] Push (Sprint 19): permissão, token, deep link
- [ ] SignalR conecta e reconecta
- [ ] Perfil/Ajustes acessíveis
- [ ] Nenhuma tela quebrada
- [ ] Nenhum botão sem ação
- [ ] Nenhum texto cortado
- [ ] Nenhum overlay estranho
- [ ] Nenhum crash

### 3. Itens históricos conhecidos
- [ ] Safe Area respeitada (especialmente na tela de detalhe da câmera)
- [ ] Modo escuro funcional
- [ ] Fonte aumentada do sistema
- [ ] Orientação retrato
- [ ] Performance de navegação entre abas

## Evidências

- [ ] Screenshot inicial (antes da Sprint) — Empty State genérico da aba Câmeras
- [ ] Screenshot final (depois da Sprint) — grid de câmeras + detalhe
- [ ] Vídeo curto da navegação (lista → detalhe → atualizar imagem → voltar)
- [ ] Lista de bugs encontrados durante a validação
- [ ] Bugs corrigidos antes do encerramento

## Problemas encontrados durante a 1ª tentativa (Build 1, `cefa1886`) — corrigidos antes do Build 2

| Severidade | Bug | Correção |
|---|---|---|
| Blocker | "Suas propriedades" não carregava ("servidor temporariamente indisponível") — IP da máquina mudou de `192.168.100.5` para `192.168.100.2` (DHCP) desde o build anterior; backend seguia corretamente em `0.0.0.0:5027`, só o valor de build-time (`EXPO_PUBLIC_API_URL`) estava desatualizado | `mobile/eas.json` atualizado para o IP atual + novo build gerado |
| Medium | Saudação "Olá, {nome}" sobreposta à barra de status em `SelecionarPropriedadeScreen` — o container usava `padding` fixo em vez de respeitar o inset seguro superior (só o inferior havia sido corrigido na Sprint 18.1) | `paddingTop: insets.top + spacing.md` adicionado, mesmo padrão já usado para o inset inferior |

## Resultado da Homologação

| Campo | Valor |
|---|---|
| Resultado | ☐ Aprovada / ☐ Reprovada — **pendente, aguardando execução pelo usuário no Build 2** |
| Quem homologou | — |
| Data da homologação | — |
| APK utilizado | https://expo.dev/artifacts/eas/YHkNNLwH4RfFIq365_OBvJxVUqPccDSEqpn3Rf0UMGw.apk |
| Build ID | `21aa743b-f91b-439b-b177-564167a938eb` |
| Tempo gasto | — |

### Se Reprovada

| Severidade | Bugs Encontrados | Ações Corretivas |
|---|---|---|
| Blocker | — | — |
| High | — | — |
| Medium | — | — |
| Low | — | — |

## Nota sobre o gravador de exemplo do seed

As câmeras "Entrada" e "Fundos" já nascem com uma imagem de exemplo (gerada em memória, ver ADR
0024 Decisão 8) — a lista e o detalhe devem funcionar normalmente para elas. O botão "Atualizar
imagem" **vai falhar de propósito** para as 3 câmeras (o gravador de exemplo, IP `192.168.1.100`,
não existe de verdade nesta rede) — isso é esperado e é exatamente o caso que testa o caminho de
falha amigável (mensagem "A câmera demorou demais para responder"/"Não foi possível obter uma
imagem da câmera agora", nunca um erro técnico ou crash).
