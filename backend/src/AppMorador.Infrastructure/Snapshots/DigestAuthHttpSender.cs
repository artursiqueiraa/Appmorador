using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AppMorador.Infrastructure.Snapshots;

/// <summary>
/// Envia um GET com autenticacao HTTP Digest ou Basic calculada manualmente nos
/// headers da requisicao, em vez de via <see cref="HttpClientHandler.Credentials"/>.
///
/// Motivo desta classe existir: com <see cref="IHttpClientFactory"/> o handler
/// (`SocketsHttpHandler`/`HttpClientHandler`) e gerenciado/reciclado pelo pool da
/// factory e compartilhado entre chamadas — nao da para configurar
/// `Credentials`/`PreAuthenticate` por chamada com credenciais diferentes por DVR
/// (o que a abordagem anterior, um `new HttpClientHandler` por request, permitia).
/// Calculando o Digest/Basic manualmente por requisicao, qualquer `HttpClient` vindo
/// da factory serve, sem estado por-DVR no handler.
/// </summary>
internal static class DigestAuthHttpSender
{
    public static async Task<byte[]?> GetBytesAsync(
        HttpClient client, string relativeUrl, string username, string password, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(relativeUrl, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var challenge = response.Headers.WwwAuthenticate.FirstOrDefault();
            if (challenge is null)
            {
                return null;
            }

            using var retryRequest = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            retryRequest.Headers.Authorization = challenge.Scheme.Equals("Digest", StringComparison.OrdinalIgnoreCase)
                ? BuildDigestHeader(challenge.Parameter, username, password, "/" + relativeUrl)
                : new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));

            try
            {
                response = await client.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private static AuthenticationHeaderValue BuildDigestHeader(string? challengeParams, string username, string password, string uri)
    {
        var parts = ParseChallenge(challengeParams);
        var realm = parts.GetValueOrDefault("realm", string.Empty);
        var nonce = parts.GetValueOrDefault("nonce", string.Empty);
        var qop = parts.GetValueOrDefault("qop", string.Empty);

        var ha1 = Md5Hex($"{username}:{realm}:{password}");
        var ha2 = Md5Hex($"GET:{uri}");

        string response;
        string? nc = null;
        string? cnonce = null;

        if (!string.IsNullOrEmpty(qop))
        {
            nc = "00000001";
            cnonce = Guid.NewGuid().ToString("N")[..8];
            response = Md5Hex($"{ha1}:{nonce}:{nc}:{cnonce}:{qop}:{ha2}");
        }
        else
        {
            response = Md5Hex($"{ha1}:{nonce}:{ha2}");
        }

        var header = new StringBuilder()
            .Append($"username=\"{username}\", realm=\"{realm}\", nonce=\"{nonce}\", uri=\"{uri}\", response=\"{response}\"");

        if (!string.IsNullOrEmpty(qop))
        {
            header.Append($", qop={qop}, nc={nc}, cnonce=\"{cnonce}\"");
        }

        return new AuthenticationHeaderValue("Digest", header.ToString());
    }

    private static Dictionary<string, string> ParseChallenge(string? challengeParams)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(challengeParams))
        {
            return result;
        }

        foreach (Match match in Regex.Matches(challengeParams, "(\\w+)=\"?([^\",]+)\"?"))
        {
            result[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return result;
    }

    private static string Md5Hex(string input) =>
        Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(input))).ToLowerInvariant();
}
