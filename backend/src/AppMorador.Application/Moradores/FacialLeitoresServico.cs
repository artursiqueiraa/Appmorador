using AppMorador.Application.ControlId;
using AppMorador.Application.Equipamentos;
using AppMorador.Application.Hikvision;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Moradores;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AppMorador.Application.Moradores;

public sealed class SituacaoFotoLeitorResponse
{
    public required Guid EquipamentoId { get; init; }

    public required string NomeEquipamento { get; init; }

    public required SituacaoFotoLeitor Situacao { get; init; }

    public string? Mensagem { get; init; }

    public required DateTime AtualizadoEmUtc { get; init; }
}

public interface IFacialLeitoresServico
{
    /// <summary>Envia (foto != null) ou apaga (foto == null) o rosto em todos os leitores faciais da propriedade.</summary>
    Task SincronizarAsync(Morador morador, byte[]? foto, CancellationToken cancellationToken);

    Task<IReadOnlyList<SituacaoFotoLeitorResponse>> ListarAsync(Guid moradorId, CancellationToken cancellationToken);

    /// <summary>Tenta de novo o que falhou (leitor fora do ar). Devolve quantos resolveu.</summary>
    Task<int> ReprocessarFalhasAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 26 (ADR 0042) — leva o rosto do servidor aos leitores faciais (Control iD e Hikvision) da
/// propriedade da pessoa e apaga de lá quando a foto sai. Cada (pessoa, leitor) tem um registro de
/// situação; falha fica para nova tentativa (<see cref="ReprocessarFalhasAsync"/>). Nunca lança: um
/// leitor fora do ar não pode impedir salvar ou apagar a foto no servidor.
/// </summary>
public sealed class FacialLeitoresServico : IFacialLeitoresServico
{
    private const string Enviar = "Enviar";
    private const string Remover = "Remover";

    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly ISincronizacaoFotoRepositorio _sincronizacoes;
    private readonly IMoradorRepositorio _moradores;
    private readonly IArmazenamentoFotoMorador _armazenamento;
    private readonly IControlIdProvider _controlId;
    private readonly IHikvisionAcessoProvider _hikvision;
    private readonly ICriptografiaSimetrica _criptografia;
    private readonly IModeloEquipamentoRepositorio _modelos;
    private readonly ILogger<FacialLeitoresServico> _logger;

    public FacialLeitoresServico(
        IEquipamentoRepositorio equipamentos,
        ISincronizacaoFotoRepositorio sincronizacoes,
        IMoradorRepositorio moradores,
        IArmazenamentoFotoMorador armazenamento,
        IControlIdProvider controlId,
        IHikvisionAcessoProvider hikvision,
        ICriptografiaSimetrica criptografia,
        IModeloEquipamentoRepositorio modelos,
        ILogger<FacialLeitoresServico> logger)
    {
        _equipamentos = equipamentos;
        _sincronizacoes = sincronizacoes;
        _moradores = moradores;
        _armazenamento = armazenamento;
        _controlId = controlId;
        _hikvision = hikvision;
        _criptografia = criptografia;
        _modelos = modelos;
        _logger = logger;
    }

    public static bool EhLeitorFacial(FabricanteEquipamento fabricante) =>
        fabricante is FabricanteEquipamento.ControlId or FabricanteEquipamento.Hikvision;

    public async Task SincronizarAsync(Morador morador, byte[]? foto, CancellationToken cancellationToken)
    {
        var propriedadeId = morador.Unidade?.PropriedadeId;
        if (propriedadeId is null)
        {
            return;
        }

        var leitores = new List<Equipamento>();
        foreach (var e in await _equipamentos.ListByPropriedadeAsync(propriedadeId.Value, cancellationToken).ConfigureAwait(false))
        {
            if (await RecebeRostoAsync(e, cancellationToken).ConfigureAwait(false))
            {
                leitores.Add(e);
            }
        }
        if (leitores.Count == 0)
        {
            return;
        }

        var registros = await _sincronizacoes.ListByMoradorAsync(morador.Id, cancellationToken).ConfigureAwait(false);
        foreach (var leitor in leitores)
        {
            var registro = registros.FirstOrDefault(r => r.EquipamentoId == leitor.Id);
            if (foto is null && registro is null)
            {
                continue; // nunca foi enviada a este leitor: nada a apagar
            }

            var (sucesso, erro) = await ExecutarAsync(leitor, morador, foto, cancellationToken).ConfigureAwait(false);
            if (registro is null)
            {
                registro = new SincronizacaoFoto { Id = Guid.NewGuid(), MoradorId = morador.Id, EquipamentoId = leitor.Id, Operacao = Enviar };
                await _sincronizacoes.AddAsync(registro, cancellationToken).ConfigureAwait(false);
            }

            registro.Operacao = foto is null ? Remover : Enviar;
            registro.Situacao = !sucesso ? SituacaoFotoLeitor.Falhou : foto is null ? SituacaoFotoLeitor.Removida : SituacaoFotoLeitor.Enviada;
            registro.Mensagem = erro;
            registro.AtualizadoEmUtc = DateTime.UtcNow;
        }

        await _sincronizacoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Leitor facial configurado: Control iD/Hikvision com endereço e senha; com modelo cadastrado, o
    /// modelo precisa ter a capacidade Face. Sem configuração, fica de fora até ser configurado.
    /// </summary>
    private async Task<bool> RecebeRostoAsync(Equipamento e, CancellationToken cancellationToken)
    {
        if (!EhLeitorFacial(e.Fabricante) || string.IsNullOrWhiteSpace(e.Ip) || e.Porta is null
            || string.IsNullOrWhiteSpace(e.Usuario) || e.SenhaCriptografada is null)
        {
            return false;
        }

        if (e.ModeloEquipamentoId is null)
        {
            return true;
        }

        var capacidades = await _modelos.ListCapacidadesAsync(e.ModeloEquipamentoId.Value, cancellationToken).ConfigureAwait(false);
        return capacidades.Contains(EquipamentoCapacidade.Face);
    }

    public async Task<IReadOnlyList<SituacaoFotoLeitorResponse>> ListarAsync(Guid moradorId, CancellationToken cancellationToken)
    {
        var registros = await _sincronizacoes.ListByMoradorAsync(moradorId, cancellationToken).ConfigureAwait(false);
        return registros
            .Where(r => r.Situacao != SituacaoFotoLeitor.Removida)
            .Select(r => new SituacaoFotoLeitorResponse
            {
                EquipamentoId = r.EquipamentoId,
                NomeEquipamento = r.Equipamento?.Nome ?? "Leitor",
                Situacao = r.Situacao,
                Mensagem = r.Mensagem,
                AtualizadoEmUtc = r.AtualizadoEmUtc,
            })
            .ToList();
    }

    public async Task<int> ReprocessarFalhasAsync(CancellationToken cancellationToken)
    {
        var falhas = await _sincronizacoes.ListFalhasAsync(50, cancellationToken).ConfigureAwait(false);
        var resolvidas = 0;
        foreach (var falha in falhas)
        {
            var morador = await _moradores.GetByIdIncluindoExcluidosAsync(falha.MoradorId, cancellationToken).ConfigureAwait(false);
            if (morador is null || falha.Equipamento is null || !await RecebeRostoAsync(falha.Equipamento, cancellationToken).ConfigureAwait(false))
            {
                // Pessoa sumiu ou o equipamento deixou de ser leitor configurado: sai da fila.
                falha.Situacao = SituacaoFotoLeitor.Removida;
                falha.Mensagem = null;
                falha.AtualizadoEmUtc = DateTime.UtcNow;
                continue;
            }

            // Enviar só se a pessoa ainda existe e ainda tem foto; senão a operação certa é remover.
            byte[]? foto = null;
            if (falha.Operacao == Enviar && !morador.Excluido && !string.IsNullOrEmpty(morador.FotoPath))
            {
                await using var arquivo = _armazenamento.Abrir(morador.FotoPath);
                if (arquivo is not null)
                {
                    using var memoria = new MemoryStream();
                    await arquivo.CopyToAsync(memoria, cancellationToken).ConfigureAwait(false);
                    foto = memoria.ToArray();
                }
            }

            var (sucesso, erro) = await ExecutarAsync(falha.Equipamento, morador, foto, cancellationToken).ConfigureAwait(false);
            falha.Operacao = foto is null ? Remover : Enviar;
            falha.Situacao = !sucesso ? SituacaoFotoLeitor.Falhou : foto is null ? SituacaoFotoLeitor.Removida : SituacaoFotoLeitor.Enviada;
            falha.Mensagem = erro;
            falha.AtualizadoEmUtc = DateTime.UtcNow;
            if (sucesso)
            {
                resolvidas++;
            }
        }

        await _sincronizacoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return resolvidas;
    }

    private async Task<(bool Sucesso, string? Erro)> ExecutarAsync(Equipamento leitor, Morador morador, byte[]? foto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(leitor.Ip) || leitor.Porta is null || string.IsNullOrWhiteSpace(leitor.Usuario) || leitor.SenhaCriptografada is null)
        {
            return (false, "Leitor sem endereço ou senha cadastrados.");
        }

        try
        {
            var conexao = new ConexaoEquipamento
            {
                Ip = leitor.Ip,
                Porta = leitor.Porta.Value,
                Usuario = leitor.Usuario,
                Senha = _criptografia.Descriptografar(leitor.SenhaCriptografada),
            };

            var resultado = (leitor.Fabricante, foto) switch
            {
                (FabricanteEquipamento.ControlId, not null) => await _controlId.EnviarFotoAsync(conexao, morador.Id, morador.Nome, foto, cancellationToken).ConfigureAwait(false),
                (FabricanteEquipamento.ControlId, null) => await _controlId.RemoverFotoAsync(conexao, morador.Id, cancellationToken).ConfigureAwait(false),
                (_, not null) => await _hikvision.EnviarRostoAsync(conexao, morador.Id, morador.Nome, foto, cancellationToken).ConfigureAwait(false),
                _ => await _hikvision.RemoverPessoaAsync(conexao, morador.Id, cancellationToken).ConfigureAwait(false),
            };

            return (resultado.Sucesso, resultado.MensagemErro);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao sincronizar rosto do morador {MoradorId} com o leitor {EquipamentoId}", morador.Id, leitor.Id);
            return (false, "Não foi possível falar com o leitor.");
        }
    }
}
