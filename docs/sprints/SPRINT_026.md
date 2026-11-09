# Sprint 26 — Facial ponta a ponta + LGPD

> Fase 1 (planejamento, sem código) — 2026-11-08. Origem: [Plano de Ação de Produto](../roadmap/PLANO_ACAO_PRODUTO.md).

## 1. Objetivo

O rosto cadastrado no app libera a porta de verdade, nos leitores Control iD e Hikvision, com o
consentimento da pessoa registrado como manda a LGPD (biometria é dado sensível).

## 2. Levantamento (estado real)

| Ponto | Situação |
|---|---|
| Foto do rosto | `FotoMoradorServico` guarda a foto no disco do servidor (`Morador.FotoPath`) e o app já captura pela câmera. **Não vai para nenhum leitor** (dívida 57). |
| Control iD | O provider sincroniza pessoas, credenciais e permissões e abre porta (Sprint 24.5). **Não envia foto.** |
| Hikvision | Só existe para snapshot de câmera (`HikvisionIsapiSnapshotProvider`). **Não há provider de controle de acesso** (pessoas, rosto, porta). |
| Consentimento | Não existe registro de consentimento biométrico. |
| Menores | `Morador.Tipo` = Titular/Dependente e `Parentesco`; não há data de nascimento. |
| Termos/Privacidade | Não publicados (dívida 55). |

## 3. Escopo

### Backend
1. **Foto para o leitor Control iD**: ao cadastrar ou trocar o rosto, envia a imagem ao leitor
   vinculado (`user_set_image`); ao remover a pessoa ou a foto, apaga no leitor. Validado no
   simulador local (ganha os endpoints de imagem), pendente de aparelho físico.
2. **Provider Hikvision de controle de acesso** (padrão ADR 0014, ISAPI com autenticação Digest):
   - cadastrar e remover pessoa (`UserInfo`);
   - enviar e remover rosto (`FDLib/FaceDataRecord`);
   - **abrir porta** (`RemoteControl/door`), ligando o Hikvision aos "Portões e portas" da 24.5.

   Validado num simulador HTTP novo; pendente de aparelho físico.
3. **Consentimento biométrico** (`ConsentimentosBiometricos`): quem consentiu, por quem, quando,
   versão do termo. Sem consentimento registrado, o backend recusa a foto.
4. **Remoção completa**: remover a pessoa ou a foto apaga do servidor e de todos os leitores; falha
   num leitor fica registrada e é tentada de novo na próxima sincronização.
5. **Termos de Uso e Política de Privacidade**: páginas públicas no app, versão registrada no
   consentimento.

### App
1. Antes de abrir a câmera, a tela de **consentimento**: texto curto e claro, "Concordo" registra.
   Dependente (menor): o dono confirma que é o responsável legal.
2. Status do rosto por leitor na pessoa: "Enviado ao leitor da portaria" ou "Falhou, tentar de novo".
3. Links para Termos e Privacidade no cadastro, no login e em Ajustes.

### Fora do escopo
- Leitores Intelbras/Dahua (Hikvision e Control iD apenas).
- Biometria por digital.

## 4. Decisões do usuário (2026-11-08) — escopo aprovado

- **A → publicar com "a preencher"**: razão social, CNPJ e e-mail do encarregado ficam marcados
  como pendência antes da venda.
- **B → o dono confirma** "sou o responsável legal" para Dependentes; fica registrado quem e quando.
- **C → retenção conforme a LGPD** (o usuário pediu guardar "por um tempo" se a lei permitir):
  - **foto do rosto: apagada na hora** ao remover a pessoa/foto ou revogar o consentimento — é dado
    sensível (art. 5º II, art. 11), o fim da finalidade exige eliminação (arts. 15–16) e nenhuma
    hipótese do art. 16 cobre "guardar a imagem para log";
  - **histórico de entradas (sem imagem): 6 meses**, depois anonimizado — legítimo interesse de
    segurança (art. 7º IX, art. 10), prazo declarado na Política de Privacidade.
  - Leitura técnica, não parecer jurídico: revisão por advogado registrada como pendência.

### Opções apresentadas

- **A. Dados da empresa para os Termos e a Privacidade**: razão social, CNPJ e o e-mail do
  encarregado de dados (LGPD). Sem eles, publico com "a preencher" e marco como pendência.
- **B. Consentimento de menor**: o dono marca "sou o responsável legal" para Dependentes
  (recomendado) ou exige o nome e o documento do responsável?
- **C. Retenção da foto**: apagar só quando a pessoa ou a foto for removida (recomendado) ou também
  apagar automaticamente após X meses sem uso?

## 5. Modelo de dados (migration aditiva)

| Tabela | Campos |
|---|---|
| `ConsentimentosBiometricos` (nova) | Id, MoradorId, RegistradoPorUsuarioId, VersaoTermo, ResponsavelLegal (bool), RegistradoEmUtc, RevogadoEmUtc? |
| `SincronizacoesFoto` (nova) | Id, MoradorId, EquipamentoId, Situacao (Enviada/Falhou/Removida), Mensagem?, AtualizadoEmUtc |

## 6. Ordem de implementação

1. Consentimento (backend + tela) — bloqueia a foto sem consentimento.
2. Foto para o Control iD + simulador.
3. Provider Hikvision (pessoa, rosto, porta) + simulador Hikvision.
4. Remoção completa e reenvio de falhas.
5. Termos e Privacidade.
6. ADR 0042, documentação, emulador → APK → homologação, Reviewer.

## 7. Riscos

| Risco | Mitigação |
|---|---|
| Formato de imagem/endpoint diferente no aparelho real | Isolado nos providers; simuladores; homologação física já pendente. |
| Foto de menor sem responsável | Consentimento com confirmação de responsável obrigatório para Dependente. |
| Leitor fora do ar ao remover alguém | Registro da falha + nova tentativa; a pessoa removida já não abre pelo app. |

## 8. Critérios de aceite

1. Sem consentimento, a câmera de cadastro do rosto não abre (e o backend recusa).
2. Cadastrar o rosto envia a imagem aos simuladores Control iD e Hikvision.
3. Remover a pessoa apaga a imagem nos dois e no servidor.
4. Portão ligado a um terminal Hikvision abre pelo app (simulador).
5. Termos e Privacidade acessíveis no app.
