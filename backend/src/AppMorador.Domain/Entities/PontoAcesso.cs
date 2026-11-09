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

    /// <summary>
    /// Sprint 24.5 — o que abre esta porta: a central JFL (saída PGM) ou o leitor facial Control iD
    /// (porta/relé). Opcional: um ponto pode existir só para permissões de acesso.
    /// </summary>
    public Guid? EquipamentoAcionadorId { get; set; }

    public Equipamento? EquipamentoAcionador { get; set; }

    /// <summary>Número da PGM (JFL) ou da porta (Control iD) — começa em 1.</summary>
    public int? NumeroSaida { get; set; }

    /// <summary>Sprint 24.5 — câmera que mostra esta porta (aparece antes de abrir).</summary>
    public Guid? CameraId { get; set; }

    public Camera? Camera { get; set; }
}
