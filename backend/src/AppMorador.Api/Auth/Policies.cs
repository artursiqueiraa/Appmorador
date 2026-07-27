namespace AppMorador.Api.Auth;

/// <summary>
/// Sprint 21 (ADR 0021) — nomes centralizados das Policies de autorização,
/// registradas uma única vez em <c>Program.cs</c>. Nenhum Controller checa
/// role/perfil manualmente — sempre via <c>[Authorize(Policy = ...)]</c>
/// (checagem GROSSEIRA de papel) + <c>IPermissaoService</c> (refinamento por
/// recurso específico, ver Fase 5.2/5.3 da missão).
/// </summary>
public static class Policies
{
    public const string RequerMaster = "RequerMaster";

    /// <summary>Master OU Técnico.</summary>
    public const string RequerTecnico = "RequerTecnico";

    /// <summary>Master OU Suporte — Técnico deliberadamente NÃO incluído (não tem capacidade de impersonation/ver-tudo, ver tabela de papéis da missão).</summary>
    public const string RequerSuporte = "RequerSuporte";

    /// <summary>Qualquer interno da plataforma (Master | Tecnico | Suporte).</summary>
    public const string RequerInterno = "RequerInterno";

    /// <summary>
    /// Qualquer cliente (não-interno) — hoje equivale sempre a Administrador
    /// (único Perfil de propriedade que login algum produz nesta Sprint, ver ADR
    /// 0021). Mantido como nome próprio para quando Morador ganhar login de
    /// verdade — o Controller não precisa mudar, só a semântica por trás.
    /// </summary>
    public const string RequerCliente = "RequerCliente";

    public const string RequerAdministrador = "RequerAdministrador";

    /// <summary>
    /// Reservado — nenhum fluxo de login desta Sprint produz uma claim que
    /// satisfaça esta Policy (Morador não é autenticável ainda, ver ADR 0021).
    /// Existe para o contrato já estar pronto quando essa evolução acontecer.
    /// </summary>
    public const string RequerMorador = "RequerMorador";
}
