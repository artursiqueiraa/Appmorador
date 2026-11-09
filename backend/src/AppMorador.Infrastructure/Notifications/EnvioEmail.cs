using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppMorador.Application.Conta;
using AppMorador.Application.Email;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppMorador.Infrastructure.Notifications;

/// <summary>
/// Sprint 25 (ADR 0041) — e-mail pelo Resend (https://resend.com, decisão do usuário). Sem
/// <c>Email:ResendApiKey</c> configurada, registra no log o que enviaria (e-mail mascarado) e
/// devolve false — o dono ainda tem o link para compartilhar no WhatsApp.
/// </summary>
public sealed class EnvioEmailResend : IEnvioEmail
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailOptions _opcoes;
    private readonly ILogger<EnvioEmailResend> _logger;

    public EnvioEmailResend(IHttpClientFactory httpClientFactory, EmailOptions opcoes, ILogger<EnvioEmailResend> logger)
    {
        _httpClientFactory = httpClientFactory;
        _opcoes = opcoes;
        _logger = logger;
    }

    public async Task<bool> EnviarAsync(string para, string assunto, string html, CancellationToken cancellationToken)
    {
        if (!_opcoes.Configurado)
        {
            _logger.LogWarning("[EMAIL] (simulado, sem Email:ResendApiKey) enviaria \"{Assunto}\" para {Para}", assunto, Mascarar(para));
            return false;
        }

        try
        {
            var client = _httpClientFactory.CreateClient("Resend");
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
            {
                Content = JsonContent.Create(new { from = _opcoes.Remetente, to = new[] { para }, subject = assunto, html }),
            };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opcoes.ResendApiKey);
            using var resposta = await client.SendAsync(requisicao, cancellationToken).ConfigureAwait(false);
            if (!resposta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Resend recusou o e-mail \"{Assunto}\" ({Status})", assunto, (int)resposta.StatusCode);
            }

            return resposta.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao enviar e-mail pelo Resend");
            return false;
        }
    }

    internal static string Mascarar(string email)
    {
        var arroba = email.IndexOf('@');
        return arroba <= 1 ? "***" : $"{email[0]}***{email[arroba..]}";
    }
}

/// <summary>Sprint 25 — encerra todas as sessões (refresh tokens ainda válidos) do usuário.</summary>
internal sealed class SessoesUsuario : ISessoesUsuario
{
    private readonly AppDbContext _db;

    public SessoesUsuario(AppDbContext db)
    {
        _db = db;
    }

    public async Task EncerrarTodasAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var ativos = await _db.RefreshTokens
            .Where(t => t.UsuarioId == usuarioId && t.RevokedAtUtc == null && t.ExpiresAtUtc > agora)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var token in ativos)
        {
            token.RevokedAtUtc = agora;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
