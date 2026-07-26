using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD de escrita para o log de auditoria de credenciais — sem leitura nesta Sprint (ver ADR 0010).</summary>
public interface IHistoricoCredencialRepositorio
{
    Task AddAsync(HistoricoCredencial historico, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
