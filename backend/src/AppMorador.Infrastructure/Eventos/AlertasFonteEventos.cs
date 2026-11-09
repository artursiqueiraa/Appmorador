using AppMorador.Application.Alertas;
using AppMorador.Application.Eventos;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Eventos;

/// <summary>
/// Sprint 24 (ADR 0040) — alertas críticos na Central de Eventos: um item por alerta, sempre
/// Crítico, com a situação na descrição (quem confirmou e quando, ou até onde o aviso já subiu).
/// O disparo da central também aparece como Ocorrência (JflFonteEventos); este item mostra o
/// acompanhamento do alerta, não repete o sensor.
/// </summary>
internal sealed class AlertasFonteEventos : IFonteEventos
{
    private static readonly TimeSpan FusoBrasilia = TimeSpan.FromHours(-3);

    private readonly AppDbContext _db;

    public AlertasFonteEventos(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(IReadOnlyList<EventoTimeline> Itens, int Total)> ConsultarEventosAsync(
        Guid propriedadeId, FiltroEventos filtro, int pagina, int tamanhoPagina, CancellationToken cancellationToken)
    {
        if ((filtro.Origem is not null && filtro.Origem != OrigemEvento.Aplicacao)
            || (filtro.Categoria is not null && filtro.Categoria != CategoriaEvento.Alarme)
            || (filtro.Severidade is not null && filtro.Severidade != SeveridadeEvento.Critico)
            || filtro.Fabricante is not null)
        {
            return (Array.Empty<EventoTimeline>(), 0);
        }

        var query = _db.AlertasCriticos.Where(a => a.PropriedadeId == propriedadeId);

        if (filtro.EquipamentoId is not null)
        {
            query = query.Where(a => a.EquipamentoId == filtro.EquipamentoId);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            query = query.Where(a => a.Descricao.Contains(filtro.Busca));
        }

        if (filtro.DesdeUtc is not null)
        {
            query = query.Where(a => a.CriadoEmUtc >= filtro.DesdeUtc);
        }

        if (filtro.AteUtc is not null)
        {
            query = query.Where(a => a.CriadoEmUtc <= filtro.AteUtc);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var alertas = await query
            .Include(a => a.ConfirmadoPorUsuario)
            .OrderByDescending(a => a.CriadoEmUtc)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var itens = alertas.Select(a => new EventoTimeline
        {
            Id = a.Id,
            Origem = OrigemEvento.Aplicacao,
            Categoria = CategoriaEvento.Alarme,
            Severidade = SeveridadeEvento.Critico,
            Titulo = AlertaCriticoResponse.TituloDe(a.Tipo),
            Descricao = $"{a.Descricao} · {Situacao(a)}",
            OcorridoEmUtc = a.CriadoEmUtc,
        }).ToList();

        return (itens, total);
    }

    internal static string Situacao(AlertaCritico alerta)
    {
        if (alerta.ConfirmadoEmUtc is not null)
        {
            var quem = alerta.ConfirmadoPorUsuario?.Nome ?? "alguém";
            return $"Confirmado por {quem} às {Hora(alerta.ConfirmadoEmUtc.Value)}";
        }

        if (alerta.EscalonadoContatosEmUtc is not null)
        {
            return $"Sem confirmação — contatos de emergência avisados às {Hora(alerta.EscalonadoContatosEmUtc.Value)}";
        }

        if (alerta.EscalonadoMembrosEmUtc is not null)
        {
            return $"Sem confirmação — membros avisados às {Hora(alerta.EscalonadoMembrosEmUtc.Value)}";
        }

        return "Aguardando confirmação";
    }

    private static string Hora(DateTime utc) => (utc + FusoBrasilia).ToString("HH:mm");
}
