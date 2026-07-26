namespace AppMorador.Domain.Entities;

/// <summary>Tipo de alteração registrada em <see cref="HistoricoCredencial"/>.</summary>
public enum TipoEventoHistorico
{
    CredencialCriada,
    CredencialSuspensa,
    CredencialReativada,
    CredencialRevogada,
    CredencialExpirada,
    CredencialExcluida,
    PermissaoCriada,
    PermissaoAlterada,
    PermissaoExcluida,
}
