namespace AppMorador.Domain.Entities;

/// <summary>Conta de acesso ao app. Um usuario pode ser dono de varias propriedades.</summary>
public class Usuario
{
    public Guid Id { get; set; }

    public required string Nome { get; set; }

    public required string Email { get; set; }

    /// <summary>Hash da senha (BCrypt) — a senha em texto puro nunca e persistida.</summary>
    public required string SenhaHash { get; set; }

    public int TentativasFalhas { get; set; }

    /// <summary>Enquanto no futuro, novas tentativas de entrada sao rejeitadas mesmo com senha certa.</summary>
    public DateTime? BloqueadoAteUtc { get; set; }

    /// <summary>
    /// Rotacionado quando a senha muda (ainda nao implementado, mas o campo ja existe)
    /// — permite invalidar todos os tokens emitidos antes da troca, mesmo tokens de
    /// acesso ainda nao expirados. Mesmo padrao usado no Teste-portaria-main1
    /// (SystemUser.SecurityStamp).
    /// </summary>
    public Guid SecurityStamp { get; set; } = Guid.NewGuid();

    public required DateTime CreatedAtUtc { get; set; }
}
