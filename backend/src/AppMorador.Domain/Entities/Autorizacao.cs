using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Vínculo Visitante ↔ Unidade, responsabilizado por um Morador da própria Unidade. O
/// Status efetivo é híbrido (ver <see cref="AppMorador.Application.Autorizacoes.AutorizacaoServico"/>):
/// Pendente/Ativa/Expirada são computados a partir das datas/horários em tempo de leitura
/// (sem job/scheduler); <see cref="StatusManual"/> só é gravado quando o usuário cancela ou
/// marca como utilizada — um estado manual sempre vence o cálculo por data.
/// </summary>
public class Autorizacao : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid MoradorResponsavelId { get; set; }

    public Morador? MoradorResponsavel { get; set; }

    public Guid UnidadeId { get; set; }

    public Unidade? Unidade { get; set; }

    public Guid VisitanteId { get; set; }

    public Visitante? Visitante { get; set; }

    public required TipoVisita Tipo { get; set; }

    public required DateTime DataInicial { get; set; }

    public required DateTime DataFinal { get; set; }

    public TimeOnly? HorarioInicial { get; set; }

    public TimeOnly? HorarioFinal { get; set; }

    /// <summary>Nulo = sem override manual (status efetivo computado por data). Só assume Cancelada ou Utilizada.</summary>
    public StatusAutorizacao? StatusManual { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
