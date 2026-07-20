# Regras: Documentação

## Objetivo

Garantir que o histórico de decisões e mudanças do projeto seja reconstruível por qualquer
pessoa (ou sessão nova) sem depender da memória de quem implementou — através de registros
consistentes, datados e organizados.

## Princípios

- Nenhuma decisão estrutural relevante fica só na cabeça de quem a tomou — é sempre registrada
  formalmente, num formato padrão e encontrável.
- Documentação é escrita para quem não estava presente na decisão — nunca assume contexto
  implícito que só quem participou entenderia.
- Toda entrega gera, no mínimo, um registro do que mudou e por quê — nunca só uma lista de
  arquivos tocados.
- Dívida técnica nunca fica invisível — se um atalho ou compromisso temporário é aceito, ele é
  registrado explicitamente, com motivo e impacto.

## Convenções

- Decisões de arquitetura seguem sempre o mesmo formato: título, contexto, decisão,
  consequência — nunca um texto livre sem estrutura.
- Um registro de mudança de schema/dados sempre contém: operações realizadas, impacto nos dados
  existentes, se houve algo destrutivo, avaliação de segurança e recomendação final.
- Toda decisão e todo registro de mudança carrega uma data, para preservar valor histórico.
- Registros são numerados sequencialmente e sem lacunas, permitindo navegação cronológica clara.

## Boas práticas

- Escrever o "porquê" de uma decisão, não só o "o quê" — o motivo é o que dá valor ao registro
  meses depois.
- Revisar periodicamente se o roteiro de prioridades/backlog ainda reflete a realidade, em vez de
  deixá-lo como uma lista estática esquecida.
- Registrar um bug de ambiente ou workaround não óbvio assim que descoberto, evitando que a mesma
  investigação precise ser refeita do zero depois.
- Consolidar, ao final de cada entrega, um resumo que qualquer pessoa consiga ler e entender o
  que mudou, mesmo sem ter acompanhado o processo.

## Padrões obrigatórios

- Toda entrega atualiza o registro de mudanças com o que foi feito e por quê.
- Toda mudança de schema/dado é documentada com o resumo técnico completo antes de ser aplicada
  contra dados reais.
- Toda decisão estrutural relevante vira um registro formal antes de considerada definitiva.
- Dívida técnica identificada por qualquer pessoa/agente é registrada no mesmo ciclo em que foi
  encontrada, nunca adiada indefinidamente.

## Anti-padrões

- Escrever documentação genérica que poderia se aplicar a qualquer projeto, sem informação
  específica de valor.
- Deixar o roteiro de prioridades desatualizado até ele parar de refletir a realidade e virar uma
  lista que ninguém mais consulta.
- Aceitar um atalho técnico "só dessa vez" sem registrar em lugar nenhum.
- Registrar uma entrega como concluída sem ela ter passado pela revisão esperada.

## Checklist

- [ ] O registro de mudanças foi atualizado com o que foi feito e por quê?
- [ ] O roteiro de prioridades reflete o que já foi entregue e o que vem a seguir?
- [ ] Toda mudança de schema/dado tem um registro técnico completo antes de aplicada?
- [ ] Alguma dívida técnica identificada ficou sem registro formal?
- [ ] Uma decisão estrutural relevante foi tomada sem virar um registro formal?

## Exemplos

- Uma mudança de schema que adiciona uma coluna nova sobre uma tabela com dados reais é
  documentada com o valor de preenchimento retroativo escolhido e o motivo dessa escolha, antes
  de ser aplicada.
- Uma decisão de manter uma funcionalidade apenas visual (sem integração real ainda) é registrada
  como uma decisão explícita, incluindo o momento em que a integração real deve ser revisitada.
- Um workaround para um problema de ambiente não óbvio (uma ferramenta que falha de forma
  específica) é registrado com o comando exato que resolve, para nunca precisar ser
  redescoberto do zero numa sessão futura.
