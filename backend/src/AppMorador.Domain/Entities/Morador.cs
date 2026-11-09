using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Um morador de uma <see cref="Unidade"/>. Biometria/reconhecimento facial não são
/// implementados nesta Sprint (Sprint 6) — <see cref="FotoPath"/> só prepara o campo
/// (mesmo padrão de <see cref="Ocorrencia.ImagePath"/>: caminho relativo, sem
/// pipeline de upload/armazenamento ainda).
/// </summary>
public class Morador : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid UnidadeId { get; set; }

    public Unidade? Unidade { get; set; }

    public required string Nome { get; set; }

    /// <summary>Caminho relativo de uma foto de perfil — nunca a biometria em si. Preparado, não usado ainda.</summary>
    public string? FotoPath { get; set; }

    public string? Telefone { get; set; }

    public string? Email { get; set; }

    /// <summary>Documento de identificação (CPF/RG) — texto livre, sem validação de formato nesta Sprint.</summary>
    public string? Documento { get; set; }

    public required StatusMorador Status { get; set; }

    /// <summary>Akteck Seg (ADR 0036) — titular ou dependente. Registros anteriores viram Titular (backfill da migration).</summary>
    public TipoMorador Tipo { get; set; } = TipoMorador.Titular;

    /// <summary>Akteck Seg (ADR 0036) — preenchido só para dependentes; sempre nulo em titulares.</summary>
    public Parentesco? Parentesco { get; set; }

    public string? Observacoes { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
