namespace AppMorador.Domain.Entities;

/// <summary>Sprint 27 (ADR 0043) — comandos que mudam o estado de um equipamento.</summary>
public enum TipoComando
{
    Armar,
    ArmarStay,
    ArmarAway,
    Desarmar,
    AcionarPgm,
    DesligarPgm,
    InibirZona,
    DesinibirZona,
    AbrirAcesso,
}

/// <summary>Como quem pediu o comando confirmou que era ele.</summary>
public enum FormaConfirmacao
{
    /// <summary>Comando que não pede confirmação, ou usuário que ainda não criou o PIN.</summary>
    Nenhuma,
    Pin,
    /// <summary>Digital/rosto do celular liberou o PIN guardado no aparelho (o servidor validou o PIN).</summary>
    Biometria,
}

/// <summary>
/// Sprint 27 (ADR 0043) — auditoria única de comandos: quem armou, desarmou, acionou ou abriu, como
/// confirmou e o que o equipamento respondeu. Base dos relatórios da Sprint 28. Registro de
/// segurança: sem soft delete, nunca apagado.
/// </summary>
public class ComandoExecutado
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Guid UsuarioId { get; set; }

    public Guid? EquipamentoId { get; set; }

    public Guid? PontoAcessoId { get; set; }

    public TipoComando Tipo { get; set; }

    /// <summary>Parâmetro em texto curto ("partição 1", "PGM 2", "zona 5").</summary>
    public string? Parametros { get; set; }

    public FormaConfirmacao Confirmacao { get; set; }

    public bool Sucesso { get; set; }

    /// <summary>Motivo da falha em linguagem simples (null quando deu certo).</summary>
    public string? Mensagem { get; set; }

    /// <summary>Chave enviada pelo app para não executar duas vezes o mesmo toque (única por usuário).</summary>
    public string? ChaveIdempotencia { get; set; }

    /// <summary>Resposta devolvida na primeira execução, repetida quando a mesma chave chega de novo.</summary>
    public string? RespostaJson { get; set; }

    /// <summary>False enquanto o comando ainda está sendo executado (chave reservada).</summary>
    public bool Concluido { get; set; }

    public DateTime OcorridoEmUtc { get; set; }
}
