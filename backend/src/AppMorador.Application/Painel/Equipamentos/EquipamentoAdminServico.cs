using System.Text.Json;
using AppMorador.Application.Common;
using AppMorador.Application.ControlId;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Intelbras;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel.Equipamentos;

public sealed class EquipamentoAdminServico : IEquipamentoAdminServico
{
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IModeloEquipamentoRepositorio _modelosEquipamento;
    private readonly IControlIdProvider _controlId;
    private readonly IIntelbrasProvider _intelbras;
    private readonly ICriptografiaSimetrica _criptografia;

    public EquipamentoAdminServico(
        IEquipamentoRepositorio equipamentos, IPropriedadeRepositorio propriedades, IModeloEquipamentoRepositorio modelosEquipamento,
        IControlIdProvider controlId, IIntelbrasProvider intelbras, ICriptografiaSimetrica criptografia)
    {
        _equipamentos = equipamentos;
        _propriedades = propriedades;
        _modelosEquipamento = modelosEquipamento;
        _controlId = controlId;
        _intelbras = intelbras;
        _criptografia = criptografia;
    }

    public async Task<EquipamentosAdminPaginadosResponse> ListarAsync(
        int pagina, int tamanhoPagina, string? busca, FabricanteEquipamento? fabricante,
        EstadoOperacionalEquipamento? estadoOperacional, bool incluirRemovidos, CancellationToken cancellationToken)
    {
        pagina = pagina <= 0 ? 1 : pagina;
        tamanhoPagina = tamanhoPagina is <= 0 or > 100 ? 20 : tamanhoPagina;

        var (itens, total) = await _equipamentos
            .ListarGlobalAsync(pagina, tamanhoPagina, busca, fabricante, estadoOperacional, incluirRemovidos, cancellationToken)
            .ConfigureAwait(false);

        return new EquipamentosAdminPaginadosResponse
        {
            Itens = itens.Select(ToDto).ToList(),
            PaginaAtual = pagina,
            TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)tamanhoPagina),
            TotalItens = total,
        };
    }

    public async Task<Result<EquipamentoAdminResponse>> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<EquipamentoAdminResponse>.Fail("Equipamento não encontrado.");
        }

        return Result<EquipamentoAdminResponse>.Ok(ToDto(equipamento));
    }

    /// <summary>
    /// Sprint 22C.2 — cada fabricante tem sua própria estratégia de cadastro/descoberta (ver
    /// mission brief): JFL só pede Número de Série e nunca disca para a central (ADR 0015);
    /// Control iD/Intelbras pedem IP/Porta(/Usuário)/Senha e, após salvar, tentam conectar e
    /// descobrir o que o Provider realmente devolver — uma falha de conexão nunca reprova o
    /// cadastro (mesmo padrão de resiliência de `EquipamentoIntegracaoServico`), só deixa o
    /// equipamento como Offline até uma tentativa futura funcionar.
    /// </summary>
    public async Task<Result<EquipamentoAdminResponse>> CriarAsync(CriarEquipamentoAdminRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(request.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<EquipamentoAdminResponse>.Fail("Propriedade não encontrada.");
        }

        var erroValidacao = ValidarCamposPorFabricante(
            request.Fabricante, request.NumeroSerie, request.Ip, request.Porta, request.Usuario, request.Senha);
        if (erroValidacao is not null)
        {
            return Result<EquipamentoAdminResponse>.Fail(erroValidacao);
        }

        string? numeroSerie = null;
        if (request.Fabricante == FabricanteEquipamento.Jfl)
        {
            numeroSerie = request.NumeroSerie!.Trim();
            if (await _equipamentos.ExisteNumeroSerieDuplicadoAsync(request.PropriedadeId, numeroSerie, null, cancellationToken).ConfigureAwait(false))
            {
                return Result<EquipamentoAdminResponse>.Fail("Já existe um equipamento com este Número de Série nesta propriedade.");
            }
        }

        var modeloEquipamentoId = await ResolverOuCriarModeloAsync(request.Fabricante, request.Modelo, cancellationToken).ConfigureAwait(false);

        var equipamento = new Equipamento
        {
            Id = Guid.NewGuid(),
            PropriedadeId = request.PropriedadeId,
            Propriedade = propriedade,
            Nome = request.Nome.Trim(),
            ModeloEquipamentoId = modeloEquipamentoId,
            Fabricante = request.Fabricante,
            MacAddress = NullIfBlank(request.MacAddress),
            Observacoes = NullIfBlank(request.Observacoes),
            Identificador = numeroSerie,
            Status = StatusEquipamento.Desconhecido,
            EstadoOperacional = request.EstadoOperacional,
            CreatedAtUtc = DateTime.UtcNow,
        };

        if (request.Fabricante == FabricanteEquipamento.ControlId)
        {
            equipamento.Ip = request.Ip!.Trim();
            equipamento.Porta = request.Porta!.Value;
            equipamento.Usuario = request.Usuario!.Trim();
            equipamento.SenhaCriptografada = _criptografia.Criptografar(request.Senha!);
        }
        else if (request.Fabricante == FabricanteEquipamento.Intelbras)
        {
            equipamento.Ip = request.Ip!.Trim();
            equipamento.Porta = request.Porta!.Value;
            equipamento.SenhaCriptografada = _criptografia.Criptografar(request.Senha!);
        }

        await _equipamentos.AddAsync(equipamento, cancellationToken).ConfigureAwait(false);
        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (request.Fabricante == FabricanteEquipamento.ControlId)
        {
            await ConectarEDescobrirControlIdAsync(
                equipamento, request.Ip!.Trim(), request.Porta!.Value, request.Usuario!.Trim(), request.Senha!, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (request.Fabricante == FabricanteEquipamento.Intelbras)
        {
            await ConectarIntelbrasAsync(equipamento, request.Ip!.Trim(), request.Porta!.Value, request.Senha!, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result<EquipamentoAdminResponse>.Ok(ToDto(equipamento));
    }

    /// <summary>Mesma regra condicional de <see cref="CriarAsync"/>; sem reconexão automática na edição (decisão de escopo — reconectar é responsabilidade do fluxo de sincronização já existente, não deste formulário).</summary>
    public async Task<Result<EquipamentoAdminResponse>> AtualizarAsync(
        Guid id, AtualizarEquipamentoAdminRequest request, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<EquipamentoAdminResponse>.Fail("Equipamento não encontrado.");
        }

        var erroValidacao = ValidarCamposPorFabricanteAtualizacao(
            request.Fabricante, request.NumeroSerie, request.Ip, request.Porta, request.Usuario, request.Senha,
            !string.IsNullOrEmpty(equipamento.SenhaCriptografada));
        if (erroValidacao is not null)
        {
            return Result<EquipamentoAdminResponse>.Fail(erroValidacao);
        }

        if (request.Fabricante == FabricanteEquipamento.Jfl)
        {
            var numeroSerie = request.NumeroSerie!.Trim();
            if (await _equipamentos.ExisteNumeroSerieDuplicadoAsync(equipamento.PropriedadeId, numeroSerie, id, cancellationToken).ConfigureAwait(false))
            {
                return Result<EquipamentoAdminResponse>.Fail("Já existe um equipamento com este Número de Série nesta propriedade.");
            }

            equipamento.Identificador = numeroSerie;
            equipamento.Ip = null;
            equipamento.Porta = null;
            equipamento.Usuario = null;
            equipamento.SenhaCriptografada = null;
        }
        else if (request.Fabricante == FabricanteEquipamento.ControlId)
        {
            equipamento.Ip = request.Ip!.Trim();
            equipamento.Porta = request.Porta!.Value;
            equipamento.Usuario = request.Usuario!.Trim();
            if (!string.IsNullOrWhiteSpace(request.Senha))
            {
                equipamento.SenhaCriptografada = _criptografia.Criptografar(request.Senha);
            }
        }
        else if (request.Fabricante == FabricanteEquipamento.Intelbras)
        {
            equipamento.Ip = request.Ip!.Trim();
            equipamento.Porta = request.Porta!.Value;
            equipamento.Usuario = null;
            if (!string.IsNullOrWhiteSpace(request.Senha))
            {
                equipamento.SenhaCriptografada = _criptografia.Criptografar(request.Senha);
            }
        }

        equipamento.Nome = request.Nome.Trim();
        equipamento.Fabricante = request.Fabricante;
        equipamento.ModeloEquipamentoId = await ResolverOuCriarModeloAsync(request.Fabricante, request.Modelo, cancellationToken).ConfigureAwait(false);
        equipamento.MacAddress = NullIfBlank(request.MacAddress);
        equipamento.Observacoes = NullIfBlank(request.Observacoes);

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EquipamentoAdminResponse>.Ok(ToDto(equipamento));
    }

    public async Task<Result<EquipamentoAdminResponse>> AtualizarEstadoOperacionalAsync(
        Guid id, EstadoOperacionalEquipamento estadoOperacional, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result<EquipamentoAdminResponse>.Fail("Equipamento não encontrado.");
        }

        equipamento.EstadoOperacional = estadoOperacional;
        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EquipamentoAdminResponse>.Ok(ToDto(equipamento));
    }

    public async Task<Result> ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var equipamento = await _equipamentos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (equipamento is null)
        {
            return Result.Fail("Equipamento não encontrado.");
        }

        equipamento.Excluido = true;
        equipamento.DataExclusaoUtc = DateTime.UtcNow;

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    /// <summary>
    /// Sprint 22C.2 — únicos fabricantes com Provider real hoje (ver auditoria da Fase 0):
    /// Hikvision/Dahua/Outro são recusados explicitamente aqui em vez de aceitar um cadastro
    /// que nunca vai conectar/descobrir nada de verdade (Hikvision adiado inteiramente, decisão
    /// confirmada — nenhum Provider existe para ele hoje).
    /// </summary>
    private static string? ValidarCamposPorFabricante(
        FabricanteEquipamento fabricante, string? numeroSerie, string? ip, int? porta, string? usuario, string? senha) =>
        fabricante switch
        {
            FabricanteEquipamento.Jfl => string.IsNullOrWhiteSpace(numeroSerie)
                ? "Número de Série é obrigatório para centrais JFL."
                : null,
            FabricanteEquipamento.ControlId => string.IsNullOrWhiteSpace(ip) || porta is null || string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha)
                ? "IP, Porta, Usuário e Senha são obrigatórios para equipamentos Control iD."
                : null,
            FabricanteEquipamento.Intelbras => string.IsNullOrWhiteSpace(ip) || porta is null || string.IsNullOrWhiteSpace(senha)
                ? "IP, Porta e Senha são obrigatórios para centrais Intelbras."
                : null,
            _ => $"Cadastro de equipamentos do fabricante {fabricante} ainda não é suportado.",
        };

    /// <summary>Mesma regra de <see cref="ValidarCamposPorFabricante"/>, mas Senha é opcional quando o equipamento já tem uma cadastrada (edição nunca obriga redigitar).</summary>
    private static string? ValidarCamposPorFabricanteAtualizacao(
        FabricanteEquipamento fabricante, string? numeroSerie, string? ip, int? porta, string? usuario, string? senha, bool possuiSenhaAtual) =>
        fabricante switch
        {
            FabricanteEquipamento.Jfl => string.IsNullOrWhiteSpace(numeroSerie)
                ? "Número de Série é obrigatório para centrais JFL."
                : null,
            FabricanteEquipamento.ControlId => string.IsNullOrWhiteSpace(ip) || porta is null || string.IsNullOrWhiteSpace(usuario) || (string.IsNullOrWhiteSpace(senha) && !possuiSenhaAtual)
                ? "IP, Porta, Usuário e Senha são obrigatórios para equipamentos Control iD."
                : null,
            FabricanteEquipamento.Intelbras => string.IsNullOrWhiteSpace(ip) || porta is null || (string.IsNullOrWhiteSpace(senha) && !possuiSenhaAtual)
                ? "IP, Porta e Senha são obrigatórios para centrais Intelbras."
                : null,
            _ => $"Cadastro de equipamentos do fabricante {fabricante} ainda não é suportado.",
        };

    /// <summary>
    /// Conecta no Control iD recém-cadastrado e persiste só o que o Provider realmente
    /// devolveu (<see cref="InformacoesEquipamento"/> tem só Versao/NomeDispositivo/NumeroSerie
    /// hoje — nunca fabricar Modelo/Firmware/MAC/Hostname que o Provider não expõe). Uma falha
    /// aqui nunca reprova o cadastro: só marca o equipamento como Offline (mesmo padrão de
    /// resiliência já usado por `EquipamentoIntegracaoServico`).
    /// </summary>
    private async Task ConectarEDescobrirControlIdAsync(
        Equipamento equipamento, string ip, int porta, string usuario, string senha, CancellationToken cancellationToken)
    {
        var conexao = new ConexaoEquipamento { Ip = ip, Porta = porta, Usuario = usuario, Senha = senha };

        try
        {
            var info = await _controlId.ConsultarInformacoesAsync(conexao, cancellationToken).ConfigureAwait(false);

            equipamento.Status = StatusEquipamento.Online;
            if (!string.IsNullOrWhiteSpace(info.NumeroSerie))
            {
                equipamento.Identificador = info.NumeroSerie;
            }

            var descobertas = new Dictionary<string, string> { ["Versao"] = info.Versao };
            if (!string.IsNullOrWhiteSpace(info.NomeDispositivo))
            {
                descobertas["NomeDispositivo"] = info.NomeDispositivo;
            }

            if (!string.IsNullOrWhiteSpace(info.NumeroSerie))
            {
                descobertas["NumeroSerie"] = info.NumeroSerie;
            }

            equipamento.InformacoesDescobertasJson = JsonSerializer.Serialize(descobertas);
            equipamento.UltimaDescobertaUtc = DateTime.UtcNow;
            equipamento.UltimaSincronizacaoUtc = DateTime.UtcNow;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            equipamento.Status = StatusEquipamento.Offline;
        }

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Testa a conexão com a central Intelbras recém-cadastrada — só atualiza Status (a
    /// integração não tem nenhuma descoberta real de dispositivo hoje, ver auditoria da Fase 0;
    /// nunca inventar um dicionário de "informações descobertas" vazio de conteúdo real).
    /// </summary>
    private async Task ConectarIntelbrasAsync(Equipamento equipamento, string ip, int porta, string senha, CancellationToken cancellationToken)
    {
        var conexao = new ConexaoIntelbras { Ip = ip, Porta = porta, Senha = senha };

        try
        {
            var resultado = await _intelbras.TestarConexaoAsync(conexao, cancellationToken).ConfigureAwait(false);
            equipamento.Status = resultado.Sucesso ? StatusEquipamento.Online : StatusEquipamento.Offline;
            if (resultado.Sucesso)
            {
                equipamento.UltimaSincronizacaoUtc = DateTime.UtcNow;
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            equipamento.Status = StatusEquipamento.Offline;
        }

        await _equipamentos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sprint 22B — mesma lógica de `EquipamentoServico.ResolverOuCriarModeloAsync` (Sprint 21),
    /// duplicada deliberadamente em vez de extraída para um helper compartilhado: extrair
    /// exigiria alterar o construtor/internals de `EquipamentoServico` (mobile-facing, estável,
    /// já testado) só para servir este Servico novo — risco desnecessário para ~10 linhas de
    /// get-or-create. Ver ADR 0031.
    /// </summary>
    private async Task<Guid?> ResolverOuCriarModeloAsync(FabricanteEquipamento fabricante, string? nomeModelo, CancellationToken cancellationToken)
    {
        var nome = nomeModelo?.Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return null;
        }

        var existente = await _modelosEquipamento.GetByFabricanteENomeAsync(fabricante, nome, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            return existente.Id;
        }

        var modelo = new ModeloEquipamento { Id = Guid.NewGuid(), Fabricante = fabricante, Nome = nome, CreatedAtUtc = DateTime.UtcNow };
        await _modelosEquipamento.AddAsync(modelo, cancellationToken).ConfigureAwait(false);
        await _modelosEquipamento.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return modelo.Id;
    }

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static IReadOnlyDictionary<string, string>? DesserializarDescobertas(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<Dictionary<string, string>>(json);

    private static EquipamentoAdminResponse ToDto(Equipamento equipamento) => new()
    {
        Id = equipamento.Id,
        PropriedadeId = equipamento.PropriedadeId,
        PropriedadeNome = equipamento.Propriedade?.Nome,
        Nome = equipamento.Nome,
        Fabricante = equipamento.Fabricante,
        Modelo = equipamento.ModeloEquipamento?.Nome,
        NumeroSerie = equipamento.Identificador,
        Status = equipamento.Status,
        EstadoOperacional = equipamento.EstadoOperacional,
        Ip = equipamento.Ip,
        Porta = equipamento.Porta,
        Usuario = equipamento.Usuario,
        MacAddress = equipamento.MacAddress,
        Observacoes = equipamento.Observacoes,
        CreatedAtUtc = equipamento.CreatedAtUtc,
        Excluido = equipamento.Excluido,
        DataExclusaoUtc = equipamento.DataExclusaoUtc,
        InformacoesDescobertas = DesserializarDescobertas(equipamento.InformacoesDescobertasJson),
        UltimaDescobertaUtc = equipamento.UltimaDescobertaUtc,
        UltimaSincronizacaoUtc = equipamento.UltimaSincronizacaoUtc,
    };
}
