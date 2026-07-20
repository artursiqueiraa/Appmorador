namespace AppMorador.Jfl.Messages;

/// <summary>
/// Payload (campo Dados) da resposta ao comando de evento: OK(1) + CONTADOR(4, ecoado).
/// Responder isso imediatamente ao receber o 0x24 e obrigatorio pelo protocolo — sem
/// isso o equipamento reenvia o mesmo evento ate 3 vezes e encerra a conexao.
/// </summary>
public static class EventoResponse
{
    private const byte Ok = 0x01;

    public static byte[] BuildAck(EventoRequest requisicao) =>
        [Ok, .. requisicao.ContadorParaEco()];
}
