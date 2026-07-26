namespace AppMorador.Application.Equipamentos;

public sealed class TesteConexaoResponse
{
    public required bool Sucesso { get; init; }

    public string? MensagemErro { get; init; }
}

public sealed class InformacoesEquipamentoResponse
{
    public required string Versao { get; init; }

    public string? NomeDispositivo { get; init; }

    public string? NumeroSerie { get; init; }
}

public sealed class SincronizacaoResponse
{
    public required int QuantidadeProcessada { get; init; }
}

public sealed class ImportacaoEventosResponse
{
    public required int QuantidadeImportada { get; init; }
}
