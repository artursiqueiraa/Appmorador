# ADR 0006 — Central de Eventos: modelo unificado por fontes plugáveis (IFonteEventos)

**Data**: 2026-07-19

## Contexto

A Sprint 3 pediu uma "Central de Eventos" cobrindo timeline de alarmes, acessos e eventos de
central, com filtros/busca/paginação. Investigação do domínio revelou que: (1) só existe 1 código
Contact ID homologado hoje (`1130 — Disparo de zona`); (2) não existe nenhuma entidade ou
integração de controle de acesso no AppMorador; (3) a referência apontada pelo usuário
(`Teste-portaria-main1`) tem uma integração Control iD real (cliente HTTP contra a API REST
documentada), mas nunca validada contra hardware real (seed com IPs/credenciais fake), e a parte
validada contra hardware real lá (Intelbras, protocolo CGI) acabou sendo um leitor de controle de
acesso da família ASI — com ajustes ainda documentados como pendentes naquele repositório.

## Problema

Como modelar a Central de Eventos para que ela funcione hoje com a única fonte real disponível
(central de alarme JFL) sem impedir, no futuro, a adição de fontes reais de controle de acesso —
sem redesenhar a Timeline quando essa integração existir de fato?

## Alternativas consideradas

- **Timeline acoplada diretamente a `Ocorrencia`**: mais simples de implementar agora, mas
  amarra o contrato interno e a Api à entidade concreta de um único protocolo — adicionar uma
  fonte de acesso no futuro exigiria revisitar o modelo inteiro.
- **Implementar a integração de acesso real agora** (Control iD ou Intelbras ASI/CGI), usando a
  referência como base: rejeitada nesta Sprint — exigiria uma entidade de dispositivo nova,
  cadastro por propriedade e um cliente HTTP novo; nenhuma Propriedade tem esse hardware
  provisionado hoje, e nem a referência tem essa integração validada contra hardware real. É uma
  fase própria, de dimensão comparável à Fase 2 (Snapshot), não um item dentro de uma Sprint de UI.
- **Modelo unificado por fontes plugáveis** (`EventoTimeline` + porta `IFonteEventos`, uma
  implementação real hoje — `JflFonteEventos` — outras adicionadas via DI no futuro): mais
  trabalho de abstração agora, mas a Timeline nunca precisa mudar quando uma fonte nova existir.

## Decisão

Modelo unificado por fontes plugáveis. `EventoTimeline` (Application) nunca conhece `Ocorrencia`
— só `IFonteEventos` sabe traduzir sua fonte concreta para o formato comum. Hoje existe uma única
implementação real, `JflFonteEventos` (Infrastructure), que por dentro consulta `Ocorrencia`.
Futuras integrações (`ControlIdFonteEventos`, `IntelbrasFonteEventos`, `SistemaFonteEventos`) se
registram como mais uma implementação de `IFonteEventos`, sem alterar `IEventosServico` nem o
formato de `EventoTimeline`.

`EventoTimeline` foi enriquecido para essa evolução futura: `Origem` (enum: `Jfl`, `Aplicacao` —
`ControlId`/`Intelbras`/`Sistema` entram quando existirem de fato), `Categoria` (`Alarme`/
`Acesso`/`Sistema`), `Severidade` (`Informativo`/`Atencao`/`Critico`), `Titulo`, `Descricao`,
`Metadados` (`IReadOnlyDictionary<string, object?>?`, livre para cada fonte guardar dado extra
sem mudar o formato comum). Nenhum desses campos internos vaza para a Api: `EventoResponse`
expõe só `Titulo`/`Descricao`/`OcorridoEmUtc`/`Destaque` (bool, derivado de
`Severidade == Critico`) — linguagem de produto, não a taxonomia interna, preservando liberdade
de evoluir a classificação sem quebrar o contrato público.

Integração real de controle de acesso (Control iD/Intelbras ASI) fica para uma fase futura
própria — não cortada do produto, só corretamente dimensionada como hardware-integration, não
UI.

## Consequências

- Adicionar uma fonte de eventos nova (ex.: controle de acesso) no futuro é aditivo: nova classe
  implementando `IFonteEventos`, registro de DI — nenhuma mudança em `EventosServico`,
  `EventoResponse` ou nas telas mobile.
- Com 1 fonte só hoje, `EventosServico` chama essa fonte diretamente — agregação/paginação entre
  múltiplas fontes é lógica que ainda não existe (não há uma segunda fonte real para validar
  contra), registrada como dívida técnica.
- `Metadados` existe no formato mas não é populado por `JflFonteEventos` ainda — campo disponível
  para quando uma fonte real precisar guardar dado extra próprio.

## Impactos

`AppMorador.Application/Eventos` (modelo, porta, serviço), `AppMorador.Infrastructure/Eventos`
(`JflFonteEventos`), `AppMorador.Api/Controllers/EventosController`, mobile
(`src/screens/eventos/`). Não afeta `AppMorador.Jfl` nem `AlarmEventProcessor` — `Ocorrencia`
continua sendo escrita exatamente como antes; `JflFonteEventos` só lê.

## Arquivos afetados

- `backend/src/AppMorador.Application/Eventos/*.cs` (novo)
- `backend/src/AppMorador.Infrastructure/Eventos/JflFonteEventos.cs` (novo)
- `backend/src/AppMorador.Api/Controllers/EventosController.cs` (novo)
- `backend/src/AppMorador.Infrastructure/Identity/AuthServiceCollectionExtensions.cs` (registro DI)
- `backend/src/AppMorador.Infrastructure/Persistence/AppDbContext.cs` (índice composto)

## Como revisar futuramente

Revisitar quando uma segunda fonte real de eventos existir (controle de acesso ou outra): nesse
momento, `EventosServico` precisará agregar+ordenar+paginar entre múltiplas fontes — decisão de
estratégia (paginação por fonte vs. paginação unificada) fica para quando essa fonte for real.