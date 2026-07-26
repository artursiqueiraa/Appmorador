using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Veiculos;

/// <summary>
/// Ownership resolvido via Veiculo→Morador→Unidade→Propriedade.ProprietarioId, mesmo
/// padrão já usado em todo o domínio principal. Placa é normalizada (maiúscula, sem
/// espaço nas pontas) e validada como única entre veículos não excluídos — a
/// unicidade é verificada em código, não por índice único no banco (ver ADR 0012),
/// para permitir que uma placa seja recadastrada depois que o veículo antigo é
/// excluído logicamente.
/// </summary>
public sealed class VeiculoServico : IVeiculoServico
{
    private readonly IMoradorRepositorio _moradores;
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IVinculoVeiculoVagaRepositorio _vinculos;
    private readonly IPermissaoVeicularRepositorio _permissoesVeiculares;
    private readonly IHistoricoVeiculoRepositorio _historico;

    public VeiculoServico(
        IMoradorRepositorio moradores,
        IVeiculoRepositorio veiculos,
        IVinculoVeiculoVagaRepositorio vinculos,
        IPermissaoVeicularRepositorio permissoesVeiculares,
        IHistoricoVeiculoRepositorio historico)
    {
        _moradores = moradores;
        _veiculos = veiculos;
        _vinculos = vinculos;
        _permissoesVeiculares = permissoesVeiculares;
        _historico = historico;
    }

    public async Task<Result<VeiculoResponse>> CreateAsync(
        Guid proprietarioId, Guid moradorId, CriarVeiculoRequest request, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(morador, proprietarioId))
        {
            return Result<VeiculoResponse>.Fail("Morador não encontrado.");
        }

        var placaNormalizada = NormalizarPlaca(request.Placa);
        var existente = await _veiculos.GetByPlacaAsync(placaNormalizada, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return Result<VeiculoResponse>.Fail("Já existe um veículo cadastrado com essa placa.");
        }

        var veiculo = new Veiculo
        {
            Id = Guid.NewGuid(),
            MoradorId = moradorId,
            Placa = placaNormalizada,
            Marca = NullIfBlank(request.Marca),
            Modelo = NullIfBlank(request.Modelo),
            Cor = NullIfBlank(request.Cor),
            Ano = request.Ano,
            Observacoes = NullIfBlank(request.Observacoes),
            Tipo = request.Tipo,
            Status = StatusVeiculo.Ativo,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _veiculos.AddAsync(veiculo, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(veiculo.Id, TipoEventoHistoricoVeiculo.VeiculoCriado, $"Veículo {veiculo.Placa} criado.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _veiculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VeiculoResponse>.Ok(ToDto(veiculo));
    }

    public async Task<Result<IReadOnlyList<VeiculoResponse>>> ListByMoradorAsync(
        Guid proprietarioId, Guid moradorId, CancellationToken cancellationToken)
    {
        var morador = await _moradores.GetByIdAsync(moradorId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(morador, proprietarioId))
        {
            return Result<IReadOnlyList<VeiculoResponse>>.Fail("Morador não encontrado.");
        }

        var veiculos = await _veiculos.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<VeiculoResponse>>.Ok(veiculos.Select(ToDto).ToList());
    }

    public async Task<Result<VeiculoResponse>> UpdateAsync(
        Guid proprietarioId, Guid veiculoId, AtualizarVeiculoRequest request, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(veiculo, proprietarioId))
        {
            return Result<VeiculoResponse>.Fail("Veículo não encontrado.");
        }

        var placaNormalizada = NormalizarPlaca(request.Placa);
        if (placaNormalizada != veiculo!.Placa)
        {
            var existente = await _veiculos.GetByPlacaAsync(placaNormalizada, cancellationToken).ConfigureAwait(false);
            if (existente is not null && existente.Id != veiculoId)
            {
                return Result<VeiculoResponse>.Fail("Já existe um veículo cadastrado com essa placa.");
            }
        }

        veiculo.Placa = placaNormalizada;
        veiculo.Marca = NullIfBlank(request.Marca);
        veiculo.Modelo = NullIfBlank(request.Modelo);
        veiculo.Cor = NullIfBlank(request.Cor);
        veiculo.Ano = request.Ano;
        veiculo.Observacoes = NullIfBlank(request.Observacoes);
        veiculo.Tipo = request.Tipo;
        veiculo.Status = request.Status;

        await RegistrarHistoricoAsync(veiculo.Id, TipoEventoHistoricoVeiculo.VeiculoAlterado, $"Veículo {veiculo.Placa} alterado.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _veiculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VeiculoResponse>.Ok(ToDto(veiculo));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(veiculo, proprietarioId))
        {
            return Result.Fail("Veículo não encontrado.");
        }

        var agora = DateTime.UtcNow;
        var vinculosDoVeiculo = await _vinculos.ListByVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        var permissoesDoVeiculo = await _permissoesVeiculares.ListByVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);

        veiculo!.Excluido = true;
        veiculo.DataExclusaoUtc = agora;
        veiculo.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var vinculo in vinculosDoVeiculo)
        {
            vinculo.Excluido = true;
            vinculo.DataExclusaoUtc = agora;
            vinculo.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissao in permissoesDoVeiculo)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        await RegistrarHistoricoAsync(veiculo.Id, TipoEventoHistoricoVeiculo.VeiculoRemovido, $"Veículo {veiculo.Placa} excluído.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _veiculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private async Task RegistrarHistoricoAsync(Guid veiculoId, TipoEventoHistoricoVeiculo tipoEvento, string descricao, Guid usuarioId, CancellationToken cancellationToken) =>
        await _historico.AddAsync(
            new HistoricoVeiculo { Id = Guid.NewGuid(), VeiculoId = veiculoId, TipoEvento = tipoEvento, Descricao = descricao, UsuarioId = usuarioId, CreatedAtUtc = DateTime.UtcNow },
            cancellationToken).ConfigureAwait(false);

    private static string NormalizarPlaca(string placa) => placa.Trim().ToUpperInvariant();

    private static bool PertenceAoProprietario(Morador? morador, Guid proprietarioId) =>
        morador?.Unidade?.Propriedade is not null && morador.Unidade.Propriedade.ProprietarioId == proprietarioId;

    private static bool PertenceAoProprietario(Veiculo? veiculo, Guid proprietarioId) =>
        veiculo?.Morador?.Unidade?.Propriedade is not null && veiculo.Morador.Unidade.Propriedade.ProprietarioId == proprietarioId;

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static VeiculoResponse ToDto(Veiculo veiculo) => new()
    {
        Id = veiculo.Id,
        MoradorId = veiculo.MoradorId,
        Placa = veiculo.Placa,
        Marca = veiculo.Marca,
        Modelo = veiculo.Modelo,
        Cor = veiculo.Cor,
        Ano = veiculo.Ano,
        Observacoes = veiculo.Observacoes,
        Tipo = veiculo.Tipo,
        Status = veiculo.Status,
    };
}
