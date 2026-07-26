using AppMorador.Application.Common;

namespace AppMorador.Application.Equipamentos;

/// <summary>Ações de integração real de um Equipamento já cadastrado — nunca CRUD (isso é <see cref="IEquipamentoServico"/>).</summary>
public interface IEquipamentoIntegracaoServico
{
    Task<Result<TesteConexaoResponse>> TestarConexaoAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<InformacoesEquipamentoResponse>> ConsultarInformacoesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<SincronizacaoResponse>> SincronizarMoradoresAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<SincronizacaoResponse>> SincronizarCredenciaisAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<SincronizacaoResponse>> SincronizarPermissoesAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);

    Task<Result<ImportacaoEventosResponse>> ImportarEventosAsync(Guid proprietarioId, Guid equipamentoId, CancellationToken cancellationToken);
}
