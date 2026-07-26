using System.Net.Sockets;
using System.Text;
using JflSimulator;

// Simulador TCP simplificado de uma central JFL Active 100 Bus — criado na Sprint 12
// (Migração da Integração JFL Active 100 Bus) só para validar a comunicação real do
// AppMorador.Jfl via TCP de verdade (localhost), já que nenhuma central física está
// disponível neste ambiente (decisão confirmada na Fase 1: simulador simplificado
// escrito do zero, não o Simulator completo do repositório legado). Mantém estado em
// memória (partições/PGMs/zonas inibidas) para que Armar/Desarmar/PGM/Inibir Zona
// realmente reflitam no próximo status consultado — NUNCA rodar em produção.
var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 ? int.Parse(args[1]) : 8085;
var numeroSerie = args.Length > 2 ? args[2] : "1234567890";

using var client = new TcpClient();
await client.ConnectAsync(host, port);
var stream = client.GetStream();
Console.WriteLine($"Conectado a {host}:{port} como central {numeroSerie}");

var central = new CentralSimulada();
byte proximoSeq = 1;

var handshakeDados = new List<byte>();
handshakeDados.AddRange(Encoding.ASCII.GetBytes(numeroSerie.PadRight(10)[..10]));
handshakeDados.AddRange(new byte[15]); // IMEI vazio
handshakeDados.AddRange(new byte[12]); // MAC vazio
handshakeDados.Add(0xA4); // MOD = Active 100 Bus
handshakeDados.AddRange(Encoding.ASCII.GetBytes("650")); // VER = 6.5
handshakeDados.Add(0x00); // IP
handshakeDados.Add(0x00); // SIMCARD
handshakeDados.Add(0x01); // VIA = Ethernet
handshakeDados.Add(0x00); // OPERADORA

await stream.WriteAsync(JflWire.MontarPacote(proximoSeq++, 0x21, handshakeDados.ToArray()));
Console.WriteLine("Handshake (0x21) enviado.");

var respostaHandshake = await JflWire.LerPacoteAsync(stream);
if (respostaHandshake is null)
{
    Console.WriteLine("Conexão encerrada antes da resposta do handshake.");
    return;
}

Console.WriteLine($"Resposta do handshake: RESULT=0x{respostaHandshake.Dados[0]:X2} KEEP={respostaHandshake.Dados[1]} min");

using var cts = new CancellationTokenSource();
_ = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), cts.Token);
        await stream.WriteAsync(JflWire.MontarPacote(proximoSeq++, 0x40, Array.Empty<byte>()), cts.Token);
        Console.WriteLine("Keep-alive (0x40) enviado.");
    }
}, cts.Token);

while (true)
{
    var pacote = await JflWire.LerPacoteAsync(stream);
    if (pacote is null)
    {
        Console.WriteLine("Conexão encerrada pelo servidor.");
        break;
    }

    switch (pacote.Cmd)
    {
        case 0x4D: // Consultar status
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine("Status consultado.");
            break;
        case 0x4E: // Armar
        {
            var p = pacote.Dados[0];
            central.Armar(p, stay: false);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"Armar partição {p}.");
            break;
        }
        case 0x4F: // Desarmar
        {
            var p = pacote.Dados[0];
            central.Desarmar(p);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"Desarmar partição {p}.");
            break;
        }
        case 0x53: // Armar Stay
        {
            var p = pacote.Dados[0];
            central.Armar(p, stay: true);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"Armar Stay partição {p}.");
            break;
        }
        case 0x54: // Armar Away
        {
            var p = pacote.Dados[0];
            central.Armar(p, stay: false);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"Armar Away partição {p}.");
            break;
        }
        case 0x50: // Acionar PGM
        {
            var n = pacote.Dados[0];
            central.AcionarPgm(n, true);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"PGM {n} acionada.");
            break;
        }
        case 0x51: // Desacionar PGM
        {
            var n = pacote.Dados[0];
            central.AcionarPgm(n, false);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"PGM {n} desacionada.");
            break;
        }
        case 0x52: // Inibir zonas (substitui o conjunto inteiro — MSB-first, ver ZoneInhibitCommandService)
        {
            central.SubstituirZonasInibidas(pacote.Dados);
            await ResponderStatusAsync(pacote.Seq);
            Console.WriteLine($"Zonas inibidas agora: {string.Join(",", central.ZonasInibidas)}");
            break;
        }
        default:
            Console.WriteLine($"Comando 0x{pacote.Cmd:X2} não tratado pelo simulador.");
            break;
    }
}

cts.Cancel();
return;

async Task ResponderStatusAsync(byte seq) =>
    await stream.WriteAsync(JflWire.MontarPacote(seq, 0x4D, central.MontarStatusResponse()));
