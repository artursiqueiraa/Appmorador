using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Uma unidade dentro de uma <see cref="Propriedade"/> — obrigatoriamente vinculada a
/// uma propriedade (mesmo uma residência simples tem uma única Unidade, ex.: "Casa
/// principal"). Agregado intermediário entre Propriedade e <see cref="Morador"/>.
/// </summary>
public class Unidade : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required TipoUnidade Tipo { get; set; }

    /// <summary>Identificação da unidade dentro da propriedade (ex.: "302", "Bloco B - Casa 12").</summary>
    public required string Identificacao { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
