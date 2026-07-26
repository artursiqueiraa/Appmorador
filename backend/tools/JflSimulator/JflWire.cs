using System.Net.Sockets;

namespace JflSimulator;

internal sealed record JflPacote(byte Seq, byte Cmd, byte[] Dados);

/// <summary>Framing 0x7B mínimo — replica só o necessário do protocolo (ver AppMorador.Jfl/Protocol) para este simulador.</summary>
internal static class JflWire
{
    public static byte[] MontarPacote(byte seq, byte cmd, byte[] dados)
    {
        var tamanho = 5 + dados.Length;
        var buffer = new byte[tamanho];
        buffer[0] = 0x7B;
        buffer[1] = (byte)tamanho;
        buffer[2] = seq;
        buffer[3] = cmd;
        Array.Copy(dados, 0, buffer, 4, dados.Length);

        byte checksum = 0;
        for (var i = 0; i < tamanho - 1; i++)
        {
            checksum ^= buffer[i];
        }

        buffer[tamanho - 1] = checksum;
        return buffer;
    }

    public static async Task<JflPacote?> LerPacoteAsync(NetworkStream stream)
    {
        var cabecalho = new byte[2];
        if (!await PreencherAsync(stream, cabecalho, 0, 2).ConfigureAwait(false))
        {
            return null;
        }

        if (cabecalho[0] != 0x7B)
        {
            throw new InvalidOperationException($"Cabeçalho inválido: 0x{cabecalho[0]:X2} (esperado 0x7B).");
        }

        var tamanho = cabecalho[1];
        var resto = new byte[tamanho - 2];
        if (!await PreencherAsync(stream, resto, 0, resto.Length).ConfigureAwait(false))
        {
            return null;
        }

        var seq = resto[0];
        var cmd = resto[1];
        var dados = resto[2..^1];
        return new JflPacote(seq, cmd, dados);
    }

    private static async Task<bool> PreencherAsync(NetworkStream stream, byte[] buffer, int offset, int quantidade)
    {
        var total = 0;
        while (total < quantidade)
        {
            var lido = await stream.ReadAsync(buffer.AsMemory(offset + total, quantidade - total)).ConfigureAwait(false);
            if (lido == 0)
            {
                return false;
            }

            total += lido;
        }

        return true;
    }
}
