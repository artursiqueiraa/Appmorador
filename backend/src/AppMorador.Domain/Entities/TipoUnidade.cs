namespace AppMorador.Domain.Entities;

/// <summary>
/// Classificação da unidade dentro de uma propriedade. Deliberadamente mais ampla que
/// os exemplos da Sprint 6 (Casa/Apartamento/Loja/SalaComercial/Bloco) para reduzir a
/// chance de precisar de outra migration de enum logo na próxima Sprint de domínio.
/// </summary>
public enum TipoUnidade
{
    Casa,
    Apartamento,
    Loja,
    SalaComercial,
    Galpao,
    Quiosque,
    Escritorio,
    Outro,
}
