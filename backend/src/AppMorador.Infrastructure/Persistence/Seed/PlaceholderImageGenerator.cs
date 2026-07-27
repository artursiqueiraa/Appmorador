using System.IO.Compression;
using System.Text;

namespace AppMorador.Infrastructure.Persistence.Seed;

/// <summary>
/// Sprint 20 — gera um PNG mínimo (cor sólida) inteiramente em memória, sem depender
/// de biblioteca de imagem (System.Drawing.Common não é portável fora do Windows) —
/// usado só pelo seed de desenvolvimento, para a câmera de exemplo ter uma "última
/// imagem" real e decodificável antes de qualquer gravador de verdade existir. Nunca
/// usado no caminho de captura real (esse sempre grava os bytes que vieram do
/// provider). CRC32/Adler32 são os checksums padrão exigidos pelo formato PNG —
/// implementação direta do algoritmo de referência, não uma aproximação.
/// </summary>
internal static class PlaceholderImageGenerator
{
    private static readonly byte[] Assinatura = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] TabelaCrc32 = MontarTabelaCrc32();

    public static byte[] GerarPngSolido(int largura, int altura, byte r, byte g, byte b)
    {
        using var destino = new MemoryStream();
        destino.Write(Assinatura);

        EscreverChunk(destino, "IHDR", MontarIhdr(largura, altura));
        EscreverChunk(destino, "IDAT", MontarIdat(largura, altura, r, g, b));
        EscreverChunk(destino, "IEND", []);

        return destino.ToArray();
    }

    private static byte[] MontarIhdr(int largura, int altura)
    {
        var dados = new byte[13];
        EscreverUInt32BigEndian(dados, 0, (uint)largura);
        EscreverUInt32BigEndian(dados, 4, (uint)altura);
        dados[8] = 8; // bit depth
        dados[9] = 2; // color type: truecolor (RGB)
        dados[10] = 0; // compression method
        dados[11] = 0; // filter method
        dados[12] = 0; // interlace method
        return dados;
    }

    private static byte[] MontarIdat(int largura, int altura, byte r, byte g, byte b)
    {
        var linhas = new byte[altura * (1 + (largura * 3))];
        var pos = 0;
        for (var y = 0; y < altura; y++)
        {
            linhas[pos++] = 0; // filtro "None"
            for (var x = 0; x < largura; x++)
            {
                linhas[pos++] = r;
                linhas[pos++] = g;
                linhas[pos++] = b;
            }
        }

        using var comprimido = new MemoryStream();
        comprimido.WriteByte(0x78);
        comprimido.WriteByte(0x9C); // cabecalho zlib (deflate, nivel default)

        using (var deflate = new DeflateStream(comprimido, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(linhas, 0, linhas.Length);
        }

        var adlerBytes = new byte[4];
        EscreverUInt32BigEndian(adlerBytes, 0, CalcularAdler32(linhas));
        comprimido.Write(adlerBytes, 0, 4);

        return comprimido.ToArray();
    }

    private static void EscreverChunk(Stream destino, string tipo, byte[] dados)
    {
        var tamanho = new byte[4];
        EscreverUInt32BigEndian(tamanho, 0, (uint)dados.Length);
        destino.Write(tamanho, 0, 4);

        var tipoBytes = Encoding.ASCII.GetBytes(tipo);
        destino.Write(tipoBytes, 0, 4);
        destino.Write(dados, 0, dados.Length);

        var bufferCrc = new byte[tipoBytes.Length + dados.Length];
        Buffer.BlockCopy(tipoBytes, 0, bufferCrc, 0, tipoBytes.Length);
        Buffer.BlockCopy(dados, 0, bufferCrc, tipoBytes.Length, dados.Length);

        var crcBytes = new byte[4];
        EscreverUInt32BigEndian(crcBytes, 0, CalcularCrc32(bufferCrc));
        destino.Write(crcBytes, 0, 4);
    }

    private static void EscreverUInt32BigEndian(byte[] destino, int offset, uint valor)
    {
        destino[offset] = (byte)(valor >> 24);
        destino[offset + 1] = (byte)(valor >> 16);
        destino[offset + 2] = (byte)(valor >> 8);
        destino[offset + 3] = (byte)valor;
    }

    private static uint CalcularAdler32(byte[] dados)
    {
        const uint modulo = 65521;
        uint a = 1;
        uint b = 0;
        foreach (var t in dados)
        {
            a = (a + t) % modulo;
            b = (b + a) % modulo;
        }

        return (b << 16) | a;
    }

    private static uint[] MontarTabelaCrc32()
    {
        var tabela = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            tabela[n] = c;
        }

        return tabela;
    }

    private static uint CalcularCrc32(byte[] dados)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var t in dados)
        {
            crc = TabelaCrc32[(crc ^ t) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }
}
