using AppMorador.Application.Common;

namespace AppMorador.Application.Operacional;

/// <summary>Única porta pela qual Dashboard/Mobile/APIs futuras obtêm informação operacional consolidada (ver ADR 0016) — nunca um Provider diretamente.</summary>
public interface ISnapshotOperacionalServico
{
    /// <summary>Lê o último snapshot persistido; gera um na hora se a Propriedade ainda não tem nenhum (bootstrap — nunca chama Provider, só agrega dado já existente).</summary>
    Task<Result<SnapshotOperacionalResponse>> ObterAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>Recalcula e persiste um novo snapshot agora — ação explícita do usuário (botão "Atualizar"), nunca automática.</summary>
    Task<Result<SnapshotOperacionalResponse>> AtualizarAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);

    /// <summary>
    /// Sprint 14 (ADR 0017) — recalcula, persiste e publica em tempo real, disparada por
    /// uma mutação já concluída em Equipamento/StatusCentralJfl/Ocorrencia (nunca pelo
    /// próprio usuário via HTTP, por isso não recebe proprietarioId — o chamador já
    /// conhece a Propriedade com segurança, sem precisar reautenticar posse).
    /// </summary>
    Task RegenerarEPublicarAsync(Guid propriedadeId, MotivoAtualizacaoOperacional motivo, CancellationToken cancellationToken);
}
