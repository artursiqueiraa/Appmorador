namespace AppMorador.Domain.Entities;

/// <summary>Atributo da visita (Autorizacao), nao da pessoa — o mesmo Visitante pode vir por motivos diferentes em ocasioes diferentes.</summary>
public enum TipoVisita
{
    Visitante,
    PrestadorServico,
    Entregador,
    Evento,
    Temporario,
}
