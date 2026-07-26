using AppMorador.Application.Eventos;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Eventos;

/// <summary>
/// Segunda fonte real de <see cref="IFonteEventos"/> (Sprint 11) — traduz
/// <see cref="EventoEquipamento"/> (importado via Provider de integração, ver ADR 0014)
/// para <see cref="EventoTimeline"/>. A Application nunca vê Equipamento/EventoEquipamento
/// diretamente através desta porta — mesmo desenho de <see cref="JflFonteEventos"/>.
/// </summary>
internal sealed class EquipamentoFonteEventos : IFonteEventos
{
    private readonly AppDbContext _db;

    public EquipamentoFonteEventos(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<EventoTimeline> Itens, int Total)> ConsultarEventosAsync(
        Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        // Sprint 13 (ADR 0016) — esta fonte representa exclusivamente eventos
        // importados de Equipamento (hoje só Control iD, ver ADR 0014): Origem=
        // ControlId, Categoria=Acesso sempre. Um filtro pedindo qualquer outro valor
        // nesses campos nunca pode ser satisfeito por esta fonte.
        if (filtro.Origem is not null && filtro.Origem != OrigemEvento.ControlId)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        if (filtro.Categoria is not null && filtro.Categoria != CategoriaEvento.Acesso)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        // Severidade nunca e Critico nesta fonte (so Atencao/Informativo, ver
        // mapeamento abaixo) — pedir Critico nunca pode ser satisfeito.
        if (filtro.Severidade == SeveridadeEvento.Critico)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        // Sprint 15 (ADR 0018) — achado real corrigido na arquitetura: esta fonte
        // representa exclusivamente Control iD, mas sua query base nunca havia
        // filtrado por Fabricante — funcionava por coincidência enquanto Control iD
        // era o único fabricante escrevendo em EventoEquipamento. A chegada de um
        // segundo fabricante reaproveitando a mesma tabela genérica (Intelbras,
        // Sprint 15) expôs a lacuna: sem este filtro, eventos Intelbras apareceriam
        // aqui também (duplicados e mal-rotulados como Origem=ControlId). Correção
        // genérica, beneficia qualquer fabricante futuro que reaproveite esta tabela.
        var query = _db.EventosEquipamento
            .Where(e => e.Equipamento!.PropriedadeId == propriedadeId && e.Equipamento!.Fabricante == FabricanteEquipamento.ControlId);

        if (filtro.EquipamentoId is not null)
        {
            query = query.Where(e => e.EquipamentoId == filtro.EquipamentoId.Value);
        }

        if (filtro.Fabricante is not null)
        {
            query = query.Where(e => e.Equipamento!.Fabricante == filtro.Fabricante.Value);
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

        // "Negado" e o unico sinal de severidade elevada que esta fonte reconhece (ver
        // mapeamento no final do metodo) — mesma string usada la, mantida em sincronia.
        if (filtro.Severidade == SeveridadeEvento.Atencao)
        {
            query = query.Where(e => e.Descricao.Contains("negado"));
        }
        else if (filtro.Severidade == SeveridadeEvento.Informativo)
        {
            query = query.Where(e => !e.Descricao.Contains("negado"));
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
            Origem = OrigemEvento.ControlId,
            Categoria = CategoriaEvento.Acesso,
            Severidade = linha.Descricao.Contains("negado", StringComparison.OrdinalIgnoreCase)
                ? SeveridadeEvento.Atencao
                : SeveridadeEvento.Informativo,
            Titulo = linha.Descricao,
            Descricao = linha.NomeEquipamento,
            OcorridoEmUtc = linha.OcorridoEmUtc,
        }).ToList();

        return (itens, total);
    }
}
