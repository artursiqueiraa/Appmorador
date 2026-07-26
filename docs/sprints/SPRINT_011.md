# Sprint 11 — Migração da Integração Control iD

## Missão

Esta Sprint é obrigatoriamente uma Sprint de migração arquitetural. O objetivo NÃO é desenvolver
uma nova integração. O objetivo é migrar a integração existente (referência
`C:\Users\artur\Documents\Teste-portaria-main1`) para a arquitetura Clean Architecture do
AppMorador, preservando funcionalidades/compatibilidade/comportamento já homologado sempre que
genuinamente existirem, e estabelecendo o padrão OFICIAL de integração para todos os fabricantes
futuros (Intelbras, Hikvision, Dahua, JFL para controle de acesso).

## Fase 1 — Descoberta obrigatória

Antes de qualquer código, investigação completa da referência (releitura do código atual, não de
memória de sessões anteriores). Achado crítico reportado ao usuário: a missão descreve a
integração legada como "funcional, comportamento já homologado", mas a investigação não confirma
isso — dados de seed inteiramente fake, nenhum teste/fixture/log de validação contra hardware
real, um documento interno do próprio legado admitindo ter sido feito sem acesso a ambiente real.
Achados adicionais: não existe mecanismo de importação geral de eventos para reaproveitar (só um
poll de 30s específico de cadastro), bug real no worker de sincronização (cobertura parcial), e uma
falha de segurança real (senha em texto puro, exposta pela API e vazada no log de auditoria).

Decisão de enquadramento (confirmada com o usuário via 2 perguntas de esclarecimento, ambas
respondidas com a opção recomendada): tratar o legado como referência de protocolo (formato de
payload, endpoints, sequência de chamadas), não como comportamento homologado a preservar
byte-a-byte; validar contra um simulador HTTP local (sem equipamento real disponível); construir
o mecanismo de importação de eventos do zero, mínimo e sob demanda (nunca um serviço de
background).

## Escopo

1. **Arquitetura de integração** — `Application → Interfaces → Infrastructure → Provider → API
   Control iD`. Controllers/Entidades/Casos de uso nunca conhecem detalhes do fabricante.
2. **Provider** — `IControlIdProvider`/`ControlIdProvider`. Toda comunicação com o equipamento
   passa exclusivamente por esta interface.
3. **DTOs** — internos (Application/ControlId) e de wire-format (Infrastructure/ControlId)
   totalmente separados, com um mapper como única fronteira de tradução.
4. **Cadastro de Equipamentos** — Nome, Modelo, Fabricante, IP, Porta, Usuário, Senha,
   Identificador, Status, Última sincronização. Pertence obrigatoriamente a uma Propriedade.
5. **Teste de comunicação** — teste de conexão, validação de autenticação, consulta de
   versão/informações, usando comunicação real (HTTP de verdade contra o simulador local).
6. **Sincronização** — manual (ação do usuário), de Moradores/Credenciais/Permissões, usando o
   domínio já existente (Sprints 6/7) — nunca um domínio novo.
7. **Importação de Eventos** — mapeada para o domínio de Eventos já implementado (Sprint 3) via
   uma segunda implementação real de `IFonteEventos` — nunca uma estrutura paralela.
8. **Dashboard** — Equipamentos Online/Offline, Última sincronização, Último evento recebido.
9. **Mobile** — telas Equipamentos, Detalhes do Equipamento (teste de conexão, informações,
   sincronização manual por domínio, importar eventos).
10. **Segurança** — senha do equipamento nunca exposta (nem cifrada) na API; armazenamento
    cifrado em repouso via Data Protection API.

## Fora de Escopo

WebSocket, SignalR, atualização automática, Push Notification, IA, Analytics, OCR, Reconhecimento
Facial, Captura Biométrica, controle remoto de portas.

## Processo Obrigatório

Fase 1 (Descoberta + 2 perguntas de esclarecimento) → aprovação implícita (respostas às perguntas)
→ Fase 2 em etapas pequenas: entidades → repositórios/migration → CRUD de Equipamento →
Provider/DTOs/mapper → orquestração de integração → evento/Central de Eventos → cascade/DI →
Controller → Dashboard → simulador → validação end-to-end via curl → mobile → documentação.

## Critérios de Aceite

Backend compilando; Mobile compilando; CRUD de Equipamentos funcional; Teste de conexão/consulta
de informações/sincronização/importação de eventos validados via comunicação HTTP real (simulador
local); Dashboard com dado real; Soft delete funcionando; Sem regressões; ADR criada; CHANGELOG
atualizado; ROADMAP atualizado; Reviewer aprovando todos os pilares.

## Diretriz de Engenharia

Esta Sprint estabelece o padrão OFICIAL de integração para o AppMorador. Toda integração futura de
hardware (JFL, Intelbras, Hikvision, Dahua) deve seguir exatamente esta arquitetura. Nenhum
fabricante pode alterar o domínio — todos implementam apenas um Provider próprio.

## Decisões tomadas na Fase 1

1. **Legado tratado como referência de protocolo, não como comportamento homologado** — a
   investigação encontrou evidência definitiva de que o legado nunca foi validado contra hardware
   real (dados de seed fake, nenhum teste, documentação interna admitindo a limitação).
2. **Validação via simulador HTTP local** (`backend/tools/ControlIdSimulator`) — sem equipamento
   Control iD real acessível neste ambiente; pendência de validação contra hardware físico
   registrada explicitamente em `DIVIDA_TECNICA.md`.
3. **Importação de eventos construída do zero, poller mínimo sob demanda** — o legado não tinha
   mecanismo de importação geral de eventos para reaproveitar (só um poll de 30s específico de
   cadastro de tag, escopo completamente diferente).

Ver ADR 0014 para o detalhamento completo da arquitetura decidida.
