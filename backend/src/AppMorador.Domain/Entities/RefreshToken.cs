namespace AppMorador.Domain.Entities;

/// <summary>
/// Um refresh token emitido para um <see cref="Usuario"/>. Nunca guarda o token em
/// texto puro — so o hash (SHA-256) dele, para que um vazamento do banco nao
/// exponha tokens utilizaveis diretamente. Rotacionado a cada uso: o token antigo e
/// revogado e um novo e emitido (ReplacedByTokenHash rastreia a cadeia).
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public required string TokenHash { get; set; }

    public required DateTime ExpiresAtUtc { get; set; }

    public required DateTime CreatedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
}
