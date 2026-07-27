using AppMorador.Domain.Entities;

namespace AppMorador.Application.Provisionamentos;

public sealed class CriarProvisionamentoRequest
{
    public required string Nome { get; init; }

    public required TemplateProvisionamento Template { get; init; }
}

public sealed class ProvisionamentoResponse
{
    public Guid Id { get; init; }

    public Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public required TemplateProvisionamento Template { get; init; }

    public required StatusProvisionamento Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? AtualizadoEmUtc { get; init; }
}
