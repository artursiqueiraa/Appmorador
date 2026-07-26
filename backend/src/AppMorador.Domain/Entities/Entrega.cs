using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Pertence a um Morador destinatário e a uma Unidade (validado consistente: o
/// Morador precisa pertencer a essa Unidade — mesmo padrão de <see cref="Autorizacao"/>,
/// ADR 0011). DataRecebimentoUtc/DataRetiradaUtc começam nulas — são preenchidas pelas
/// ações explícitas "marcar disponível"/"registrar retirada" (ver ADR 0013), nunca por
/// um job.
/// </summary>
public class Entrega : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid MoradorDestinatarioId { get; set; }

    public Morador? MoradorDestinatario { get; set; }

    public Guid UnidadeId { get; set; }

    public Unidade? Unidade { get; set; }

    public required TipoEntrega Tipo { get; set; }

    public string? Descricao { get; set; }

    /// <summary>Quem recebeu fisicamente a entrega (texto livre — sem cadastro de porteiro/funcionário neste domínio). Preenchido junto da transição para DisponivelParaRetirada.</summary>
    public string? RecebidoPor { get; set; }

    public DateTime? DataRecebimentoUtc { get; set; }

    public DateTime? DataRetiradaUtc { get; set; }

    public string? Observacoes { get; set; }

    public required StatusEntrega Status { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
