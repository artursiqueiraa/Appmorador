using AppMorador.Jfl.Protocol;

namespace AppMorador.Jfl.Messages;

/// <summary>
/// Payload (campo Dados) do comando de evento 0x24, secao 3.4 do protocolo:
/// CONTA(4,ASCII) + EVENTO(4,ASCII, codigo Contact ID) + PART(2,ASCII) + U/Z(3,ASCII,
/// usuario OU zona conforme o codigo) + CONTADOR(4,HEX big-endian) + SPART(1) + PROB(1)
/// = 19 bytes.
///
/// Esta classe nao existe em nenhuma das fontes de referencia (nem
/// Integra-o-FL-main, onde so ha um handler stub para 0x24) — e a implementacao real,
/// escrita a partir da especificacao documentada em Documentation/Protocol/09_EVENTS.md
/// daquele repositorio.
///
/// Nao ha byte de qualificador separado no protocolo 0x7B: o codigo Contact ID de 4
/// digitos ja e o valor completo (ex.: "1130"). A classificacao de qual codigo
/// representa um disparo real fica para uma fase futura (ver Fase 1 - nenhuma
/// classificacao/filtro e aplicada aqui; toda Evento recebido vira uma Ocorrencia).
/// </summary>
public sealed class EventoRequest
{
    private const int TamanhoConta = 4;
    private const int TamanhoCodigoEvento = 4;
    private const int TamanhoParticao = 2;
    private const int TamanhoUsuarioOuZona = 3;
    private const int TamanhoContador = 4;
    private const int TamanhoSpart = 1;
    private const int TamanhoProb = 1;

    private const int TamanhoTotal =
        TamanhoConta + TamanhoCodigoEvento + TamanhoParticao + TamanhoUsuarioOuZona +
        TamanhoContador + TamanhoSpart + TamanhoProb;

    /// <summary>Conta da particao (4 digitos ASCII).</summary>
    public required string Conta { get; init; }

    /// <summary>Codigo Contact ID completo, 4 digitos ASCII (ex.: "1130", "1401").</summary>
    public required string CodigoEvento { get; init; }

    /// <summary>Particao, "00" a "99".</summary>
    public required string Particao { get; init; }

    /// <summary>
    /// Usuario OU zona (3 digitos ASCII) — o protocolo base 0x7B nao distingue os dois;
    /// a distincao depende da familia do codigo Contact ID (fora de escopo da Fase 1).
    /// </summary>
    public required string UsuarioOuZona { get; init; }

    /// <summary>Identificador sequencial do evento (nao e timestamp) — usado para ecoar na resposta.</summary>
    public required uint Contador { get; init; }

    public required byte Spart { get; init; }

    public required byte Prob { get; init; }

    public static EventoRequest Parse(ReadOnlySpan<byte> dados)
    {
        if (dados.Length != TamanhoTotal)
        {
            throw new JflProtocolException(
                $"Comando de evento com tamanho inesperado: recebido {dados.Length} bytes, esperado exatamente {TamanhoTotal}.");
        }

        var offset = 0;

        var conta = System.Text.Encoding.ASCII.GetString(dados.Slice(offset, TamanhoConta));
        offset += TamanhoConta;

        var codigoEvento = System.Text.Encoding.ASCII.GetString(dados.Slice(offset, TamanhoCodigoEvento));
        offset += TamanhoCodigoEvento;

        var particao = System.Text.Encoding.ASCII.GetString(dados.Slice(offset, TamanhoParticao));
        offset += TamanhoParticao;

        var usuarioOuZona = System.Text.Encoding.ASCII.GetString(dados.Slice(offset, TamanhoUsuarioOuZona));
        offset += TamanhoUsuarioOuZona;

        var contadorBytes = dados.Slice(offset, TamanhoContador);
        var contador = ((uint)contadorBytes[0] << 24) | ((uint)contadorBytes[1] << 16) |
                       ((uint)contadorBytes[2] << 8) | contadorBytes[3];
        offset += TamanhoContador;

        var spart = dados[offset];
        offset += TamanhoSpart;

        var prob = dados[offset];
        offset += TamanhoProb;

        return new EventoRequest
        {
            Conta = conta,
            CodigoEvento = codigoEvento,
            Particao = particao,
            UsuarioOuZona = usuarioOuZona,
            Contador = contador,
            Spart = spart,
            Prob = prob,
        };
    }

    /// <summary>Os 4 bytes do Contador, na mesma ordem recebida — usados para ecoar na resposta (OK + Contador).</summary>
    public byte[] ContadorParaEco() =>
    [
        (byte)(Contador >> 24),
        (byte)(Contador >> 16),
        (byte)(Contador >> 8),
        (byte)Contador,
    ];
}
