namespace AppMorador.Infrastructure.Intelbras;

/// <summary>
/// DTOs de wire-format da API HTTP simulada da central Intelbras (ver ADR 0018 —
/// decisão de modelar via HTTP em vez de um protocolo TCP proprietário sem fonte
/// verificável). Internos a este namespace, nunca vazam para Application (mesmo
/// princípio de ControlIdWireDtos, ADR 0014).
/// </summary>
internal sealed class IntelbrasLoginRequest
{
    public string? Senha { get; set; }
}

internal sealed class IntelbrasLoginResponse
{
    public string? Sessao { get; set; }
}

internal sealed class IntelbrasParticaoWire
{
    public int Numero { get; set; }

    public bool Armada { get; set; }
}

internal sealed class IntelbrasStatusResponse
{
    public List<IntelbrasParticaoWire> Particoes { get; set; } = new();

    public bool TemProblemaAtivo { get; set; }
}

internal sealed class IntelbrasComandoRequest
{
    public int Particao { get; set; }
}

internal sealed class IntelbrasEventoWire
{
    public string Codigo { get; set; } = string.Empty;

    public string Descricao { get; set; } = string.Empty;

    public long OcorridoEmUnix { get; set; }
}

internal sealed class IntelbrasEventosResponse
{
    public List<IntelbrasEventoWire> Eventos { get; set; } = new();
}
