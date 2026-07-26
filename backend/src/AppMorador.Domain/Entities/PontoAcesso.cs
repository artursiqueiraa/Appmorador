using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Um ponto físico de acesso (Portão Principal, Garagem, Academia...) de uma
/// <see cref="Propriedade"/> — infraestrutura compartilhada de toda a propriedade,
/// nunca de uma Unidade específica. Sprint 7 — sem integração real com equipamento.
/// </summary>
public class PontoAcesso : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required string Nome { get; set; }

    /// <summary>Sprint 9 — Geral por padrão; Veicular é o único tipo que <see cref="PermissaoVeicular"/> pode referenciar.</summary>
    public required TipoPontoAcesso Tipo { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
