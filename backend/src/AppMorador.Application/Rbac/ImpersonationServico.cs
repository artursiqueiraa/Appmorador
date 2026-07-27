using AppMorador.Application.Auditoria;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Rbac;

public sealed class ImpersonationServico : IImpersonationServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly ITokenService _tokenService;
    private readonly IAuditoriaService _auditoria;

    public ImpersonationServico(
        IPropriedadeRepositorio propriedades, IUsuarioRepositorio usuarios, ITokenService tokenService, IAuditoriaService auditoria)
    {
        _propriedades = propriedades;
        _usuarios = usuarios;
        _tokenService = tokenService;
        _auditoria = auditoria;
    }

    public async Task<Result<ImpersonarResponse>> IniciarAsync(
        Guid masterId, Guid propriedadeId, string? ipAddress, CancellationToken cancellationToken)
    {
        var master = await _usuarios.GetByIdAsync(masterId, cancellationToken).ConfigureAwait(false);
        if (master is null)
        {
            return Result<ImpersonarResponse>.Fail("Usuário master não encontrado.");
        }

        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<ImpersonarResponse>.Fail("Propriedade não encontrada.");
        }

        var cliente = await _usuarios.GetByIdAsync(propriedade.ProprietarioId, cancellationToken).ConfigureAwait(false);
        if (cliente is null)
        {
            return Result<ImpersonarResponse>.Fail("Cliente desta propriedade não encontrado.");
        }

        var masterNome = master.Nome;
        var token = _tokenService.GenerateImpersonationToken(cliente, masterId, masterNome);

        await _auditoria.RegistrarAsync(
            masterId,
            masterNome,
            TipoAcaoAuditoria.ImpersonationInicio,
            entidade: "Propriedade",
            entidadeId: propriedadeId.ToString(),
            detalhes: $"{{\"clienteId\":\"{cliente.Id}\",\"clienteNome\":\"{EscaparJson(cliente.Nome)}\"}}",
            ipAddress,
            cancellationToken).ConfigureAwait(false);

        return Result<ImpersonarResponse>.Ok(new ImpersonarResponse
        {
            AccessToken = token,
            ExpiresInSeconds = (int)_tokenService.ImpersonationTokenLifetime.TotalSeconds,
            PropriedadeId = propriedade.Id,
            PropriedadeNome = propriedade.Nome,
            ClienteNome = cliente.Nome,
        });
    }

    public async Task EncerrarAsync(Guid masterId, Guid propriedadeId, string? ipAddress, CancellationToken cancellationToken)
    {
        var master = await _usuarios.GetByIdAsync(masterId, cancellationToken).ConfigureAwait(false);
        var masterNome = master?.Nome ?? "(desconhecido)";

        await _auditoria.RegistrarAsync(
            masterId,
            masterNome,
            TipoAcaoAuditoria.ImpersonationFim,
            entidade: "Propriedade",
            entidadeId: propriedadeId.ToString(),
            detalhes: null,
            ipAddress,
            cancellationToken).ConfigureAwait(false);
    }

    private static string EscaparJson(string valor) => valor.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
