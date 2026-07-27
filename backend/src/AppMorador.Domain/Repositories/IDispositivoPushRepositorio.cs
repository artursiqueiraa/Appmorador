using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>Porta DDD para o agregado DispositivoPush — a implementação (EF Core) vive em Infrastructure.</summary>
public interface IDispositivoPushRepositorio
{
    Task<DispositivoPush?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Usado no registro: se o mesmo token já existir (reinstalação, app já registrado), atualiza em vez de duplicar.</summary>
    Task<DispositivoPush?> GetByTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Alvo real de todo envio de notificação: a Propriedade não "tem" dispositivos
    /// diretamente — quem recebe é o dono dela (mesma regra de ownership usada em
    /// Dashboard/Eventos/Equipamentos, nunca compartilhamento de acesso).
    /// </summary>
    Task<IReadOnlyList<DispositivoPush>> ListAtivosByUsuarioAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task AddAsync(DispositivoPush dispositivo, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
