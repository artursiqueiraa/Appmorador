namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 25 (ADR 0041) — convite para alguém acessar uma propriedade com um papel. O código só
/// existe no link enviado (e-mail e/ou WhatsApp); aqui fica só o hash. Uso único, vale 48 h e o dono
/// pode revogar. Aceito, vira um <see cref="UsuarioPropriedade"/> com o papel escolhido.
/// </summary>
public class ConvitePropriedade
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public PerfilPropriedade Papel { get; set; }

    /// <summary>E-mail para onde o convite foi enviado (também mostrado na lista de convites pendentes).</summary>
    public required string Email { get; set; }

    /// <summary>SHA-256 do código (hex). O código em si nunca é guardado.</summary>
    public required string CodigoHash { get; set; }

    public Guid CriadoPorUsuarioId { get; set; }

    public DateTime CriadoEmUtc { get; set; }

    public DateTime ExpiraEmUtc { get; set; }

    public Guid? AceitoPorUsuarioId { get; set; }

    public DateTime? AceitoEmUtc { get; set; }

    public DateTime? RevogadoEmUtc { get; set; }

    public bool Pendente(DateTime agoraUtc) => AceitoEmUtc is null && RevogadoEmUtc is null && ExpiraEmUtc > agoraUtc;
}
