using AppMorador.Application.Sms;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Notifications;

/// <summary>
/// Sprint 24 (ADR 0040, Decisão B) — implementação de <see cref="IEnvioSms"/> enquanto não há conta
/// num provedor de SMS: registra no log o que seria enviado (telefone mascarado — dado pessoal de
/// terceiro) e devolve sucesso. Trocar por um provedor real é registrar outra implementação.
/// </summary>
public sealed class EnvioSmsSimulado : IEnvioSms
{
    private readonly ILogger<EnvioSmsSimulado> _logger;

    public EnvioSmsSimulado(ILogger<EnvioSmsSimulado> logger)
    {
        _logger = logger;
    }

    public Task<bool> EnviarAsync(string telefone, string mensagem, CancellationToken cancellationToken)
    {
        _logger.LogWarning("[SMS] (simulado, sem provedor configurado) enviaria para {Telefone}: {Mensagem}", Mascarar(telefone), mensagem);
        return Task.FromResult(true);
    }

    internal static string Mascarar(string telefone) =>
        telefone.Length <= 4 ? "****" : new string('*', telefone.Length - 4) + telefone[^4..];
}
