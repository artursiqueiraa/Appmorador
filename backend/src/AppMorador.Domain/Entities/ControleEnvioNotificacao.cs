namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 24 (ADR 0040) — último envio de cada notificação, para o debounce sobreviver a um
/// reinício do servidor (fecha a dívida 41). Chave = "{tipo}:{id}".
/// </summary>
public class ControleEnvioNotificacao
{
    public required string Chave { get; set; }

    public DateTime UltimoEnvioEmUtc { get; set; }
}
