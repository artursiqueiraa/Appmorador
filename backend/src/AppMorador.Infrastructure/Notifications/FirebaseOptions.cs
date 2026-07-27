namespace AppMorador.Infrastructure.Notifications;

/// <summary>
/// Sprint 19 (ADR 0023). Sem <see cref="CredenciaisPath"/> configurado, o
/// <c>FirebaseNotificationProvider</c> opera em modo documentado "sem Firebase" —
/// nunca lança, nunca bloqueia o fluxo, só registra o que teria sido enviado (ver
/// ADR 0023 "Modo sem Firebase configurado"). Configurar via
/// <c>dotnet user-secrets set "Firebase:CredenciaisPath" "..."</c> (mesmo padrão de
/// segredo de `Jwt:Key`/connection string — nunca em `appsettings.json`
/// committado).
/// </summary>
public sealed class FirebaseOptions
{
    public string? ProjectId { get; set; }

    /// <summary>Caminho absoluto do JSON da conta de serviço (nunca versionado — ver `.gitignore`).</summary>
    public string? CredenciaisPath { get; set; }

    public bool Configurado => !string.IsNullOrWhiteSpace(CredenciaisPath) && File.Exists(CredenciaisPath);
}
