namespace AppMorador.Jfl.Protocol;

/// <summary>
/// Bytes de comando (campo CMD) do protocolo 0x7B, conforme documentado para a
/// Active 100 Bus. Nomeados por secao do documento oficial.
/// Trimmed para a Fase 1 (confiabilidade do evento): so os comandos efetivamente
/// tratados (conexao, keep-alive, evento) estao aqui. Os demais (status, armar,
/// desarmar, PGM, inibir zonas etc.) pertencem a fases futuras e serao adicionados
/// quando essa funcionalidade for de fato implementada.
/// </summary>
public enum JflCommand : byte
{
    /// <summary>3.1 - Comando de conexao (mandatorio). Enviado pelo equipamento ao abrir o socket.</summary>
    Conexao = 0x21,

    /// <summary>3.1 - Comando de conexao para os modulos M-300+/M-300 Flex (mesma estrutura do 0x21).</summary>
    ConexaoModulo = 0x2A,

    /// <summary>3.3 - Comando de keep alive (mandatorio).</summary>
    KeepAlive = 0x40,

    /// <summary>3.4 - Comando de evento (mandatorio). Enviado pelo equipamento a qualquer momento.</summary>
    Evento = 0x24,
}
