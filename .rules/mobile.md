# Regras: Mobile

## Objetivo

Garantir que o aplicativo mobile permaneça legível, componentizado e visualmente consistente à
medida que cresce, sem telas monolíticas nem valores visuais soltos pelo código.

## Princípios

- Nenhuma tela é um arquivo único quando ela cresce em complexidade — composição em camadas
  (tela → seção → cartão → componente → primitivo) é a forma padrão de organizar interface.
- Todo componente responde a uma pergunta antes de nascer: "isso pode ser reutilizado em outro
  lugar?" — se sim, é um componente compartilhado; se não, fica interno à tela que o usa.
- O aplicativo é apresentação e estado de sessão — nenhuma regra de negócio é duplicada no
  cliente que já existe no backend.
- Sessão e credenciais sensíveis nunca vivem em armazenamento não seguro — sempre no mecanismo de
  armazenamento seguro da plataforma.
- Toda animação e todo feedback tátil existem para comunicar algo ao usuário, nunca como
  decoração pura.

## Convenções

- Um componente que ultrapassa um tamanho razoável (algumas centenas de linhas) é sinal de que
  uma sub-responsabilidade precisa ser extraída.
- Todo valor visual (cor, espaçamento, raio, tipografia, duração de animação) vem da fonte única
  de tokens do Design System — nunca um valor numérico solto dentro de um componente.
- Feedback tátil passa sempre por um serviço central dedicado a essa responsabilidade — nenhuma
  tela chama a biblioteca nativa de vibração/haptics diretamente.
- Tipos que espelham o contrato da API (`Request`/`Response`) ficam isolados da camada de
  apresentação, nunca misturados com o estado interno de uma tela.

## Boas práticas

- Extrair rótulos, mapas de exibição e formatação reutilizável em funções/util dedicados, em vez
  de duplicar a mesma lógica em várias telas.
- Manter estado o mais local possível a cada componente, evitando um estado global que force
  a árvore inteira a re-renderizar por uma mudança pontual.
- Pensar todo fluxo em pelo menos três estados: carregando, vazio e com dados — nenhuma tela deve
  assumir que só o estado "com dados" existe.
- Escrever a interação (o que a tela faz ao usuário tocar em algo) pensando primeiro na intenção,
  depois na implementação.

## Padrões obrigatórios

- Toda dependência nova de UI/animação nativa é configurada exatamente como a biblioteca exige
  (arquivo de configuração de build, ordem de import) antes de ser usada em qualquer tela.
- Toda tela que consome dados da API trata o estado de carregamento com um placeholder visual, não
  com uma tela em branco ou um espaço vazio.
- Verificação de tipos (compilagainst strict, sem erros) é pré-requisito para considerar qualquer
  tarefa de mobile concluída.
- Toda ação que ainda não tem uma implementação real por trás (funcionalidade futura) é isolada
  atrás de uma função nomeada, nunca embutida solta num manipulador de evento, para permitir
  trocar por uma chamada real sem reescrever a tela.

## Anti-padrões

- Uma tela cobrindo busca de dados, formatação, apresentação visual e navegação sem nenhuma
  extração de componente.
- Um valor de cor, espaçamento ou duração de animação hardcoded "só para essa tela".
- Ícone girando, elemento piscando ou qualquer efeito puramente decorativo sem propósito
  comunicativo.
- Armazenar token de sessão fora do mecanismo seguro da plataforma "para simplificar o
  desenvolvimento".

## Checklist

- [ ] A tela nova está componentizada, sem um único arquivo virando monólito?
- [ ] Todo valor visual vem da fonte de tokens do Design System?
- [ ] Toda animação se justifica por comunicar algo ao usuário?
- [ ] Feedback tátil passa pelo serviço central dedicado, nunca direto pela biblioteca nativa?
- [ ] A verificação de tipos está limpa?
- [ ] Tokens de sessão estão sempre no armazenamento seguro, nunca em alternativa não segura em
      produção?

## Exemplos

- Uma tela de detalhe complexa nasce como um orquestrador fino que busca dados e decide qual
  estado renderizar, delegando cada seção visual (cabeçalho, cartão de resumo, ações rápidas) a
  um componente próprio.
- Uma ação que ainda não tem endpoint real por trás é implementada como uma função nomeada com um
  comentário explícito marcando onde a chamada real deve entrar depois, em vez de lógica solta
  dentro do manipulador de toque.
- Um valor de duração de animação novo, se realmente não existir um equivalente na fonte de
  tokens, é adicionado lá antes de usado — nunca escrito como número solto dentro do componente
  que precisa dele.
