namespace AppMorador.Domain.Entities;

public enum TipoAcaoAuditoria
{
    Login = 1,
    Logout = 2,
    ImpersonationInicio = 3,
    ImpersonationFim = 4,
    Criar = 5,
    Editar = 6,
    Excluir = 7,
    Visualizar = 8,
    FalhaAutorizacao = 9,
}

/// <summary>
/// Sprint 21 (ADR 0021) — trilha de auditoria genérica ("quem fez o quê"), distinta
/// de <see cref="RegistroEventoAlarme"/> (que é estritamente do pipeline JFL/Contact
/// ID). Nasce só com ações internas/de segurança (login, impersonation, falha de
/// autorização) — nunca é confundida com o histórico de negócio de cada domínio
/// (ex.: HistoricoVisitante, HistoricoEntrega), que continuam existindo à parte.
/// </summary>
public class AuditoriaMaster
{
    public Guid Id { get; set; }

    /// <summary>Quem fez a ação — durante impersonation, é sempre o Master/Suporte, nunca o cliente impersonado (ver ADR 0021).</summary>
    public Guid UsuarioId { get; set; }

    public required string UsuarioNome { get; set; }

    public TipoAcaoAuditoria Acao { get; set; }

    /// <summary>Nome do tipo de entidade afetada (ex.: "Propriedade", "Usuario") — texto livre deliberado, nunca um enum fechado (a lista de entidades cresce a cada Sprint).</summary>
    public string? Entidade { get; set; }

    public string? EntidadeId { get; set; }

    /// <summary>JSON livre com detalhes/antes-depois — nunca dado sensível em texto puro (senha, token).</summary>
    public string? Detalhes { get; set; }

    public string? IpAddress { get; set; }

    public required DateTime DataHoraUtc { get; set; }
}
