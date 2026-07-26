using AppMorador.Application.Eventos;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Eventos;

/// <summary>
/// Terceira fonte real de <see cref="IFonteEventos"/> (Sprint 15, ADR 0018) — traduz
/// <see cref="EventoEquipamento"/> (mesma entidade genérica já usada pelo Control iD
/// desde a Sprint 11, reaproveitada sem nenhuma alteração) para
/// <see cref="EventoTimeline"/>, agora com semântica de alarme (Origem=Intelbras,
/// Categoria=Alarme) em vez de controle de acesso — prova que a Central de Eventos
/// já suportava essa combinação sem mudança de contrato.
/// </summary>
internal sealed class IntelbrasFonteEventos : IFonteEventos
{
    private readonly AppDbContext _db;

    public IntelbrasFonteEventos(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<EventoTimeline> Itens, int Total)> ConsultarEventosAsync(
        Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        if (filtro.Origem is not null && filtro.Origem != OrigemEvento.Intelbras)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        if (filtro.Categoria is not null && filtro.Categoria != CategoriaEvento.Alarme)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        if (filtro.Fabricante is not null && filtro.Fabricante != FabricanteEquipamento.Intelbras)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        // Severidade nunca e Informativo nesta fonte (so Critico/Atencao, ver
        // mapeamento abaixo) — pedir Informativo nunca pode ser satisfeito.
        if (filtro.Severidade == SeveridadeEvento.Informativo)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        var query = _db.EventosEquipamento
            .Where(e => e.Equipamento!.PropriedadeId == propriedadeId && e.Equipamento!.Fabricante == FabricanteEquipamento.Intelbras);

        if (filtro.EquipamentoId is not null)
        {
            query = query.Where(e => e.EquipamentoId == filtro.EquipamentoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            query = query.Where(e => e.Equipamento!.Nome.Contains(filtro.Busca) || e.Descricao.Contains(filtro.Busca));
        }

        if (filtro.DesdeUtc is not null)
        {
            query = query.Where(e => e.OcorridoEmUtc >= filtro.DesdeUtc);
        }

        if (filtro.AteUtc is not null)
        {
            query = query.Where(e => e.OcorridoEmUtc <= filtro.AteUtc);
        }

        // "Disparo"/"alarme" e o unico sinal de severidade critica que esta fonte
        // reconhece (mesma estrategia de heuristica textual ja usada por
        // EquipamentoFonteEventos com "negado") — mantido em sincronia com o mapeamento
        // no final do metodo.
        if (filtro.Severidade == SeveridadeEvento.Critico)
        {
            query = query.Where(e => e.Descricao.Contains("disparo") || e.Descricao.Contains("alarme"));
        }
        else if (filtro.Severidade == SeveridadeEvento.Atencao)
        {
            query = query.Where(e => !e.Descricao.Contains("disparo") && !e.Descricao.Contains("alarme"));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var linhas = await query
            .OrderByDescending(e => e.OcorridoEmUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(e => new
            {
                e.Id,
                e.Descricao,
                e.OcorridoEmUtc,
                NomeEquipamento = e.Equipamento!.Nome,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itens = linhas.Select(linha => new EventoTimeline
        {
            Id = linha.Id,
            Origem = OrigemEvento.Intelbras,
            Categoria = CategoriaEvento.Alarme,
            Severidade = linha.Descricao.Contains("disparo", StringComparison.OrdinalIgnoreCase)
                    || linha.Descricao.Contains("alarme", StringComparison.OrdinalIgnoreCase)
                ? SeveridadeEvento.Critico
                : SeveridadeEvento.Atencao,
            Titulo = linha.Descricao,
            Descricao = linha.NomeEquipamento,
            OcorridoEmUtc = linha.OcorridoEmUtc,
        }).ToList();

        return (itens, total);
    }
}
