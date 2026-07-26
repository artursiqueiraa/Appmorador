using AppMorador.Api.Auth;
using AppMorador.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AppMorador.Api.Realtime;

/// <summary>
/// Sprint 14 (ADR 0017) — camada de transporte exclusiva da Camada Operacional
/// (ADR 0016). Nunca consulta Equipamento/Snapshot/Ocorrencia, nunca conhece um
/// Provider, nunca executa uma regra de negócio operacional — a única "consulta ao
/// banco" feita aqui é a mesma checagem de posse (Propriedade.ProprietarioId) que
/// todo Controller já faz antes de expor qualquer dado, necessária para decidir se a
/// conexão pode entrar no grupo daquela Propriedade. Toda publicação de fato
/// (<see cref="OperacionalHubPublicador"/>) acontece fora do Hub, disparada pelo
/// domínio através de <see cref="AppMorador.Application.Operacional.IOperacionalEventoPublicador"/>.
/// Vive em Api (não em Infrastructure) pelo mesmo motivo dos Controllers: SignalR
/// exige o modelo de hospedagem Web (Microsoft.NET.Sdk.Web).
/// </summary>
[Authorize]
public sealed class OperacionalHub : Hub
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly ILogger<OperacionalHub> _logger;

    public OperacionalHub(IPropriedadeRepositorio propriedades, ILogger<OperacionalHub> logger)
    {
        _propriedades = propriedades;
        _logger = logger;
    }

    public static string GrupoPropriedade(Guid propriedadeId) => $"propriedade:{propriedadeId}";

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "OperacionalHub: conexao {ConnectionId} estabelecida (usuario {UsuarioId})",
            Context.ConnectionId, Context.User?.GetUsuarioId());
        return base.OnConnectedAsync();
    }

    /// <summary>Chamado pelo cliente logo após conectar, uma vez por Propriedade que deseja acompanhar (Sprint 14 — grupos só por Propriedade, ver dívida técnica item 25 sobre grupos por perfil).</summary>
    public async Task EntrarNaPropriedade(Guid propriedadeId)
    {
        var usuarioId = Context.User!.GetUsuarioId();
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, Context.ConnectionAborted).ConfigureAwait(false);

        if (propriedade is null || propriedade.ProprietarioId != usuarioId)
        {
            _logger.LogWarning(
                "OperacionalHub: conexao {ConnectionId} (usuario {UsuarioId}) recusada para a propriedade {PropriedadeId} — sem posse",
                Context.ConnectionId, usuarioId, propriedadeId);
            throw new HubException("Propriedade não encontrada.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoPropriedade(propriedadeId)).ConfigureAwait(false);
        _logger.LogInformation(
            "OperacionalHub: conexao {ConnectionId} entrou no grupo da propriedade {PropriedadeId}",
            Context.ConnectionId, propriedadeId);
    }

    /// <summary>Chamado pelo cliente ao trocar de Propriedade (SelecionarPropriedadeScreen) — evita acumular grupos de propriedades que o usuário não está mais acompanhando.</summary>
    public async Task SairDaPropriedade(Guid propriedadeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GrupoPropriedade(propriedadeId)).ConfigureAwait(false);
        _logger.LogInformation(
            "OperacionalHub: conexao {ConnectionId} saiu do grupo da propriedade {PropriedadeId}",
            Context.ConnectionId, propriedadeId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            exception, "OperacionalHub: conexao {ConnectionId} encerrada", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
