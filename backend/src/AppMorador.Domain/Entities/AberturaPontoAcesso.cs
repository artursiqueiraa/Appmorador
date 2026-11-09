namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 24.5 — cada vez que alguém tocou em "Abrir" num ponto de acesso pelo app: quem, quando e
/// se o equipamento obedeceu. Registro de segurança: sem soft delete, nunca apagado.
/// </summary>
public class AberturaPontoAcesso
{
    public Guid Id { get; set; }

    public Guid PontoAcessoId { get; set; }

    public PontoAcesso? PontoAcesso { get; set; }

    public Guid UsuarioId { get; set; }

    public bool Sucesso { get; set; }

    /// <summary>Motivo da falha em linguagem simples (null quando abriu).</summary>
    public string? Mensagem { get; set; }

    public DateTime OcorridoEmUtc { get; set; }
}
