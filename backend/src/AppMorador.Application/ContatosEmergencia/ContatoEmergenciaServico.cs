using AppMorador.Application.Common;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.ContatosEmergencia;

public sealed class ContatoEmergenciaResponse
{
    public required Guid Id { get; init; }

    public required string Nome { get; init; }

    /// <summary>Só dígitos (ex.: 11987654321) — o app formata para exibir e usa direto no "Ligar".</summary>
    public required string Telefone { get; init; }

    public required int Ordem { get; init; }

    public static ContatoEmergenciaResponse FromEntity(ContatoEmergencia c) => new()
    {
        Id = c.Id,
        Nome = c.Nome,
        Telefone = c.Telefone,
        Ordem = c.Ordem,
    };
}

public sealed class SalvarContatoEmergenciaRequest
{
    public string Nome { get; init; } = string.Empty;

    public string Telefone { get; init; } = string.Empty;
}

public sealed class ReordenarContatosEmergenciaRequest
{
    /// <summary>Ids na nova ordem (o primeiro é avisado/mostrado primeiro).</summary>
    public IReadOnlyList<Guid> Ids { get; init; } = [];
}

public interface IContatoEmergenciaServico
{
    Task<Result<IReadOnlyList<ContatoEmergenciaResponse>>> ListarAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<ContatoEmergenciaResponse>> CriarAsync(Guid usuarioId, Guid propriedadeId, SalvarContatoEmergenciaRequest request, CancellationToken cancellationToken);

    Task<Result<ContatoEmergenciaResponse>> AtualizarAsync(Guid usuarioId, Guid contatoId, SalvarContatoEmergenciaRequest request, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ContatoEmergenciaResponse>>> ReordenarAsync(
        Guid usuarioId, Guid propriedadeId, ReordenarContatosEmergenciaRequest request, CancellationToken cancellationToken);

    Task<Result> RemoverAsync(Guid usuarioId, Guid contatoId, CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 24 (ADR 0040) — contatos de emergência da propriedade. Só o dono cadastra (é ele quem
/// decide quem recebe SMS em seu nome). Telefone guardado só com dígitos; limite de
/// <see cref="LimitePorPropriedade"/> para não virar disparo em massa de SMS.
/// </summary>
public sealed class ContatoEmergenciaServico : IContatoEmergenciaServico
{
    public const int LimitePorPropriedade = 5;

    private readonly IContatoEmergenciaRepositorio _contatos;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IAcessoPropriedadeServico _acesso;

    public ContatoEmergenciaServico(IContatoEmergenciaRepositorio contatos, IPropriedadeRepositorio propriedades, IAcessoPropriedadeServico acesso)
    {
        _acesso = acesso;
        _contatos = contatos;
        _propriedades = propriedades;
    }

    public async Task<Result<IReadOnlyList<ContatoEmergenciaResponse>>> ListarAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        if (!await EhDonoAsync(usuarioId, propriedadeId, cancellationToken, NivelAcesso.Ver).ConfigureAwait(false))
        {
            return Result<IReadOnlyList<ContatoEmergenciaResponse>>.Fail("Propriedade não encontrada.");
        }

        var contatos = await _contatos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<ContatoEmergenciaResponse>>.Ok(contatos.Select(ContatoEmergenciaResponse.FromEntity).ToList());
    }

    public async Task<Result<ContatoEmergenciaResponse>> CriarAsync(
        Guid usuarioId, Guid propriedadeId, SalvarContatoEmergenciaRequest request, CancellationToken cancellationToken)
    {
        if (!await EhDonoAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false))
        {
            return Result<ContatoEmergenciaResponse>.Fail("Propriedade não encontrada.");
        }

        var erro = Validar(request, out var nome, out var telefone);
        if (erro is not null)
        {
            return Result<ContatoEmergenciaResponse>.Fail(erro);
        }

        var existentes = await _contatos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (existentes.Count >= LimitePorPropriedade)
        {
            return Result<ContatoEmergenciaResponse>.Fail($"Você pode ter até {LimitePorPropriedade} contatos de emergência.");
        }

        var contato = new ContatoEmergencia
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Nome = nome,
            Telefone = telefone,
            Ordem = existentes.Count == 0 ? 1 : existentes.Max(c => c.Ordem) + 1,
            CriadoEmUtc = DateTime.UtcNow,
        };

        await _contatos.AddAsync(contato, cancellationToken).ConfigureAwait(false);
        await _contatos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ContatoEmergenciaResponse>.Ok(ContatoEmergenciaResponse.FromEntity(contato));
    }

    public async Task<Result<ContatoEmergenciaResponse>> AtualizarAsync(
        Guid usuarioId, Guid contatoId, SalvarContatoEmergenciaRequest request, CancellationToken cancellationToken)
    {
        var contato = await ObterDoDonoAsync(usuarioId, contatoId, cancellationToken).ConfigureAwait(false);
        if (contato is null)
        {
            return Result<ContatoEmergenciaResponse>.Fail("Contato não encontrado.");
        }

        var erro = Validar(request, out var nome, out var telefone);
        if (erro is not null)
        {
            return Result<ContatoEmergenciaResponse>.Fail(erro);
        }

        contato.Nome = nome;
        contato.Telefone = telefone;
        await _contatos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ContatoEmergenciaResponse>.Ok(ContatoEmergenciaResponse.FromEntity(contato));
    }

    public async Task<Result<IReadOnlyList<ContatoEmergenciaResponse>>> ReordenarAsync(
        Guid usuarioId, Guid propriedadeId, ReordenarContatosEmergenciaRequest request, CancellationToken cancellationToken)
    {
        if (!await EhDonoAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false))
        {
            return Result<IReadOnlyList<ContatoEmergenciaResponse>>.Fail("Propriedade não encontrada.");
        }

        var contatos = await _contatos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var ids = request.Ids.Distinct().ToList();
        if (ids.Count != contatos.Count || contatos.Any(c => !ids.Contains(c.Id)))
        {
            return Result<IReadOnlyList<ContatoEmergenciaResponse>>.Fail("A lista precisa ter todos os contatos, uma vez cada.");
        }

        foreach (var contato in contatos)
        {
            contato.Ordem = ids.IndexOf(contato.Id) + 1;
        }

        await _contatos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<ContatoEmergenciaResponse>>.Ok(
            contatos.OrderBy(c => c.Ordem).Select(ContatoEmergenciaResponse.FromEntity).ToList());
    }

    public async Task<Result> RemoverAsync(Guid usuarioId, Guid contatoId, CancellationToken cancellationToken)
    {
        var contato = await ObterDoDonoAsync(usuarioId, contatoId, cancellationToken).ConfigureAwait(false);
        if (contato is null)
        {
            return Result.Fail("Contato não encontrado.");
        }

        contato.Excluido = true;
        contato.DataExclusaoUtc = DateTime.UtcNow;
        contato.ExcluidoPorUsuarioId = usuarioId;
        await _contatos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <summary>Telefone: só dígitos; aceita 10–11 (DDD + número) ou 12–13 começando com 55.</summary>
    public static string? Validar(SalvarContatoEmergenciaRequest request, out string nome, out string telefone)
    {
        nome = (request.Nome ?? string.Empty).Trim();
        telefone = new string((request.Telefone ?? string.Empty).Where(char.IsDigit).ToArray());

        if (nome.Length is < 2 or > 80)
        {
            return "Informe o nome do contato.";
        }

        var valido = telefone.Length is 10 or 11 || (telefone.Length is 12 or 13 && telefone.StartsWith("55", StringComparison.Ordinal));
        return valido ? null : "Informe o telefone com DDD.";
    }

    private async Task<bool> EhDonoAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken, NivelAcesso nivel = NivelAcesso.Alterar)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return propriedade is not null && _acesso.Pode(usuarioId, propriedade, nivel);
    }

    private async Task<ContatoEmergencia?> ObterDoDonoAsync(Guid usuarioId, Guid contatoId, CancellationToken cancellationToken)
    {
        var contato = await _contatos.GetByIdAsync(contatoId, cancellationToken).ConfigureAwait(false);
        if (contato is null)
        {
            return null;
        }

        return await EhDonoAsync(usuarioId, contato.PropriedadeId, cancellationToken).ConfigureAwait(false) ? contato : null;
    }
}
