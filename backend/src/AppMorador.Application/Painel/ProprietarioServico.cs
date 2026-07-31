using AppMorador.Application.Common;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel;

public sealed class ProprietarioServico : IProprietarioServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPropriedadeRepositorio _propriedades;

    public ProprietarioServico(IUsuarioRepositorio usuarios, IPropriedadeRepositorio propriedades)
    {
        _usuarios = usuarios;
        _propriedades = propriedades;
    }

    public async Task<Result<ProprietarioDetalheResponse>> ObterDetalheAsync(Guid id, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (usuario is null || usuario.RoleGlobal is not null)
        {
            return Result<ProprietarioDetalheResponse>.Fail("Cliente não encontrado.");
        }

        var propriedades = await _propriedades.ListByOwnerAsync(id, cancellationToken).ConfigureAwait(false);

        return Result<ProprietarioDetalheResponse>.Ok(new ProprietarioDetalheResponse
        {
            Id = usuario.Id,
            Nome = usuario.Nome,
            Email = usuario.Email,
            Ativo = usuario.Ativo,
            CreatedAtUtc = usuario.CreatedAtUtc,
            Propriedades = propriedades
                .Select(p => new PropriedadeResumoResponse { Id = p.Id, Nome = p.Nome, Tipo = p.Tipo.ToString() })
                .ToList(),
        });
    }

    public async Task<ProprietariosPaginadosResponse> ListarAsync(
        int pagina, int tamanhoPagina, string? busca, CancellationToken cancellationToken)
    {
        pagina = pagina <= 0 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is <= 0 or > 100 ? 20 : tamanhoPagina;

        var (itens, total) = await _usuarios.ListProprietariosAsync(pagina, tamanhoPagina, busca, cancellationToken).ConfigureAwait(false);
        var contagem = await _propriedades
            .ContarPorProprietariosAsync(itens.Select(u => u.Id).ToList(), cancellationToken)
            .ConfigureAwait(false);

        return new ProprietariosPaginadosResponse
        {
            Itens = itens.Select(u => new ProprietarioResponse
            {
                Id = u.Id,
                Nome = u.Nome,
                Email = u.Email,
                Ativo = u.Ativo,
                CreatedAtUtc = u.CreatedAtUtc,
                QuantidadePropriedades = contagem.GetValueOrDefault(u.Id, 0),
            }).ToList(),
            PaginaAtual = pagina,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanhoPagina),
            TotalItens = total,
        };
    }
}
