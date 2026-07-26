namespace AppMorador.Application.ControlId;

/// <summary>
/// Dado de conexão já decifrado — nunca a entidade <see cref="AppMorador.Domain.Entities.Equipamento"/>
/// EF nem a senha cifrada cruzam essa fronteira. Quem monta isso
/// (<see cref="AppMorador.Application.Equipamentos.EquipamentoIntegracaoServico"/>) é o único ponto
/// que decifra a senha antes de chamar o Provider.
/// </summary>
public sealed class ConexaoEquipamento
{
    public required string Ip { get; init; }

    public required int Porta { get; init; }

    public required string Usuario { get; init; }

    public required string Senha { get; init; }
}

public sealed class ResultadoTesteConexao
{
    public required bool Sucesso { get; init; }

    public string? MensagemErro { get; init; }
}

public sealed class InformacoesEquipamento
{
    public required string Versao { get; init; }

    public string? NomeDispositivo { get; init; }

    public string? NumeroSerie { get; init; }
}

/// <summary>Um morador candidato a sincronizar — montado a partir do domínio já existente, nunca um tipo novo de "usuário".</summary>
public sealed class MoradorParaSincronizar
{
    public required Guid MoradorId { get; init; }

    public required string Nome { get; init; }
}

public sealed class CredencialParaSincronizar
{
    public required Guid CredencialId { get; init; }

    public required Guid MoradorId { get; init; }

    public required string TipoCredencial { get; init; }

    /// <summary>Valor bruto da credencial (ex.: número da tag/PIN) — Facial/Biometria não têm valor sincronizável nesta Sprint (fora de escopo: captura biométrica real).</summary>
    public string? Valor { get; init; }
}

public sealed class PermissaoParaSincronizar
{
    public required Guid CredencialId { get; init; }

    public required string DiasPermitidos { get; init; }

    public TimeOnly? HorarioInicial { get; init; }

    public TimeOnly? HorarioFinal { get; init; }
}

public sealed class ResultadoSincronizacao
{
    public required bool Sucesso { get; init; }

    public required int QuantidadeProcessada { get; init; }

    public string? MensagemErro { get; init; }
}

/// <summary>Um evento bruto trazido do equipamento — mapeado para EventoEquipamento pela camada que chama o Provider, nunca aqui.</summary>
public sealed class EventoImportado
{
    public required string CodigoEventoOriginal { get; init; }

    public required string Descricao { get; init; }

    public required DateTime OcorridoEmUtc { get; init; }
}
