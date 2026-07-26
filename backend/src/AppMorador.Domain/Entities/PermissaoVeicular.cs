using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Vínculo Veículo ↔ PontoAcesso (mesmo formato de <see cref="PermissaoAcesso"/>, Sprint 7) —
/// o PontoAcesso referenciado precisa ter <see cref="TipoPontoAcesso.Veicular"/>. Sem regras de
/// dia/horário (diferente de PermissaoAcesso) — Sprint 9 só estrutura o domínio, sem
/// integração real com equipamento.
/// </summary>
public class PermissaoVeicular : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid VeiculoId { get; set; }

    public Veiculo? Veiculo { get; set; }

    public Guid PontoAcessoId { get; set; }

    public PontoAcesso? PontoAcesso { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
