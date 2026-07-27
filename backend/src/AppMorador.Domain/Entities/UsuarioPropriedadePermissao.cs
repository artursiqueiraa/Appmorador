namespace AppMorador.Domain.Entities;

/// <summary>Vínculo NxN entre <see cref="UsuarioPropriedade"/> e <see cref="PermissaoFuncionalidade"/> — a presença de uma linha significa "concedida".</summary>
public class UsuarioPropriedadePermissao
{
    public Guid Id { get; set; }

    public Guid UsuarioPropriedadeId { get; set; }

    public UsuarioPropriedade? UsuarioPropriedade { get; set; }

    public PermissaoFuncionalidade Permissao { get; set; }
}
