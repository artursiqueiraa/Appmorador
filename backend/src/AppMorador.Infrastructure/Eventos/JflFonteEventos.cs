using AppMorador.Application.Eventos;
using AppMorador.Domain.ContactId;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Eventos;

/// <summary>
/// Única implementação real de <see cref="IFonteEventos"/> hoje — traduz
/// <see cref="Ocorrencia"/> (central de alarme JFL) para <see cref="EventoTimeline"/>.
/// Este é o único ponto do sistema que sabe que a Central de Eventos, por trás, é
/// alimentada por Ocorrencia — a Application nunca vê esse tipo. Futuras fontes
/// (controle de acesso, eventos de sistema) implementam esta mesma porta sem que este
/// arquivo precise mudar.
/// </summary>
internal sealed class JflFonteEventos : IFonteEventos
{
    private readonly AppDbContext _db;

    public JflFonteEventos(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<EventoTimeline> Itens, int Total)> ConsultarEventosAsync(
        Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        var query = _db.Ocorrencias.Where(o => o.PropriedadeId == propriedadeId);

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            query = query.Where(o => o.Zona != null && o.Zona.Nome.Contains(filtro.Busca));
        }

        if (filtro.DesdeUtc is not null)
        {
            query = query.Where(o => o.CreatedAtUtc >= filtro.DesdeUtc);
        }

        if (filtro.AteUtc is not null)
        {
            query = query.Where(o => o.CreatedAtUtc <= filtro.AteUtc);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var linhas = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(o => new
            {
                o.Id,
                o.CodigoEvento,
                o.CreatedAtUtc,
                o.StatusResolucao,
                NomeZona = o.Zona != null ? o.Zona.Nome : null,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itens = linhas.Select(linha =>
        {
            // Fallback "Evento registrado": um código pode deixar de existir no catálogo
            // depois que a Ocorrencia já foi criada (catálogo é código, não dado de banco)
            // — nunca mostra o código Contact ID cru ao usuário final.
            var titulo = ContactIdCatalog.TryGet(linha.CodigoEvento, out var definicao)
                ? definicao!.FriendlyMessage
                : "Evento registrado";

            return new EventoTimeline
            {
                Id = linha.Id,
                Origem = OrigemEvento.Jfl,
                Categoria = CategoriaEvento.Alarme,
                Severidade = linha.StatusResolucao == StatusResolucao.Resolvido
                    ? SeveridadeEvento.Critico
                    : SeveridadeEvento.Atencao,
                Titulo = titulo,
                Descricao = linha.NomeZona,
                OcorridoEmUtc = linha.CreatedAtUtc,
            };
        }).ToList();

        return (itens, total);
    }
}