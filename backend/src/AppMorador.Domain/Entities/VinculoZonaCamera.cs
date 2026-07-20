namespace AppMorador.Domain.Entities;

/// <summary>
/// Vinculo entre uma <see cref="Zona"/> e uma <see cref="Camera"/>. Entidade de
/// vinculo deliberada (nao FK cravada dentro de Zona): hoje o uso e 1:1, mas
/// permitir zona com mais de uma camera (ou camera cobrindo mais de uma zona) no
/// futuro nao deve exigir mudanca de schema.
/// </summary>
public class VinculoZonaCamera
{
    public Guid Id { get; set; }

    public Guid ZonaId { get; set; }

    public Zona? Zona { get; set; }

    public Guid CameraId { get; set; }

    public Camera? Camera { get; set; }
}
