using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 24 (ADR 0040) — pessoa de confiança que não tem o app (vizinho, parente) e recebe SMS
/// quando um alerta crítico fica sem confirmação. Ordem define quem aparece primeiro na tela de
/// alerta ("Ligar para…"). Soft delete (ADR 0009).
/// </summary>
public class ContatoEmergencia : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required string Nome { get; set; }

    /// <summary>Só dígitos, com DDD (e DDI opcional) — normalizado na Application.</summary>
    public required string Telefone { get; set; }

    public int Ordem { get; set; }

    public DateTime CriadoEmUtc { get; set; }
}
