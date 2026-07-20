namespace AppMorador.Domain.Entities;

/// <summary>
/// Indica se uma <see cref="Ocorrencia"/> conseguiu resolver a central e a zona de
/// origem contra o cadastro, ou se foi criada so com os dados brutos do evento
/// (central e/ou zona ainda nao provisionadas).
/// </summary>
public enum StatusResolucao
{
    /// <summary>CentralId e ZonaId foram resolvidos com sucesso.</summary>
    Resolvido,

    /// <summary>Central e/ou zona nao foram encontradas no cadastro.</summary>
    NaoResolvido,
}
