using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.Credenciais;

public sealed class CriarCredencialRequest
{
    [Required(ErrorMessage = "Tipo é obrigatório.")]
    public required TipoCredencial Tipo { get; set; }
}

/// <summary>
/// Só o Status muda depois de criada — o Tipo é fixo (uma credencial Facial não vira
/// PIN; se o morador precisa de outro tipo, cadastra uma credencial nova).
/// </summary>
public sealed class AtualizarStatusCredencialRequest
{
    [Required(ErrorMessage = "Status é obrigatório.")]
    public required StatusCredencial Status { get; set; }
}

public sealed class CredencialResponse
{
    public required Guid Id { get; init; }

    public required Guid MoradorId { get; init; }

    public required TipoCredencial Tipo { get; init; }

    public required StatusCredencial Status { get; init; }
}
