# Sprint 13 — Camada Operacional Unificada

## Missão

Sprint de consolidação arquitetural — não integra novos fabricantes, não cria novos protocolos,
não altera regras de negócio já homologadas. Consolida os dados já produzidos pelas integrações
Control iD (Sprint 11) e JFL (Sprint 12) numa única camada operacional reutilizável por
Dashboard, Mobile, Web e APIs futuras. Nenhuma interface pode consultar Providers diretamente.

## Escopo

1. **Estado Operacional** — agregado que consolida saúde de equipamentos, comunicação, eventos
   recentes, alarmes ativos, sincronizações e últimos erros de uma Propriedade.
2. **Snapshot Operacional** — rollup persistido (1:1 por Propriedade, upsert): data/hora, online/
   offline, última comunicação, quantidade de eventos, alarmes ativos, falhas detectadas.
   Dashboard consome exclusivamente este Snapshot.
3. **Estado Bruto** — cada Provider continua devolvendo só dado bruto (Equipamento.Status,
   StatusCentralJfl); toda classificação acontece exclusivamente no domínio.
4. **Classificação Operacional** — serviço único transformando Estado Bruto em Saudável/Atenção/
   Crítico/Offline.
5. **Saúde da Propriedade** — indicador consolidado considerando equipamentos, comunicação,
   eventos críticos e falhas.
6. **Central de Eventos** — evoluída (nunca um novo domínio): filtros por Propriedade/Equipamento/
   Fabricante/Origem/Categoria/Severidade/Período.
7. **Timeline Operacional** — reaproveita a Central de Eventos já existente, ordenada por data/
   hora, com múltiplas origens (Control iD, JFL).
8. **Dashboard** — atualizado com Saúde da Propriedade, Equipamentos Online/Offline, Eventos Hoje,
   Alarmes Ativos, Última Atualização, Última Comunicação — reutilizando componentes existentes,
   sem redesign.
9. **Mobile** — telas Central Operacional (consulta + atualização manual do snapshot + link para
   Timeline/Central de Eventos) e Saúde da Propriedade (drill-down por equipamento). Nenhum
   comando remoto.
10. **Arquitetura** — fluxo obrigatório Provider → Estado Bruto → Classificador Operacional →
    Snapshot Operacional → Dashboard/Mobile. Nenhuma interface acessa Providers diretamente.
11. **Performance** — Snapshot cacheado (upsert), nunca recalculado por polling; geração é pura
    agregação de dado já persistido (nunca chama Provider), então mesmo a leitura pode gerar sob
    demanda sem custo real.

## Fora de Escopo

SignalR, WebSocket, Push Notification, Hikvision, Intelbras, Dahua, Analytics, IA, controle
remoto automático.

## Processo Obrigatório

Executado em etapas pequenas: domínio (EstadoOperacional/SnapshotOperacional) → migration →
Classificador Operacional → SnapshotOperacionalServico → integração no Dashboard → filtros da
Central de Eventos → Controller → validação via curl (classificação Saudável/Atenção/Offline
real, filtros, regressão Control iD/JFL) → mobile → documentação.

## Critérios de Aceite

Backend compilando; Mobile compilando; Estado Operacional funcional; Snapshot Operacional
funcional; Saúde da Propriedade funcional; Dashboard consumindo apenas o Snapshot para os campos
consolidados; Timeline operacional funcional (via Central de Eventos); Central de Eventos com
filtros novos; nenhuma tela consultando Providers diretamente; sem regressões; ADR 0016 criada;
CHANGELOG/ROADMAP/DIVIDA_TECNICA atualizados; Reviewer aprovando todos os pilares.

## Diretriz de Engenharia

A partir desta Sprint, toda informação operacional exposta ao Dashboard/Mobile/APIs futuras segue
obrigatoriamente o fluxo Provider → Estado Bruto → Classificador Operacional → Snapshot
Operacional. Vinculante para toda integração futura (Hikvision, Intelbras, Dahua, novos
fabricantes) — nenhuma pode expor um Provider diretamente a uma interface.

## Decisões desta Sprint

Ver ADR 0016 para o detalhamento completo — resumo:
1. Estado Bruto nunca chama um Provider — é lido exclusivamente de dados que as Sprints 11/12 já
   persistem via ações explícitas do usuário. Isso torna a geração do Snapshot uma operação
   barata (pura agregação de banco), permitindo gerá-lo sob demanda numa simples leitura.
2. Classificador Operacional centraliza as 4 regras de estado (por equipamento e por
   Propriedade), com prioridade explícita para alarmes ativos sobre qualquer outra condição.
3. Snapshot Operacional é um rollup 1:1 (upsert), não um histórico — mesmo padrão já usado por
   StatusCentralJfl (Sprint 12).
4. Timeline Operacional reaproveita a Central de Eventos já existente — eventos de transição de
   conectividade ("Equipamento Offline/Reconectado") ficam como dívida técnica registrada, não
   implementados, para não recriar um domínio de eventos (proibido pela missão).
5. Dashboard mantém todos os campos/componentes específicos de fabricante já existentes
   (Sprints 11/12) intactos — os campos novos do Snapshot são aditivos.
