using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel.Diagnostico;

public sealed class DiagnosticoServico : IDiagnosticoServico
{
    private readonly IDiagnosticoEquipamentoRepositorio _diagnostico;

    public DiagnosticoServico(IDiagnosticoEquipamentoRepositorio diagnostico)
    {
        _diagnostico = diagnostico;
    }

    public async Task<DiagnosticoEquipamentosPaginadosResponse> ObterStatusEquipamentosAsync(
        int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        pagina = pagina <= 0 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is <= 0 or > 100 ? 20 : tamanhoPagina;

        var (itens, total) = await _diagnostico.ListarStatusAsync(pagina, tamanhoPagina, cancellationToken).ConfigureAwait(false);

        return new DiagnosticoEquipamentosPaginadosResponse
        {
            Itens = itens.Select(ToDto).ToList(),
            PaginaAtual = pagina,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanhoPagina),
            TotalItens = total,
        };
    }

    private static DiagnosticoEquipamentoResponse ToDto(DiagnosticoEquipamentoDados dados)
    {
        DateTime? ultimoPing = dados.UltimaSincronizacaoUtc is null
            ? dados.StatusCentralCapturadoEmUtc
            : dados.StatusCentralCapturadoEmUtc is null
                ? dados.UltimaSincronizacaoUtc
                : dados.UltimaSincronizacaoUtc > dados.StatusCentralCapturadoEmUtc
                    ? dados.UltimaSincronizacaoUtc
                    : dados.StatusCentralCapturadoEmUtc;

        return new DiagnosticoEquipamentoResponse
        {
            EquipamentoId = dados.EquipamentoId,
            EquipamentoNome = dados.EquipamentoNome,
            Fabricante = dados.Fabricante,
            PropriedadeId = dados.PropriedadeId,
            PropriedadeNome = dados.PropriedadeNome,
            Status = dados.Status,
            EstadoOperacional = dados.EstadoOperacional,
            UltimoPingUtc = ultimoPing,
            TemProblemaAtivo = dados.StatusCentralTemProblemaAtivo,
            QuantidadeEventosRecentes = dados.QuantidadeEventosRecentes,
            UltimoEventoDescricao = dados.UltimoEventoDescricao,
            UltimoEventoEmUtc = dados.UltimoEventoEmUtc,
        };
    }
}
