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

    /// <summary>
    /// Sprint 21 (ADR 0021) — null para todo cliente (dono de propriedade); só
    /// preenchido para os 3 papéis internos da plataforma (Master/Tecnico/Suporte).
    /// Nunca coexiste com um vínculo em <see cref="UsuarioPropriedade"/> — um usuário
    /// é interno OU cliente, nunca as duas coisas.
    /// </summary>
    public RoleSistema? RoleGlobal { get; set; }

    /// <summary>Sprint 21 — desativar uma conta interna (ex.: técnico que saiu da equipe) sem excluir o histórico/auditoria associado.</summary>
    public bool Ativo { get; set; } = true;

    public required DateTime CreatedAtUtc { get; set; }
}
