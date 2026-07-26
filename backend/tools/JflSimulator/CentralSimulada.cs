namespace JflSimulator;

/// <summary>Estado em memória da central fake — reflete Armar/Desarmar/PGM/Inibir Zona no próximo status consultado.</summary>
internal sealed class CentralSimulada
{
    private readonly bool[] _particaoArmada = new bool[17]; // indice 1-16
    private readonly bool[] _particaoStay = new bool[17];
    private readonly bool[] _pgmAcionada = new bool[17]; // indice 1-16
    private readonly HashSet<int> _zonasInibidas = new();

    public IReadOnlyCollection<int> ZonasInibidas => _zonasInibidas;

    public void Armar(int particao, bool stay)
    {
        if (particao is < 1 or > 16)
        {
            return;
        }

        _particaoArmada[particao] = true;
        _particaoStay[particao] = stay;
    }

    public void Desarmar(int particao)
    {
        if (particao is < 1 or > 16)
        {
            return;
        }

        _particaoArmada[particao] = false;
        _particaoStay[particao] = false;
    }

    public void AcionarPgm(int numero, bool acionada)
    {
        if (numero is < 1 or > 16)
        {
            return;
        }

        _pgmAcionada[numero] = acionada;
    }

    /// <summary>Bitmap MSB-first de 13 bytes (99 zonas) — mesma convenção OPOSTA ao P-INIB usada por ZoneInhibitCommandService.EmpacotarBitmap.</summary>
    public void SubstituirZonasInibidas(byte[] bitmap)
    {
        _zonasInibidas.Clear();
        for (var zona = 1; zona <= 99; zona++)
        {
            var byteIndex = (zona - 1) / 8;
            var bit = 7 - ((zona - 1) % 8);
            if (byteIndex < bitmap.Length && (bitmap[byteIndex] & (1 << bit)) != 0)
            {
                _zonasInibidas.Add(zona);
            }
        }
    }

    /// <summary>Monta os 113 bytes da resposta "tela monitorar" (seção 4.10) refletindo o estado atual — ver CentralStatusResponse.Parse no lado do AppMorador.</summary>
    public byte[] MontarStatusResponse()
    {
        var dados = new byte[113];
        var offset = 2; // KP — nao usado.

        var agora = DateTime.UtcNow;
        dados[offset++] = ParaBcd(agora.Day);
        dados[offset++] = ParaBcd(agora.Month);
        dados[offset++] = ParaBcd(agora.Year % 100);
        dados[offset++] = ParaBcd(agora.Hour);
        dados[offset++] = ParaBcd(agora.Minute);
        dados[offset++] = ParaBcd(agora.Second);

        dados[offset++] = 0x50; // BAT: litio, 80%

        byte pgmByte = 0;
        for (var i = 0; i < 8; i++)
        {
            if (_pgmAcionada[i + 1])
            {
                pgmByte |= (byte)(1 << i);
            }
        }

        dados[offset++] = pgmByte;

        for (var p = 1; p <= 16; p++)
        {
            dados[offset++] = _particaoArmada[p] ? (_particaoStay[p] ? (byte)0x03 : (byte)0x02) : (byte)0x01;
        }

        dados[offset++] = 0x01; // ELET: desarmado (eletrificador nao exercitado por este simulador)

        for (var i = 0; i < 50; i++)
        {
            var zonaAlta = (i * 2) + 1;
            var zonaBaixa = (i * 2) + 2;
            byte nibbleAlto = _zonasInibidas.Contains(zonaAlta) ? (byte)1 : (byte)8;
            byte nibbleBaixo = zonaBaixa <= 99 && _zonasInibidas.Contains(zonaBaixa) ? (byte)1 : (byte)8;
            dados[offset++] = (byte)((nibbleAlto << 4) | nibbleBaixo);
        }

        for (var i = 0; i < 5; i++)
        {
            dados[offset++] = 0x00; // PROB: sem problemas
        }

        dados[offset++] = 0x09; // P-ELET: permite desarmar + armar away

        dados[offset++] = 0xFF; // P-PGM: todas as 8 PGMs permitidas

        for (var p = 0; p < 16; p++)
        {
            dados[offset++] = 0x1F; // P-PART: permite desarmar/armar/stay/away, pronta
        }

        for (var i = 0; i < 13; i++)
        {
            dados[offset++] = 0xFF; // P-INIB: permite inibir qualquer zona
        }

        return dados;
    }

    private static byte ParaBcd(int valor) => (byte)(((valor / 10) << 4) | (valor % 10));
}
