namespace AppMorador.Application.Email;

/// <summary>
/// Sprint 25 (ADR 0041) — porta de envio de e-mail (convites). Implementação real: Resend (decisão
/// do usuário), ativa só com <c>Email:ResendApiKey</c> configurada; sem chave, envio simulado (log).
/// </summary>
public interface IEnvioEmail
{
    /// <returns>true quando o provedor aceitou a mensagem.</returns>
    Task<bool> EnviarAsync(string para, string assunto, string html, CancellationToken cancellationToken);
}

/// <summary>Configuração de e-mail e do link público do app (usado no link do convite).</summary>
public sealed class EmailOptions
{
    public string? ResendApiKey { get; set; }

    /// <summary>Remetente verificado no Resend, ex.: "Akteck Seg &lt;convites@seudominio.com.br&gt;".</summary>
    public string Remetente { get; set; } = "Akteck Seg <onboarding@resend.dev>";

    /// <summary>Endereço onde o app web abre o convite: {UrlApp}/convite/{codigo}.</summary>
    public string UrlApp { get; set; } = "http://localhost:8081";

    public bool Configurado => !string.IsNullOrWhiteSpace(ResendApiKey);
}
