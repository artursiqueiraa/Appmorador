using System.Net.Http.Json;
using AppMorador.Application.ControlId;

namespace AppMorador.Infrastructure.ControlId;

/// <summary>
/// Única implementação real de <see cref="IControlIdProvider"/> — todo o protocolo
/// Control iD (login por sessão, endpoints .fcgi) fica isolado aqui, nunca vaza para
/// Application/Api (ver ADR 0014). Sessão é obtida uma vez por operação (mesmo padrão
/// de sincronização em lote do legado) e repassada como query string, replicando o
/// protocolo de referência investigado na Fase 1 — nunca validado contra hardware
/// real, então cada chamada é defensiva (timeout curto, erros nunca derrubam a Api).
/// </summary>
internal sealed class ControlIdProvider : IControlIdProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ControlIdProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ResultadoTesteConexao> TestarConexaoAsync(ConexaoEquipamento conexao, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var session = await LoginAsync(client, conexao, cancellationToken).ConfigureAwait(false);
            return session is not null
                ? new ResultadoTesteConexao { Sucesso = true }
                : new ResultadoTesteConexao { Sucesso = false, MensagemErro = "Equipamento respondeu, mas não retornou uma sessão válida." };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new ResultadoTesteConexao { Sucesso = false, MensagemErro = DescreverFalhaConexao(ex) };
        }
    }

    public async Task<InformacoesEquipamento> ConsultarInformacoesAsync(ConexaoEquipamento conexao, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var session = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

        var url = $"{MontarBaseUrl(conexao)}/system_information.fcgi?session={session}";
        using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var corpo = await response.Content.ReadFromJsonAsync<ControlIdInformationResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? new ControlIdInformationResponse();

        return ControlIdMapper.ParaInformacoesEquipamento(corpo);
    }

    public async Task<ResultadoSincronizacao> SincronizarMoradoresAsync(
        ConexaoEquipamento conexao, IReadOnlyList<MoradorParaSincronizar> moradores, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var session = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

        var processados = 0;
        foreach (var morador in moradores)
        {
            var payload = ControlIdMapper.ParaObjetoUsuario(morador);
            if (await EnviarCreateObjectAsync(client, conexao, session, payload, cancellationToken).ConfigureAwait(false))
            {
                processados++;
            }
        }

        return new ResultadoSincronizacao { Sucesso = true, QuantidadeProcessada = processados };
    }

    public async Task<ResultadoSincronizacao> SincronizarCredenciaisAsync(
        ConexaoEquipamento conexao, IReadOnlyList<CredencialParaSincronizar> credenciais, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var session = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

        var processados = 0;
        foreach (var credencial in credenciais)
        {
            var payload = ControlIdMapper.ParaObjetoCredencial(credencial);
            if (await EnviarCreateObjectAsync(client, conexao, session, payload, cancellationToken).ConfigureAwait(false))
            {
                processados++;
            }
        }

        return new ResultadoSincronizacao { Sucesso = true, QuantidadeProcessada = processados };
    }

    public async Task<ResultadoSincronizacao> SincronizarPermissoesAsync(
        ConexaoEquipamento conexao, IReadOnlyList<PermissaoParaSincronizar> permissoes, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var session = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

        var processados = 0;
        foreach (var permissao in permissoes)
        {
            var payload = ControlIdMapper.ParaObjetoPermissao(permissao);
            if (await EnviarCreateObjectAsync(client, conexao, session, payload, cancellationToken).ConfigureAwait(false))
            {
                processados++;
            }
        }

        return new ResultadoSincronizacao { Sucesso = true, QuantidadeProcessada = processados };
    }

    public async Task<IReadOnlyList<EventoImportado>> ImportarEventosAsync(ConexaoEquipamento conexao, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        var session = await ExigirSessaoAsync(client, conexao, cancellationToken).ConfigureAwait(false);

        var requisicao = new ControlIdLoadObjectsRequest
        {
            Object = "access_logs",
            Fields = new[] { "id", "event", "time", "user_id" },
            Limit = 100,
        };

        var url = $"{MontarBaseUrl(conexao)}/load_objects.fcgi?session={session}";
        using var response = await client.PostAsJsonAsync(url, requisicao, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var corpo = await response.Content.ReadFromJsonAsync<ControlIdLoadAccessLogsResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        var registros = corpo?.AccessLogs ?? new List<ControlIdAccessLogEntry>();

        return registros.Select(ControlIdMapper.ParaEventoImportado).ToList();
    }

    private static async Task<bool> EnviarCreateObjectAsync(
        HttpClient client, ConexaoEquipamento conexao, string session, ControlIdCreateObjectsRequest payload, CancellationToken cancellationToken)
    {
        var url = $"{MontarBaseUrl(conexao)}/create_objects.fcgi?session={session}";
        using var response = await client.PostAsJsonAsync(url, payload, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }

    private static async Task<string> ExigirSessaoAsync(HttpClient client, ConexaoEquipamento conexao, CancellationToken cancellationToken)
    {
        var session = await LoginAsync(client, conexao, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new InvalidOperationException("Equipamento respondeu, mas não retornou uma sessão válida.");
        }

        return session;
    }

    private static async Task<string?> LoginAsync(HttpClient client, ConexaoEquipamento conexao, CancellationToken cancellationToken)
    {
        var request = new ControlIdLoginRequest { Login = conexao.Usuario, Password = conexao.Senha };
        var url = $"{MontarBaseUrl(conexao)}/login.fcgi";
        using var response = await client.PostAsJsonAsync(url, request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var corpo = await response.Content.ReadFromJsonAsync<ControlIdLoginResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return corpo?.Session;
    }

    private static string MontarBaseUrl(ConexaoEquipamento conexao) => $"http://{conexao.Ip}:{conexao.Porta}";

    private static string DescreverFalhaConexao(Exception ex) => ex switch
    {
        TaskCanceledException => "Tempo esgotado ao tentar conectar ao equipamento.",
        _ => $"Não foi possível conectar ao equipamento: {ex.Message}",
    };

    internal const string HttpClientName = "ControlId";
}
