namespace AppMorador.Domain.ContactId;

/// <summary>
/// Sprint 24 (ADR 0040) — o que um código Contact ID significa para o dono do imóvel. Decide
/// se o evento vira alerta crítico, notificação comum ou só histórico.
/// </summary>
public enum CategoriaContactId
{
    /// <summary>Invasão detectada por um sensor (alerta crítico).</summary>
    Disparo,

    /// <summary>Pânico ou coação acionados no teclado/controle (alerta crítico).</summary>
    Panico,

    /// <summary>Tentativa de abrir ou arrancar a central ou um sensor (alerta crítico).</summary>
    Violacao,

    /// <summary>Falta de energia elétrica na central (notificação comum).</summary>
    FalhaEnergia,

    /// <summary>Bateria baixa da central ou de um sensor sem fio (notificação comum).</summary>
    BateriaBaixa,

    /// <summary>Volta ao normal de algum dos anteriores (só histórico, sem push).</summary>
    Restauracao,
}
