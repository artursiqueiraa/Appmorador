using System.Net.Http.Json;
using AppMorador.Application.Intelbras;

namespace AppMorador.Infrastructure.Intelbras;

/// <summary>
/// Única implementação real de <see cref="IIntelbrasProvider"/> — todo o protocolo
/// Intelbras (login por sessão via senha remota, endpoints HTTP) fica isolado aqui,
/// nunca vaza para Application/Api (ver ADR 0014/0018). Modelado como API HTTP local
/// (mesmo padrão dial-out do Control iD) por decisão consciente da Fase 1 desta
/// Sprint: não há documentação oficial pública nem uma referência já investigada
/// neste projeto para um protocolo TCP proprietário Intelbras — nunca validado
/// contra hardware real, então cada chamada é defensiva (timeout curto, erros nunca
/// derrubam a Api).
/// </summary>
internal sealed class IntelbrasProvider : IIntelbrasProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public IntelbrasProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ResultadoTesteConexaoIntelbras> TestarConexaoAsync(ConexaoIntelbras conexao, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var sessao = await LoginAsync(client, conexao, cancellationToken).ConfigureAwait(false);
            return sessao is not null
                ? new ResultadoTesteConexaoIntelbras { Sucesso = true }
                : new ResultadoTesteConexaoIntelbras { Sucesso = false, MensagemErro = "Central respondeu, mas não retornou uma sessão válida." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ResultadoTesteConexaoIntelbras { Sucesso = false, MensagemErro = DescreverFalhaConexao(ex) };
        }
    }

    public async Task<ResultadoComandoIntelbras> ConsultarStatusAsync(ConexaoIntelbras conexao, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var sessao = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);
            var status = await ConsultarStatusWireAsync(conexao, sessao, cancellationToken).ConfigureAwait(false);
            return new ResultadoComandoIntelbras { Sucesso = true, StatusResultante = IntelbrasMapper.ParaStatusInfo(status) };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new ResultadoComandoIntelbras { Sucesso = false, MensagemErro = DescreverFalhaConexao(ex) };
        }
    }

    public async Task<ResultadoComandoIntelbras> ArmarAsync(ConexaoIntelbras conexao, int particao, CancellationToken cancellationToken) =>
        await ExecutarComandoAsync(conexao, "armar", particao, cancellationToken).ConfigureAwait(false);

    public async Task<ResultadoComandoIntelbras> DesarmarAsync(ConexaoIntelbras conexao, int particao, CancellationToken cancellationToken) =>
        await ExecutarComandoAsync(conexao, "desarmar", particao, cancellationToken).ConfigureAwait(false);

    public async Task<IReadOnlyList<EventoImportadoIntelbras>> ImportarEventosAsync(ConexaoIntelbras conexao, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var sessao = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

        var url = $"{MontarBaseUrl(conexao)}/eventos?sessao={sessao}";
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var corpo = await response.Content.ReadFromJsonAsync<IntelbrasEventosResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? new IntelbrasEventosResponse();

        return corpo.Eventos.Select(IntelbrasMapper.ParaEventoImportado).ToList();
    }

    private async Task<ResultadoComandoIntelbras> ExecutarComandoAsync(ConexaoIntelbras conexao, string acao, int particao, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var sessao = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

            var url = $"{MontarBaseUrl(conexao)}/{acao}?sessao={sessao}";
            using var response = await client.PostAsJsonAsync(url, new IntelbrasComandoRequest { Particao = particao }, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var status = await ConsultarStatusWireAsync(conexao, sessao, cancellationToken).ConfigureAwait(false);
            return new ResultadoComandoIntelbras { Sucesso = true, StatusResultante = IntelbrasMapper.ParaStatusInfo(status) };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return new ResultadoComandoIntelbras { Sucesso = false, MensagemErro = DescreverFalhaConexao(ex) };
        }
    }

    private async Task<IntelbrasStatusResponse> ConsultarStatusWireAsync(ConexaoIntelbras conexao, string sessao, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var url = $"{MontarBaseUrl(conexao)}/status?sessao={sessao}";
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<IntelbrasStatusResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? new IntelbrasStatusResponse();
    }

    private static async Task<string> ExigirSessaoAsync(HttpClient client, ConexaoIntelbras conexao, CancellationToken cancellationToken)
    {
        var sessao = await LoginAsync(client, conexao, cancellationToken).ConfigureAwait(false);
        if (sessao is null)
        {
            throw new InvalidOperationException("Central respondeu, mas não retornou uma sessão válida.");
        }

        return sessao;
    }

    private static async Task<string?> LoginAsync(HttpClient client, ConexaoIntelbras conexao, CancellationToken cancellationToken)
    {
        var request = new IntelbrasLoginRequest { Senha = conexao.Senha };
        var url = $"{MontarBaseUrl(conexao)}/login";
        using var response = await client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var corpo = await response.Content.ReadFromJsonAsync<IntelbrasLoginResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return corpo?.Sessao;
    }

    private static string MontarBaseUrl(ConexaoIntelbras conexao) => $"http://{conexao.Ip}:{conexao.Porta}";

    private static string DescreverFalhaConexao(Exception ex) => ex switch
    {
        TaskCanceledException => "Tempo esgotado ao tentar conectar à central.",
        _ => $"Não foi possível conectar à central: {ex.Message}",
    };

    internal const string HttpClientName = "Intelbras";
}
