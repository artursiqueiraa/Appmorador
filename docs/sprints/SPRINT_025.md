# Sprint 25 — Contas e multiusuário

> Fase 1 (planejamento, sem código) — 2026-11-08. Origem: [Plano de Ação de Produto](../roadmap/PLANO_ACAO_PRODUTO.md).

## 1. Objetivo

O dono divide o acesso da propriedade com quem precisa (família, funcionário, zelador), cada um
com o que pode fazer, e tira o acesso na hora quando quiser.

## 2. Levantamento (estado real)

| Ponto | Situação |
|---|---|
| Checagem de acesso | **42 checagens `ProprietarioId != usuario` em 19 serviços**. Qualquer pessoa que não seja o dono recebe "não encontrado" em todas as telas, mesmo com vínculo. Os alertas (Sprint 24) já aceitam membro; o resto não. |
| Vínculos | `UsuariosPropriedade` (Sprint 21) existe, com `Perfil` = Administrador ou Morador e `Ativo`. Só o vínculo do dono é criado. |
| Permissões | `UsuariosPropriedadePermissao` + `PermissaoFuncionalidade` (ex.: `AbrirPortao`) já existem e o app já as lê (`usePermissao`). |
| Conta do usuário | Só cadastro, login, refresh e logout. Não há editar dados, trocar senha nem excluir conta (dívida 47). |
| E-mail | Nenhum provedor de e-mail no projeto. |
| Sessão | Access token de 20 min + refresh token. |
| Perfil no app | Morador/técnico é preferência local (dívida 34). |
| Telas sem uso real | Vagas e Entregas aparecem para residência (dívida 58). |

## 3. Escopo

### Backend
1. **Serviço único de acesso** (`IAcessoPropriedadeServico`): "este usuário pode fazer X nesta
   propriedade?". Lê o vínculo ativo e o papel. As 42 checagens passam a usá-lo. Ver tela
   continua liberado para todos os papéis; alterar, só para quem o papel permite.
2. **Papéis prontos** sobre o RBAC existente:

   | Papel | Pode |
   |---|---|
   | Proprietário | tudo, inclusive convidar e remover pessoas |
   | Gerente | tudo, menos gestão de pessoas e excluir a propriedade |
   | Membro | ver, armar/desarmar, abrir portões, confirmar alertas, pânico |
   | Visualizador | só ver (câmeras, histórico, status) e confirmar alertas |

   O vínculo atual `Administrador` vira `Proprietario`; `Morador` vira `Membro`.
3. **Convite** (Decisão A): o dono gera um convite com papel e validade (48 h, uso único). Quem
   recebe cria a conta (ou entra na existente) e aceita; o vínculo nasce com o papel escolhido.
4. **Remover/suspender** pessoa: o vínculo fica inativo e o acesso cai **na próxima chamada** (a
   checagem é por requisição, no banco). A sessão de tempo real dessa pessoa sai do grupo da
   propriedade.
5. **Minha conta**: editar nome/telefone, trocar senha (pede a atual) e excluir conta (Decisão C).
6. **Simplificação por tipo**: Vagas e Entregas só para Condomínio (feature flags por tipo).

### App
1. Tela **Pessoas com acesso** (Menu): lista com papel, convidar (escolhe papel → link para
   compartilhar no WhatsApp), mudar papel, suspender/remover.
2. Tela de **aceite do convite** (abre pelo link; cadastro ou login).
3. **Minha conta**: dados, senha, excluir conta com confirmação.
4. Menu e botões respeitam o papel (sem "Pessoas" para Membro; sem armar para Visualizador).
5. Perfil morador/técnico local substituído pelo papel real (fecha dívida 34).

### Fora do escopo
- Permissões finas por porta/horário (já existem no domínio de controle de acesso).
- Painel admin gerenciando membros de cliente.

## 4. Decisões do usuário (2026-11-08) — escopo aprovado

- **A → os dois:** o convite vai por **e-mail (Resend)** e o dono também pode copiar o link para
  WhatsApp/SMS. E-mail atrás de uma interface (`IEnvioEmail`); sem chave do Resend, envio simulado
  (log), como o SMS.
- **B → os 4 papéis** como na tabela.
- **C → anonimizar** a conta excluída.

### Contexto das decisões

- **A. Como o convite chega:**
  - **link para compartilhar** (WhatsApp/SMS), recomendado: sem custo, sem provedor;
  - e-mail automático: exige contratar um provedor de e-mail.
- **B. Papéis:** os 4 da tabela acima servem, ou quer ajustar?
- **C. Excluir conta (LGPD):**
  - **anonimizar**, recomendado: apaga nome, e-mail, telefone e fotos; mantém o histórico de
    segurança sem identificar a pessoa;
  - apagar tudo: perde históricos de alarme/acesso ligados à conta.

## 5. Modelo de dados (migration aditiva, exceto a renomeação de valores)

| Tabela | Mudança |
|---|---|
| `ConvitesPropriedade` (nova) | Id, PropriedadeId, Papel, CodigoHash, CriadoPorUsuarioId, ExpiraEmUtc, AceitoPorUsuarioId?, AceitoEmUtc?, RevogadoEmUtc? |
| `UsuariosPropriedade` | valores de `Perfil` atualizados (Administrador→Proprietario, Morador→Membro) + Gerente/Visualizador |
| `Usuarios` | + `Telefone` (opcional), + `ExcluidoEmUtc` (anonimização) |

## 6. Ordem de implementação

1. Serviço único de acesso + troca das 42 checagens, com testes (sem mudar comportamento para o dono).
2. Papéis e migration.
3. Convite (backend + app).
4. Pessoas com acesso (app).
5. Minha conta (backend + app).
6. Simplificação por tipo de propriedade.
7. ADR 0041, documentação, emulador → APK → homologação, Reviewer.

## 7. Riscos

| Risco | Mitigação |
|---|---|
| Trocar 42 checagens quebra algo do dono | Passo 1 sozinho, com testes de regressão por serviço; o dono continua passando em tudo. |
| Convite vazado | Uso único, 48 h, guardado como hash; o dono pode revogar. |
| Membro vendo o que não deve | Testes por papel em cada endpoint de alteração. |

## 8. Critérios de aceite

1. O dono convida um funcionário como Membro pelo link; o funcionário cria a conta e entra.
2. O Membro arma/desarma e abre portão, mas não vê "Pessoas com acesso".
3. O Visualizador vê câmeras e histórico e não consegue armar.
4. Removido, o funcionário perde o acesso na próxima ação.
5. "Minha conta": trocar senha e excluir conta (anonimizada) funcionam.
