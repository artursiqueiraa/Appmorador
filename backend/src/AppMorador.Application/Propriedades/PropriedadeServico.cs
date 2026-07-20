using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Propriedades;

public sealed class PropriedadeServico : IPropriedadeServico
{
    private readonly IPropriedadeRepositorio _propriedades;

    public PropriedadeServico(IPropriedadeRepositorio propriedades)
    {
        _propriedades = propriedades;
    }

    public async Task<PropriedadeResponse> CreateAsync(Guid proprietarioId, CriarPropriedadeRequest request, CancellationToken cancellationToken)
    {
        var propriedade = new Propriedade
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome.Trim(),
            Tipo = request.Tipo,
            Endereco = request.Endereco?.Trim(),
            ProprietarioId = proprietarioId,
        };

        await _propriedades.AddAsync(propriedade, cancellationToken).ConfigureAwait(false);
        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ToDto(propriedade);
    }

    public async Task<IReadOnlyList<PropriedadeResponse>> ListByOwnerAsync(Guid proprietarioId, CancellationToken cancellationToken)
    {
        var propriedades = await _propriedades.ListByOwnerAsync(proprietarioId, cancellationToken).ConfigureAwait(false);
        return propriedades.Select(ToDto).ToList();
    }

    public async Task<Result<PropriedadeResponse>> UpdateAsync(
        Guid proprietarioId, Guid propriedadeId, AtualizarPropriedadeRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);

        // Mesma mensagem para "nao existe" e "existe mas nao e do usuario" — nao
        // revela para o cliente que uma propriedade de outro dono existe com este Id.
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<PropriedadeResponse>.Fail("Propriedade não encontrada.");
        }

        propriedade.Nome = request.Nome.Trim();
        propriedade.Tipo = request.Tipo;
        propriedade.Endereco = request.Endereco?.Trim();
        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PropriedadeResponse>.Ok(ToDto(propriedade));
    }

    private static PropriedadeResponse ToDto(Propriedade propriedade) => new()
    {
        Id = propriedade.Id,
        Nome = propriedade.Nome,
        Tipo = propriedade.Tipo,
        Endereco = propriedade.Endereco,
    };
}
