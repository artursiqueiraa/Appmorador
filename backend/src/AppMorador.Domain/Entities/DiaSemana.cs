namespace AppMorador.Domain.Entities;

/// <summary>Combinação de dias em que uma <see cref="PermissaoAcesso"/> é válida.</summary>
[Flags]
public enum DiaSemana
{
    Nenhum = 0,
    Segunda = 1,
    Terca = 2,
    Quarta = 4,
    Quinta = 8,
    Sexta = 16,
    Sabado = 32,
    Domingo = 64,
    Todos = Segunda | Terca | Quarta | Quinta | Sexta | Sabado | Domingo,
}
