# Decisões de Arquitetura (ADRs) — AppMorador

Esta pasta é o histórico permanente de toda decisão arquitetural relevante do projeto. Cada
arquivo é uma mini-ADR (Architecture Decision Record): um registro autocontido de uma decisão,
escrito para que alguém sem nenhum contexto da discussão original consiga entender o que foi
decidido, por quê, e o que isso afeta.

Nada aqui é temporário. Uma vez criado, um registro nunca é apagado ou reescrito para esconder o
que foi decidido antes — mesmo quando uma decisão é revertida ou substituída, o histórico
permanece intacto.

**Histórico**: até a Sprint 4, os registros viviam split entre `.decisions/` (numeração própria) e
`docs/adr/` (só `0001-conectividade-dvr.md`). Consolidados numa única pasta/sequência antes da
Sprint 4, preservando todo o conteúdo original — nenhuma decisão foi reescrita, só relocada e
renumerada. `docs/DECISOES_ARQUITETURA.md` (o antigo índice/mirror em prosa) foi descontinuado;
esta pasta é a única fonte de verdade agora.

## Índice

| # | Decisão | Data | Status |
|---|---|---|---|
| [0002](0002-conectividade-dvr.md) | Conectividade com o DVR na LAN do cliente | 2026-07-18 | Em aberto |
| [0003](0003-dominio-negocio-pt-br.md) | Domínio de negócio em português (pt-BR), infraestrutura em inglês | 2026-07-19 | Decidido |
| [0004](0004-rename-tabela-via-migration-manual.md) | Rename de tabela via migration manual, nunca Drop+Create | 2026-07-19 | Decidido |
| [0005](0005-serializacao-global-enums-negocio.md) | Enums de negócio serializam como texto via configuração global | 2026-07-19 | Decidido |
| [0006](0006-modelo-eventos-fontes-plugaveis.md) | Central de Eventos: modelo unificado por fontes plugáveis (IFonteEventos) | 2026-07-19 | Decidido |
| [0007](0007-squash-migrations-v030-alpha.md) | Squash do histórico de migrations num único InitialCreate (pré-v0.3.0-alpha) | 2026-07-19 | Decidido |
| [0008](0008-migrations-automaticas-no-startup.md) | Migrations aplicadas automaticamente no startup da Api | 2026-07-20 | Decidido |

## Quando criar uma decisão nova

Cria-se um registro sempre que a decisão for:

- **Estrutural**: muda a divisão de camadas, cria/remove um projeto ou módulo, altera a direção
  de dependência entre partes do sistema.
- **De tecnologia**: adota, troca ou remove uma biblioteca/framework/ferramenta usada de forma
  ampla no projeto.
- **De política permanente**: define uma regra de segurança, de dado, ou de processo que vai
  valer para todo o projeto daqui em diante (não só para uma entrega específica).
- **De difícil reversão**: uma vez implementada, desfazer custa caro (mudança de schema com dado
  real, mudança de contrato público, mudança de convenção de nomenclatura em massa).
- **De arbitragem**: resolve um conflito de responsabilidade entre duas áreas do projeto que, sem
  registro, provavelmente ressurgiria mais tarde.

**Não** se cria um registro para: escopo de uma entrega específica (isso vive no roteiro de
prioridades do projeto), detalhe de implementação sem impacto estrutural, ou qualquer escolha
facilmente reversível sem custo.

## Como criar uma decisão nova

1. Copiar `0001-template.md`.
2. Nomear o novo arquivo como `NNNN-titulo-curto-em-kebab-case.md`, usando o próximo número
   sequencial disponível (ver seção de numeração abaixo).
3. Preencher todas as seções do template por completo — nenhuma seção fica com placeholder
   ("a definir") no arquivo final.
4. Escrever para quem não participou da discussão: contexto suficiente para que a decisão faça
   sentido isoladamente, sem precisar perguntar a ninguém.

## Numeração

- `0001` é reservado para o próprio template (`0001-template.md`) e nunca é reutilizado para uma
  decisão real.
- A primeira decisão real começa em `0002`.
- A numeração é sequencial e nunca reaproveitada, mesmo que uma decisão antiga seja revertida —
  o número de um registro é permanente, assim como o próprio registro.

## Quando uma decisão é revertida ou substituída

O arquivo antigo nunca é apagado nem editado para remover o conteúdo original. Adiciona-se uma
nota curta no topo do arquivo antigo apontando para o novo registro (ex.: "Substituída pela
decisão 000X — ver motivo lá"), preservando a decisão original como ela foi tomada.

## Quem escreve

Qualquer pessoa (ou agente, ver `.agents/`) pode registrar uma decisão relevante do seu domínio.
Decisões que atravessam mais de uma área do projeto são de responsabilidade do agente
`arquiteto`, que atua como árbitro final quando há divergência.

## Padrão de qualidade

Um registro só está pronto quando alguém de fora da discussão original consegue lê-lo e entender,
sem ajuda externa: o que era o problema, o que foi decidido, por que essa alternativa venceu as
outras, e o que muda em consequência.
