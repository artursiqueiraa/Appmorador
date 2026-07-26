using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Equipamentos;

/// <summary>
/// CRUD do agregado Equipamento — ownership via Propriedade.ProprietarioId (mesmo
/// padrão de PontoAcesso/Vaga/Visitante). Nunca conhece Control iD/qualquer protocolo
/// de fabricante — isso é responsabilidade exclusiva do Provider (ver ADR 0014), que é
/// orquestrado por <see cref="EquipamentoIntegracaoServico"/>, nunca por este serviço.
/// </summary>
public sealed class EquipamentoServico : IEquipamentoServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly ICriptografiaSimetrica _criptografia;

    public EquipamentoServico(
        IPropriedadeRepositorio propriedades, IEquipamentoRepositorio equipamentos, ICriptografiaSimetrica criptografia)
    {
        _propriedades = propriedades;
        _equipamentos = equipamentos;
        _criptografia = criptografia;
    }

    public async Task<Result<EquipamentoResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<EquipamentoResponse>.Fail("Propriedade não encontrada.");
        }

        var erroValidacao = ValidarCamposPorFabricante(
            request.Fabricante, request.Ip, request.Porta, request.Usuario, request.Senha, request.Identificador);
        if (erroValidacao is not null)
        {
            return Result<EquipamentoResponse>.Fail(erroValidacao);
        }

        var equipamento = new Equipamento
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Nome = request.Nome.Trim(),
            Modelo = NullIfBlank(request.Modelo),
            Fabricante = request.Fabricante,
            Ip = NullIfBlank(request.Ip),
            Porta = request.Porta,
            Usuario = NullIfBlank(request.Usuario),
            SenhaCriptografada = string.IsNullOrWhiteSpace(request.Senha) ? null : _criptografia.Criptografar(request.Senha),
            Identificador = NullIfBlank(request.Identificador),
            Status = StatusEquipamento.Desconhecido,
            UltimaSincronizacaoUtc = null,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _equipamentos.AddAsync(equipamento, cancellationToken).ConfigureAwait(false);
        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EquipamentoResponse>.Ok(ToDto(equipamento));
    }

    /// <summary>
    /// Obrigatoriedade real de Ip/Porta/Usuario/Senha depende do fabricante (ver ADR
    /// 0015): Control iD disca para o equipamento (todos obrigatórios); JFL é o
    /// oposto — a central disca para o AppMorador, então só o Identificador (número
    /// de série) importa, usado para localizar a sessão TCP já aberta. Intelbras
    /// (Sprint 15, ADR 0018) também disca (Ip/Porta/Senha obrigatórios), mas não tem
    /// conceito de usuário — só uma senha de acesso remoto.
    /// </summary>
    private static string? ValidarCamposPorFabricante(
        FabricanteEquipamento fabricante, string? ip, int? porta, string? usuario, string? senha, string? identificador)
    {
        if (fabricante == FabricanteEquipamento.Jfl)
        {
            return string.IsNullOrWhiteSpace(identificador)
                ? "Número de série é obrigatório para centrais JFL — é ele que localiza a conexão já aberta com a central."
                : null;
        }

        if (fabricante == FabricanteEquipamento.Intelbras)
        {
            if (string.IsNullOrWhiteSpace(ip)) return "IP é obrigatório.";
            if (porta is null) return "Porta é obrigatória.";
            if (string.IsNullOrWhiteSpace(senha)) return "Senha é obrigatória.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(ip)) return "IP é obrigatório.";
        if (porta is null) return "Porta é obrigatória.";
        if (string.IsNullOrWhiteSpace(usuario)) return "Usuário é obrigatório.";
        if (string.IsNullOrWhiteSpace(senha)) return "Senha é obrigatória.";
        return null;
    }

    public async Task<Result<IReadOnlyList<EquipamentoResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<EquipamentoResponse>>.Fail("Propriedade não encontrada.");
        }

        var equipamentos = await _equipamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<EquipamentoResponse>>.Ok(equipamentos.Select(ToDto).ToList());
    }

    public async Task<Result<EquipamentoResponse>> GetByIdAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(equipamento, proprietarioId))
        {
            return Result<EquipamentoResponse>.Fail("Equipamento não encontrado.");
        }

        return Result<EquipamentoResponse>.Ok(ToDto(equipamento!));
    }

    public async Task<Result<EquipamentoResponse>> UpdateAsync(
        Guid proprietarioId, Guid equipamentoId, AtualizarEquipamentoRequest request, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(equipamento, proprietarioId))
        {
            return Result<EquipamentoResponse>.Fail("Equipamento não encontrado.");
        }

        var erroValidacao = request.Fabricante == FabricanteEquipamento.Jfl
            ? (string.IsNullOrWhiteSpace(request.Identificador) ? "Número de série é obrigatório para centrais JFL." : null)
            : request.Fabricante == FabricanteEquipamento.Intelbras
                ? (string.IsNullOrWhiteSpace(request.Ip) ? "IP é obrigatório."
                    : request.Porta is null ? "Porta é obrigatória."
                    : null)
                : (string.IsNullOrWhiteSpace(request.Ip) ? "IP é obrigatório."
                    : request.Porta is null ? "Porta é obrigatória."
                    : string.IsNullOrWhiteSpace(request.Usuario) ? "Usuário é obrigatório."
                    : null);
        if (erroValidacao is not null)
        {
            return Result<EquipamentoResponse>.Fail(erroValidacao);
        }

        equipamento!.Nome = request.Nome.Trim();
        equipamento.Modelo = NullIfBlank(request.Modelo);
        equipamento.Fabricante = request.Fabricante;
        equipamento.Ip = NullIfBlank(request.Ip);
        equipamento.Porta = request.Porta;
        equipamento.Usuario = NullIfBlank(request.Usuario);
        equipamento.Identificador = NullIfBlank(request.Identificador);

        if (!string.IsNullOrWhiteSpace(request.Senha))
        {
            equipamento.SenhaCriptografada = _criptografia.Criptografar(request.Senha);
        }

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EquipamentoResponse>.Ok(ToDto(equipamento));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(equipamento, proprietarioId))
        {
            return Result.Fail("Equipamento não encontrado.");
        }

        equipamento!.Excluido = true;
        equipamento.DataExclusaoUtc = DateTime.UtcNow;
        equipamento.ExcluidoPorUsuarioId = proprietarioId;

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    private static bool PertenceAoProprietario(Equipamento? equipamento, Guid proprietarioId) =>
        equipamento?.Propriedade is not null && equipamento.Propriedade.ProprietarioId == proprietarioId;

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static EquipamentoResponse ToDto(Equipamento equipamento) => new()
    {
        Id = equipamento.Id,
        PropriedadeId = equipamento.PropriedadeId,
        Nome = equipamento.Nome,
        Modelo = equipamento.Modelo,
        Fabricante = equipamento.Fabricante,
        Ip = equipamento.Ip,
        Porta = equipamento.Porta,
        Usuario = equipamento.Usuario,
        Identificador = equipamento.Identificador,
        Status = equipamento.Status,
        UltimaSincronizacaoUtc = equipamento.UltimaSincronizacaoUtc,
    };
}
