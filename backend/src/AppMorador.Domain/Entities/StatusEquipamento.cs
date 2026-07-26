namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 11 — resultado da última comunicação real com o equipamento. Nunca
/// computado por polling automático (fora de escopo) — só muda quando o usuário
/// executa "testar conexão" ou uma sincronização manual.
/// </summary>
public enum StatusEquipamento
{
    /// <summary>Cadastrado, mas nenhuma tentativa de comunicação foi feita ainda.</summary>
    Desconhecido,
    Online,
    Offline,
}
