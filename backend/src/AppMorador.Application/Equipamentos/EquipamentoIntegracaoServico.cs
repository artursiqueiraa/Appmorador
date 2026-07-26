using AppMorador.Application.Common;
using AppMorador.Application.ControlId;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Equipamentos;

/// <summary>
/// Orquestra a integração real de um Equipamento: decifra a senha, resolve o Provider
/// pelo Fabricante (só Control iD tem Provider real hoje — ver ADR 0014), carrega dado
/// real do domínio já existente (Morador/Credencial/PermissaoAcesso) para sincronizar,
/// e persiste eventos importados no domínio de Eventos já existente. Nunca conhece o
/// protocolo do fabricante por dentro — isso é exclusividade de <see cref="IControlIdProvider"/>.
/// </summary>
public sealed class EquipamentoIntegracaoServico : IEquipamentoIntegracaoServico
{
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IMoradorRepositorio _moradores;
    private readonly ICredencialRepositorio _credenciais;
    private readonly IPermissaoAcessoRepositorio _permissoes;
    private readonly IEventoEquipamentoRepositorio _eventosEquipamento;
    private readonly ICriptografiaSimetrica _criptografia;
    private readonly IControlIdProvider _controlIdProvider;
    private readonly ISnapshotOperacionalServico _snapshotOperacional;

    public EquipamentoIntegracaoServico(
        IEquipamentoRepositorio equipamentos,
        IMoradorRepositorio moradores,
        ICredencialRepositorio credenciais,
        IPermissaoAcessoRepositorio permissoes,
        IEventoEquipamentoRepositorio eventosEquipamento,
        ICriptografiaSimetrica criptografia,
        IControlIdProvider controlIdProvider,
        ISnapshotOperacionalServico snapshotOperacional)
    {
        _equipamentos = equipamentos;
        _moradores = moradores;
        _credenciais = credenciais;
        _permissoes = permissoes;
        _eventosEquipamento = eventosEquipamento;
        _criptografia = criptografia;
        _controlIdProvider = controlIdProvider;
        _snapshotOperacional = snapshotOperacional;
    }

    // Sprint 14 (ADR 0017) — todo ponto que altera Equipamento.Status chama isto por
    // último, depois do SaveChangesAsync que já persistiu a mudança. Nunca lança: uma
    // falha de publicação em tempo real não pode transformar uma operação que já
    // teve sucesso em erro para o usuário.
    private Task PublicarAtualizacaoAsync(Guid propriedadeId, CancellationToken cancellationToken) =>
        _snapshotOperacional.RegenerarEPublicarAsync(propriedadeId, MotivoAtualizacaoOperacional.EquipamentoStatusAlterado, cancellationToken);

    public async Task<Result<TesteConexaoResponse>> TestarConexaoAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ObterEquipamentoDoProprietarioAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<TesteConexaoResponse>.Fail("Equipamento não encontrado.");
        }

        var providerResult = ResolverProvider(equipamento.Fabricante);
        if (providerResult is null)
        {
            return Result<TesteConexaoResponse>.Fail(MensagemFabricanteSemProvider(equipamento.Fabricante));
        }

        var conexao = MontarConexao(equipamento);
        var resultado = await providerResult.TestarConexaoAsync(conexao, cancellationToken).ConfigureAwait(false);

        equipamento.Status = resultado.Sucesso ? StatusEquipamento.Online : StatusEquipamento.Offline;
        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

        return Result<TesteConexaoResponse>.Ok(new TesteConexaoResponse { Sucesso = resultado.Sucesso, MensagemErro = resultado.MensagemErro });
    }

    public async Task<Result<InformacoesEquipamentoResponse>> ConsultarInformacoesAsync(
        Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ObterEquipamentoDoProprietarioAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<InformacoesEquipamentoResponse>.Fail("Equipamento não encontrado.");
        }

        var provider = ResolverProvider(equipamento.Fabricante);
        if (provider is null)
        {
            return Result<InformacoesEquipamentoResponse>.Fail(MensagemFabricanteSemProvider(equipamento.Fabricante));
        }

        var conexao = MontarConexao(equipamento);
        try
        {
            var info = await provider.ConsultarInformacoesAsync(conexao, cancellationToken).ConfigureAwait(false);
            equipamento.Status = StatusEquipamento.Online;
            await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

            return Result<InformacoesEquipamentoResponse>.Ok(new InformacoesEquipamentoResponse
            {
                Versao = info.Versao,
                NomeDispositivo = info.NomeDispositivo,
                NumeroSerie = info.NumeroSerie,
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            equipamento.Status = StatusEquipamento.Offline;
            await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
            return Result<InformacoesEquipamentoResponse>.Fail($"Não foi possível consultar o equipamento: {ex.Message}");
        }
    }

    public async Task<Result<SincronizacaoResponse>> SincronizarMoradoresAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ObterEquipamentoDoProprietarioAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<SincronizacaoResponse>.Fail("Equipamento não encontrado.");
        }

        var provider = ResolverProvider(equipamento.Fabricante);
        if (provider is null)
        {
            return Result<SincronizacaoResponse>.Fail(MensagemFabricanteSemProvider(equipamento.Fabricante));
        }

        var moradores = await _moradores.ListByPropriedadeAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
        var paraSincronizar = moradores
            .Select(m => new MoradorParaSincronizar { MoradorId = m.Id, Nome = m.Nome })
            .ToList();

        return await ExecutarSincronizacaoAsync(
            equipamento, () => provider.SincronizarMoradoresAsync(MontarConexao(equipamento), paraSincronizar, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<SincronizacaoResponse>> SincronizarCredenciaisAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ObterEquipamentoDoProprietarioAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<SincronizacaoResponse>.Fail("Equipamento não encontrado.");
        }

        var provider = ResolverProvider(equipamento.Fabricante);
        if (provider is null)
        {
            return Result<SincronizacaoResponse>.Fail(MensagemFabricanteSemProvider(equipamento.Fabricante));
        }

        var credenciais = await _credenciais.ListByPropriedadeAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
        var paraSincronizar = credenciais
            .Select(c => new CredencialParaSincronizar
            {
                CredencialId = c.Id,
                MoradorId = c.MoradorId,
                TipoCredencial = c.Tipo.ToString(),
                Valor = null, // Sprint 7 nao modelou um valor de credencial real (tag/PIN) — fora de escopo desta Sprint.
            })
            .ToList();

        return await ExecutarSincronizacaoAsync(
            equipamento, () => provider.SincronizarCredenciaisAsync(MontarConexao(equipamento), paraSincronizar, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<SincronizacaoResponse>> SincronizarPermissoesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ObterEquipamentoDoProprietarioAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<SincronizacaoResponse>.Fail("Equipamento não encontrado.");
        }

        var provider = ResolverProvider(equipamento.Fabricante);
        if (provider is null)
        {
            return Result<SincronizacaoResponse>.Fail(MensagemFabricanteSemProvider(equipamento.Fabricante));
        }

        var permissoes = await _permissoes.ListByPropriedadeAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
        var paraSincronizar = permissoes
            .Select(p => new PermissaoParaSincronizar
            {
                CredencialId = p.CredencialId,
                DiasPermitidos = p.DiasPermitidos.ToString(),
                HorarioInicial = p.HorarioInicial,
                HorarioFinal = p.HorarioFinal,
            })
            .ToList();

        return await ExecutarSincronizacaoAsync(
            equipamento, () => provider.SincronizarPermissoesAsync(MontarConexao(equipamento), paraSincronizar, cancellationToken), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<ImportacaoEventosResponse>> ImportarEventosAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await ObterEquipamentoDoProprietarioAsync(proprietarioId, equipamentoId, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<ImportacaoEventosResponse>.Fail("Equipamento não encontrado.");
        }

        var provider = ResolverProvider(equipamento.Fabricante);
        if (provider is null)
        {
            return Result<ImportacaoEventosResponse>.Fail(MensagemFabricanteSemProvider(equipamento.Fabricante));
        }

        try
        {
            var importados = await provider.ImportarEventosAsync(MontarConexao(equipamento), cancellationToken).ConfigureAwait(false);
            equipamento.Status = StatusEquipamento.Online;

            var agora = DateTime.UtcNow;
            var novosEventos = importados
                .Select(e => new EventoEquipamento
                {
                    Id = Guid.NewGuid(),
                    EquipamentoId = equipamento.Id,
                    CodigoEventoOriginal = e.CodigoEventoOriginal,
                    Descricao = e.Descricao,
                    OcorridoEmUtc = e.OcorridoEmUtc,
                    CreatedAtUtc = agora,
                })
                .ToList();

            if (novosEventos.Count > 0)
            {
                await _eventosEquipamento.AddRangeAsync(novosEventos, cancellationToken).ConfigureAwait(false);
            }

            await _eventosEquipamento.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

            return Result<ImportacaoEventosResponse>.Ok(new ImportacaoEventosResponse { QuantidadeImportada = novosEventos.Count });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            equipamento.Status = StatusEquipamento.Offline;
            await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
            return Result<ImportacaoEventosResponse>.Fail($"Não foi possível importar eventos do equipamento: {ex.Message}");
        }
    }

    private async Task<Result<SincronizacaoResponse>> ExecutarSincronizacaoAsync(
        Equipamento equipamento, Func<Task<ResultadoSincronizacao>> executar, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await executar().ConfigureAwait(false);
            equipamento.Status = StatusEquipamento.Online;
            equipamento.UltimaSincronizacaoUtc = DateTime.UtcNow;
            await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);

            return Result<SincronizacaoResponse>.Ok(new SincronizacaoResponse { QuantidadeProcessada = resultado.QuantidadeProcessada });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            equipamento.Status = StatusEquipamento.Offline;
            await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await PublicarAtualizacaoAsync(equipamento.PropriedadeId, cancellationToken).ConfigureAwait(false);
            return Result<SincronizacaoResponse>.Fail($"Não foi possível sincronizar com o equipamento: {ex.Message}");
        }
    }

    private async Task<Equipamento?> ObterEquipamentoDoProprietarioAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false);
        return equipamento?.Propriedade is not null && equipamento.Propriedade.ProprietarioId == proprietarioId ? equipamento : null;
    }

    // Hoje só ControlId tem Provider real (ver ADR 0014) — outros fabricantes ganham o
    // proprio quando existirem de fato, nunca simulados aqui.
    private IControlIdProvider? ResolverProvider(FabricanteEquipamento fabricante) =>
        fabricante == FabricanteEquipamento.ControlId ? _controlIdProvider : null;

    private static string MensagemFabricanteSemProvider(FabricanteEquipamento fabricante) =>
        $"Integração real para o fabricante {fabricante} ainda não foi implementada.";

    // Ip/Porta/Usuario/Senha sao obrigatorios so para fabricantes que discam para o
    // equipamento (Control iD) — ja validado em EquipamentoServico na criacao/edicao,
    // entao aqui e seguro assumir presentes (nunca chamado para Fabricante=Jfl, que
    // nao usa ConexaoEquipamento — ver ResolverProvider).
    private ConexaoEquipamento MontarConexao(Equipamento equipamento) => new()
    {
        Ip = equipamento.Ip!,
        Porta = equipamento.Porta!.Value,
        Usuario = equipamento.Usuario!,
        Senha = _criptografia.Descriptografar(equipamento.SenhaCriptografada!),
    };
}
