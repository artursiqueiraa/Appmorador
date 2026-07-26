using AppMorador.Application.Operacional;
using AppMorador.Domain.ContactId;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using AppMorador.Infrastructure.Snapshots;
using AppMorador.Jfl.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Jfl;

/// <summary>
/// Todo o processamento de negocio de um evento JFL ja parseado e confirmado (ACK)
/// vive aqui — nao no <see cref="EventoCommandHandler"/> (que so faz protocolo:
/// parse + ACK + delegar) nem no hosted service que sobe o servidor TCP (que so
/// inicia/encerra o listener). Servico registrado como Scoped porque depende do
/// <see cref="AppDbContext"/>.
///
/// Fluxo (Fase 1.1, catalogo em Fase 1.2): grava SEMPRE em <see cref="RegistroEventoAlarme"/>
/// (auditoria, nenhuma regra de negocio depende dela) -> consulta
/// <see cref="ContactIdCatalog"/> -> so cria uma <see cref="Ocorrencia"/> quando o
/// codigo esta catalogado com GeneratesOccurrence=true. Codigo fora do catalogo vira
/// <see cref="ResultadoProcessamentoEvento.CodigoDesconhecido"/> (log de Warning, sem
/// Ocorrencia) — homologar um painel/firmware novo e so adicionar uma entrada ao
/// catalogo, sem tocar nesta classe. Quando o painel/zona nao estao cadastrados, a
/// Ocorrencia ainda e criada (com <see cref="StatusResolucao.NaoResolvido"/>) — nao
/// provisionar nao pode significar perder o disparo.
/// </summary>
public sealed class AlarmEventProcessor
{
    private readonly AppDbContext _db;
    private readonly SnapshotCaptureService _snapshotCaptureService;
    private readonly ISnapshotOperacionalServico _snapshotOperacional;
    private readonly IOperacionalEventoPublicador _publicador;
    private readonly ILogger<AlarmEventProcessor> _logger;

    public AlarmEventProcessor(
        AppDbContext db,
        SnapshotCaptureService snapshotCaptureService,
        ISnapshotOperacionalServico snapshotOperacional,
        IOperacionalEventoPublicador publicador,
        ILogger<AlarmEventProcessor> logger)
    {
        _db = db;
        _snapshotCaptureService = snapshotCaptureService;
        _snapshotOperacional = snapshotOperacional;
        _publicador = publicador;
        _logger = logger;
    }

    public async Task ProcessarAsync(
        string? numeroSerie,
        byte[] payloadBruto,
        EventoRequest evento,
        DateTime recebidoEmUtc,
        CancellationToken cancellationToken)
    {
        var resultado = ResultadoProcessamentoEvento.ErroAoProcessar;

        try
        {
            if (!ContactIdCatalog.TryGet(evento.CodigoEvento, out var definicao))
            {
                resultado = ResultadoProcessamentoEvento.CodigoDesconhecido;
                _logger.LogWarning(
                    "Codigo Contact ID {Codigo} nao catalogado, recebido da central {NumeroSerie} — nenhuma Ocorrencia criada. " +
                    "Adicione uma entrada em ContactIdCatalog apos confirmar o significado (homologacao de painel/firmware).",
                    evento.CodigoEvento, numeroSerie ?? "desconhecida");
            }
            else if (definicao!.GeneratesOccurrence)
            {
                await CriarOcorrenciaAsync(numeroSerie, evento, recebidoEmUtc, cancellationToken)
                    .ConfigureAwait(false);
                resultado = ResultadoProcessamentoEvento.OcorrenciaCriada;
            }
            else
            {
                resultado = ResultadoProcessamentoEvento.IgnoradoPorFiltro;
                _logger.LogInformation(
                    "Evento {Codigo} ({Descricao}) da central {NumeroSerie} catalogado como GeneratesOccurrence=false — nenhuma Ocorrencia criada",
                    definicao.Code, definicao.Description, numeroSerie ?? "desconhecida");
            }
        }
        catch (Exception ex)
        {
            resultado = ResultadoProcessamentoEvento.ErroAoProcessar;
            _logger.LogError(
                ex, "Erro ao processar evento (central {NumeroSerie}, codigo {Codigo})",
                numeroSerie ?? "desconhecida", evento.CodigoEvento);
        }
        finally
        {
            await GravarLogAsync(numeroSerie, payloadBruto, evento, recebidoEmUtc, resultado, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task CriarOcorrenciaAsync(
        string? numeroSerie, EventoRequest evento, DateTime recebidoEmUtc, CancellationToken cancellationToken)
    {
        Central? central = null;
        Zona? zona = null;

        if (!string.IsNullOrEmpty(numeroSerie))
        {
            central = await _db.Centrais
                .FirstOrDefaultAsync(c => c.NumeroSerie == numeroSerie, cancellationToken)
                .ConfigureAwait(false);

            if (central is null)
            {
                _logger.LogWarning(
                    "Central {NumeroSerie} nao cadastrada — Ocorrencia sera criada como NaoResolvido (dado bruto do evento preservado)",
                    numeroSerie);
            }
            else
            {
                zona = await _db.Zonas
                    .FirstOrDefaultAsync(z => z.CentralId == central.Id && z.Numero == evento.UsuarioOuZona, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var resolvido = central is not null && zona is not null;

        var ocorrencia = new Ocorrencia
        {
            Id = Guid.NewGuid(),
            NumeroSeriePainel = numeroSerie ?? "desconhecida",
            CodigoEvento = evento.CodigoEvento,
            ZonaOuUsuario = evento.UsuarioOuZona,
            Particao = evento.Particao,
            CreatedAtUtc = recebidoEmUtc,
            CentralId = central?.Id,
            PropriedadeId = central?.PropriedadeId,
            ZonaId = zona?.Id,
            StatusResolucao = resolvido ? StatusResolucao.Resolvido : StatusResolucao.NaoResolvido,
        };

        // SaveChanges #1 (nao eliminavel): a Ocorrencia precisa existir no banco
        // imediatamente, antes de qualquer tentativa de snapshot — essa e a garantia
        // de confiabilidade da Fase 1 ("mesmo que o DVR esteja offline, a ocorrencia
        // deve existir"). Adiar este save para juntar com o de baixo (apos o
        // snapshot) faria a Ocorrencia depender da latencia/sucesso de uma chamada
        // HTTP externa para ser persistida, o que contradiz essa garantia.
        _db.Ocorrencias.Add(ocorrencia);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Ocorrencia {OcorrenciaId} criada (PropriedadeId={PropriedadeId}, ZonaId={ZonaId}, StatusResolucao={StatusResolucao})",
            ocorrencia.Id, ocorrencia.PropriedadeId, ocorrencia.ZonaId, ocorrencia.StatusResolucao);

        // Sprint 14 (ADR 0017) — único gatilho verdadeiramente assíncrono da Camada
        // Operacional: uma central pode discar este evento a qualquer momento, fora de
        // qualquer requisição HTTP. So publica quando a Propriedade e conhecida (sem
        // Central cadastrada nao ha grupo para notificar).
        if (ocorrencia.PropriedadeId is not null)
        {
            await PublicarAtualizacaoOperacionalAsync(ocorrencia, cancellationToken).ConfigureAwait(false);
        }

        // So faz sentido tentar o snapshot quando central e zona foram resolvidos —
        // sem ZonaId nao ha como achar o VinculoZonaCamera, e sem PropriedadeId nao ha
        // onde salvar o arquivo. Ocorrencia.ImagePath fica null nesse caso, sem
        // bloquear nada (mesma regra de nao travar a ocorrencia por causa do DVR).
        if (ocorrencia.StatusResolucao == StatusResolucao.Resolvido)
        {
            try
            {
                var snapshot = await _snapshotCaptureService
                    .CapturarAsync(ocorrencia.PropriedadeId!.Value, ocorrencia.ZonaId!.Value, recebidoEmUtc, cancellationToken)
                    .ConfigureAwait(false);

                if (snapshot.Success)
                {
                    // SaveChanges #2 (nao eliminavel): so existe porque o resultado do
                    // snapshot so e conhecido depois de uma chamada HTTP externa (que
                    // pode demorar ate TimeoutSeconds) que acontece DEPOIS do save #1
                    // acima, de proposito. Nao ha como saber ImagePath antes de #1 sem
                    // atrasar a criacao da Ocorrencia em si.
                    ocorrencia.ImagePath = snapshot.ImagePath;
                    await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Snapshot salvo para Ocorrencia {OcorrenciaId}: {ImagePath}", ocorrencia.Id, snapshot.ImagePath);
                }
                else
                {
                    _logger.LogWarning("Snapshot nao capturado para Ocorrencia {OcorrenciaId}: {Error}", ocorrencia.Id, snapshot.Error);
                }
            }
            catch (Exception ex)
            {
                // A Ocorrencia ja foi criada e salva — uma falha aqui (ex.: disco cheio
                // ao gravar o arquivo) nunca deve reclassificar o resultado do evento
                // como erro geral, nem derrubar a Ocorrencia ja persistida.
                _logger.LogError(ex, "Falha inesperada ao capturar/salvar snapshot para Ocorrencia {OcorrenciaId}", ocorrencia.Id);
            }
        }
    }

    private async Task PublicarAtualizacaoOperacionalAsync(Ocorrencia ocorrencia, CancellationToken cancellationToken)
    {
        var propriedadeId = ocorrencia.PropriedadeId!.Value;

        try
        {
            // Mesmo mapeamento já estabelecido em JflFonteEventos/ADR 0016 (Título via
            // catálogo Contact ID, Destaque quando resolvido) — não é uma regra nova,
            // apenas reaplicada aqui no momento da criação do evento, para a notificação
            // em tempo real (a Central de Eventos via GET continua a fonte de verdade).
            var titulo = ContactIdCatalog.TryGet(ocorrencia.CodigoEvento, out var definicao)
                ? definicao!.FriendlyMessage
                : "Evento registrado";

            var eventoResponse = new AppMorador.Application.Eventos.EventoResponse
            {
                Id = ocorrencia.Id,
                Titulo = titulo,
                Descricao = null,
                OcorridoEmUtc = ocorrencia.CreatedAtUtc,
                Destaque = ocorrencia.StatusResolucao == StatusResolucao.Resolvido,
            };

            await _publicador.PublicarNovoEventoAsync(propriedadeId, eventoResponse, cancellationToken).ConfigureAwait(false);
            await _snapshotOperacional
                .RegenerarEPublicarAsync(propriedadeId, MotivoAtualizacaoOperacional.AlarmeDisparado, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Best-effort: a Ocorrencia já foi persistida com sucesso antes desta
            // chamada — uma falha de notificação em tempo real nunca pode reclassificar
            // o processamento do evento de alarme como erro.
            _logger.LogWarning(ex, "Falha ao publicar atualizacao operacional em tempo real apos Ocorrencia {OcorrenciaId}", ocorrencia.Id);
        }
    }

    private async Task GravarLogAsync(
        string? numeroSerie,
        byte[] payloadBruto,
        EventoRequest evento,
        DateTime recebidoEmUtc,
        ResultadoProcessamentoEvento resultado,
        CancellationToken cancellationToken)
    {
        var log = new RegistroEventoAlarme
        {
            Id = Guid.NewGuid(),
            Payload = Convert.ToHexString(payloadBruto),
            NumeroSerie = numeroSerie ?? "desconhecida",
            CodigoEvento = evento.CodigoEvento,
            Zona = evento.UsuarioOuZona,
            Timestamp = recebidoEmUtc,
            ResultadoProcessamento = resultado,
        };

        // SaveChanges #3 (nao eliminavel): roda no "finally" de ProcessarAsync,
        // independente do que aconteceu no try (CodigoDesconhecido, IgnoradoPorFiltro,
        // Ocorrencia criada, ou excecao). Juntar este save com o #1/#2 acopraria o
        // registro de auditoria ao sucesso do caminho de negocio (Ocorrencia/
        // snapshot) — hoje o log e gravado mesmo se aquele caminho falhar, que e o
        // ponto de existir uma trilha de auditoria independente (Fase 1.1).
        _db.RegistrosEventoAlarme.Add(log);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
