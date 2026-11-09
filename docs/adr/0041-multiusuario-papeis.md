# ADR 0041 — Multiusuário: acesso por papel num serviço único

**Data**: 2026-11-08

## Contexto

O RBAC da Sprint 21 criou vínculos (`UsuariosPropriedade`) e permissões funcionais, mas o código
continuou perguntando "você é o dono?" em **56 lugares** de 21 serviços, além do hub de tempo real
e da lista de propriedades. Qualquer pessoa que não fosse a dona era barrada em tudo, mesmo com
vínculo. A Sprint 25 precisava de convite, papéis e remoção com efeito imediato.

## Decisão

1. **`IAcessoPropriedadeServico`** é o ponto único da pergunta "pode?".
   - Ele trabalha com quatro níveis: **Ver**, **Operar**, **Alterar** e **Proprietário**.
   - O dono sempre pode tudo.
   - Os demais dependem do vínculo ativo e do papel:

     | Papel | Ver | Operar | Alterar | Proprietário |
     |---|---|---|---|---|
     | Proprietário (`Administrador`) | sim | sim | sim | sim |
     | Gerente | sim | sim | sim | não |
     | Membro (`Morador`) | sim | sim | não | não |
     | Visualizador | sim | não | não | não |
2. **Carga única por requisição.** Um middleware carrega os vínculos ativos do usuário logado
   numa consulta, e a checagem vira uma conta em memória.
   - Remover alguém vale na **próxima requisição** dele, mesmo com o token ainda válido.
   - Sem a carga (testes, serviços de fundo), só o dono passa: é o comportamento anterior.
3. **Troca mecânica das checagens.**
   - Cada uma das 56 checagens pede o nível do seu método:
     - listar e ver → Ver;
     - comandos → Operar;
     - criar, editar e excluir → Alterar;
     - excluir a propriedade → Proprietário.
   - As funções auxiliares ficaram **fechadas por padrão** (Alterar) e só os caminhos de leitura
     pedem Ver.
   - Sem mudança de comportamento para o dono: os 221 testes anteriores passaram sem alteração.
4. **Convite.** O código aleatório (24 bytes) só existe no link; o banco guarda o hash SHA-256.
   - O convite é de uso único, vale 48 h e pode ser revogado.
   - Vai por e-mail via **Resend** (`IEnvioEmail`; sem chave, simulado) e o dono também
     compartilha o link no WhatsApp/SMS (decisão A do usuário).
5. **Papéis ↔ permissões funcionais.** O papel define as `UsuarioPropriedadePermissao` do vínculo,
   as mesmas que o app já lia para mostrar ou esconder botões.
6. **Excluir conta = anonimizar** (decisão C, LGPD).
   - Apaga nome, e-mail, telefone e senha.
   - Encerra as sessões.
   - Faz soft delete das propriedades próprias.
   - É bloqueado enquanto houver outras pessoas nelas.

Os nomes do enum foram mantidos (`Administrador`, `Morador`) para não exigir migração de dados; o
app exibe "Proprietário" e "Membro".

## Alternativas consideradas

- **Checagem assíncrona no banco em cada ponto.** Recusada: 56 consultas extras e assinaturas
  assíncronas espalhadas.
- **Validar o papel no controller.** Recusada: muitas rotas só têm o id do recurso, não o da
  propriedade.

## Consequências

- O tempo real (`OperacionalHub`) aceita membros.
- A lista de propriedades inclui as compartilhadas, com o papel e as permissões do próprio usuário.
- Código novo nunca deve comparar `ProprietarioId` direto: usa `IAcessoPropriedadeServico`.
- Ainda pendente: chave do Resend (e-mail simulado) e o app ler o link de convite no celular
  nativo (hoje o link abre no navegador).
