# Regras: Produto

## Objetivo

Garantir que toda entrega do projeto seja avaliada como produto, não apenas como sistema
funcionando — priorizando percepção de qualidade, confiança e simplicidade para quem usa,
sobre conveniência técnica de quem constrói.

## Princípios

- Tecnologia é meio, produto é o objetivo — nenhuma decisão técnica é tomada sem considerar o
  impacto na percepção de quem usa o app.
- Toda entrega responde "sim" a três perguntas antes de ser considerada pronta: isso parece
  profissional? Isso transmite confiança? Isso é simples de usar?
- Escopo é sempre o menor recorte que já entrega valor real e verificável — nunca a versão
  "completa" de uma vez, adiando validação.
- O que fica de fora de uma entrega é uma decisão explícita e documentada, nunca uma omissão
  silenciosa.
- Nenhuma entrega é composta só de infraestrutura — toda entrega carrega algo perceptível por
  quem usa o produto, mesmo que pequeno.

## Convenções

- Toda decisão de classificação ou vocabulário de negócio (categorias, status, tipos) é definida
  antes da implementação começar, nunca inferida pela pessoa que está implementando.
- Taxonomias de negócio (categorias, tipos, classificações) permanecem enxutas — cobrem os casos
  reais conhecidos, sem antecipar granularidade que ninguém pediu.
- Escopo cortado é sempre registrado com o motivo, não apenas mencionado de passagem.

## Boas práticas

- Descrever toda decisão de escopo em termos do que o usuário final ganha, não em termos
  puramente técnicos.
- Trazer exemplos concretos de linguagem/comportamento esperado ao definir uma funcionalidade
  nova, reduzindo a ambiguidade de quem vai implementá-la.
- Priorizar clareza sobre sofisticação aparente sempre que as duas estiverem em tensão.
- Validar uma entrega pequena e real antes de comprometer-se com a versão maior da mesma ideia.

## Padrões obrigatórios

- Toda entrega de produto avalia, antes de ser considerada concluída, se entrega simultaneamente:
  valor perceptível ao usuário, qualidade visual consistente, qualidade técnica adequada e
  documentação atualizada.
- Nenhuma funcionalidade nova começa sem escopo explícito definido antes da implementação.
- Scope creep (algo não combinado no escopo original) nunca entra durante a execução sem
  realinhamento explícito do escopo.

## Anti-padrões

- Aprovar uma entrega que só contém infraestrutura, sem nada perceptível para quem usa o
  produto.
- Adicionar "só mais uma coisinha" no meio da execução de um escopo já definido.
- Definir escopo em termos puramente técnicos, sem tradução para o que muda na experiência de
  quem usa.
- Criar uma taxonomia de negócio extensa demais para cobrir casos hipotéticos que ainda não
  existem.

## Checklist

- [ ] Esta entrega é perceptível para quem usa o produto, ou é só infraestrutura?
- [ ] O que ficou de fora do escopo está documentado explicitamente, com o motivo?
- [ ] Alguma decisão de classificação/vocabulário de negócio ficou ambígua para quem vai
      implementar?
- [ ] A entrega atende simultaneamente valor, qualidade visual, qualidade técnica e
      documentação?

## Exemplos

- Uma funcionalidade nova é dividida no menor recorte que já é usável e testável de verdade, em
  vez de esperar a versão completa para validar qualquer parte dela.
- Uma decisão de deixar uma ação apenas visual (sem uma integração real por trás ainda) é
  registrada explicitamente como corte de escopo, com a razão e o momento em que a integração
  real deve entrar.
- Uma classificação de negócio nova nasce com poucos valores bem definidos, cobrindo os casos
  reais conhecidos, em vez de uma lista longa de categorias hipotéticas.
