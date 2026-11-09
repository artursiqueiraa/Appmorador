namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 26 (ADR 0042, LGPD art. 11) — consentimento específico para tratar o rosto de uma pessoa
/// (dado sensível). Sem um consentimento ativo, o rosto não é recebido nem enviado a leitores.
/// Revogar apaga a foto do servidor e dos leitores (art. 8º §5º, arts. 15–16).
/// </summary>
public class ConsentimentoBiometrico
{
    public Guid Id { get; set; }

    public Guid MoradorId { get; set; }

    public Morador? Morador { get; set; }

    /// <summary>Quem registrou no app (o próprio titular ou o dono da propriedade).</summary>
    public Guid RegistradoPorUsuarioId { get; set; }

    /// <summary>Versão do termo aceito (ver <see cref="VersaoTermoAtual"/>).</summary>
    public required string VersaoTermo { get; set; }

    /// <summary>Para Dependente (menor): quem registrou declarou ser o responsável legal.</summary>
    public bool ResponsavelLegal { get; set; }

    public DateTime RegistradoEmUtc { get; set; }

    public DateTime? RevogadoEmUtc { get; set; }

    public const string VersaoTermoAtual = "2026-11-08";
}

public enum SituacaoFotoLeitor
{
    Enviada,
    Falhou,
    Removida,
}

/// <summary>
/// Sprint 26 (ADR 0042) — onde está a foto de cada pessoa: um registro por (pessoa, leitor). Falhas
/// ficam aqui para nova tentativa e para o app mostrar "Falhou, tentar de novo".
/// </summary>
public class SincronizacaoFoto
{
    public Guid Id { get; set; }

    public Guid MoradorId { get; set; }

    public Guid EquipamentoId { get; set; }

    public Equipamento? Equipamento { get; set; }

    public SituacaoFotoLeitor Situacao { get; set; }

    /// <summary>"Enviar" ou "Remover" — o que a próxima tentativa deve fazer quando Falhou.</summary>
    public required string Operacao { get; set; }

    public string? Mensagem { get; set; }

    public DateTime AtualizadoEmUtc { get; set; }
}
