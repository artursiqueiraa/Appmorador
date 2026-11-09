using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AppMorador.Application.ControlId;
using AppMorador.Application.Hikvision;

namespace AppMorador.Infrastructure.Hikvision;

/// <summary>
/// Sprint 26 (ADR 0042) — terminal facial Hikvision via ISAPI, com autenticação Digest (padrão do
/// fabricante). Endpoints públicos da ISAPI de controle de acesso:
/// - pessoa: <c>/ISAPI/AccessControl/UserInfo/Record</c> (cria) e <c>/UserInfo/Delete</c>;
/// - rosto: <c>/ISAPI/Intelligent/FDLib/FaceDataRecord</c> (multipart: JSON + imagem) e
///   <c>/FDLib/FDSearch/Delete</c>;
/// - porta: <c>/ISAPI/AccessControl/RemoteControl/door/{n}</c> com &lt;cmd&gt;open&lt;/cmd&gt;.
/// Validado no simulador local (<c>backend/tools/HikvisionSimulator</c>); pendente de aparelho físico.
/// </summary>
internal sealed class HikvisionAcessoProvider : IHikvisionAcessoProvider
{
    private static readonly TimeSpan Tempo = TimeSpan.FromSeconds(10);

    public async Task<ResultadoTesteConexao> EnviarRostoAsync(
        ConexaoEquipamento conexao, Guid moradorId, string nome, byte[] foto, CancellationToken cancellationToken)
    {
        return await ExecutarAsync(conexao, async client =>
        {
            var numero = Numero(moradorId);
            var pessoa = new
            {
                UserInfo = new
                {
                    employeeNo = numero,
                    name = nome.Length > 32 ? nome[..32] : nome,
                    userType = "normal",
                    Valid = new { enable = true, beginTime = "2020-01-01T00:00:00", endTime = "2037-12-31T23:59:59" },
                },
            };
            // Pessoa já existente responde erro de duplicidade: não impede o envio do rosto.
            using (var resposta = await client.PostAsync("/ISAPI/AccessControl/UserInfo/Record?format=json", Json(pessoa), cancellationToken).ConfigureAwait(false))
            {
                if (resposta.StatusCode is HttpStatusCode.Unauthorized)
                {
                    return Falha("Usuário ou senha do terminal recusados.");
                }
            }

            using var multipart = new MultipartFormDataContent();
            var registro = JsonSerializer.Serialize(new { faceLibType = "blackFD", FDID = "1", FPID = numero });
            multipart.Add(new StringContent(registro, Encoding.UTF8, "application/json"), "FaceDataRecord");
            var imagem = new ByteArrayContent(foto);
            imagem.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            multipart.Add(imagem, "img", "rosto.jpg");

            using var envio = await client.PostAsync("/ISAPI/Intelligent/FDLib/FaceDataRecord?format=json", multipart, cancellationToken).ConfigureAwait(false);
            return envio.IsSuccessStatusCode ? Ok() : Falha("O terminal recusou a foto do rosto.");
        }).ConfigureAwait(false);
    }

    public async Task<ResultadoTesteConexao> RemoverPessoaAsync(ConexaoEquipamento conexao, Guid moradorId, CancellationToken cancellationToken)
    {
        return await ExecutarAsync(conexao, async client =>
        {
            var numero = Numero(moradorId);
            using var rosto = await client.PutAsync(
                "/ISAPI/Intelligent/FDLib/FDSearch/Delete?format=json&FDID=1&faceLibType=blackFD",
                Json(new { FPID = new[] { new { value = numero } } }), cancellationToken).ConfigureAwait(false);
            using var pessoa = await client.PutAsync(
                "/ISAPI/AccessControl/UserInfo/Delete?format=json",
                Json(new { UserInfoDelCond = new { EmployeeNoList = new[] { new { employeeNo = numero } } } }), cancellationToken).ConfigureAwait(false);
            return rosto.IsSuccessStatusCode || pessoa.IsSuccessStatusCode ? Ok() : Falha("O terminal não confirmou a remoção.");
        }).ConfigureAwait(false);
    }

    public async Task<ResultadoTesteConexao> AbrirPortaAsync(ConexaoEquipamento conexao, int porta, CancellationToken cancellationToken)
    {
        return await ExecutarAsync(conexao, async client =>
        {
            var corpo = new StringContent("<RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>", Encoding.UTF8, "application/xml");
            using var resposta = await client.PutAsync($"/ISAPI/AccessControl/RemoteControl/door/{porta}", corpo, cancellationToken).ConfigureAwait(false);
            return resposta.IsSuccessStatusCode ? Ok() : Falha("O terminal recusou o comando de abrir a porta.");
        }).ConfigureAwait(false);
    }

    /// <summary>employeeNo do terminal: id do morador sem hífens (32 caracteres, limite da ISAPI).</summary>
    internal static string Numero(Guid moradorId) => moradorId.ToString("N");

    private static async Task<ResultadoTesteConexao> ExecutarAsync(ConexaoEquipamento conexao, Func<HttpClient, Task<ResultadoTesteConexao>> acao)
    {
        var baseUri = new Uri($"http://{conexao.Ip}:{conexao.Porta}");
        using var handler = new HttpClientHandler
        {
            Credentials = new CredentialCache { { baseUri, "Digest", new NetworkCredential(conexao.Usuario, conexao.Senha) } },
            PreAuthenticate = false,
        };
        using var client = new HttpClient(handler) { BaseAddress = baseUri, Timeout = Tempo };
        try
        {
            return await acao(client).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Falha(ex is TaskCanceledException ? "Tempo esgotado ao falar com o terminal." : "Não foi possível conectar ao terminal.");
        }
    }

    private static StringContent Json(object corpo) => new(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

    private static ResultadoTesteConexao Ok() => new() { Sucesso = true };

    private static ResultadoTesteConexao Falha(string mensagem) => new() { Sucesso = false, MensagemErro = mensagem };
}
