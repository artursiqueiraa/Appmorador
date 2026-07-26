using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.VinculosVeiculoVaga;

/// <summary>
/// "Alterar vaga" e "Vincular" são a mesma operação (<see cref="VincularAsync"/>) —
/// vincular a uma vaga diferente da atual encerra o vínculo antigo e cria um novo,
/// preservando o histórico completo (nunca sobrescrito), preparando vagas rotativas
/// futuras sem redesenho (ver ADR 0012).
/// </summary>
public sealed class VeiculoVagaServico : IVeiculoVagaServico
{
    private readonly IVeiculoRepositorio _veiculos;
    private readonly IVagaRepositorio _vagas;
    private readonly IVinculoVeiculoVagaRepositorio _vinculos;
    private readonly IHistoricoVeiculoRepositorio _historico;

    public VeiculoVagaServico(
        IVeiculoRepositorio veiculos,
        IVagaRepositorio vagas,
        IVinculoVeiculoVagaRepositorio vinculos,
        IHistoricoVeiculoRepositorio historico)
    {
        _veiculos = veiculos;
        _vagas = vagas;
        _vinculos = vinculos;
        _historico = historico;
    }

    public async Task<Result<VinculoVeiculoVagaResponse>> VincularAsync(
        Guid proprietarioId, Guid veiculoId, VincularVeiculoVagaRequest request, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (veiculo?.Morador?.Unidade?.Propriedade is null || veiculo.Morador.Unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<VinculoVeiculoVagaResponse>.Fail("Veículo não encontrado.");
        }

        var vaga = await _vagas.GetByIdAsync(request.VagaId, cancellationToken).ConfigureAwait(false);
        if (vaga is null || vaga.Propriedade is null ||
            vaga.PropriedadeId != veiculo.Morador.Unidade.PropriedadeId || vaga.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<VinculoVeiculoVagaResponse>.Fail("Vaga não encontrada.");
        }

        var vinculoAtivoDaVaga = await _vinculos.GetAtivoByVagaAsync(request.VagaId, cancellationToken).ConfigureAwait(false);
        var vinculoAtivoDoVeiculo = await _vinculos.GetAtivoByVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);

        if (vinculoAtivoDaVaga is not null && vinculoAtivoDaVaga.VeiculoId == veiculoId)
        {
            // Ja esta nessa vaga — nada a fazer.
            return Result<VinculoVeiculoVagaResponse>.Ok(ToDto(vinculoAtivoDaVaga, vaga.Numero));
        }

        if (vaga.StatusManual is StatusVaga.Bloqueada or StatusVaga.Reservada)
        {
            return Result<VinculoVeiculoVagaResponse>.Fail("Vaga não está disponível.");
        }

        if (vinculoAtivoDaVaga is not null)
        {
            return Result<VinculoVeiculoVagaResponse>.Fail("Vaga já está ocupada por outro veículo.");
        }

        var agora = DateTime.UtcNow;

        if (vinculoAtivoDoVeiculo is not null)
        {
            vinculoAtivoDoVeiculo.DataFimUtc = agora;
            await RegistrarHistoricoAsync(
                veiculoId, TipoEventoHistoricoVeiculo.VeiculoDesvinculado,
                $"Veículo {veiculo.Placa} desvinculado da vaga {vinculoAtivoDoVeiculo.Vaga?.Numero}.", proprietarioId, cancellationToken)
                .ConfigureAwait(false);
        }

        var novoVinculo = new VinculoVeiculoVaga
        {
            Id = Guid.NewGuid(),
            VeiculoId = veiculoId,
            VagaId = request.VagaId,
            DataInicioUtc = agora,
            DataFimUtc = null,
            CreatedAtUtc = agora,
        };

        await _vinculos.AddAsync(novoVinculo, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(
            veiculoId, TipoEventoHistoricoVeiculo.VeiculoVinculado, $"Veículo {veiculo.Placa} vinculado à vaga {vaga.Numero}.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<VinculoVeiculoVagaResponse>.Ok(ToDto(novoVinculo, vaga.Numero));
    }

    public async Task<Result> DesvincularAsync(Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (veiculo?.Morador?.Unidade?.Propriedade is null || veiculo.Morador.Unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result.Fail("Veículo não encontrado.");
        }

        var vinculoAtivo = await _vinculos.GetAtivoByVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (vinculoAtivo is null)
        {
            return Result.Fail("Veículo não está vinculado a nenhuma vaga.");
        }

        vinculoAtivo.DataFimUtc = DateTime.UtcNow;
        await RegistrarHistoricoAsync(
            veiculoId, TipoEventoHistoricoVeiculo.VeiculoDesvinculado,
            $"Veículo {veiculo.Placa} desvinculado da vaga {vinculoAtivo.Vaga?.Numero}.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<VinculoVeiculoVagaResponse>>> ListHistoricoByVeiculoAsync(
        Guid proprietarioId, Guid veiculoId, CancellationToken cancellationToken)
    {
        var veiculo = await _veiculos.GetByIdAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        if (veiculo?.Morador?.Unidade?.Propriedade is null || veiculo.Morador.Unidade.Propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<VinculoVeiculoVagaResponse>>.Fail("Veículo não encontrado.");
        }

        var vinculos = await _vinculos.ListByVeiculoAsync(veiculoId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<VinculoVeiculoVagaResponse>>.Ok(
            vinculos.Select(v => ToDto(v, v.Vaga?.Numero ?? "")).ToList());
    }

    private async Task RegistrarHistoricoAsync(Guid veiculoId, TipoEventoHistoricoVeiculo tipoEvento, string descricao, Guid usuarioId, CancellationToken cancellationToken) =>
        await _historico.AddAsync(
            new HistoricoVeiculo { Id = Guid.NewGuid(), VeiculoId = veiculoId, TipoEvento = tipoEvento, Descricao = descricao, UsuarioId = usuarioId, CreatedAtUtc = DateTime.UtcNow },
            cancellationToken).ConfigureAwait(false);

    private static VinculoVeiculoVagaResponse ToDto(VinculoVeiculoVaga vinculo, string vagaNumero) => new()
    {
        Id = vinculo.Id,
        VeiculoId = vinculo.VeiculoId,
        VagaId = vinculo.VagaId,
        VagaNumero = vagaNumero,
        DataInicioUtc = vinculo.DataInicioUtc,
        DataFimUtc = vinculo.DataFimUtc,
    };
}
