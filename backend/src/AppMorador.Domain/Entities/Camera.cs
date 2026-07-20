namespace AppMorador.Domain.Entities;

/// <summary>Um canal de câmera de um <see cref="Gravador"/>.</summary>
public class Camera
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public Guid GravadorId { get; set; }

    public Gravador? Gravador { get; set; }

    /// <summary>Numero do canal no gravador (ex.: 1, 2, 3...).</summary>
    public required int Canal { get; set; }

    public required string Nome { get; set; }
}
