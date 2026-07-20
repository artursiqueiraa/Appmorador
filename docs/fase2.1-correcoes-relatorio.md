# Relatório — Fase 2.1: correções pontuais (CancellationToken, timeout, IHttpClientFactory, SaveChanges)

Data: 2026-07-18
Status: **implementado e compilando. Nenhuma migration foi tocada (nenhuma entidade/schema mudou).**

## Arquivos alterados

| Arquivo | Motivo |
|---|---|
| `AppMorador.Domain/Snapshots/SnapshotRequest.cs` | Adicionado `CancellationToken` como propriedade `required` — consolida no request em vez de ser parâmetro solto, seguindo o próprio princípio de design do `SnapshotRequest` ("evitar parâmetros espalhados"). |
| `AppMorador.Domain/Snapshots/ISnapshotProvider.cs` | `CaptureAsync` perdeu o parâmetro `CancellationToken` separado — agora só recebe `SnapshotRequest` (o token vem de `request.CancellationToken`). |
| `AppMorador.Infrastructure/Snapshots/SnapshotStorageOptions.cs` | Novo campo `TimeoutSeconds` (default 5) — substitui o `TimeSpan.FromSeconds(5)` hardcoded que existia em dois providers. |
| `AppMorador.Infrastructure/Snapshots/SnapshotHttpClientNames.cs` | **Novo.** Constante com o nome do client nomeado registrado via `IHttpClientFactory`, evita string mágica duplicada em 3 arquivos. |
| `AppMorador.Infrastructure/Snapshots/DigestAuthHttpSender.cs` | **Novo.** Calcula Digest/Basic manualmente por requisição (ver motivo abaixo) — substitui o uso de `HttpClientHandler.Credentials`/`CredentialCache`. |
| `AppMorador.Infrastructure/Snapshots/CgiSnapshotProviderBase.cs` | Recebe `IHttpClientFactory` e `SnapshotStorageOptions` por injeção; usa `_httpClientFactory.CreateClient(...)` em vez de `new HttpClient(handler)`; timeout vem de `_options.TimeoutSeconds`; delega autenticação ao `DigestAuthHttpSender`. |
| `AppMorador.Infrastructure/Snapshots/DahuaCgiSnapshotProvider.cs` / `IntelbrasCgiSnapshotProvider.cs` | Construtores repassando `IHttpClientFactory`/`SnapshotStorageOptions` para a base. |
| `AppMorador.Infrastructure/Snapshots/HikvisionIsapiSnapshotProvider.cs` | Mesma mudança da base CGI, adaptada (não herda da base porque o endpoint é diferente): `IHttpClientFactory` + `SnapshotStorageOptions` injetados, sem `new HttpClient()`, autenticação via `DigestAuthHttpSender`. |
| `AppMorador.Infrastructure/Snapshots/SnapshotCaptureService.cs` | Ao montar o `SnapshotRequest`, preenche `CancellationToken = cancellationToken`; chamada a `provider.CaptureAsync(request)` sem o segundo parâmetro. |
| `AppMorador.Infrastructure/Snapshots/SnapshotServiceCollectionExtensions.cs` | Registra `services.AddHttpClient(SnapshotHttpClientNames.Default)` (client nomeado, sem `BaseAddress`/credenciais fixadas no registro). |
| `AppMorador.Infrastructure/AppMorador.Infrastructure.csproj` | Novo pacote `Microsoft.Extensions.Http` (necessário para `IHttpClientFactory`/`AddHttpClient`). |
| `AppMorador.Api/Program.cs` | `options.TimeoutSeconds = builder.Configuration.GetValue("Snapshots:TimeoutSeconds", 5)`. |
| `AppMorador.Api/appsettings.json` | `Snapshots:TimeoutSeconds: 5`. |
| `AppMorador.Infrastructure/Jfl/AlarmEventProcessor.cs` | Só comentários novos explicando por que os 3 `SaveChangesAsync` não podem ser eliminados (nenhuma lógica mudou — ver item 4). |

## Motivo de cada ponto

**1. CancellationToken no SnapshotRequest**: já existia um parâmetro `CancellationToken` solto em `CaptureAsync`, ao lado do `SnapshotRequest` — isso contrariava o próprio motivo de o `SnapshotRequest` existir. Agora o token é uma propriedade dele e se propaga só através do objeto: `SnapshotCaptureService` preenche, os providers leem de `request.CancellationToken`, e o `DigestAuthHttpSender` recebe esse valor.

**2. Timeout configurável**: `TimeSpan.FromSeconds(5)` estava hardcoded, duplicado em `CgiSnapshotProviderBase` e `HikvisionIsapiSnapshotProvider`. Agora vem de `SnapshotStorageOptions.TimeoutSeconds`, configurável via `Snapshots:TimeoutSeconds` no `appsettings.json`.

**3. IHttpClientFactory obrigatório**: a implementação anterior fazia `new HttpClient(handler)` a cada captura, com um `HttpClientHandler` configurado com `Credentials`/`PreAuthenticate` específicos daquele DVR. Isso não é compatível com `IHttpClientFactory` diretamente — a factory gerencia/recicla o handler internamente e o compartilha entre chamadas, então não dá para configurar `Credentials` por chamada com credenciais diferentes por DVR (a central pode ter dezenas de DVRs, cada um com usuário/senha próprios). A correção troca a abordagem: os providers agora pedem o `HttpClient` via `_httpClientFactory.CreateClient(...)` (nunca `new HttpClient()`) e a autenticação Digest/Basic é **calculada manualmente por requisição** em `DigestAuthHttpSender` (envia sem auth, lê o desafio `WWW-Authenticate` do 401, monta o header `Authorization` — Digest com MD5 conforme RFC 2617, ou Basic — e reenvia). Isso preserva o comportamento (aceita o esquema que o DVR pedir) sem depender de estado por-DVR no handler.

**4. SaveChanges**: analisei os 3 `SaveChangesAsync` de `AlarmEventProcessor` e **nenhum pode ser eliminado sem mudar comportamento**:
- **#1** (criar a `Occurrence`) precisa acontecer antes da tentativa de snapshot — é a garantia de confiabilidade da Fase 1 ("a ocorrência deve existir mesmo que o DVR esteja offline"). Juntar com #2 atrasaria a existência da `Occurrence` até depois de uma chamada HTTP externa (até `TimeoutSeconds`), contradizendo essa garantia.
- **#2** (gravar `ImagePath`) só existe porque o resultado do snapshot só é conhecido depois de uma chamada de rede que roda deliberadamente depois de #1 — não há como saber o `ImagePath` antes de criar a ocorrência sem atrasá-la.
- **#3** (gravar `AlarmEventLog`, no `finally`) é intencionalmente independente do caminho de negócio (Occurrence/snapshot) — é gravado mesmo se esse caminho falhar. Juntar este save com #1/#2 acoplaria a trilha de auditoria ao sucesso de um código não relacionado a ela, o que reduziria a robustez que a Fase 1.1 pediu explicitamente.

Documentei essa análise como comentário em cada um dos três pontos no código, e mantive a estrutura como estava — nenhuma mudança de comportamento.

## Build

`dotnet build` da solução inteira: **0 erros, 0 avisos.**
