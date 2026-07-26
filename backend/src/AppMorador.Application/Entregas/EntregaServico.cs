using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Entregas;

/// <summary>
/// Ownership resolvido via Entrega→Unidade→Propriedade.ProprietarioId (visão unificada
/// por propriedade — Create/List não são aninhados sob o Morador, diferente de
/// Credencial/Veiculo, porque o caso de uso natural é "ver todas as entregas da
/// propriedade", ver ADR 0013). Status é 100% manual — sem StatusManual/calculadora
/// híbrida (diferente de Autorizacao/Vaga): cada transição é uma ação explícita do
/// usuário, validada contra uma máquina de estados simples.
/// </summary>
public sealed class EntregaServico : IEntregaServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUnidadeRepositorio _unidades;
    private readonly IMoradorRepositorio _moradores;
    private readonly IEntregaRepositorio _entregas;
    private readonly IHistoricoEntregaRepositorio _historico;

    public EntregaServico(
        IPropriedadeRepositorio propriedades,
        IUnidadeRepositorio unidades,
        IMoradorRepositorio moradores,
        IEntregaRepositorio entregas,
        IHistoricoEntregaRepositorio historico)
    {
        _propriedades = propriedades;
        _unidades = unidades;
        _moradores = moradores;
        _entregas = entregas;
        _historico = historico;
    }

    public async Task<Result<EntregaResponse>> CreateAsync(
        Guid proprietarioId, Guid propriedadeId, CriarEntregaRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<EntregaResponse>.Fail("Propriedade não encontrada.");
        }

        var unidade = await _unidades.GetByIdAsync(request.UnidadeId, cancellationToken).ConfigureAwait(false);
        if (unidade is null || unidade.PropriedadeId != propriedadeId)
        {
            return Result<EntregaResponse>.Fail("Unidade não encontrada.");
        }

        var morador = await _moradores.GetByIdAsync(request.MoradorDestinatarioId, cancellationToken).ConfigureAwait(false);
        if (morador is null || morador.UnidadeId != request.UnidadeId)
        {
            return Result<EntregaResponse>.Fail("Morador não encontrado.");
        }

        var entrega = new Entrega
        {
            Id = Guid.NewGuid(),
            MoradorDestinatarioId = request.MoradorDestinatarioId,
            UnidadeId = request.UnidadeId,
            Tipo = request.Tipo,
            Descricao = NullIfBlank(request.Descricao),
            RecebidoPor = null,
            DataRecebimentoUtc = null,
            DataRetiradaUtc = null,
            Observacoes = NullIfBlank(request.Observacoes),
            Status = StatusEntrega.AguardandoRecebimento,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _entregas.AddAsync(entrega, cancellationToken).ConfigureAwait(false);
        await RegistrarHistoricoAsync(
            entrega.Id, TipoEventoHistoricoEntrega.EntregaCadastrada, $"Entrega registrada para {morador.Nome} ({unidade.Identificacao}).", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _entregas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EntregaResponse>.Ok(ToDto(entrega, morador.Nome, unidade.Identificacao));
    }

    public async Task<Result<IReadOnlyList<EntregaResponse>>> ListByPropriedadeAsync(
        Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || propriedade.ProprietarioId != proprietarioId)
        {
            return Result<IReadOnlyList<EntregaResponse>>.Fail("Propriedade não encontrada.");
        }

        var entregas = await _entregas.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<EntregaResponse>>.Ok(
            entregas.Select(e => ToDto(e, e.MoradorDestinatario?.Nome ?? "", e.Unidade?.Identificacao ?? "")).ToList());
    }

    public async Task<Result<EntregaResponse>> GetByIdAsync(Guid proprietarioId, Guid entregaId, CancellationToken cancellationToken)
    {
        var entrega = await _entregas.GetByIdAsync(entregaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(entrega, proprietarioId))
        {
            return Result<EntregaResponse>.Fail("Entrega não encontrada.");
        }

        return Result<EntregaResponse>.Ok(ToDto(entrega!, entrega!.MoradorDestinatario?.Nome ?? "", entrega.Unidade?.Identificacao ?? ""));
    }

    public async Task<Result<EntregaResponse>> UpdateAsync(
        Guid proprietarioId, Guid entregaId, AtualizarEntregaRequest request, CancellationToken cancellationToken)
    {
        var entrega = await _entregas.GetByIdAsync(entregaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(entrega, proprietarioId))
        {
            return Result<EntregaResponse>.Fail("Entrega não encontrada.");
        }

        if (entrega!.Status is StatusEntrega.Retirada or StatusEntrega.Cancelada)
        {
            return Result<EntregaResponse>.Fail("Entrega retirada ou cancelada não pode ser alterada.");
        }

        entrega.Tipo = request.Tipo;
        entrega.Descricao = NullIfBlank(request.Descricao);
        entrega.Observacoes = NullIfBlank(request.Observacoes);

        await RegistrarHistoricoAsync(entrega.Id, TipoEventoHistoricoEntrega.EntregaAlterada, "Entrega alterada.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _entregas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EntregaResponse>.Ok(ToDto(entrega, entrega.MoradorDestinatario?.Nome ?? "", entrega.Unidade?.Identificacao ?? ""));
    }

    public async Task<Result<EntregaResponse>> AtualizarStatusAsync(
        Guid proprietarioId, Guid entregaId, AtualizarStatusEntregaRequest request, CancellationToken cancellationToken)
    {
        var entrega = await _entregas.GetByIdAsync(entregaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(entrega, proprietarioId))
        {
            return Result<EntregaResponse>.Fail("Entrega não encontrada.");
        }

        var transicaoValida = (entrega!.Status, request.Status) switch
        {
            (StatusEntrega.AguardandoRecebimento, StatusEntrega.DisponivelParaRetirada) => true,
            (StatusEntrega.AguardandoRecebimento, StatusEntrega.Cancelada) => true,
            (StatusEntrega.DisponivelParaRetirada, StatusEntrega.Retirada) => true,
            (StatusEntrega.DisponivelParaRetirada, StatusEntrega.Cancelada) => true,
            _ => false,
        };

        if (!transicaoValida)
        {
            return Result<EntregaResponse>.Fail($"Não é possível mudar de {entrega.Status} para {request.Status}.");
        }

        var agora = DateTime.UtcNow;
        TipoEventoHistoricoEntrega tipoEvento;
        string descricaoEvento;

        switch (request.Status)
        {
            case StatusEntrega.DisponivelParaRetirada:
                entrega.DataRecebimentoUtc = agora;
                entrega.RecebidoPor = NullIfBlank(request.RecebidoPor);
                tipoEvento = TipoEventoHistoricoEntrega.EntregaRecebida;
                descricaoEvento = "Entrega marcada como disponível para retirada.";
                break;
            case StatusEntrega.Retirada:
                entrega.DataRetiradaUtc = agora;
                tipoEvento = TipoEventoHistoricoEntrega.EntregaRetirada;
                descricaoEvento = "Entrega retirada.";
                break;
            default: // Cancelada
                tipoEvento = TipoEventoHistoricoEntrega.EntregaCancelada;
                descricaoEvento = "Entrega cancelada.";
                break;
        }

        entrega.Status = request.Status;

        await RegistrarHistoricoAsync(entrega.Id, tipoEvento, descricaoEvento, proprietarioId, cancellationToken).ConfigureAwait(false);
        await _entregas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<EntregaResponse>.Ok(ToDto(entrega, entrega.MoradorDestinatario?.Nome ?? "", entrega.Unidade?.Identificacao ?? ""));
    }

    public async Task<Result> DeleteAsync(Guid proprietarioId, Guid entregaId, CancellationToken cancellationToken)
    {
        var entrega = await _entregas.GetByIdAsync(entregaId, cancellationToken).ConfigureAwait(false);
        if (!PertenceAoProprietario(entrega, proprietarioId))
        {
            return Result.Fail("Entrega não encontrada.");
        }

        entrega!.Excluido = true;
        entrega.DataExclusaoUtc = DateTime.UtcNow;
        entrega.ExcluidoPorUsuarioId = proprietarioId;

        await RegistrarHistoricoAsync(entrega.Id, TipoEventoHistoricoEntrega.EntregaExcluida, "Entrega excluída.", proprietarioId, cancellationToken)
            .ConfigureAwait(false);
        await _entregas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Ok();
    }

    private async Task RegistrarHistoricoAsync(
        Guid entregaId, TipoEventoHistoricoEntrega tipoEvento, string descricao, Guid usuarioId, CancellationToken cancellationToken) =>
        await _historico.AddAsync(
            new HistoricoEntrega { Id = Guid.NewGuid(), EntregaId = entregaId, TipoEvento = tipoEvento, Descricao = descricao, UsuarioId = usuarioId, CreatedAtUtc = DateTime.UtcNow },
            cancellationToken).ConfigureAwait(false);

    private static bool PertenceAoProprietario(Entrega? entrega, Guid proprietarioId) =>
        entrega?.MoradorDestinatario?.Unidade?.Propriedade is not null && entrega.MoradorDestinatario.Unidade.Propriedade.ProprietarioId == proprietarioId;

    private static string? NullIfBlank(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static EntregaResponse ToDto(Entrega entrega, string moradorNome, string unidadeIdentificacao) => new()
    {
        Id = entrega.Id,
        MoradorDestinatarioId = entrega.MoradorDestinatarioId,
        MoradorDestinatarioNome = moradorNome,
        UnidadeId = entrega.UnidadeId,
        UnidadeIdentificacao = unidadeIdentificacao,
        Tipo = entrega.Tipo,
        Descricao = entrega.Descricao,
        RecebidoPor = entrega.RecebidoPor,
        DataRecebimentoUtc = entrega.DataRecebimentoUtc,
        DataRetiradaUtc = entrega.DataRetiradaUtc,
        Observacoes = entrega.Observacoes,
        Status = entrega.Status,
    };
}
