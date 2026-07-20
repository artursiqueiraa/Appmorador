# Regras: Backend

## Objetivo

Garantir que toda regra de negócio implementada no backend seja consistente, testável e livre de
vazamento técnico para quem consome a API — independentemente de qual funcionalidade estiver
sendo construída.

## Princípios

- Toda regra de negócio vive num caso de uso de Application, nunca dentro de um controller.
- Um serviço cobre um recorte coeso de responsabilidade — nunca um serviço genérico acumulando
  várias entidades sem relação direta.
- Falhas de negócio esperadas (não encontrado, sem permissão, credenciais inválidas) são tratadas
  como resultado de retorno, nunca como exceção usada para controle de fluxo normal.
- Nada que chegue ao cliente (DTO de resposta) expõe vocabulário técnico interno — só linguagem
  que o consumidor da API deveria conhecer.
- Toda operação que altera estado é explícita sobre o que persiste e quando, sem efeitos
  colaterais escondidos.

## Convenções

- DTOs de entrada e saída seguem sufixo consistente de propósito (requisição vs. resposta),
  nomeados pelo verbo/substantivo de negócio que representam.
- Repositórios expõem um vocabulário estável e prévisível de operações (buscar por identificador,
  listar por dono, adicionar, salvar alterações) — nunca um método novo por capricho de
  implementação.
- Mensagens de erro voltadas ao usuário final nunca distinguem "não existe" de "existe mas não é
  seu" — a ambiguidade é proposital, para não vazar informação a quem não deveria ter acesso.
- Enums de negócio expostos pela API são configurados para (de)serializar como texto legível,
  nunca como número interno, tanto na leitura quanto na escrita.

## Boas práticas

- Validar toda entrada pública (DataAnnotations ou equivalente) antes que ela alcance a regra de
  negócio.
- Manter o controller como um tradutor fino entre HTTP e caso de uso — nenhuma decisão de negócio
  acontece ali.
- Escrever cada caso de uso pensando em quem vai lê-lo depois, não só em quem está escrevendo
  agora.
- Ao adicionar um campo que precisa de persistência, alinhar a mudança de schema antes de
  considerar a funcionalidade completa.

## Padrões obrigatórios

- Toda entrada de enum vinda do cliente é testada de ponta a ponta (não só compilada) antes de
  a funcionalidade ser considerada pronta — falhas de serialização só aparecem em execução real.
- Toda regra de autenticação/autorização segue a política de segurança vigente, nunca uma
  variação criada ad hoc para conveniência de uma funcionalidade específica.
- Toda mudança de contrato de API (campo novo, campo removido, rota nova) é comunicada
  explicitamente a quem consome antes de finalizada.
- Build limpo (sem erros, sem avisos) é pré-requisito para considerar qualquer tarefa de backend
  concluída.

## Anti-padrões

- Lógica de negócio dentro de um controller ou de um middleware.
- Acesso direto ao mecanismo de persistência a partir de dentro de um caso de uso, sem passar por
  uma porta de repositório.
- Mensagem de erro que revela detalhe interno (nome de tabela, stack trace, exceção crua) ao
  cliente da API.
- Um serviço "genérico" que cresce para cobrir múltiplas entidades não relacionadas só por
  conveniência de não criar um serviço novo.

## Checklist

- [ ] A regra de negócio nova está no caso de uso certo, não no controller?
- [ ] O DTO de resposta contém algum vocabulário técnico que o consumidor não deveria ver?
- [ ] Enums que chegam como entrada da API foram testados de ponta a ponta, não só compilados?
- [ ] Mensagens de erro de autenticação/autorização mantêm a ambiguidade proposital?
- [ ] O build está limpo, sem erros nem avisos?

## Exemplos

- Um caso de uso de atualização verifica sempre se o recurso pertence a quem está pedindo a
  alteração, retornando a mesma mensagem genérica tanto para "não existe" quanto para "existe mas
  é de outro dono".
- Um campo de classificação (enum) exposto pela API é configurado para aceitar e devolver o nome
  legível do valor, nunca um número — e esse comportamento é confirmado com uma chamada real
  antes de considerado funcional.
- Um serviço novo nasce coeso a uma única responsabilidade de negócio; se um segundo conceito
  não relacionado precisar de lógica própria, ganha seu próprio serviço, não um método a mais no
  existente.
