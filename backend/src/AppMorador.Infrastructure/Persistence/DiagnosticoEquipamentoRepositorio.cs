using AppMorador.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Persistence;

internal sealed class DiagnosticoEquipamentoRepositorio : IDiagnosticoEquipamentoRepositorio
{
    private readonly AppDbContext _db;

    public DiagnosticoEquipamentoRepositorio(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<DiagnosticoEquipamentoDados> Itens, int Total)> ListarStatusAsync(
        int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        var total = await _db.Equipamentos.CountAsync(cancellationToken).ConfigureAwait(false);
        var desde = DateTime.UtcNow.AddDays(-7);

        var itens = await _db.Equipamentos
            .AsNoTracking()
            .OrderBy(e => e.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(e => new DiagnosticoEquipamentoDados
            {
                EquipamentoId = e.Id,
                EquipamentoNome = e.Nome,
                Fabricante = e.Fabricante,
                PropriedadeId = e.PropriedadeId,
                PropriedadeNome = e.Propriedade!.Nome,
                Status = e.Status,
                EstadoOperacional = e.EstadoOperacional,
                UltimaSincronizacaoUtc = e.UltimaSincronizacaoUtc,
                StatusCentralCapturadoEmUtc = _db.StatusCentraisJfl
                    .Where(s => s.EquipamentoId == e.Id)
                    .Select(s => (DateTime?)s.CapturadoEmUtc)
                    .FirstOrDefault(),
                StatusCentralTemProblemaAtivo = _db.StatusCentraisJfl
                    .Where(s => s.EquipamentoId == e.Id)
                    .Select(s => (bool?)s.TemProblemaAtivo)
                    .FirstOrDefault(),
                QuantidadeEventosRecentes = _db.EventosEquipamento
                    .Count(ev => ev.EquipamentoId == e.Id && ev.OcorridoEmUtc >= desde),
                UltimoEventoDescricao = _db.EventosEquipamento
                    .Where(ev => ev.EquipamentoId == e.Id)
                    .OrderByDescending(ev => ev.OcorridoEmUtc)
                    .Select(ev => ev.Descricao)
                    .FirstOrDefault(),
                UltimoEventoEmUtc = _db.EventosEquipamento
                    .Where(ev => ev.EquipamentoId == e.Id)
                    .OrderByDescending(ev => ev.OcorridoEmUtc)
                    .Select(ev => (DateTime?)ev.OcorridoEmUtc)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (itens, total);
    }
}
