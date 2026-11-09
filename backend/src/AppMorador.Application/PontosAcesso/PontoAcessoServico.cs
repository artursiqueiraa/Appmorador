using AppMorador.Application.Rbac;
using AppMorador.Application.Common;
using AppMorador.Application.ControlId;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Jfl;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.PontosAcesso;

/// <summary>Mesmo padrão de ownership/soft delete de <see cref="Application.Unidades.UnidadeServico"/>.</summary>
public sealed class PontoAcessoServico : IPontoAcessoServico
{
    private readonly IAcessoPropriedadeServico _acesso;

    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IPontoAcessoRepositorio _pontosAcesso;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IPermissaoVeicularRepositorio _permissoesVeiculares;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly ICameraRepositorio _cameras;
    private readonly IJflComandoServico _jfl;
    private readonly IControlIdProvider _controlId;
    private readonly AppMorador.Application.Hikvision.IHikvisionAcessoProvider _hikvision;
    private readonly ICriptografiaSimetrica _criptografia;

    public PontoAcessoServico(
        IPropriedadeRepositorio propriedades,
        IPontoAcessoRepositorio pontosAcesso,
        IPermissaoAcessoRepositorio permissoes,
        IPermissaoVeicularRepositorio permissoesVeiculares,
        IEquipamentoRepositorio equipamentos,
        ICameraRepositorio cameras,
        IJflComandoServico jfl,
        IControlIdProvider controlId,
        AppMorador.Application.Hikvision.IHikvisionAcessoProvider hikvision,
        ICriptografiaSimetrica criptografia,
        IAcessoPropriedadeServico acesso)
    {
        _acesso = acesso;
        _propriedades = propriedades;
        _pontosAcesso = pontosAcesso;
        _permissoes = permissoes;
        _permissoesVeiculares = permissoesVeiculares;
        _equipamentos = equipamentos;
        _cameras = cameras;
        _jfl = jfl;
        _controlId = controlId;
        _hikvision = hikvision;
        _criptografia = criptografia;
    }

    public async Task<Result<PontoAcessoResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || !_acesso.Pode(proprietarioId, propriedade, NivelAcesso.Alterar))
        {
            return Result<PontoAcessoResponse>.Fail("Propriedade não encontrada.");
        }

        var pontoAcesso = new PontoAcesso
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Nome = request.Nome.Trim(),
            Tipo = request.Tipo ?? TipoPontoAcesso.Geral,
            CreatedAtUtc = DateTime.UtcNow,
        };

        var erroVinculo = await AplicarVinculosAsync(pontoAcesso, request.EquipamentoAcionadorId, request.NumeroSaida, request.CameraId, cancellationToken)
            .ConfigureAwait(false);
        if (erroVinculo is not null)
        {
            return Result<PontoAcessoResponse>.Fail(erroVinculo);
        }

        await _pontosAcesso.AddAsync(pontoAcesso, cancellationToken).ConfigureAwait(false);
        await _pontosAcesso.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PontoAcessoResponse>.Ok(ToDto(pontoAcesso));
    }

    public async Task<Result<IReadOnlyList<PontoAcessoResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || !_acesso.Pode(proprietarioId, propriedade, NivelAcesso.Ver))
        {
            return Result<IReadOnlyList<PontoAcessoResponse>>.Fail("Propriedade não encontrada.");
        }

        var pontos = await _pontosAcesso.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<PontoAcessoResponse>>.Ok(pontos.Select(ToDto).ToList());
    }

    public async Task<Result<PontoAcessoResponse>> UpdateAsync(
        Guid proprietarioId, Guid pontoAcessoId, AtualizarPontoAcessoRequest request, CancellationToken cancellationToken)
    {
        var pontoAcesso = await _pontosAcesso.GetByIdAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (pontoAcesso is null || pontoAcesso.Propriedade is null || !_acesso.Pode(proprietarioId, pontoAcesso.Propriedade, NivelAcesso.Alterar))
        {
            return Result<PontoAcessoResponse>.Fail("Ponto de acesso não encontrado.");
        }

        pontoAcesso.Nome = request.Nome.Trim();
        pontoAcesso.Tipo = request.Tipo ?? pontoAcesso.Tipo;

        var erroVinculo = await AplicarVinculosAsync(pontoAcesso, request.EquipamentoAcionadorId, request.NumeroSaida, request.CameraId, cancellationToken)
            .ConfigureAwait(false);
        if (erroVinculo is not null)
        {
            return Result<PontoAcessoResponse>.Fail(erroVinculo);
        }
        await _pontosAcesso.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PontoAcessoResponse>.Ok(ToDto(pontoAcesso));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid pontoAcessoId, CancellationToken cancellationToken)
    {
        var pontoAcesso = await _pontosAcesso.GetByIdAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (pontoAcesso is null || pontoAcesso.Propriedade is null || !_acesso.Pode(proprietarioId, pontoAcesso.Propriedade, NivelAcesso.Alterar))
        {
            return Result.Fail("Ponto de acesso não encontrado.");
        }

        var agora = DateTime.UtcNow;
        // Nenhuma Permissao (de acesso ou veicular, Sprint 9) pode continuar apontando
        // pra um Ponto de Acesso excluido.
        var permissoesDoPonto = await _permissoes.ListByPontoAcessoAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        var permissoesVeicularesDoPonto = await _permissoesVeiculares.ListByPontoAcessoAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);

        pontoAcesso.Excluido = true;
        pontoAcesso.DataExclusaoUtc = agora;
        pontoAcesso.ExcluidoPorUsuarioId = proprietarioId;

        foreach (var permissao in permissoesDoPonto)
        {
            permissao.Excluido = true;
            permissao.DataExclusaoUtc = agora;
            permissao.ExcluidoPorUsuarioId = proprietarioId;
        }

        foreach (var permissaoVeicular in permissoesVeicularesDoPonto)
        {
            permissaoVeicular.Excluido = true;
            permissaoVeicular.DataExclusaoUtc = agora;
            permissaoVeicular.ExcluidoPorUsuarioId = proprietarioId;
        }

        await _pontosAcesso.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result<IReadOnlyList<AcionadorDisponivelResponse>>> ListarAcionadoresAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || !_acesso.Pode(proprietarioId, propriedade, NivelAcesso.Alterar))
        {
            return Result<IReadOnlyList<AcionadorDisponivelResponse>>.Fail("Propriedade não encontrada.");
        }

        var equipamentos = await _equipamentos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<AcionadorDisponivelResponse>>.Ok(equipamentos
            .Where(e => SabeAbrir(e.Fabricante))
            .Select(e => new AcionadorDisponivelResponse
            {
                EquipamentoId = e.Id,
                Nome = e.Nome,
                Fabricante = e.Fabricante,
                Online = e.Status == StatusEquipamento.Online,
                RotuloSaida = e.Fabricante == FabricanteEquipamento.Jfl ? "Saída (PGM)" : "Porta",
            })
            .ToList());
    }

    public async Task<Result<ResultadoAberturaResponse>> AbrirAsync(Guid usuarioId, Guid pontoAcessoId, CancellationToken cancellationToken)
    {
        var ponto = await _pontosAcesso.GetByIdAsync(pontoAcessoId, cancellationToken).ConfigureAwait(false);
        if (ponto?.Propriedade is null || !_acesso.Pode(usuarioId, ponto.Propriedade, NivelAcesso.Operar))
        {
            return Result<ResultadoAberturaResponse>.Fail("Ponto de acesso não encontrado.");
        }

        var equipamento = ponto.EquipamentoAcionador;
        if (equipamento is null || ponto.NumeroSaida is null)
        {
            return Result<ResultadoAberturaResponse>.Ok(new ResultadoAberturaResponse
            {
                Sucesso = false,
                Mensagem = "Esta porta ainda não está ligada a nenhum equipamento. Edite e escolha o que a abre.",
            });
        }

        var (sucesso, falha) = equipamento.Fabricante switch
        {
            FabricanteEquipamento.Jfl => await AbrirPelaCentralAsync(usuarioId, equipamento, ponto.NumeroSaida.Value, cancellationToken).ConfigureAwait(false),
            FabricanteEquipamento.ControlId or FabricanteEquipamento.Hikvision => await AbrirPeloLeitorAsync(equipamento, ponto.NumeroSaida.Value, cancellationToken).ConfigureAwait(false),
            _ => (false, "Este equipamento ainda não sabe abrir portas pelo app."),
        };

        // Sprint 27 (ADR 0043) — quem abriu e o resultado ficam em ComandosExecutados (ExecucaoComandoServico).
        return Result<ResultadoAberturaResponse>.Ok(new ResultadoAberturaResponse
        {
            Sucesso = sucesso,
            Mensagem = sucesso ? $"{ponto.Nome}: comando de abrir enviado." : falha ?? "Não foi possível abrir agora.",
        });
    }

    /// <summary>Reaproveita o comando de PGM existente (mesma sessão TCP, notificação e marcação de offline).</summary>
    private async Task<(bool Sucesso, string? Falha)> AbrirPelaCentralAsync(Guid usuarioId, Equipamento equipamento, int pgm, CancellationToken cancellationToken)
    {
        var resultado = await _jfl.AcionarPgmAsync(usuarioId, equipamento.Id, pgm, cancellationToken).ConfigureAwait(false);
        if (!resultado.Success)
        {
            return (false, resultado.Error);
        }

        return resultado.Data!.Sucesso ? (true, null) : (false, resultado.Data.MensagemErro ?? "A central não respondeu.");
    }

    private async Task<(bool Sucesso, string? Falha)> AbrirPeloLeitorAsync(Equipamento equipamento, int porta, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(equipamento.Ip) || equipamento.Porta is null
            || string.IsNullOrWhiteSpace(equipamento.Usuario) || equipamento.SenhaCriptografada is null)
        {
            return (false, "O leitor está sem endereço ou senha cadastrados.");
        }

        var conexao = new ConexaoEquipamento
        {
            Ip = equipamento.Ip,
            Porta = equipamento.Porta.Value,
            Usuario = equipamento.Usuario,
            Senha = _criptografia.Descriptografar(equipamento.SenhaCriptografada),
        };
        var resultado = equipamento.Fabricante == FabricanteEquipamento.Hikvision
            ? await _hikvision.AbrirPortaAsync(conexao, porta, cancellationToken).ConfigureAwait(false)
            : await _controlId.AbrirPortaAsync(conexao, porta, cancellationToken).ConfigureAwait(false);
        return resultado.Sucesso ? (true, null) : (false, resultado.MensagemErro ?? "O leitor não respondeu.");
    }

    /// <summary>Acionador e câmera precisam ser da mesma propriedade; devolve null quando está tudo certo.</summary>
    private async Task<string?> AplicarVinculosAsync(
        PontoAcesso ponto, Guid? equipamentoId, int? numeroSaida, Guid? cameraId, CancellationToken cancellationToken)
    {
        if (equipamentoId is null)
        {
            ponto.EquipamentoAcionadorId = null;
            ponto.EquipamentoAcionador = null;
            ponto.NumeroSaida = null;
        }
        else
        {
            var equipamento = await _equipamentos.GetByIdAsync(equipamentoId.Value, cancellationToken).ConfigureAwait(false);
            if (equipamento is null || equipamento.PropriedadeId != ponto.PropriedadeId || !SabeAbrir(equipamento.Fabricante))
            {
                return "Escolha um equipamento desta propriedade que abra portas.";
            }

            if (numeroSaida is null or < 1)
            {
                return "Informe qual saída do equipamento abre esta porta.";
            }

            ponto.EquipamentoAcionadorId = equipamento.Id;
            ponto.EquipamentoAcionador = equipamento;
            ponto.NumeroSaida = numeroSaida;
        }

        if (cameraId is null)
        {
            ponto.CameraId = null;
            ponto.Camera = null;
        }
        else
        {
            var camera = await _cameras.GetByIdAsync(cameraId.Value, cancellationToken).ConfigureAwait(false);
            if (camera is null || camera.PropriedadeId != ponto.PropriedadeId)
            {
                return "Escolha uma câmera desta propriedade.";
            }

            ponto.CameraId = camera.Id;
            ponto.Camera = camera;
        }

        return null;
    }

    private static bool SabeAbrir(FabricanteEquipamento fabricante) =>
        fabricante is FabricanteEquipamento.Jfl or FabricanteEquipamento.ControlId or FabricanteEquipamento.Hikvision;

    private static PontoAcessoResponse ToDto(PontoAcesso pontoAcesso) => new()
    {
        Id = pontoAcesso.Id,
        PropriedadeId = pontoAcesso.PropriedadeId,
        Nome = pontoAcesso.Nome,
        Tipo = pontoAcesso.Tipo,
        EquipamentoAcionadorId = pontoAcesso.EquipamentoAcionadorId,
        NomeEquipamentoAcionador = pontoAcesso.EquipamentoAcionador?.Nome,
        FabricanteAcionador = pontoAcesso.EquipamentoAcionador?.Fabricante,
        NumeroSaida = pontoAcesso.NumeroSaida,
        PodeAbrir = pontoAcesso.EquipamentoAcionadorId is not null && pontoAcesso.NumeroSaida is not null,
        AcionadorOnline = pontoAcesso.EquipamentoAcionador?.Status == StatusEquipamento.Online,
        CameraId = pontoAcesso.CameraId,
        NomeCamera = pontoAcesso.Camera?.Nome,
    };
}
