using AppMorador.Application.Notificacoes;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppMorador.Infrastructure.Notifications;

/// <summary>
/// Sprint 19 (ADR 0023) — única implementação de <see cref="INotificationProvider"/>
/// que fala com um provedor de push de verdade nesta Sprint (Firebase Cloud
/// Messaging via <c>FirebaseAdmin</c>, SDK oficial do Google). Nenhum outro código
/// do projeto referencia <c>FirebaseAdmin</c> diretamente — só esta classe.
///
/// Modo sem Firebase configurado: sem <see cref="FirebaseOptions.CredenciaisPath"/>
/// (arquivo de credencial da conta de serviço), esta classe nunca lança — registra
/// no log exatamente o que teria enviado e devolve sucesso simulado, para que todo
/// o resto da arquitetura (dispatcher, debounce, desativação de token) continue
/// funcionando e testável mesmo sem um projeto Firebase real. Ver ADR 0023.
/// </summary>
public sealed class FirebaseNotificationProvider : INotificationProvider
{
    /// <summary>Sprint 24 — precisam bater com o canal e o arquivo de som registrados no app (webapp/app.json).</summary>
    public const string CanalCritico = "alertas_criticos";

    public const string SomCritico = "alerta_critico.wav";

    private static readonly object InicializacaoLock = new();
    private static FirebaseApp? _app;

    private readonly FirebaseOptions _options;
    private readonly ILogger<FirebaseNotificationProvider> _logger;

    public FirebaseNotificationProvider(IOptions<FirebaseOptions> options, ILogger<FirebaseNotificationProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ResultadoEnvioNotificacao> EnviarAsync(NotificacaoPayload payload, IReadOnlyList<string> tokens, CancellationToken cancellationToken)
    {
        if (tokens.Count == 0)
        {
            return new ResultadoEnvioNotificacao { Sucesso = true };
        }

        if (!_options.Configurado)
        {
            _logger.LogInformation(
                "[PUSH] (sem Firebase configurado) enviaria \"{Titulo}\" para {Quantidade} dispositivo(s) — configure Firebase:CredenciaisPath para enviar de verdade",
                payload.Titulo, tokens.Count);
            return new ResultadoEnvioNotificacao { Sucesso = true, TokensComSucesso = tokens };
        }

        try
        {
            var messaging = ObterMessaging();
            var mensagem = new MulticastMessage
            {
                Tokens = tokens,
                Notification = new Notification { Title = payload.Titulo, Body = payload.Corpo },
                Data = new Dictionary<string, string>
                {
                    ["eventoTipo"] = payload.EventoTipo.ToString(),
                    ["propriedadeId"] = payload.PropriedadeId.ToString(),
                    ["equipamentoId"] = payload.EquipamentoId?.ToString() ?? string.Empty,
                    ["timestamp"] = DateTime.UtcNow.ToString("O"),
                    ["acao"] = payload.Acao,
                    ["alertaId"] = payload.AlertaId?.ToString() ?? string.Empty,
                },
                Android = new AndroidConfig
                {
                    Priority = payload.Prioridade is PrioridadeNotificacao.Alta or PrioridadeNotificacao.Critica ? Priority.High : Priority.Normal,
                    Notification = new AndroidNotification
                    {
                        // Sprint 24 (ADR 0040) — crítico vai para o canal "alertas_criticos" (som de
                        // alarme próprio, criado pelo app com importância máxima).
                        ChannelId = payload.Prioridade == PrioridadeNotificacao.Critica ? CanalCritico : ResolverCanalAndroid(payload.EventoTipo),
                        Sound = payload.Prioridade == PrioridadeNotificacao.Critica ? SomCritico : "default",
                    },
                },
            };

            var resposta = await messaging.SendEachForMulticastAsync(mensagem, cancellationToken).ConfigureAwait(false);
            return InterpretarResposta(tokens, resposta);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao enviar notificacao via Firebase para {Quantidade} dispositivo(s)", tokens.Count);
            return new ResultadoEnvioNotificacao { Sucesso = false, Erro = "Falha ao enviar notificação." };
        }
    }

    public Task<bool> ValidarTokenAsync(string token, CancellationToken cancellationToken)
    {
        // Sprint 19 — o FCM não tem uma chamada dedicada de "validar token sem
        // enviar"; a validação real acontece no primeiro envio (SendEachForMulticastAsync
        // já desativa tokens que voltarem MessagingErrorCode.Unregistered). Sem
        // Firebase configurado, qualquer token não vazio é aceito.
        return Task.FromResult(!string.IsNullOrWhiteSpace(token));
    }

    private static string ResolverCanalAndroid(EventoNotificacaoTipo tipo) => tipo switch
    {
        EventoNotificacaoTipo.AlarmeDisparado or EventoNotificacaoTipo.EquipamentoOffline or EventoNotificacaoTipo.FalhaEnergia
            or EventoNotificacaoTipo.BateriaBaixa => "alertas",
        EventoNotificacaoTipo.ComandoAcionado or EventoNotificacaoTipo.VisitanteAutorizado or EventoNotificacaoTipo.EntregaRecebida => "atividades",
        _ => "geral",
    };

    private static ResultadoEnvioNotificacao InterpretarResposta(IReadOnlyList<string> tokens, BatchResponse resposta)
    {
        var comSucesso = new List<string>();
        var invalidos = new List<string>();

        for (var i = 0; i < resposta.Responses.Count; i++)
        {
            var item = resposta.Responses[i];
            if (item.IsSuccess)
            {
                comSucesso.Add(tokens[i]);
                continue;
            }

            // MessagingErrorCode.Unregistered = o app foi desinstalado ou o token nunca
            // mais vai funcionar — só nesse caso desativamos o dispositivo. Falhas
            // transitórias (Unavailable/Internal/QuotaExceeded) não desativam nada.
            var codigo = item.Exception?.MessagingErrorCode;
            if (codigo is MessagingErrorCode.Unregistered or MessagingErrorCode.InvalidArgument or MessagingErrorCode.SenderIdMismatch)
            {
                invalidos.Add(tokens[i]);
            }
        }

        return new ResultadoEnvioNotificacao
        {
            Sucesso = comSucesso.Count > 0 || tokens.Count == 0,
            TokensComSucesso = comSucesso,
            TokensInvalidos = invalidos,
        };
    }

    private FirebaseMessaging ObterMessaging()
    {
        if (_app is null)
        {
            lock (InicializacaoLock)
            {
                _app ??= FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(_options.CredenciaisPath),
                    ProjectId = _options.ProjectId,
                });
            }
        }

        return FirebaseMessaging.GetMessaging(_app);
    }
}
