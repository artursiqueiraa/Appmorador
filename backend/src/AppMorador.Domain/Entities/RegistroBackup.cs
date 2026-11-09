namespace AppMorador.Domain.Entities;

public enum TipoRegistroBackup
{
    Backup,
    TesteRestauracao,
}

/// <summary>
/// Sprint 27 (ADR 0043) — resultado de cada backup agendado e de cada teste de restauração, para o
/// painel mostrar "último backup" e o vigia de saúde avisar quando falhar ou atrasar.
/// </summary>
public class RegistroBackup
{
    public Guid Id { get; set; }

    public TipoRegistroBackup Tipo { get; set; }

    public bool Sucesso { get; set; }

    public string? Arquivo { get; set; }

    public long? TamanhoBytes { get; set; }

    public string? Mensagem { get; set; }

    public DateTime OcorridoEmUtc { get; set; }
}
