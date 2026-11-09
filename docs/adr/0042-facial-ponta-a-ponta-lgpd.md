# ADR 0042 — Facial ponta a ponta com consentimento (LGPD)

**Data**: 2026-11-08

## Contexto

A foto do rosto ficava só no servidor: nenhum leitor recebia (dívida 57) e não havia registro de
consentimento. Biometria é dado pessoal sensível (LGPD art. 5º, II), e tratá-la exige consentimento
específico (art. 11, I) — de responsável legal no caso de menores (art. 14). O Hikvision só existia
para snapshot de câmera.

## Decisão

1. **Consentimento antes da foto.**
   - `ConsentimentosBiometricos` registra quem consentiu, por quem, quando e a versão do termo.
   - Sem consentimento ativo, o backend recusa a foto e o app não abre a câmera.
   - Para Dependente, quem cadastra precisa confirmar que é o responsável legal (decisão B).
   - Revogar apaga a foto do servidor e dos leitores (art. 8º §5º).
2. **Rosto nos leitores** (`FacialLeitoresServico`). Ao salvar ou trocar a foto, ela é enviada a
   todos os leitores faciais da propriedade; ao remover, é apagada deles.
   - **Control iD:** procura a pessoa pela `registration` (id do morador) ou a cria, e envia a
     imagem com `user_set_image`. Remove com `user_destroy_image`.
   - **Hikvision**, provider novo seguindo a ADR 0014, via ISAPI com autenticação Digest:
     - pessoa: `UserInfo/Record` e `UserInfo/Delete`;
     - rosto: `FDLib/FaceDataRecord` (multipart) e `FDSearch/Delete`;
     - porta: `RemoteControl/door` — o terminal Hikvision passa a abrir "Portões e portas".
   - A situação de cada par (pessoa, leitor) fica em `SincronizacoesFoto`; uma falha é tentada de
     novo a cada minuto.
3. **Retenção, conforme a LGPD** (decisão C, conferida com a lei):
   - **Foto:** apagada **na hora** ao remover a pessoa ou a foto, ou ao revogar o consentimento. A
     finalidade acabou (arts. 15–16) e nenhuma hipótese do art. 16 permite guardar a imagem
     "para log".
   - **Pessoa excluída junto com unidade ou propriedade:** a rotina de 1 minuto apaga a foto dela.
   - **Histórico de entradas, sem imagem:** **6 meses** após a remoção, depois é anonimizado
     ("Pessoa removida"). A base é o legítimo interesse de segurança (art. 7º IX, art. 10), e o
     prazo está declarado na Política de Privacidade.
4. **Termos de Uso e Política de Privacidade** viraram telas no app (abrem sem login), com os dados
   da empresa marcados como "a preencher" (decisão A).

Os simuladores locais `ControlIdSimulator` (ganhou endpoints de pessoa e rosto) e
`HikvisionSimulator` (novo, porta 9600) validam o protocolo sem aparelho.

## Consequências

- **Fotos antigas:** fotos cadastradas antes desta Sprint continuam no servidor, mas sem
  consentimento registrado. Trocar ou recadastrar o rosto passa a exigir o consentimento.
- **Pendências:**
  - homologação nos aparelhos físicos;
  - dados da empresa nos textos legais;
  - revisão dos textos por advogado.
