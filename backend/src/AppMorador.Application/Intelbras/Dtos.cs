using System.ComponentModel.DataAnnotations;

namespace AppMorador.Application.Intelbras;

public sealed class ParticaoIntelbrasRequest
{
    [Required(ErrorMessage = "Partição é obrigatória.")]
    public required int Particao { get; set; }
}

/// <summary>
/// Dado de conexão já decifrado — nunca a entidade <see cref="AppMorador.Domain.Entities.Equipamento"/>
/// EF nem a senha cifrada cruzam essa fronteira (mesmo princípio de
/// <see cref="AppMorador.Application.ControlId.ConexaoEquipamento"/>). Diferente do Control iD,
/// a central Intelbras (linha AMT) não tem conceito de usuário — só uma senha de acesso remoto.
/// </summary>
public sealed class ConexaoIntelbras
{
    public required string Ip { get; init; }

    public required int Porta { get; init; }

    public required string Senha { get; init; }
}

public sealed class ResultadoTesteConexaoIntelbras
{
    public required bool Sucesso { get; init; }

    public string? MensagemErro { get; init; }
}

public sealed class ParticaoIntelbrasStatusInfo
{
    public required int Numero { get; init; }

    public required bool Armada { get; init; }
}

public sealed class StatusCentralIntelbrasInfo
{
    public required IReadOnlyList<ParticaoIntelbrasStatusInfo> Particoes { get; init; }

    public required bool TemProblemaAtivo { get; init; }
}

public sealed class ResultadoComandoIntelbras
{
    public required bool Sucesso { get; init; }

    public string? MensagemErro { get; init; }

    public StatusCentralIntelbrasInfo? StatusResultante { get; init; }
}

/// <summary>Um evento bruto trazido da central — mapeado para EventoEquipamento por quem chama o Provider, nunca aqui (mesmo princípio de EventoImportado do Control iD).</summary>
public sealed class EventoImportadoIntelbras
{
    public required string CodigoEventoOriginal { get; init; }

    public required string Descricao { get; init; }

    public required DateTime OcorridoEmUtc { get; init; }
}

public sealed class ImportacaoEventosIntelbrasResponse
{
    public required int QuantidadeImportada { get; init; }
}
