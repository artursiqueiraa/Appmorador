# Sprint 12 — Migração da Integração JFL Active 100 Bus

## Missão

Esta Sprint é obrigatoriamente uma Sprint de migração arquitetural. O objetivo NÃO é reimplementar
o protocolo JFL. O objetivo é migrar a implementação existente (referência
`https://github.com/artursiqueiraa/Integra-o-FL`, projeto "CentralHub") para a arquitetura oficial
de integrações do AppMorador (ADR 0014, estabelecida na Sprint 11), preservando comportamento já
homologado, comunicação com centrais reais, comandos existentes, estabilidade e compatibilidade.

## Fase 1 — Descoberta obrigatória

Investigação completa do repositório legado (`CentralHub`) e da infraestrutura JFL já existente no
AppMorador (`AppMorador.Jfl`, construída na Fase 1/1.1 do projeto, antes de qualquer Sprint
numerada). Dois achados moldaram a estratégia:

1. **O AppMorador já tinha uma migração parcial do mesmo SDK**: handshake, keep-alive e
   recebimento de eventos já portados; os comandos de superusuário (status/armar/PGM/zonas) foram
   deliberadamente removidos na época, com um comentário explícito no código dizendo que pertencem
   a "fases futuras". Esta Sprint completa essa migração.
2. **Achado crítico**: a missão descreve a referência como "comunicação homologada com central
   real" para Armar/Desarmar/PGM/Inibição de zonas — os próprios documentos da referência marcam
   essas funcionalidades como validadas só via simulador, hardware real pendente. Handshake/
   keep-alive/status têm evidência real de hardware físico (fingerprint de painel documentado,
   logs de conexão reais, relato de troubleshooting de campo genuíno). Decisão: tratar a
   referência como fonte de protocolo confiável, não como comportamento a preservar
   byte-a-byte para os comandos de superusuário.

Confirmado com o usuário via 2 perguntas de esclarecimento, ambas respondidas com a opção
recomendada:
1. **Validação via simulador TCP simplificado, escrito do zero** (não o Simulator completo do
   legado) — sem central Active 100 Bus real disponível neste ambiente.
2. **Central (Fase 1, eventos) e Equipamento (Sprint 11, comandos) permanecem cadastros
   separados**, com auto-vínculo de leitura por Número de Série — sem alterar o schema/pipeline de
   eventos já em produção.

## Escopo

1. **Arquitetura** — `Application → IJflProvider → JflProvider → Infraestrutura TCP → Protocolo
   JFL`, reaproveitando integralmente o padrão da ADR 0014. Nenhum Controller conhece detalhes do
   protocolo.
2. **Provider** — `IJflProvider`/`JflProvider`. Diferente do Control iD, nunca disca para o
   equipamento — localiza a sessão TCP já aberta pela central (via `SessionManager`, existente
   desde a Fase 1) e envia o comando dentro dela.
3. **Equipamentos** — reaproveita a entidade `Equipamento` (Sprint 11), sem criar entidade
   específica de JFL. Ip/Porta/Usuario/Senha viraram opcionais (JFL só usa o Número de Série).
4. **Comunicação TCP** — migrados handshake, keep-alive, parser, checksum, framing e tratamento de
   erros já existentes; adicionado o mecanismo de correlação servidor-inicia-comando
   (`SendAndWaitAsync`), que já existia como scaffolding dormente.
5. **Operações** — Armar, Desarmar, Armar Stay, Armar Away, Acionar/Desligar PGM, Inibir/Desinibir
   Zona, Consultar Status (Partições/Zonas/PGMs/Geral reaproveitam a mesma resposta "tela
   monitorar", seção 4.10 do protocolo — não são comandos de fio separados).
6. **Eventos** — recebimento de eventos já estava migrado desde a Fase 1 (`EventoCommandHandler`/
   `AlarmEventProcessor`/`Ocorrencia`/`JflFonteEventos`) e alimenta a Central de Eventos já
   existente — nenhuma mudança necessária, apenas confirmado que continua funcionando.
7. **Dashboard** — Centrais JFL Online/Offline, Partições Armadas/Desarmadas, Problemas ativos
   (rollup persistido em `StatusCentralJfl`, atualizado só por ação explícita do usuário).
8. **Mobile** — telas Centrais JFL, Detalhes da Central (testar conexão, consultar status, armar/
   desarmar por partição, PGMs, inibir/desinibir zonas).
9. **Segurança** — nenhuma senha/token/configuração sensível exposta (JFL não usa nenhuma hoje —
   protocolo não exige credenciais nos comandos migrados).

## Fora de Escopo

IA, Analytics, WebSocket, SignalR, Push Notification, integrações adicionais, novos comandos que
não existam no legado (ex.: comandos com envelope de senha `0x37`, atualização de data/hora).

## Processo Obrigatório

Fase 1 (Descoberta + 2 perguntas de esclarecimento) → respostas do usuário → Fase 2 em etapas
pequenas: protocolo (comandos + parsers) → ativação de `SendAndWaitAsync` → domínio (Equipamento
opcional + StatusCentralJfl) → migration → Provider/mapper → orquestração (auto-vínculo) →
Controller → Dashboard → simulador → validação end-to-end via TCP real → mobile → documentação.

## Critérios de Aceite

Backend compilando; Mobile compilando; Cadastro da Central funcional; Comunicação TCP funcional;
Handshake funcionando; Keep-Alive funcionando; Armar/Desarmar/Armar Stay/Armar Away funcionando;
PGM funcionando; Inibição/Desinibição de Zona funcionando; Eventos chegando na Central de Eventos
(já funcionava, confirmado); Dashboard atualizado; sem regressões; ADR criada; CHANGELOG
atualizado; ROADMAP atualizado; Reviewer aprovando todos os pilares.

## Diretriz de Engenharia

Esta Sprint consolida definitivamente o padrão de integração do AppMorador (ADR 0014), agora
comprovado em dois modelos de conexão opostos (Control iD disca para o equipamento; JFL é discado
pelo equipamento) — ambos resolvidos pela mesma forma de Provider, sem exceção ao domínio.

## Decisões tomadas na Fase 1

Ver ADR 0015 para o detalhamento completo — resumo:
1. Legado tratado como referência de protocolo confiável para os bytes/formato de comando dos
   comandos de superusuário, não como comportamento a preservar byte-a-byte (achado crítico da
   Fase 1: só handshake/keep-alive/status têm evidência real de hardware; Armar/PGM/Zonas são
   auto-declarados como validados só via simulador na própria referência).
2. `IJflProvider` nunca disca para o equipamento — localiza a sessão TCP já aberta via
   `SessionManager` (protocolo invertido em relação ao Control iD).
3. `Equipamento.Ip/Porta/Usuario/SenhaCriptografada` viraram opcionais para acomodar fabricantes
   sem esses campos aplicáveis.
4. `Central` (Fase 1, eventos) e `Equipamento` (Sprint 11, comandos) permanecem cadastros
   separados, com auto-vínculo de leitura por Número de Série — decisão do usuário, confirmada
   como a opção que preserva o pipeline de eventos já em produção sem risco.
5. Validação via simulador TCP simplificado escrito do zero, não o Simulator completo do legado —
   decisão do usuário, dado o tempo/escopo desta Sprint.
