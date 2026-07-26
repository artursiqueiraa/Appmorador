using AppMorador.Domain.Entities;

namespace AppMorador.Application.Vagas;

/// <summary>
/// Única fonte da regra de status efetivo de uma Vaga — usada por
/// <see cref="VagaServico"/> (mapeamento de DTO) e por
/// <see cref="Application.Dashboard.DashboardServico"/> (contadores), para nunca
/// duplicar a regra de negócio entre os dois. Mesmo espírito de
/// <see cref="Application.Autorizacoes.StatusAutorizacaoCalculator"/> (ADR 0011).
/// </summary>
public static class VagaStatusCalculator
{
    /// <summary>
    /// Um override manual (Bloqueada/Reservada) sempre vence; sem override, o status
    /// é computado a partir da existência de um VinculoVeiculoVaga ativo para esta
    /// vaga — nunca exige job/scheduler.
    /// </summary>
    public static StatusVaga CalcularEfetivo(Vaga vaga, bool temVinculoAtivo)
    {
        if (vaga.StatusManual is not null)
        {
            return vaga.StatusManual.Value;
        }

        return temVinculoAtivo ? StatusVaga.Ocupada : StatusVaga.Livre;
    }
}
