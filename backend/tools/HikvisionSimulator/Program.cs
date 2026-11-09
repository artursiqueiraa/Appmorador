// Simulador HTTP local da ISAPI de controle de acesso Hikvision — Sprint 26 (ADR 0042). Só para
// validar AppMorador.Infrastructure.Hikvision.HikvisionAcessoProvider com requisições reais, sem
// aparelho físico. NUNCA rodar em produção. Não exige Digest (o cliente só manda credencial se o
// servidor pedir): aceita tudo e registra no console o que chegou.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.Urls.Add("http://localhost:9600");

app.MapPost("/ISAPI/AccessControl/UserInfo/Record", async (HttpRequest r) =>
{
    using var leitor = new StreamReader(r.Body);
    Console.WriteLine($"[HIKVISION] UserInfo/Record: {await leitor.ReadToEndAsync()}");
    return Results.Ok(new { statusCode = 1, statusString = "OK" });
});

app.MapPost("/ISAPI/Intelligent/FDLib/FaceDataRecord", async (HttpRequest r) =>
{
    var form = await r.ReadFormAsync();
    var imagem = form.Files.GetFile("img");
    Console.WriteLine($"[HIKVISION] FaceDataRecord: {form["FaceDataRecord"]} imagem={imagem?.Length ?? 0} bytes");
    return imagem is { Length: > 0 } ? Results.Ok(new { statusCode = 1, statusString = "OK" }) : Results.BadRequest();
});

app.MapPut("/ISAPI/Intelligent/FDLib/FDSearch/Delete", async (HttpRequest r) =>
{
    using var leitor = new StreamReader(r.Body);
    Console.WriteLine($"[HIKVISION] FDSearch/Delete: {await leitor.ReadToEndAsync()}");
    return Results.Ok(new { statusCode = 1 });
});

app.MapPut("/ISAPI/AccessControl/UserInfo/Delete", async (HttpRequest r) =>
{
    using var leitor = new StreamReader(r.Body);
    Console.WriteLine($"[HIKVISION] UserInfo/Delete: {await leitor.ReadToEndAsync()}");
    return Results.Ok(new { statusCode = 1 });
});

app.MapPut("/ISAPI/AccessControl/RemoteControl/door/{porta:int}", async (int porta, HttpRequest r) =>
{
    using var leitor = new StreamReader(r.Body);
    var corpo = await leitor.ReadToEndAsync();
    Console.WriteLine($"[HIKVISION] RemoteControl/door/{porta}: {corpo}");
    return corpo.Contains("<cmd>open</cmd>") ? Results.Ok() : Results.BadRequest();
});

app.Run();
