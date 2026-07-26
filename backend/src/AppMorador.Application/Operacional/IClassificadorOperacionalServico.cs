using AppMorador.Domain.Entities;

namespace AppMorador.Application.Operacional;

/// <summary>
/// Sprint 13 — Camada Operacional Unificada (ADR 0016). Único ponto do sistema que
/// decide o que significa "Saudável"/"Atenção"/"Crítico"/"Offline" — nenhum Provider,
/// Controller ou tela reimplementa esta regra.
/// </summary>
public interface IClassificadorOperacionalServico
{
    EstadoOperacional ClassificarEquipamento(EstadoBrutoEquipamento estado);

    EstadoOperacional ClassificarPropriedade(IReadOnlyList<EstadoBrutoEquipamento> equipamentos, int quantidadeAlarmesAtivos);
}
