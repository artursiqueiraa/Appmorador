namespace AppMorador.Application.Notificacoes;

/// <summary>Resultado bruto de uma tentativa de envio — separa sucesso de token inválido (que precisa desativar o dispositivo) de falha transitória (que não deveria desativar nada).</summary>
public sealed class ResultadoEnvioNotificacao
{
    public required bool Sucesso { get; init; }

    public IReadOnlyList<string> TokensComSucesso { get; init; } = [];

    /// <summary>Tokens que o provedor rejeitou como definitivamente inválidos (desinstalado, nunca mais vai funcionar) — o chamador deve marcar `Ativo=false`.</summary>
    public IReadOnlyList<string> TokensInvalidos { get; init; } = [];

    public string? Erro { get; init; }
}

/// <summary>
/// Sprint 19 (ADR 0023) — porta de abstração de envio de push. Nenhum código de
/// domínio ou aplicação pode referenciar Firebase/FCM diretamente — sempre através
/// desta interface (mesmo padrão já estabelecido para toda integração de
/// fabricante: <c>IJflProvider</c>, <c>IControlIdProvider</c>, <c>IIntelbrasProvider</c>).
/// Firebase é a implementação de hoje (<c>FirebaseNotificationProvider</c>);
/// OneSignal/Azure/Huawei amanhã implementam a mesma porta, sem tocar
/// <c>NotificationDispatcher</c>/<c>NotificationService</c>.
/// </summary>
public interface INotificationProvider
{
    Task<ResultadoEnvioNotificacao> EnviarAsync(NotificacaoPayload payload, IReadOnlyList<string> tokens, CancellationToken cancellationToken);

    Task<bool> ValidarTokenAsync(string token, CancellationToken cancellationToken);
}
