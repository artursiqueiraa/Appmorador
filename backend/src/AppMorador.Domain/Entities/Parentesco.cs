namespace AppMorador.Domain.Entities;

/// <summary>Akteck Seg (ADR 0036) — relação do dependente com o titular. Só faz sentido para <see cref="TipoMorador.Dependente"/>.</summary>
public enum Parentesco
{
    Conjuge = 1,
    Filho = 2,
    PaiOuMae = 3,
    Irmao = 4,
    AvoOuAva = 5,
    Outro = 6,
}
