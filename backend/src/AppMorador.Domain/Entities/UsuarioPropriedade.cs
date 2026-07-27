namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0021) — vínculo entre um Usuario cliente e uma Propriedade, com o
/// Perfil que ele tem nela. Existe como entidade própria (preparando o domínio para
/// múltiplos usuários por propriedade no futuro — ver dívida técnica), mas NESTA
/// Sprint o único vínculo criado automaticamente é o do dono (Administrador),
/// espelhando <see cref="Propriedade.ProprietarioId"/> — nenhum código existente foi
/// migrado para depender desta tabela em vez do campo já existente. Código NOVO
/// (Permissões Funcionais, Fase 25+) já lê por aqui, nunca por
/// <see cref="Propriedade.ProprietarioId"/> diretamente, para não criar um segundo
/// ponto de acoplamento que precisaria ser desfeito na Sprint de Multiusuário.
/// </summary>
public class UsuarioPropriedade
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public PerfilPropriedade Perfil { get; set; }

    public required DateTime CreatedAtUtc { get; set; }

    public bool Ativo { get; set; } = true;
}
