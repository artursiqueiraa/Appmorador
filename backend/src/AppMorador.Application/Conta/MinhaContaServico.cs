using AppMorador.Application.Autenticacao;
using AppMorador.Application.Common;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Conta;

public sealed class MinhaContaResponse
{
    public required string Nome { get; init; }

    public required string Email { get; init; }

    public string? Telefone { get; init; }
}

public sealed class AtualizarMinhaContaRequest
{
    public string Nome { get; init; } = string.Empty;

    public string? Telefone { get; init; }
}

public sealed class TrocarSenhaRequest
{
    public string SenhaAtual { get; init; } = string.Empty;

    public string NovaSenha { get; init; } = string.Empty;
}

public sealed class ExcluirContaRequest
{
    public string Senha { get; init; } = string.Empty;
}

public interface IMinhaContaServico
{
    Task<Result<MinhaContaResponse>> ObterAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task<Result<MinhaContaResponse>> AtualizarAsync(Guid usuarioId, AtualizarMinhaContaRequest request, CancellationToken cancellationToken);

    Task<Result> TrocarSenhaAsync(Guid usuarioId, TrocarSenhaRequest request, CancellationToken cancellationToken);

    Task<Result> ExcluirAsync(Guid usuarioId, ExcluirContaRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 25 (ADR 0041) — o próprio usuário cuida da conta (fecha a dívida 47). Excluir é
/// anonimizar (decisão do usuário, LGPD): some nome, e-mail, telefone e senha; o histórico de
/// segurança (alarmes, aberturas, alertas) fica, sem identificar a pessoa. Quem é dono de uma
/// propriedade com outras pessoas precisa remover essas pessoas antes — senão elas perderiam o acesso
/// sem aviso.
/// </summary>
public sealed class MinhaContaServico : IMinhaContaServico
{
    public const int TamanhoMinimoSenha = 8;

    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioPropriedadeRepositorio _vinculos;
    private readonly IPasswordHasher _hasher;
    private readonly ISessoesUsuario _sessoes;

    public MinhaContaServico(
        IUsuarioRepositorio usuarios,
        IPropriedadeRepositorio propriedades,
        IUsuarioPropriedadeRepositorio vinculos,
        IPasswordHasher hasher,
        ISessoesUsuario sessoes)
    {
        _usuarios = usuarios;
        _propriedades = propriedades;
        _vinculos = vinculos;
        _hasher = hasher;
        _sessoes = sessoes;
    }

    public async Task<Result<MinhaContaResponse>> ObterAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        return usuario is null
            ? Result<MinhaContaResponse>.Fail("Conta não encontrada.")
            : Result<MinhaContaResponse>.Ok(new MinhaContaResponse { Nome = usuario.Nome, Email = usuario.Email, Telefone = usuario.Telefone });
    }

    public async Task<Result<MinhaContaResponse>> AtualizarAsync(Guid usuarioId, AtualizarMinhaContaRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario is null)
        {
            return Result<MinhaContaResponse>.Fail("Conta não encontrada.");
        }

        var nome = (request.Nome ?? string.Empty).Trim();
        if (nome.Length is < 2 or > 100)
        {
            return Result<MinhaContaResponse>.Fail("Informe seu nome.");
        }

        var telefone = new string((request.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (telefone.Length > 0 && telefone.Length is < 10 or > 13)
        {
            return Result<MinhaContaResponse>.Fail("Informe o telefone com DDD.");
        }

        usuario.Nome = nome;
        usuario.Telefone = telefone.Length == 0 ? null : telefone;
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<MinhaContaResponse>.Ok(new MinhaContaResponse { Nome = usuario.Nome, Email = usuario.Email, Telefone = usuario.Telefone });
    }

    public async Task<Result> TrocarSenhaAsync(Guid usuarioId, TrocarSenhaRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario is null || !_hasher.Verify(request.SenhaAtual ?? string.Empty, usuario.SenhaHash))
        {
            return Result.Fail("A senha atual não confere.");
        }

        if ((request.NovaSenha ?? string.Empty).Length < TamanhoMinimoSenha)
        {
            return Result.Fail($"A nova senha precisa ter pelo menos {TamanhoMinimoSenha} caracteres.");
        }

        usuario.SenhaHash = _hasher.Hash(request.NovaSenha!);
        usuario.SecurityStamp = Guid.NewGuid();
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Outras sessões (outro celular, navegador) precisam entrar de novo com a senha nova.
        await _sessoes.EncerrarTodasAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> ExcluirAsync(Guid usuarioId, ExcluirContaRequest request, CancellationToken cancellationToken)
    {
        var usuario = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        if (usuario is null || !_hasher.Verify(request.Senha ?? string.Empty, usuario.SenhaHash))
        {
            return Result.Fail("A senha não confere.");
        }

        var minhas = await _propriedades.ListByOwnerAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        foreach (var propriedade in minhas)
        {
            var vinculos = await _vinculos.ListByPropriedadeAsync(propriedade.Id, cancellationToken).ConfigureAwait(false);
            if (vinculos.Any(v => v.Ativo && v.UsuarioId != usuarioId))
            {
                return Result.Fail($"Antes de excluir a conta, remova as pessoas com acesso a \"{propriedade.Nome}\".");
            }
        }

        var agora = DateTime.UtcNow;
        foreach (var propriedade in minhas)
        {
            propriedade.Excluido = true;
            propriedade.DataExclusaoUtc = agora;
            propriedade.ExcluidoPorUsuarioId = usuarioId;
        }

        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Vínculos como membro em propriedades de outras pessoas: sai de todas.
        foreach (var vinculo in await _vinculos.ListAtivosByUsuarioAsync(usuarioId, cancellationToken).ConfigureAwait(false))
        {
            var rastreado = await _vinculos.GetByIdAsync(vinculo.Id, cancellationToken).ConfigureAwait(false);
            if (rastreado is not null)
            {
                rastreado.Ativo = false;
            }
        }

        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        usuario.Nome = "Conta excluída";
        usuario.Email = $"excluida-{usuario.Id:N}@anonimo.invalid";
        usuario.Telefone = null;
        usuario.SenhaHash = _hasher.Hash(Guid.NewGuid().ToString("N"));
        usuario.SecurityStamp = Guid.NewGuid();
        usuario.Ativo = false;
        usuario.ExcluidoEmUtc = agora;
        await _usuarios.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _sessoes.EncerrarTodasAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
