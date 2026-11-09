using AppMorador.Application.Eventos;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Eventos;

/// <summary>
/// Sprint 24 (ADR 0040) — quedas e retornos de comunicação das centrais na Central de Eventos
/// (fecha a dívida 24). Perda = Atenção, retorno = Informativo; categoria Sistema.
/// </summary>
internal sealed class ConectividadeFonteEventos : IFonteEventos
{
    private readonly AppDbContext _db;

    public ConectividadeFonteEventos(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<EventoTimeline> Itens, int Total)> ConsultarEventosAsync(
        Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        if ((filtro.Origem is not null && filtro.Origem != OrigemEvento.Jfl)
            || (filtro.Categoria is not null && filtro.Categoria != CategoriaEvento.Sistema)
            || filtro.Severidade == SeveridadeEvento.Critico
            || (filtro.Fabricante is not null && filtro.Fabricante != FabricanteEquipamento.Jfl))
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        var query = _db.EventosConectividade.Where(c => c.PropriedadeId == propriedadeId);

        if (filtro.Severidade == SeveridadeEvento.Atencao)
        {
            query = query.Where(c => c.Tipo == TipoEventoConectividade.Perdida);
        }
        else if (filtro.Severidade == SeveridadeEvento.Informativo)
        {
            query = query.Where(c => c.Tipo == TipoEventoConectividade.Restabelecida);
        }

        if (filtro.EquipamentoId is not null)
        {
            query = query.Where(c => c.EquipamentoId == filtro.EquipamentoId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            query = query.Where(c => c.Equipamento != null && c.Equipamento.Nome.Contains(filtro.Busca));
        }

        if (filtro.DesdeUtc is not null)
        {
            query = query.Where(c => c.OcorridoEmUtc >= filtro.DesdeUtc);
        }

        if (filtro.AteUtc is not null)
        {
            query = query.Where(c => c.OcorridoEmUtc <= filtro.AteUtc);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var linhas = await query
            .OrderByDescending(c => c.OcorridoEmUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(c => new { c.Id, c.Tipo, c.OcorridoEmUtc, NomeEquipamento = c.Equipamento != null ? c.Equipamento.Nome : null })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itens = linhas.Select(l => new EventoTimeline
        {
            Id = l.Id,
            Origem = OrigemEvento.Jfl,
            Categoria = CategoriaEvento.Sistema,
            Severidade = l.Tipo == TipoEventoConectividade.Perdida ? SeveridadeEvento.Atencao : SeveridadeEvento.Informativo,
            Titulo = l.Tipo == TipoEventoConectividade.Perdida ? "Central sem comunicação" : "Comunicação restabelecida",
            Descricao = l.NomeEquipamento,
            OcorridoEmUtc = l.OcorridoEmUtc,
        }).ToList();

        return (itens, total);
    }
}
