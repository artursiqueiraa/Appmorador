namespace AppMorador.Application.Sms;

/// <summary>
/// Sprint 24 (ADR 0040, Decisão B) — porta de envio de SMS para contatos de emergência (pessoas
/// sem o app). Nenhum código fora da implementação concreta conhece o provedor (Zenvia, Twilio…).
/// Hoje a única implementação é simulada: registra no log o que enviaria.
/// </summary>
public interface IEnvioSms
{
    /// <param name="telefone">Só dígitos, com DDD (DDI 55 opcional).</param>
    /// <returns>true quando o provedor aceitou a mensagem.</returns>
    Task<bool> EnviarAsync(string telefone, string mensagem, CancellationToken cancellationToken);
}
