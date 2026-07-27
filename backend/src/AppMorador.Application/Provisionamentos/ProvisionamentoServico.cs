using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Provisionamentos;

public sealed class ProvisionamentoServico : IProvisionamentoServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IProvisionamentoRepositorio _provisionamentos;

    public ProvisionamentoServico(IPropriedadeRepositorio propriedades, IProvisionamentoRepositorio provisionamentos)
    {
        _propriedades = propriedades;
        _provisionamentos = provisionamentos;
    }

    public async Task<Result<ProvisionamentoResponse>> CriarAsync(Guid propriedadeId, CriarProvisionamentoRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<ProvisionamentoResponse>.Fail("Propriedade não encontrada.");
        }

        var provisionamento = new Provisionamento
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Nome = request.Nome.Trim(),
            Template = request.Template,
            Status = StatusProvisionamento.Rascunho,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _provisionamentos.AddAsync(provisionamento, cancellationToken).ConfigureAwait(false);
        await _provisionamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ProvisionamentoResponse>.Ok(ToDto(provisionamento));
    }

    public async Task<Result<IReadOnlyList<ProvisionamentoResponse>>> ListarAsync(Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<IReadOnlyList<ProvisionamentoResponse>>.Fail("Propriedade não encontrada.");
        }

        var lista = await _provisionamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<ProvisionamentoResponse>>.Ok(lista.Select(ToDto).ToList());
    }

    public async Task<Result<ProvisionamentoResponse>> ArquivarAsync(Guid id, CancellationToken cancellationToken)
    {
        var provisionamento = await _provisionamentos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (provisionamento is null)
        {
            return Result<ProvisionamentoResponse>.Fail("Provisionamento não encontrado.");
        }

        provisionamento.Status = StatusProvisionamento.Arquivado;
        provisionamento.AtualizadoEmUtc = DateTime.UtcNow;
        await _provisionamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ProvisionamentoResponse>.Ok(ToDto(provisionamento));
    }

    private static ProvisionamentoResponse ToDto(Provisionamento provisionamento) => new()
    {
        Id = provisionamento.Id,
        PropriedadeId = provisionamento.PropriedadeId,
        Nome = provisionamento.Nome,
        Template = provisionamento.Template,
        Status = provisionamento.Status,
        CreatedAtUtc = provisionamento.CreatedAtUtc,
        AtualizadoEmUtc = provisionamento.AtualizadoEmUtc,
    };
}
