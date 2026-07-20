namespace AppMorador.Domain.Entities;

/// <summary>Uma zona monitorada por uma <see cref="Central"/>.</summary>
public class Zona
{
    public Guid Id { get; set; }

    public Guid CentralId { get; set; }

    public Central? Central { get; set; }

    /// <summary>Numero da zona conforme reportado pela central (campo U/Z do evento, 3 digitos ASCII).</summary>
    public required string Numero { get; set; }

    public required string Nome { get; set; }
}
