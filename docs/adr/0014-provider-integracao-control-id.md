# ADR 0014 — Arquitetura oficial de integração de fabricante: Provider Control iD

**Data**: 2026-07-22

## Contexto

A Sprint 11 pediu a migração da integração Control iD existente na referência
`Teste-portaria-main1` — não uma reescrita, uma migração. A missão foi explícita: esta Sprint
estabelece o padrão OFICIAL de integração do AppMorador, que todo fabricante futuro (Intelbras,
Hikvision, Dahua, JFL para controle de acesso) deve seguir sem exceção.

A Fase 1 (Descoberta obrigatória, antes de qualquer código) investigou a referência e encontrou
um achado crítico: apesar de a missão descrever o legado como "integração funcional, comportamento
já homologado", a investigação (releitura completa do código, não da memória de sessões
anteriores) não confirma isso — dados de seed são inteiramente fake (IPs `192.168.1.100+`,
credenciais `admin`/`admin`, `FaceSynced = true` atribuído sem nenhuma chamada real ao
dispositivo), não há nenhum teste/fixture/log indicando validação contra hardware físico, e o
próprio documento interno do legado admite ter sido feito sem acesso a ambiente real. Além disso:
não existe mecanismo de importação geral de eventos (só um poll de 30s específico para captura de
tag durante cadastro), há um bug real no worker de sincronização (só 2 de ~6 tipos de ação
implementados), e há uma falha de segurança real (senha do dispositivo em texto puro, devolvida
crua pela API e vazada no diff de auditoria).

**Decisão de enquadramento**: o legado foi tratado como **referência de protocolo** (formato de
payload, sequência de chamadas, lista de endpoints) — nunca como "comportamento homologado que
precisa ser preservado byte-a-byte". Nenhum dos três problemas acima (dado fake, bug de fila,
senha em texto puro) foi replicado.

Sem um equipamento Control iD real disponível neste ambiente, o usuário confirmou (Fase 1,
pergunta de esclarecimento): construir e validar contra um simulador HTTP local, não contra
hardware físico — pendência explícita para uma validação futura contra equipamento real.

## Decisão 1 — Camadas: Application (porta) → Infrastructure (Provider) → API do fabricante

```
Controllers/Entidades/Casos de uso (nunca sabem o fabricante)
        │
        ▼
Application/Equipamentos (CRUD + orquestração de integração)
        │  usa a porta, nunca a implementação
        ▼
Application/ControlId/IControlIdProvider  (porta — mesmo espírito de IFonteEventos, ADR 0006)
        │
        ▼
Infrastructure/ControlId/ControlIdProvider  (única implementação real, HttpClient)
        │
        ▼
API REST do Control iD (login.fcgi, load_objects.fcgi, create_objects.fcgi, ...)
```

`IControlIdProvider` vive em `Application/ControlId/` (não em `Domain/Repositories/`, que é
reservado para portas de persistência) — mesmo precedente de `IFonteEventos` (ADR 0006):
uma porta para uma integração externa plugável, não para uma entidade EF Core.

Nenhum Controller, entidade de domínio ou caso de uso do domínio principal (Equipamento,
Morador, Credencial, PermissaoAcesso) importa qualquer tipo do namespace `Infrastructure.ControlId`
ou conhece qualquer detalhe do protocolo REST do Control iD (`login.fcgi`, `session`, etc.).

## Decisão 2 — DTOs internos e DTOs de wire-format nunca se misturam

`Application/ControlId/Dtos.cs` define os DTOs internos (`ConexaoEquipamento`,
`ResultadoTesteConexao`, `InformacoesEquipamento`, `MoradorParaSincronizar`,
`CredencialParaSincronizar`, `PermissaoParaSincronizar`, `ResultadoSincronizacao`,
`EventoImportado`) — nunca referenciam formato de payload do Control iD.

`Infrastructure/ControlId/ControlIdWireDtos.cs` define os DTOs de wire-format
(`ControlIdLoginRequest/Response`, `ControlIdCreateObjectsRequest/Response`,
`ControlIdLoadObjectsRequest`, `ControlIdAccessLogEntry`, `ControlIdLoadAccessLogsResponse`) —
internos ao namespace `Infrastructure.ControlId`, nunca vazam para fora dele.

`Infrastructure/ControlId/ControlIdMapper.cs` é a única fronteira de tradução entre os dois
mundos — nenhum outro ponto do sistema monta ou lê um payload Control iD diretamente.

## Decisão 3 — Equipamento é uma entidade genérica, nunca específica de fabricante

`Domain/Entities/Equipamento.cs` pertence direto à `Propriedade` (mesmo padrão de
`PontoAcesso`/`Vaga`/`Visitante`) e tem campos deliberadamente genéricos: `Nome`, `Modelo`,
`Fabricante` (enum `ControlId`/`Jfl`/`Intelbras`/`Hikvision`/`Dahua`/`Outro`), `Ip`, `Porta`,
`Usuario`, `SenhaCriptografada`, `Identificador`, `Status` (`Desconhecido`/`Online`/`Offline`),
`UltimaSincronizacaoUtc`. Nenhum campo é exclusivo de um fabricante — um futuro Provider
Intelbras/Hikvision/Dahua reaproveita a mesma entidade, só implementando `IIntelbrasProvider`/
equivalente e resolvendo o Provider certo pelo campo `Fabricante` (ver
`EquipamentoIntegracaoServico.ResolverProvider`).

## Decisão 4 — Senha do equipamento nunca em texto puro (corrige a falha de segurança do legado)

`Equipamento.SenhaCriptografada` é cifrada via `Microsoft.AspNetCore.DataProtection`
(`IDataProtectionProvider`/`IDataProtector`, "purpose" fixo `AppMorador.Equipamentos.Senha`) —
escolhida por ser nativa do .NET e gerenciar chaves automaticamente, sem exigir configuração
manual de AES/rotação de chave. Porta `ICriptografiaSimetrica` (Application) / implementação
`DataProtectionCriptografiaSimetrica` (Infrastructure) — mesmo padrão arquitetural de
`IPasswordHasher`/`BCryptPasswordHasher`, mas reversível (o Provider real precisa do texto puro
para autenticar no equipamento; hash de senha de usuário é propositalmente irreversível, daqui a
distinção de portas). `EquipamentoResponse` nunca inclui a senha, nem cifrada.

## Decisão 5 — Sincronização manual reaproveita o domínio existente, nunca cria um paralelo

`EquipamentoIntegracaoServico.Sincronizar{Moradores,Credenciais,Permissoes}Async` carregam dado
real via `IMoradorRepositorio`/`ICredencialRepositorio`/`IPermissaoAcessoRepositorio` (já
existentes desde as Sprints 6/7) e traduzem para os DTOs internos de sincronização — nenhuma
entidade nova de "usuário do Control iD" foi criada. `Credencial.Valor`/tag física não existe no
domínio hoje (Sprint 7 não modelou isso) — sincronização de credencial hoje envia `Valor: null`,
registrado como dívida técnica.

## Decisão 6 — Importação de eventos alimenta a Central de Eventos já existente, nunca uma estrutura paralela

O legado não tinha mecanismo de importação geral de eventos para reaproveitar (achado da Fase 1).
Construído do zero, mínimo, sob demanda: `EventoEquipamento` (nova entidade, auditoria pura — sem
soft delete/query filter, mesmo espírito de `Ocorrencia`) + `EquipamentoFonteEventos : IFonteEventos`
(Infrastructure/Eventos), segunda implementação real da porta já existente desde a Sprint 3
(`JflFonteEventos` era a única até aqui) + um valor aditivo `ControlId` em `OrigemEvento`. A
Central de Eventos (`EventosServico.GetEventosAsync`) passou a agregar múltiplas fontes reais
(antes assumia uma só) — consultadas **sequencialmente**, nunca via `Task.WhenAll`: as fontes
compartilham a mesma instância `Scoped` de `AppDbContext`, que não é thread-safe para operações
concorrentes (bug encontrado e corrigido durante a validação desta Sprint). Nenhum
WebSocket/SignalR/atualização automática — importação é sempre uma ação explícita do usuário
("importar eventos"), nunca um poller em background.

## Decisão 7 — Validação contra simulador HTTP local, não hardware real (pendência registrada)

Sem equipamento Control iD real acessível neste ambiente (confirmado com o usuário na Fase 1),
`ControlIdProvider` foi validado via `backend/tools/ControlIdSimulator` — um projeto ASP.NET Core
minimal-API descartável (fora do `AppMorador.sln` de produção) que implementa os mesmos endpoints
reais (`login.fcgi`, `system_information.fcgi`, `create_objects.fcgi`, `load_objects.fcgi`) com
respostas plausíveis. Toda comunicação em `ControlIdProvider` é HTTP real (via `HttpClient`/
`IHttpClientFactory`) contra esse simulador — não é um mock em memória do `IControlIdProvider`.
Isso comprova a comunicação de rede/protocolo/tratamento de erro, mas **não** substitui validação
contra hardware físico real, que fica como pendência explícita registrada em
`docs/DIVIDA_TECNICA.md`.

## Diretriz para integrações futuras (Intelbras, Hikvision, Dahua, JFL para controle de acesso)

Nenhum fabricante futuro pode:
- Adicionar um campo específico de fabricante em `Equipamento` (usar `Identificador`/`Modelo`
  genéricos, ou — se genuinamente necessário — um relacionamento próprio, nunca um campo
  condicional).
- Expor um tipo de wire-format fora do seu próprio namespace `Infrastructure.<Fabricante>`.
- Ser chamado diretamente por um Controller ou serviço de domínio — sempre através de uma porta
  `I<Fabricante>Provider` em `Application/<Fabricante>/`, resolvida por
  `EquipamentoIntegracaoServico` (ou um sucessor que generalize a resolução por `Fabricante`
  conforme mais Providers existirem de fato).
- Introduzir polling/WebSocket/SignalR/atualização automática — toda ação de integração é
  disparada por um usuário, mesmo padrão desta Sprint.

## Impactos

`Domain/Entities/{Equipamento,FabricanteEquipamento,StatusEquipamento,EventoEquipamento}.cs`;
`Domain/Repositories/{IEquipamentoRepositorio,IEventoEquipamentoRepositorio}.cs`;
`Infrastructure/Persistence/{EquipamentoRepositorio,EventoEquipamentoRepositorio,AppDbContext}.cs`;
`Application/Equipamentos/*.cs` (CRUD + `EquipamentoIntegracaoServico` + `ICriptografiaSimetrica`);
`Application/ControlId/*.cs` (porta + DTOs internos); `Infrastructure/ControlId/*.cs` (Provider +
DTOs de wire-format + mapper); `Infrastructure/Identity/DataProtectionCriptografiaSimetrica.cs`;
`Infrastructure/Eventos/EquipamentoFonteEventos.cs`; `Application/Eventos/{OrigemEvento,
EventosServico}.cs` (agregação multi-fonte); `Api/Controllers/EquipamentosController.cs`;
`Application/Dashboard/{DashboardResponse,DashboardServico}.cs`; `Api/Program.cs`
(`AddDataProtection`); `backend/tools/ControlIdSimulator/` (ferramenta de teste, fora do domínio).

## Como revisar futuramente

Ao implementar o próximo fabricante (Intelbras, Hikvision, Dahua, ou JFL para controle de acesso),
copiar exatamente esta estrutura: `Application/<Fabricante>/I<Fabricante>Provider.cs` + DTOs
internos, `Infrastructure/<Fabricante>/<Fabricante>Provider.cs` + DTOs de wire-format + mapper,
resolução por `Equipamento.Fabricante` em `EquipamentoIntegracaoServico`. Se um segundo fabricante
tornar a resolução condicional (`if fabricante == X`) repetitiva, extrair um `IEnumerable<I...
Provider>` resolvido por fabricante — mas só quando esse segundo Provider existir de fato, nunca
generalizar antecipadamente para um caso hipotético.
