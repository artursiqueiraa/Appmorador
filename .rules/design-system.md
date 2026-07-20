# Regras: Design System

## Objetivo

Garantir que a identidade visual do aplicativo permaneça consistente à medida que novas telas e
componentes são criados, através de uma única fonte de verdade para todo valor visual e de
animação.

## Princípios

- Existe uma única fonte de verdade para tokens visuais (cor, espaçamento, raio, tipografia,
  animação, sombra, opacidade, camada de sobreposição, tamanho de ícone) — nenhum componente
  define um valor visual paralelo.
- Cores semânticas (sucesso/proteção, atenção, erro) mantêm o mesmo significado em qualquer tela
  do aplicativo — nunca mudam de sentido conforme o contexto.
- Toda categoria de animação (transição, feedback de toque, carregamento) tem uma duração e
  curva padrão associada, reutilizada por qualquer componente que precise dela.
- Evolução do sistema de tokens é sempre aditiva e centralizada — nunca uma mudança pontual só
  para uma tela específica.

## Convenções

- Tokens são agrupados por categoria (cor, espaçamento, raio, tipografia, animação, sombra,
  opacidade, camada de sobreposição, tamanho de ícone), nunca numa lista plana sem organização.
- Nomes de conveniência antigos permanecem disponíveis ao evoluir a fonte de tokens, para não
  quebrar código já existente que os consome.
- Um valor visual novo só é criado depois de confirmado que nenhum token existente já cobre a
  mesma necessidade.

## Boas práticas

- Perguntar sempre, antes de escrever um valor visual num componente: "esse valor já existe como
  token, ou preciso adicionar um novo à fonte única?"
- Documentar o motivo de uma cor semântica nova antes de adicioná-la.
- Avaliar o impacto de uma mudança de paleta/tipografia em todas as telas existentes antes de
  aprovar a mudança, nunca só na tela que motivou o pedido.
- Manter a curva/duração de animação de uma mesma categoria (ex.: feedback de toque) idêntica em
  todo o aplicativo.

## Padrões obrigatórios

- Nenhum valor de espaçamento, cor, raio, tipografia, duração de animação, sombra, opacidade ou
  tamanho de ícone é escrito como número ou string solta dentro de um componente — sempre
  referenciado a partir da fonte única de tokens.
- Toda mudança na fonte de tokens é avaliada contra o conjunto de telas que já a consomem antes
  de finalizada.
- Cores semânticas de status mantêm o mesmo significado em toda nova tela.

## Anti-padrões

- Definir uma cor, espaçamento ou duração "só para esse componente" fora da fonte única de
  tokens.
- Duplicar um grupo de tokens em vez de estender o existente.
- Reaproveitar uma cor semântica com um significado diferente do já estabelecido em outra parte
  do aplicativo.
- Misturar unidades de medida inconsistentes dentro do mesmo grupo de tokens.

## Checklist

- [ ] Existe algum valor visual hardcoded fora da fonte única de tokens?
- [ ] Um token novo foi realmente necessário, ou já existia um equivalente?
- [ ] A mudança na fonte de tokens foi avaliada contra todas as telas que a consomem?
- [ ] Cores semânticas de status mantêm o significado consistente na tela nova?

## Exemplos

- Uma tela nova que precisa de um espaçamento específico primeiro verifica se algum valor da
  escala de espaçamento já existente serve, antes de considerar adicionar um novo.
- Uma animação de contagem numérica e uma animação de feedback de toque usam categorias de
  duração diferentes da mesma fonte de tokens, cada uma com o propósito que a justifica.
- Uma cor que já significa "estado de atenção" em uma tela nunca é reaproveitada com o
  significado de "sucesso" em outra tela do mesmo aplicativo.
