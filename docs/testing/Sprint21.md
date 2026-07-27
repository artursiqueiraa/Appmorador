# Homologação — Sprint 21 (RBAC Master — Base de Permissões da Plataforma)

## Build

| Campo | Valor |
|---|---|
| Build ID | `db665545-2164-412d-b1ed-0781947679e8` |
| Link do EAS Build | https://expo.dev/accounts/ostresmosqueteiros/projects/appmorador/builds/db665545-2164-412d-b1ed-0781947679e8 |
| APK Preview | https://expo.dev/artifacts/eas/GEQR6KO0Iri1l-y78EuYDlYa3IE7jqbV6HZwZn3uxmI.apk |
| Versão do app | 1.0.0 (`app.json`) |
| Perfil | `preview` (`eas.json`) |

## Release Notes

```
Release Notes — Sprint 21

Novidades
• Base de permissões da plataforma (RBAC): papéis internos Master/Técnico/Suporte
• Cada propriedade agora sabe o que o usuário pode fazer e o que foi contratado
• Aba Câmeras mostra aviso honesto quando a propriedade não tem câmeras contratadas
• Botões de cadastro (morador/visitante) escondidos quando o usuário não tem permissão
• Painel de Controle escondido quando o usuário não tem permissão de abrir portão
• Cadastro de credencial esconde Facial/Tag quando não permitido

Correções
• Nenhuma (Sprint de infraestrutura de permissões, sem hotfix)

Observação técnica
• Esta Sprint não altera nenhum fluxo já existente para o Administrador (dono da
  propriedade) — todas as permissões do Plano Básico continuam concedidas automaticamente,
  então nenhuma tela deveria sumir/mudar de comportamento na prática.
```

## Checklist de Validação Manual

### 1. Itens específicos desta Sprint
- [ ] Login funciona normalmente (conta Administrador/dona da propriedade)
- [ ] "Suas propriedades" carrega normalmente, sem erro novo
- [ ] Aba Moradores: botão "Adicionar morador" continua visível (Plano Básico concede
      `CadastrarMorador` automaticamente)
- [ ] Aba Visitantes: botão "Adicionar visitante" continua visível (`CriarVisitante` concedido)
- [ ] Aba Acessos: Painel de Controle (comandos PGM) continua visível (`AbrirPortao` concedido)
- [ ] Aba Câmeras: se a propriedade de teste tiver `FeatureFlag.Cameras` ativo, lista carrega
      normalmente; se não tiver, mostra "Câmeras não contratadas" (não trava, não crasha) — **a
      aba continua sempre visível e clicável, nunca some do menu inferior**
- [ ] Cadastro de credencial (dentro de um morador): chips "Facial" e "Tag RFID" continuam
      visíveis (Plano Básico concede `CadastrarFacial`/`CadastrarTag`)
- [ ] Nenhuma tela nova quebrada, nenhum botão sem ação, nenhum crash relacionado a permissões

### 2. Testes de Regressão Obrigatórios (todas as Sprints)
- [ ] Login funciona
- [ ] Logout funciona
- [ ] Troca de propriedade funciona
- [ ] Dashboard carrega (HeroCard, Snapshot, Timeline)
- [ ] Aba Acessos funciona (Painel de Controle, Moradores, Visitantes)
- [ ] Aba Câmeras funciona (lista/detalhe, mesmo comportamento da Sprint 20)
- [ ] Push (Sprint 19): permissão, token, deep link
- [ ] SignalR conecta e reconecta
- [ ] Perfil/Ajustes acessíveis
- [ ] Nenhuma tela quebrada
- [ ] Nenhum botão sem ação
- [ ] Nenhum texto cortado
- [ ] Nenhum overlay estranho
- [ ] Nenhum crash

### 3. Itens históricos conhecidos
- [ ] Safe Area respeitada
- [ ] Modo escuro funcional
- [ ] Fonte aumentada do sistema
- [ ] Orientação retrato
- [ ] Performance de navegação entre abas

## Evidências

- [ ] Screenshot: aba Câmeras com "Câmeras não contratadas" (propriedade sem a feature)
- [ ] Screenshot: aba Câmeras normal (propriedade com a feature — comportamento inalterado da
      Sprint 20)
- [ ] Lista de bugs encontrados durante a validação
- [ ] Bugs corrigidos antes do encerramento

## Nota sobre o que esta Sprint muda visualmente (e o que não muda)

Como toda propriedade — nova ou pré-existente — recebeu automaticamente as 6 permissões do "Plano
Básico" (`CadastrarMorador, CadastrarFacial, CadastrarTag, AbrirPortao, VerCameras,
CriarVisitante`), **nenhuma tela deveria mudar de comportamento na prática** para o Administrador
comum ao testar esta Sprint — a única mudança visível esperada é a aba Câmeras mostrando o aviso
"Câmeras não contratadas" para propriedades sem `FeatureFlag.Cameras` ativo (nenhuma propriedade
tem essa feature ativa por padrão hoje — ela precisa ser ligada manualmente via API por um
Técnico/Master, não existe ainda uma tela para isso). Isso é o comportamento correto e esperado,
não um bug.

## Resultado da Homologação

| Campo | Valor |
|---|---|
| Resultado | ☐ Aprovada / ☐ Reprovada — **pendente, aguardando execução pelo usuário** |
| Quem homologou | — |
| Data da homologação | — |
| APK utilizado | https://expo.dev/artifacts/eas/GEQR6KO0Iri1l-y78EuYDlYa3IE7jqbV6HZwZn3uxmI.apk |
| Build ID | `db665545-2164-412d-b1ed-0781947679e8` |
| Tempo gasto | — |

### Se Reprovada

| Severidade | Bugs Encontrados | Ações Corretivas |
|---|---|---|
| Blocker | — | — |
| High | — | — |
| Medium | — | — |
| Low | — | — |
