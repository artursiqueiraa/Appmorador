# Sprint 15 — Integração Intelbras: Prova Definitiva da Arquitetura

## Missão

O objetivo principal **não** é integrar Intelbras — é comprovar que a arquitetura construída nas
Sprints 11-14 é genérica, reutilizável e extensível. Intelbras é a prova prática. Expectativa:
nenhuma alteração nas camadas já existentes. Qualquer alteração realmente indispensável deve
corrigir uma limitação arquitetural real, beneficiar todos os fabricantes, ser documentada em ADR
e nunca ser exclusiva da Intelbras.

## Fase 0 — Auditoria da Arquitetura

Ver ADR 0018 para a tabela completa das 10 camadas. Resultado: "nenhuma alteração" em 9 das 10 —
a única exceção prevista (Central de Eventos, ganhando um valor aditivo de enum + uma nova fonte
via DI) é a exercitação do próprio mecanismo de extensão desenhado desde a Sprint 3/13, não uma
violação.

## Fase 1 — Descoberta

**Modelo**: Intelbras AMT 8000, usado como referência arquitetural. **Protocolo**: API HTTP local
(mesmo padrão dial-out do Control iD, ADR 0014), com vocabulário de comando de alarme (Armar/
Desarmar/Status/Eventos, ADR 0015) — decisão consciente por não haver documentação oficial pública
nem uma referência já investigada neste projeto para um protocolo TCP proprietário Intelbras
(diferente do que havia para JFL). **Operações**: Testar conexão, Consultar Status, Armar,
Desarmar, Importar Eventos. PGM/Inibição de zona: não implementados (backlog).

## Escopo

1. `IIntelbrasProvider`/`IntelbrasProvider` (Application/Infrastructure) — ADR 0014.
2. `Equipamento` reaproveitado integralmente — `Fabricante.Intelbras` já existia desde a Sprint 11.
3. Operações mínimas implementadas: Arme, Desarme, Consulta de Status, Consulta/Importação de
   Eventos.
4. Eventos via `IntelbrasFonteEventos : IFonteEventos`, reaproveitando `EventoEquipamento`
   (Sprint 11) — nenhuma pipeline paralela.
5. `SnapshotOperacionalServico` consome o novo Provider automaticamente — zero alteração.
6. `OperacionalHub` publica eventos Intelbras automaticamente — zero alteração (validado com
   cliente SignalR real).
7. Dashboard: Intelbras aparece via `quantidadeEquipamentosOnline/Offline` e `saude` já genéricos —
   zero alteração funcional/campo novo.
8. Mobile: `CentraisIntelbrasScreen`/`DetalhesCentralIntelbrasScreen` — Dashboard/Timeline/Central
   Operacional/Saúde da Propriedade 100% reaproveitados.
9. Achado real corrigido: `EquipamentoFonteEventos` não era escopado por `Fabricante` — corrigido
   genericamente (ver ADR 0018, Decisão 5).

## Fora de Escopo

Push Notification, Analytics, IA, novos comandos além do mínimo (PGM/Inibição de zona), alterações
específicas no domínio, adaptações específicas para Intelbras.

## Processo Obrigatório

Executado em etapas pequenas: porta+Provider HTTP → build → ComandoServico+Controller → build →
FonteEventos+enum aditivo → build → simulador HTTP → DI → build + validação via curl (cadastro,
conexão, armar/desarmar com mudança de estado real, eventos, Central de Eventos com filtros,
Snapshot, Dashboard) → regressão Control iD/JFL (encontrou e corrigiu o achado da Decisão 5) →
SignalR com cliente real → mobile (2 telas + navegação + exclusão da lista genérica) → build +
validação → documentação.

## Critérios de Aceite

Backend compilando; Mobile compilando; Auditoria concluída; Equipamento Intelbras definido
(AMT 8000); Protocolo analisado; Provider implementado; Comunicação funcional (HTTP real contra o
simulador); Arme/Desarme funcionando com mudança de estado real; Eventos chegando na Central de
Eventos (validado com filtros); Snapshot Operacional funcionando; Dashboard funcionando; Timeline
funcionando; Mobile funcionando; SignalR funcionando (validado com cliente real); nenhuma
regressão em Control iD/JFL (confirmada após a correção da Decisão 5); nenhuma adaptação
específica para Intelbras (a única alteração fora do previsto — `EquipamentoFonteEventos` — é uma
correção genérica, documentada e aprovada em ADR); ADR 0018 criada; CHANGELOG/ROADMAP/
DIVIDA_TECNICA atualizados; Reviewer aprovando todos os 8 pilares.

## Diretriz de Engenharia

Esta Sprint confirma: qualquer novo fabricante (Hikvision, Dahua, ou outro) segue exatamente o
mesmo processo — Fase 0 de auditoria, Fase 1 de descoberta honesta sobre o que é real vs. simulado,
implementação via Provider+serviço+Controller novos, reaproveitamento de `EventoEquipamento`/
`IFonteEventos` sempre escopado ao próprio fabricante desde o primeiro dia (lição da Decisão 5).
