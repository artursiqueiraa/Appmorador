namespace AppMorador.Domain.Entities;

/// <summary>
/// Uma propriedade (residencia, comercio, escritorio, clinica ou pequeno condominio)
/// cadastrada por um <see cref="Usuario"/> — o "tenant" do produto.
/// </summary>
public class Propriedade
{
    public Guid Id { get; set; }

    public required string Nome { get; set; }

    public required TipoPropriedade Tipo { get; set; }

    /// <summary>Endereco da propriedade — opcional, permite uma edicao com sentido real (nao so o nome).</summary>
    public string? Endereco { get; set; }

    /// <summary>Dono da propriedade. Unico dono por enquanto — sem compartilhamento/membros ainda.</summary>
    public Guid ProprietarioId { get; set; }

    public Usuario? Proprietario { get; set; }
}
