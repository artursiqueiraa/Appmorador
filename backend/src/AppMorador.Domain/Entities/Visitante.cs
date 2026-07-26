using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>Pertence direto à Propriedade (não à Unidade) — reaproveitável entre autorizações de unidades diferentes, mesmo padrão de <see cref="PontoAcesso"/> (ADR 0010).</summary>
public class Visitante : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required string Nome { get; set; }

    public string? Documento { get; set; }

    public string? Telefone { get; set; }

    /// <summary>Preparado para reconhecimento facial futuro — nunca preenchido nesta Sprint.</summary>
    public string? FotoPath { get; set; }

    public string? Observacoes { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
