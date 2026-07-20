namespace AppMorador.Domain.Entities;

/// <summary>Uma central de alarme JFL instalada em uma <see cref="Propriedade"/>.</summary>
public class Central
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    /// <summary>Numero de serie do equipamento (campo NS do protocolo) — chave de correlacao com a sessao TCP.</summary>
    public required string NumeroSerie { get; set; }

    public required string Nome { get; set; }
}
