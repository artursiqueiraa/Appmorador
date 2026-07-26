namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 13 — Camada Operacional Unificada (ADR 0016). Classificação consolidada do
/// estado de um equipamento ou de uma Propriedade inteira — nunca calculada em mais de
/// um lugar (ver <c>IClassificadorOperacionalServico</c>, em Application/Operacional).
/// </summary>
public enum EstadoOperacional
{
    Saudavel,
    Atencao,
    Critico,
    Offline,
}
