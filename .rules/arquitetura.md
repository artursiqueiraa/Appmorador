# Regras: Arquitetura

## Objetivo

Manter o sistema dividido em camadas com responsabilidade única e direção de dependência
previsível, para que qualquer funcionalidade nova saiba exatamente onde deve morar sem
depender de memória ou convenção verbal.

## Princípios

- Separação em camadas: Domain (entidades e regras puras) → Application (casos de uso, DTOs,
  portas) → Infrastructure (implementação técnica: banco, protocolo, integrações externas) →
  Api (exposição HTTP). A dependência sempre aponta para dentro — uma camada nunca depende de
  quem está "acima" dela nessa cadeia.
- Protocolo e integração de hardware são infraestrutura, nunca domínio de negócio — ficam
  isolados em projetos/módulos próprios, sem vazar detalhes técnicos para Domain/Application.
- Domínio de negócio é descrito em português; infraestrutura, protocolos externos e convenções
  de ecossistema (framework web, ORM, autenticação) ficam em inglês, seguindo o vocabulário
  original desses padrões.
- Toda decisão estrutural relevante é registrada como decisão de arquitetura, nunca só
  implementada silenciosamente.
- Simplicidade estrutural vence "arquitetura correta no papel" — uma camada ou abstração nova só
  se justifica com um caso de uso real presente, nunca especulativo.

## Convenções

- Nomes de camada seguem o padrão `Domain`/`Application`/`Infrastructure`/`Api` — qualquer
  camada nova segue o mesmo vocabulário, sem sinônimos.
- Interfaces de porta (contratos entre camadas) são nomeadas pelo que o consumidor precisa, não
  pela tecnologia da implementação (ex.: uma porta de leitura de dados nunca carrega o nome do
  ORM usado por trás).
- Entidades e enums de domínio recebem nomes de negócio; campos técnicos universais de
  persistência (identificador, timestamps de criação/expiração/revogação) seguem convenção do
  ecossistema técnico, não do domínio.
- Um projeto/módulo de protocolo externo (hardware, integração de terceiros) nunca referencia um
  projeto/módulo que dependa de infraestrutura de persistência — a ponte entre os dois vive
  explicitamente na camada de Infrastructure.

## Boas práticas

- Perguntar sempre, antes de criar uma classe nova: "isso é regra de negócio pura (Domain), um
  caso de uso (Application), uma implementação técnica (Infrastructure) ou uma exposição HTTP
  (Api)?" — se a resposta não for óbvia em uma frase, a responsabilidade está mal desenhada.
- Preferir composição e injeção de dependência a hierarquias de herança profundas.
- Manter o menor número de projetos/camadas que resolve o problema real.
- Revisar periodicamente os pontos de entrada de cada camada como um mapa vivo do domínio.

## Padrões obrigatórios

- Toda mudança que atravessa mais de uma camada é avaliada quanto ao impacto na direção de
  dependência antes de implementada.
- Toda entidade nova de domínio de negócio segue a fronteira de idioma estabelecida (negócio em
  português; protocolo/framework/ORM/autenticação em inglês).
- Toda porta (interface) entre camadas é definida em termos do consumidor, nunca da
  implementação.
- Decisões estruturais relevantes (nova camada, novo bounded context, mudança de direção de
  dependência) são registradas formalmente antes de consideradas finais.

## Anti-padrões

- Uma camada interna (Domain/Application) chamando diretamente uma tecnologia de
  infraestrutura (ORM, cliente HTTP, biblioteca de protocolo) sem passar por uma porta.
- Criar uma camada, projeto ou abstração nova "porque pode ser útil depois", sem um caso de uso
  presente que a justifique.
- Resolver um desacordo de responsabilidade entre dois domínios técnicos apenas verbalmente, sem
  registrar o motivo da decisão.
- Misturar vocabulário de negócio e vocabulário técnico dentro do mesmo nome (ex.: um conceito de
  negócio carregando o nome de uma tecnologia específica).

## Checklist

- [ ] A mudança respeita a direção de dependência entre camadas?
- [ ] Alguma responsabilidade está sendo duplicada entre duas camadas ou módulos?
- [ ] A entidade/campo novo segue a fronteira de idioma (negócio vs. infraestrutura/protocolo)?
- [ ] Existe uma decisão registrada cobrindo esta mudança, se ela for estrutural?
- [ ] A mudança introduz uma camada ou abstração nova sem um caso de uso real hoje?

## Exemplos

- Uma entidade de negócio nova (ex.: uma classificação de cliente) recebe nome e propriedades em
  português; um campo técnico universal como identificador ou timestamp de criação segue a
  convenção do ecossistema, em inglês.
- Um protocolo externo de hardware vive em um projeto próprio, sem depender de ORM — a
  tradução entre "evento de protocolo recebido" e "registro de negócio salvo" acontece
  explicitamente na camada de Infrastructure, nunca dentro do parser de protocolo.
- Uma porta de leitura de dados é nomeada por sua finalidade de consulta (o que ela devolve),
  nunca pelo nome da tecnologia de acesso a dados usada na implementação.
