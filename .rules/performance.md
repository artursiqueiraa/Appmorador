# Regras: Performance

## Objetivo

Tratar performance como parte da experiência do produto, não como preocupação de última hora —
medindo re-renderização, consumo de memória, tempo de carregamento e eficiência de consulta antes
que virem problema perceptível para quem usa o sistema.

## Princípios

- Performance é revisada em toda entrega, não apenas quando um problema já é visível.
- Toda otimização é motivada por um sintoma real e medido, nunca por instinto ou precaução sem
  evidência.
- A otimização mais simples que resolve o sintoma real é sempre preferida a uma reestruturação
  grande por precaução.
- Legibilidade nunca é trocada por performance sem um ganho mensurável relevante que justifique.

## Convenções

- Consultas a dados retornam exatamente o que a tela/funcionalidade precisa — nunca mais do que
  o necessário "para garantir".
- Contagens são feitas no armazenamento de dados, nunca carregando a coleção inteira para contar
  no lado que consome.
- Animações usam mecanismos que rodam fora da thread principal de interface sempre que a
  plataforma oferecer essa opção.
- Estado de interface fica no escopo mais local possível, evitando que uma mudança pontual force
  a re-renderização de partes não relacionadas.

## Boas práticas

- Instrumentar o sintoma (número de re-renderizações, tempo de consulta, tempo de abertura) antes
  de decidir qual otimização aplicar.
- Avaliar o custo de uma dependência nova no tempo de carregamento/abertura antes de adicioná-la.
- Revisar consultas que cruzam múltiplas fontes de dados em busca de padrões de consulta
  repetida evitável.
- Preferir memoização e re-estruturação de estado apenas onde o sintoma real de re-renderização
  foi observado, não de forma indiscriminada.

## Padrões obrigatórios

- Toda entrega revisa explicitamente: re-renderizações desnecessárias, consumo de memória, tempo
  de carregamento/abertura e peso dos componentes adicionados.
- Toda consulta nova a dados é avaliada quanto à possibilidade de trazer mais informação do que
  o necessário.
- Toda animação nova usa o mecanismo de animação padrão do projeto (rodando fora da thread
  principal), nunca um laço manual de atualização de estado.

## Anti-padrões

- Aplicar memoização ou reestruturação de estado de forma indiscriminada, sem um sintoma real por
  trás.
- Buscar uma coleção inteira de dados só para contar seus itens no lado que consome.
- Ignorar o custo de uma dependência nova "porque resolve rápido", sem avaliar o impacto no
  tempo de abertura.
- Fazer "otimização preventiva" que complica o código sem nenhum sintoma real observado.

## Checklist

- [ ] Alguma consulta nova traz mais dado do que a funcionalidade realmente usa?
- [ ] Existe algum padrão de consulta repetida evitável ao cruzar múltiplas fontes de dados?
- [ ] Uma animação nova usa o mecanismo padrão de animação (fora da thread principal)?
- [ ] Um componente novo introduz re-renderização evitável por estado mal escopado?
- [ ] Uma dependência nova foi avaliada quanto ao impacto no tempo de abertura?

## Exemplos

- Uma tela que precisa exibir a quantidade de itens de uma categoria usa uma operação de
  contagem no armazenamento de dados, em vez de carregar todos os itens só para contar quantos
  existem.
- Uma animação de contagem numérica usa o mecanismo de animação nativo do projeto, em vez de um
  laço manual atualizando estado a cada quadro.
- Um estado que só afeta um componente específico permanece local a esse componente, em vez de
  subir para um contexto compartilhado que forçaria toda a árvore de telas a re-renderizar.
