using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Uma credencial de acesso de um <see cref="Morador"/> (Sprint 7 — Controle de
/// Acesso Inteligente). Nesta Sprint é só domínio/regras — nenhuma comunicação real
/// com leitor biométrico/RFID/QR Code (ver ADR 0010).
/// </summary>
public class Credencial : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid MoradorId { get; set; }

    public Morador? Morador { get; set; }

    public required TipoCredencial Tipo { get; set; }

    public required StatusCredencial Status { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
