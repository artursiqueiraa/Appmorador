# Plano técnico — MVP "Alerta com Snapshot no Disparo" (v2, revisado)

Data: 2026-07-18
Status: **Fase 1 (confiabilidade do evento) implementada em 2026-07-18, revisada na Fase 1.1 (ver
`docs/fase1-confiabilidade-relatorio.md` e `docs/fase1.1-revisao-relatorio.md`). Fases seguintes
(snapshot, storage, notificação) ainda aguardam aprovação, não implementadas.**

> v2 substitui integralmente a v1. A v1 estava superdimensionada (pipeline assíncrono, fila, object
> storage em nuvem, Polly) para um MVP que só precisa mostrar uma imagem quando uma zona dispara.
> Esta versão corta tudo que não é estritamente necessário para isso.

Escopo: **backend + API apenas** (confirmado antes), sem tela mobile/admin.

Toda decisão abaixo foi filtrada pela pergunta: *"isso é realmente necessário para exibir uma imagem
quando uma zona dispara?"* — o que não passou nesse teste foi removido.

---

## 1. O que muda da v1 para a v2

| v1 (rejeitada) | v2 (este plano) |
|---|---|
| Fila em memória (`Channel<T>`) + `BackgroundService` dedicado | **Nada.** Tudo acontece dentro da mesma chamada síncrona que trata o evento |
| Notificação dispara antes do snapshot (para não esperar o DVR) | Notificação é o **último passo**, depois do snapshot (com timeout curto, então a espera é pequena e previsível) |
| `SnapshotStatus.Pending` como estado intermediário persistido | **Removido** — como o fluxo é síncrono, ao gravar a ocorrência já se sabe se o snapshot deu certo (`Ready`/`Failed`/`NotApplicable`) |
| `IObjectStorage` com S3/Cloudflare R2/Backblaze B2/Wasabi + signed URL | **Disco local** (`IImageStorage`, `File.WriteAllBytesAsync` numa pasta configurável) |
| Polly (retry + timeout policy) | **Uma tentativa só**, com `HttpClient.Timeout` curto (ex: 5s) e `CancellationToken`. Sem retry |
| `AlarmEventLog` (entidade separada de auditoria) | **Removido** — os poucos campos úteis (código, zona) já ficam na própria `Occurrence` |
| `AlarmDebounceOptions` via `IOptions<T>` | Uma constante/valor de config simples, sem classe de opções dedicada |
| Projeto de biblioteca de protocolo JFL como peça "para o futuro" | Mantido, mas só porque é a única forma de falar TCP com o painel — não é feature extra, é o transporte |

---

## 2. Fluxo (síncrono, ponta a ponta)

```
Evento JFL chega (pacote 0x24, já dentro da sessão TCP)
  → responde ACK ao painel (obrigatório pelo protocolo, senão ele reenvia e derruba a conexão)
  → identifica a Zona (via NumeroSerie da sessão + número da zona no payload)
  → é um código de disparo real? (checagem simples, ver §3) — se não, loga e para
  → já existe ocorrência recente da mesma zona? (checagem simples, ver §3) — se sim, loga e para
  → busca a Câmera vinculada à zona (ZoneCameraLink) — se não houver, pula pro fim com SnapshotStatus=NotApplicable
  → chama ISnapshotProvider.GetSnapshotAsync (uma tentativa, timeout curto)
  → sucesso: salva o JPEG em disco local (IImageStorage) → SnapshotStatus=Ready
  → falha/timeout: SnapshotStatus=Failed (não impede os próximos passos)
  → cria a Occurrence (já com o status final do snapshot)
  → notifica (INotificationSender, implementação de log por enquanto)
  → fim
```

Tudo isso é uma única cadeia de `await`s dentro do handler do evento — sem fila, sem worker, sem
"depois eu processo". Se o snapshot demorar, o evento como um todo demora um pouco mais — aceitável
porque o timeout é curto (poucos segundos) e é exatamente o que você pediu.

---

## 3. Filtro e deduplicação — mantidos, mas simples

Estes dois pontos continuam existindo (não são "infraestrutura futura", são regra de negócio para não
gerar ocorrência/snapshot para arme, desarme ou a central retransmitindo o mesmo disparo):

- **Filtro**: uma lista fixa (constante no código, sem tabela de configuração) de códigos Contact ID
  que **não** geram ocorrência (arme, desarme, teste periódico). Qualquer código fora dessa lista
  segue como disparo real (fail-open — já flagueado antes, mantenho por segurança). É um `switch`/
  `HashSet<string>` simples, não uma "camada de catálogo".
- **Deduplicação**: uma única query — "existe uma `Occurrence` para esta `ZoneId` criada nos últimos
  N segundos (constante, ex. 30)?". Se sim, ignora o evento novo. Sem cache distribuído, sem opções
  configuráveis via `appsettings` — um `int` fixo no código (fácil de tornar configurável depois, se
  precisar).

Se você preferir remover isso também para a primeira versão (aceitar ocorrências duplicadas por ora),
diga — mas por padrão mantenho, porque sem isso um único disparo físico vira várias ocorrências.

---

## 4. Modelo de domínio (enxuto)

```
Site            (Id, Nome)
AlarmPanel      (Id, SiteId, NumeroSerie, Nome)
Zone            (Id, AlarmPanelId, Numero, Nome)
Dvr             (Id, SiteId, Fabricante enum{Intelbras,Dahua,Hikvision}, Ip, Porta, Username, Password)
Camera          (Id, SiteId, DvrId, Canal, Nome)
ZoneCameraLink  (Id, ZoneId, CameraId)          — entidade de vínculo, não FK cravada na Zona
Occurrence      (Id, SiteId, ZoneId, ContactIdCode, CreatedAtUtc,
                  CameraId?, SnapshotPath?, SnapshotStatus enum{Ready,Failed,NotApplicable})
```

Removido da v1: `AlarmEventLog` (não é necessário para o objetivo do MVP — se um dia precisar de
trilha de auditoria completa do painel, é um acréscimo isolado, não um pré-requisito agora).

`SnapshotPath` é um caminho relativo local (ex.: `snapshots/{occurrenceId}.jpg`), não uma URL assinada
de nuvem.

---

## 5. Estrutura de projetos (mesma divisão em camadas, conteúdo bem mais enxuto)

```
backend/
  AppMorador.Domain/          # entidades, enums — nenhuma dependência
  AppMorador.Application/     # ISnapshotProvider, IDvrConnectivity, IImageStorage, INotificationSender, IZoneTriggerService (orquestra o fluxo do §2)
  AppMorador.Infrastructure/   # EF Core + MySQL, providers reais (Dahua/Hikvision), IImageStorage em disco local
  AppMorador.Jfl/              # protocolo JFL portado do Integra-o-FL-main (framing, sessão, dispatcher) — é o transporte, não é feature extra
  AppMorador.Api/               # Program.cs, hosted service do listener TCP, 2 endpoints de leitura
  AppMorador.Api.Tests/
```

Continua SOLID/Clean Architecture (interfaces em `Application`, implementação em `Infrastructure`,
DI na composição) — a camada existe, só que cada peça por trás da interface é a versão mais simples
possível, sem nada pensado para escala/features futuras.

## 6. Providers de snapshot — uma tentativa, sem retry

```csharp
public interface ISnapshotProvider
{
    string Fabricante { get; }
    Task<byte[]?> GetSnapshotAsync(Camera camera, Dvr dvr, CancellationToken ct);
}
```

Implementações (`DahuaCgiSnapshotProvider`, `HikvisionIsapiSnapshotProvider`): um único `GetAsync`
HTTP (endpoints já confirmados: `cgi-bin/snapshot.cgi?channel=N` e
`/ISAPI/Streaming/channels/{id}/picture`), autenticação Digest+Basic via `CredentialCache`
(reaproveitando o padrão já validado em `IntelbrasProvider.CreateHttpClient` do
`Teste-portaria-main1`), `HttpClient.Timeout` curto (ex.: 5s). Em caso de exceção/timeout, retorna
`null` — o chamador trata como `SnapshotStatus=Failed`, sem tentar de novo.

Resolução por fabricante: um `IEnumerable<ISnapshotProvider>` injetado + `.First(p => p.Fabricante ==
dvr.Fabricante)` dentro do `IZoneTriggerService` — não precisa de uma classe "resolver" separada para
isso, é uma linha.

`IDvrConnectivity` (do ADR 0001, decisão já registrada — mantido sem alteração, é uma interface +
um stub, não é infraestrutura nova) resolve o endpoint alcançável antes da chamada.

## 7. Storage — disco local

```csharp
public interface IImageStorage
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken ct); // retorna o path relativo salvo
}
```

Implementação única: grava em uma pasta configurável (`appsettings: Snapshots:BasePath`), sem signed
URL, sem expiração automática, sem lifecycle rule. Servido de volta por um endpoint simples
(`GET /api/occurrences/{id}/snapshot`) que lê o arquivo do disco e devolve os bytes — nada de storage
público nem de nuvem nesta entrega.

## 8. Notificação

`INotificationSender.NotifyAsync(Occurrence)` com implementação de log (`ILogger`) — mesmo já
combinado antes, sem mudança.

## 9. API

- `GET /api/occurrences/{id}` — dados da ocorrência (zona, câmera, status do snapshot).
- `GET /api/occurrences/{id}/snapshot` — bytes da imagem (se `SnapshotStatus=Ready`), 404 caso
  contrário.
- Sem Auth nesta entrega (já combinado antes).

## 10. Migração EF Core

Mesma regra de sempre: mostro o conteúdo completo da migration + diff do snapshot do modelo +
confirmação de zero operação destrutiva, antes de aplicar — só depois da sua aprovação específica
desse passo.

---

## Decisões que continuam assumidas por padrão (diga se quiser diferente)

1. Fail-open para códigos Contact ID não catalogados (gera ocorrência+snapshot por segurança).
2. Filtro e deduplicação mantidos (simples, sem framework de configuração) — avise se quiser remover
   também para uma primeira versão ainda mais mínima.
3. `Dvr.Password` em texto plano por ora (dívida técnica já sinalizada, resolvo quando pedir).
4. Notificação como stub de log — sem push real (FCM/APNs) nesta entrega.
5. Sem Auth na API nesta entrega.
6. Uma tentativa só no snapshot, sem retry — se o DVR falhar/travar uma vez, a ocorrência já sai como
   `Failed`, sem segunda chance automática.

---

**Aguardando sua aprovação para começar a implementação.** Nenhum arquivo de código foi criado ou
modificado até este ponto — só este documento de plano.
