using AppMorador.Application.Common;

namespace AppMorador.Application.Dashboard;

public interface IDashboardServico
{
    Task<Result<DashboardResponse>> GetAsync(Guid proprietarioId, Guid propriedadeId, CancellationToken cancellationToken);
}
