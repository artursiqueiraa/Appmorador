using AppMorador.Application.Common;
using AppMorador.Domain.ContactId;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Dashboard;

/// <summary>
/// Fórmula do Health Score e do texto de status/último evento — nada especulativo,
/// tudo derivado de dados reais já modelados (ver <see cref="IConsultaDashboardServico"/>).
/// </summary>
public sealed class DashboardServico : IDashboardServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IConsultaDashboardServico _queryService;

    public DashboardServico(IPropriedadeRepositorio propriedades, IConsultaDashboardServico queryService)
    {
        _propriedades = propriedades;
        _queryService = queryService;
    }

    public async Task<Result<DashboardResponse>> GetAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<DashboardResponse>.Fail("Propriedade não encontrada.");
        }

        var raw = await _queryService.GetRawDataAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        var statusSeguranca = raw.TemCentral ? "Protegido" : "Configuração pendente";

        var cobertura = raw.TotalZonas > 0 ? (double)raw.ZonasComCamera / raw.TotalZonas : 0d;
        var pontuacaoSaude = (raw.TemCentral ? 50 : 0) + (int)Math.Round(50 * cobertura);

        string? ultimoEvento = null;
        if (raw.UltimaOcorrenciaCodigoContactId is not null &&
            ContactIdCatalog.TryGet(raw.UltimaOcorrenciaCodigoContactId, out var definicao))
        {
            var local = raw.UltimaOcorrenciaNomeZona ?? "um local monitorado";
            ultimoEvento = $"{definicao!.FriendlyMessage} em {local}";
        }

        var dto = new DashboardResponse
        {
            Nome = propriedade.Nome,
            Tipo = propriedade.Tipo,
            StatusSeguranca = statusSeguranca,
            PontuacaoSaude = pontuacaoSaude,
            UltimoEvento = ultimoEvento,
            UltimoEventoEmUtc = raw.UltimaOcorrenciaEmUtc,
            QuantidadeCentrais = raw.QuantidadeCentrais,
            QuantidadeGravadores = raw.QuantidadeGravadores,
            QuantidadeCameras = raw.QuantidadeCameras,
            QuantidadeSensores = raw.TotalZonas,
            QuantidadePessoas = 1, // so o dono nesta sprint — sem membros/compartilhamento ainda
        };

        return Result<DashboardResponse>.Ok(dto);
    }
}
