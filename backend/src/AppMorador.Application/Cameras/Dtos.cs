using AppMorador.Domain.Entities;

namespace AppMorador.Application.Cameras;

/// <summary>Item da lista de câmeras de uma Propriedade (GET /api/properties/{id}/cameras).</summary>
public sealed class CameraResponse
{
    public Guid Id { get; init; }

    public required string Nome { get; init; }

    public required StatusCamera Status { get; init; }

    /// <summary>Caminho da Api (nunca absoluto/externo) para os bytes da imagem — o mobile monta a URL completa com o próprio host + anexa o Bearer token. Null se nunca houve captura com sucesso.</summary>
    public string? UltimaImagemUrl { get; init; }

    public DateTime? UltimaVezVistaUtc { get; init; }
}

/// <summary>
/// Metadados de um snapshot — mesma forma para GET (metadados da última captura já
/// salva) e POST (resultado de uma captura nova sob demanda). <see cref="Sucesso"/>
/// só existe para distinguir os dois casos do POST (a última imagem disponível
/// sempre é devolvida, mesmo quando a tentativa mais recente falhou).
/// </summary>
public sealed class CameraSnapshotResponse
{
    public required bool Sucesso { get; init; }

    /// <summary>Mensagem amigável — nunca o texto técnico do provider/exceção.</summary>
    public string? MensagemErro { get; init; }

    public string? UltimaImagemUrl { get; init; }

    public DateTime? CapturadaEmUtc { get; init; }

    public required StatusCamera Status { get; init; }
}

/// <summary>Sprint 20 — capturas reais são sempre JPEG (providers CGI/ISAPI), mas o content-type é sniffado pela assinatura do arquivo, nunca fixo — o seed de desenvolvimento grava um PNG (ver ADR 0024) e precisa ser servido corretamente também.</summary>
public sealed class CameraImagemArquivo
{
    public required Stream Conteudo { get; init; }

    public required string ContentType { get; init; }
}

public sealed class CameraStatusResponse
{
    public required StatusCamera Status { get; init; }

    public DateTime? UltimaTentativaCapturaUtc { get; init; }

    public DateTime? UltimoSucessoCapturaUtc { get; init; }

    /// <summary>Amigável, nunca o erro técnico do provider/CGI/ISAPI — null quando não está Offline.</summary>
    public string? MotivoIndisponibilidade { get; init; }
}
