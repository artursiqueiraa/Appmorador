namespace AppMorador.Domain.Entities;

/// <summary>
/// Registro técnico de auditoria/diagnóstico (mesmo espírito de
/// <see cref="RegistroEventoAlarme"/>): toda alteração relevante de uma
/// <see cref="Credencial"/> ou de uma <see cref="PermissaoAcesso"/> dela grava uma
/// linha aqui. Existe só para preparar uma futura auditoria visual (Sprint 7 não
/// implementa isso) — nenhuma regra de negócio lê ou depende desta tabela, por isso
/// não herda <see cref="AppMorador.Domain.Common.EntidadeComSoftDelete"/> (log de
/// auditoria nunca é excluído).
/// </summary>
public class HistoricoCredencial
{
    public Guid Id { get; set; }

    public Guid CredencialId { get; set; }

    public Credencial? Credencial { get; set; }

    public required TipoEventoHistorico TipoEvento { get; set; }

    public required string Descricao { get; set; }

    public Guid? UsuarioId { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
