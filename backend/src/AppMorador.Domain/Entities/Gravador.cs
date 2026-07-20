namespace AppMorador.Domain.Entities;

public enum FabricanteGravador
{
    Intelbras,
    Dahua,
    Hikvision,
}

/// <summary>O gravador (DVR/NVR) instalado em uma <see cref="Propriedade"/>, falado via CGI ou ISAPI.</summary>
public class Gravador
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required FabricanteGravador Fabricante { get; set; }

    public required string Ip { get; set; }

    public required int Porta { get; set; }

    public required string NomeAcesso { get; set; }

    public required string Senha { get; set; }
}
