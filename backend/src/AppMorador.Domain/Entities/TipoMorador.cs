namespace AppMorador.Domain.Entities;

/// <summary>
/// Akteck Seg (ADR 0036) — papel da pessoa na unidade. Titular é quem responde pela
/// unidade; dependente mora junto (filhos, cônjuge, pais...). Não é uma permissão de
/// sistema: o acesso ao app continua sendo da conta do proprietário.
/// </summary>
public enum TipoMorador
{
    Titular = 1,
    Dependente = 2,
}
