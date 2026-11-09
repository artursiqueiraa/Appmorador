namespace AppMorador.Domain.Entities;

/// <summary>Sprint 24 (ADR 0040) — o que gerou o alerta crítico.</summary>
public enum TipoAlertaCritico
{
    Disparo,
    Panico,
    Violacao,
    SemComunicacao,
}

/// <summary>
/// Sprint 24 (ADR 0040) — até onde o alerta já subiu. A ordem importa: o escalonamento só anda
/// para frente (Dono → Membros → Contatos de emergência).
/// </summary>
public enum NivelEscalonamentoAlerta
{
    Dono = 0,
    Membros = 1,
    ContatosEmergencia = 2,
}

/// <summary>
/// Sprint 24 (ADR 0040) — um evento que ninguém pode deixar passar (disparo, pânico, violação,
/// central sem comunicação). Fica aberto até alguém tocar em "Estou ciente"; enquanto isso, o
/// serviço de escalonamento avisa mais pessoas a cada <see cref="Propriedade.MinutosEscalonamento"/>.
/// Não tem soft delete: é registro de segurança, nunca é apagado.
/// </summary>
public class AlertaCritico
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public TipoAlertaCritico Tipo { get; set; }

    /// <summary>Texto curto para o dono ("Movimento detectado — Sala"). Nunca código técnico.</summary>
    public required string Descricao { get; set; }

    /// <summary>Ocorrência da central que originou o alerta (disparo/violação/pânico da central).</summary>
    public Guid? OcorrenciaId { get; set; }

    /// <summary>Equipamento sem comunicação (alerta <see cref="TipoAlertaCritico.SemComunicacao"/>).</summary>
    public Guid? EquipamentoId { get; set; }

    /// <summary>Quem apertou o botão de pânico no app (null quando veio da central).</summary>
    public Guid? AcionadoPorUsuarioId { get; set; }

    public DateTime CriadoEmUtc { get; set; }

    public DateTime? ConfirmadoEmUtc { get; set; }

    public Guid? ConfirmadoPorUsuarioId { get; set; }

    public Usuario? ConfirmadoPorUsuario { get; set; }

    public NivelEscalonamentoAlerta NivelEscalonamento { get; set; } = NivelEscalonamentoAlerta.Dono;

    /// <summary>Quando o próximo nível deve ser avisado; null = escalonamento encerrado (confirmado ou último nível).</summary>
    public DateTime? ProximoEscalonamentoEmUtc { get; set; }

    public DateTime? EscalonadoMembrosEmUtc { get; set; }

    public DateTime? EscalonadoContatosEmUtc { get; set; }

    public bool Confirmado => ConfirmadoEmUtc is not null;
}
