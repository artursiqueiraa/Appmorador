namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 9 — distingue pontos de acesso de pedestres (Portão Principal, Piscina) dos
/// que servem veículos (Garagem Principal, Garagem Secundária) — <see cref="PermissaoVeicular"/>
/// só pode apontar para um PontoAcesso Veicular.
/// </summary>
public enum TipoPontoAcesso
{
    Geral,
    Veicular,
}
