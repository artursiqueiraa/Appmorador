using AppMorador.Domain.Entities;

namespace AppMorador.Domain.Repositories;

/// <summary>
/// Porta DDD mínima para <see cref="Central"/> — Central não tem CRUD via API ainda
/// (cadastro fica fora do escopo desta Sprint, ver ADR 0015); este método serve só
/// para o auto-vínculo por Número de Série entre Equipamento (Fabricante=Jfl) e a
/// Central já usada pelo pipeline de eventos (Ocorrencia/Zona) existente desde a Fase 1.
/// </summary>
public interface ICentralRepositorio
{
    Task<Central?> GetByPropriedadeIdENumeroSerieAsync(Guid propriedadeId, string numeroSerie, CancellationToken cancellationToken);
}
