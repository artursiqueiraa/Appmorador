# ADR 0036 — Moradores e dependentes, foto facial no servidor e aba Pessoas

**Data**: 2026-11-02

## Contexto

O usuário pediu uma aba para cadastrar moradores e dependentes da unidade, os veículos de cada um e o rosto (facial) dos dependentes. No Web App, cadastrar um morador exigia quatro níveis de navegação (Acessos → Minha Propriedade → Unidades → Moradores), e rosto e veículo ficavam em telas separadas. Além disso:
- o domínio não sabia quem é titular e quem é dependente;
- a foto facial existia só no aparelho (ADR 0020).

## Decisão

- **Domínio:** `Morador` ganha `Tipo` (`TipoMorador`: Titular/Dependente, padrão Titular) e `Parentesco` (`Parentesco?`, só para dependentes; gravado nulo em titulares).
- **Migration `MoradorTipoEParentesco`:** só adiciona colunas, sem nada destrutivo. O padrão `'Titular'` preenche os registros existentes. Nomes em pt-BR.
- **Foto facial no servidor:** `PUT/GET/DELETE api/moradores/{id}/foto`.
  - O envio é multipart, campo `foto`, até 5 MB.
  - O formato é conferido pelos bytes (JPEG `FF D8 FF`, PNG `89 50 4E 47…`), nunca pelo nome do arquivo ou pelo Content-Type.
  - Vale a mesma regra de posse de `MoradorServico` (Morador → Unidade → Propriedade.ProprietarioId), com a mesma mensagem para "não existe" e "não é seu".
  - O armazenamento é `IArmazenamentoFotoMorador` em disco local (`FotosMoradores:BasePath`, padrão `fotos-moradores`), numa pasta separada dos snapshots.
  - O nome do arquivo nunca vem do cliente, e a leitura recusa caminhos fora da pasta base.
  - A foto é servida autenticada e com `Cache-Control: private, no-store`, nunca como arquivo estático.
- **Ao cadastrar o rosto:** o Web App envia a foto e garante uma credencial `Facial` ativa para a pessoa.
- **Web App, aba "Pessoas":** lista de titulares e dependentes com o selo "Face", detalhe com rosto, veículos e outros acessos, e cadastro guiado (vínculo e dados, depois rosto, depois veículo). No computador, a lista e o detalhe ficam lado a lado.
- **Unidade "Principal":** quando a propriedade ainda não tem unidade, uma unidade "Principal" é criada só na hora de adicionar a primeira pessoa.
- **Segurança (DIVIDA 54):** `CriarUsuarioInternoRequest` passa a validar nome, e-mail e senha no servidor, com a mesma regra do cadastro público.

## Consequências

- O envio da foto para o leitor físico (Control iD) **não existe**. A tela diz "rosto cadastrado" e nunca diz que o equipamento já reconhece a pessoa (DIVIDA 57).
- As fotos de rosto são dado pessoal sensível em disco local: entram em `docs/STORAGE.md` e no `.gitignore`. O backup precisa incluir essa pasta.
- O app mobile não mudou. Ele continua sem os campos Tipo e Parentesco, e a API aceita isso porque os dois têm valor padrão.
