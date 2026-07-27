# ADR 0024 — Visualização de Câmeras (Sprint 20)

**Data**: 2026-07-26

## Contexto

A aba "Câmeras" existe desde a Sprint 16 (ADR 0019) como um Empty State honesto — "esse
recurso está chegando" — porque, até esta Sprint, não havia nenhum endpoint, nenhuma entidade
com status, e nenhuma forma de servir uma imagem de câmera pela Api. Esta Sprint torna a aba
funcional: lista de câmeras com status/miniatura, detalhe com imagem ampliada, captura sob
demanda, tempo real via SignalR. Streaming ao vivo continua fora de escopo (Sprint 21+).

## Fase 0 — Auditoria (achados que redesenharam o escopo original da missão)

| Item | Realidade encontrada |
|---|---|
| Entidade `Camera` | Já existia (`Id, PropriedadeId, GravadorId, Canal, Nome`) — sem `Status`, sem timestamp, sem soft delete |
| Captura de snapshot real | Já existia (`SnapshotCaptureService` + providers CGI/ISAPI) — mas só como efeito colateral de alarme numa Zona vinculada (`VinculoZonaCamera`), nunca sob demanda por Id |
| Servir imagem por HTTP | Não existia nenhum jeito — a imagem ficava só em disco, sem rota nenhuma |
| Endpoints de câmera | Não existiam |
| Cadastro de câmera | Não existia (sem CRUD, sem seed) — sem dado nenhum para mostrar |
| Biblioteca de imagem no mobile | Não existia (`expo-image` não estava instalado) |

A resolução de cada achado está registrada nas Decisões abaixo.

## Decisão 1 — Evolução da entidade `Camera` (campos, não uma entidade nova)

4 campos novos: `Status` (novo enum `StatusCamera`: `Desconhecido`/`Online`/`Offline`),
`UltimoSnapshotPath` (caminho relativo, mesma convenção de `Ocorrencia.ImagePath`),
`UltimaTentativaCapturaUtc` e `UltimoSucessoCapturaUtc` (dois timestamps distintos — sem o
primeiro, não dá para saber se a câmera falha há dias ou se ninguém tentou capturar; ver
Decisão 3 sobre por que o segundo é a única fonte de "última vez vista"). Migration puramente
aditiva (`ALTER TABLE Cameras ADD COLUMN` × 4), aplicada sem risco — a tabela estava vazia.

**Por que não reaproveitar `StatusEquipamento`**: `Camera` foi deliberadamente mantida como
entidade separada de `Equipamento` desde que existe (não são o mesmo agregado — `Camera`
pertence a um `Gravador`, `Equipamento` é outra coisa inteiramente). Um enum novo e pequeno é
mais barato do que criar uma dependência implícita entre dois conceitos que o projeto já decidiu
manter desacoplados.

## Decisão 2 — Sem monitoramento contínuo: status só muda no momento de uma tentativa de captura

Não existe (nem esta Sprint cria) nenhum poller/health-check ativo de câmera. `Status` só é
escrito em dois pontos: (a) captura sob demanda (`POST /api/cameras/{id}/snapshot`, Decisão 5) e
(b) o fluxo de alarme já existente (`AlarmEventProcessor` → `SnapshotCaptureService`, inalterado).
Sucesso vira `Online`, falha vira `Offline`.

**Consequência de wording**: como não sabemos o instante exato em que uma câmera ficou
indisponível (só o instante da última tentativa), a interface nunca afirma "Offline desde 14h" —
sempre "Offline — última imagem há X" (baseado em `UltimoSucessoCapturaUtc`, o último instante em
que sabemos de verdade que a câmera respondeu). Centralizado em `cameraLabels.ts`
(`rotuloStatusDetalhado`), testado (`cameraLabels.test.ts`) para nunca conter a palavra "desde"
associada a "offline".

## Decisão 3 — Servir a imagem: endpoint autenticado, nunca static files

`GET /api/cameras/{id}/imagem` — `[Authorize]` + checagem de posse (mesma cadeia de todo o
domínio: `Camera.Propriedade.ProprietarioId`) — nunca `app.UseStaticFiles()` apontando para a
pasta de snapshots (exporia todas as imagens de todas as propriedades publicamente, sem
autenticação nem isolamento por tenant). O Mobile anexa o token via header `Authorization`
manualmente (`expo-image`/`Image` não fazem isso "automaticamente" como o `api` client faz para
JSON) — `useAuthHeader()` resolve isso uma vez por componente.

**Content-Type sniffado pela assinatura do arquivo, não fixo**: capturas reais são sempre JPEG
(providers CGI/ISAPI), mas o seed de desenvolvimento grava um PNG (Decisão 8) — o endpoint lê os
primeiros 8 bytes do arquivo para decidir `image/jpeg` vs `image/png`, correto nos dois casos, sem
depender de extensão de arquivo (que pode mentir, ver Decisão 8).

## Decisão 4 — Cache-busting via querystring

`UltimaImagemUrl` sempre inclui `?v={UltimoSucessoCapturaUtc.Ticks}` — sem isso, o cache de
imagem do `expo-image` nunca invalidaria depois de uma captura nova, porque a URL de uma câmera
nunca mudaria (o cache é por URL).

## Decisão 5 — `GET`/`POST` separados para snapshot: metadados vs. captura sob demanda

`GET /api/cameras/{id}/snapshot` só lê o que já está salvo (nunca dispara hardware) — 204 se
nunca houve captura. `POST` no mesmo caminho dispara uma captura nova de verdade (2-10s de
latência real ao gravador). **Sempre 200**, nunca 202 "processando": não existe canal assíncrono
real por trás (a chamada ao gravador é síncrona, do início ao fim, dentro da mesma requisição
HTTP) — prometer um 202 sem um endpoint de consulta de status por trás seria o mesmo erro que a
Sprint 18 (ADR 0022, Decisão 10) já identificou para comandos JFL: nunca desenhar um contrato que
finge uma assincronia que a implementação não tem. Falha na captura ainda retorna 200 com
`sucesso: false` + a última imagem disponível (nunca troca a tela por um erro quando ainda há
algo útil pra mostrar).

**Timeout de 15s (Fase 2.3 da missão) é uma camada própria da Aplicação**, não apenas o timeout
já configurado em `Snapshots:TimeoutSeconds` (que protege cada chamada HTTP ao gravador
individualmente) — um `CancellationTokenSource` vinculado com `CancelAfter(15s)` garante que a
requisição do morador nunca fica pendurada além disso, mesmo que o timeout do provider mude no
futuro.

## Decisão 6 — Reaproveitamento de `SnapshotCaptureService`, com uma porta nova em Domain

`SnapshotCaptureService` (Infrastructure) não tinha interface — só era consumido por
`AlarmEventProcessor`, também em Infrastructure. Como `CameraServico` (Application) precisa
disparar uma captura sob demanda, extraída `ISnapshotCaptureService` (Domain), implementada pela
mesma classe — mesmo padrão já usado por todo Provider de fabricante (`IJflProvider`,
`IControlIdProvider`). `SnapshotCaptureService.CapturarPorCameraIdAsync` (novo) resolve a Câmera
diretamente pelo Id (`ICameraResolver.ResolveByIdAsync`, novo), sem depender de uma Zona/alarme —
o método original (`CapturarAsync`, por Zona) continua intacto para o fluxo de alarme.

## Decisão 7 — Evento SignalR leve (`CameraStatusAlterado`), separado do Snapshot Operacional

Câmera é uma feature de exibição, não faz parte do cálculo de saúde operacional da propriedade —
inchar `SnapshotOperacionalResponse` (ADR 0016/0017) com dados de câmera acoplaria dois conceitos
sem relação hoje. Novo método em `IOperacionalEventoPublicador.PublicarCameraStatusAsync`, novo
evento SignalR `"CameraStatusAlterado"` no mesmo grupo/hub já existente (`OperacionalHub`,
`propriedade:{id}`) — reaproveita toda a infraestrutura de transporte/autenticação/grupo da
Sprint 14, só adiciona um tipo de mensagem novo. Mobile: 4º context em `RealtimeContext.tsx`
(`useRealtimeCamera`), mesma Regra 5 (ADR 0022) — só quem lê atualização de câmera re-renderiza
por isso.

## Decisão 8 — Seed com imagem de exemplo real (PNG gerado em memória, sem dependência nova)

Sem cadastro de câmera (fora de escopo) e sem seed, a Sprint seria impossível de validar num
banco vazio. `PlaceholderImageGenerator` (Infrastructure) gera um PNG mínimo (cor sólida)
inteiramente em memória — sem `System.Drawing.Common` (não portável fora do Windows) e sem
arriscar bytes de JPEG hand-rolled incorretos (DCT/Huffman são complexos demais para reproduzir
de memória com segurança) — usando só `System.IO.Compression.DeflateStream` (já embutido no BCL)
mais os dois checksums padrão do formato (CRC32, Adler32), implementados diretamente do
algoritmo de referência. Verificado por decodificação real (`System.Drawing`, fora do projeto,
script de verificação descartável) antes de entrar no seed — 320×180, cor correta em ambos os
cantos. Arquivo salvo com extensão `.jpg` (convenção existente de `SnapshotStorage.SaveAsync`,
inalterada) mesmo contendo bytes PNG — cosmeticamente inconsistente, mas inofensivo, porque o
Content-Type real é sempre sniffado pela assinatura (Decisão 3), nunca pela extensão.

**Seed idempotente por conta PRÓPRIA** (`GarantirCamerasDeExemploAsync`), deliberadamente
independente do gate da conta Morador: um banco de desenvolvimento onde a conta Morador já
existia antes desta Sprint (todo ambiente já em uso) nunca re-executaria o bloco de criação de
usuário — sem esse passo próprio, a propriedade de exemplo nunca ganharia câmeras. Confirmado
rodando contra o banco de desenvolvimento real desta sessão: a conta Morador já existia, e o novo
passo (checagem por `Cameras` da própria propriedade) criou corretamente 1 gravador + 3 câmeras +
1 vínculo de zona, com os 2 arquivos de imagem gravados em disco.

## Decisão 9 — Fora de escopo confirmado (sem sinal real por trás)

**Movimento detectado**: nenhum provider (CGI/ISAPI) hoje emite qualquer sinal de movimento —
implementar o badge "Movimento detectado" da missão seria fabricar um evento que não existe.
Registrado como dívida técnica, não implementado. **Agrupamento/cadastro/streaming/PTZ**: fora de
escopo, confirmados nesta ADR e no Roadmap.

## Testes de resiliência

Cobertos por teste automatizado: propriedade/câmera de outro usuário nunca retorna dado (404);
câmera sem imagem retorna `Ok(null)` (204) em vez de erro; falha de captura marca `Offline` com
mensagem amigável (nunca o erro técnico do provider); timeout interno (`OperationCanceledException`)
nunca propaga, vira falha amigável; câmera com sucesso de captura marca `Online` e só publica
SignalR quando o status de fato muda (nunca notificação redundante); content-type sniffado
corretamente para JPEG e PNG. Seed testado contra EF Core InMemory (criação + idempotência).

## Preparação para streaming futuro (Sprint 21+)

`INotificationProvider`-like: `ISnapshotProvider`/`ISnapshotCaptureService` já abstraem o
fabricante do gravador — um streaming real (RTSP/HLS/WebRTC) entraria como uma nova capacidade do
mesmo `Gravador`/`Camera`, sem precisar de um novo agregado. O endpoint de imagem
(`GET /api/cameras/{id}/imagem`) já estabelece o padrão de autenticação+ownership que um futuro
endpoint de stream reaproveitaria. Nenhum código desta Sprint assume RTSP/HLS/WebRTC/ONVIF/codec —
o mobile nunca viu (nem vai ver) esses termos.

## Lições aprendidas

1. **"A entidade já existe" não significa "a feature já existe"** — `Camera`/`Gravador`/captura
   de snapshot já existiam desde as Fases 1-2 (pré-Sprint 1), mas serviam a um propósito
   inteiramente diferente (snapshot no disparo de alarme) do que esta Sprint pedia (visualização
   sob demanda). Reaproveitar a infraestrutura certa (`SnapshotCaptureService`/providers) exigiu
   entender exatamente onde ela parava (captura só por Zona) antes de estender.
2. **Um seed sem dado real de imagem é um seed que não prova nada** — gerar um PNG válido em
   memória (em vez de pular essa parte ou arriscar bytes incorretos) foi o que permitiu validar o
   endpoint de imagem/content-type-sniffing ponta a ponta antes mesmo do dispositivo físico.
3. **Nem todo "não sabemos" deve virar uma mentira específica** — "Offline desde 14h" soa mais
   preciso que "última imagem há 2 horas", mas seria uma afirmação que o sistema não pode
   sustentar sem monitoramento contínuo. A versão honesta, embora menos "polida", é a única
   correta dado o que o sistema realmente sabe.
