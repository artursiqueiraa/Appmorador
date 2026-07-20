using System.ComponentModel.DataAnnotations;

namespace AppMorador.Application.Autenticacao;

public sealed class CadastrarUsuarioRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "A senha deve ter ao menos 8 caracteres, com letra maiúscula, minúscula e número.")]
    public string Senha { get; set; } = string.Empty;
}

public sealed class EntrarRequest
{
    [Required(ErrorMessage = "E-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}

public sealed class RefreshRequest
{
    [Required(ErrorMessage = "RefreshToken é obrigatório.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class SairRequest
{
    [Required(ErrorMessage = "RefreshToken é obrigatório.")]
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class EntrarResponse
{
    public required string AccessToken { get; init; }

    public required string RefreshToken { get; init; }

    public required int ExpiresInSeconds { get; init; }

    public required Guid UsuarioId { get; init; }

    public required string Nome { get; init; }

    public required string Email { get; init; }
}
