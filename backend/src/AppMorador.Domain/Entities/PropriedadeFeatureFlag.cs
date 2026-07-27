namespace AppMorador.Domain.Entities;

/// <summary>Vínculo NxN entre <see cref="Propriedade"/> e <see cref="FeatureFlag"/> — <see cref="Ativo"/> permite desativar temporariamente sem apagar o histórico de contratação.</summary>
public class PropriedadeFeatureFlag
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public FeatureFlag Feature { get; set; }

    public bool Ativo { get; set; } = true;

    public required DateTime AtivadoEmUtc { get; set; }
}
