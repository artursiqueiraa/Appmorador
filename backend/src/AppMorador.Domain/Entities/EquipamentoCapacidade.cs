namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0027) — capacidades reais de um <see cref="ModeloEquipamento"/>. O
/// app/painel nunca cria tela específica por fabricante; sempre perguntam "o que
/// esse equipamento suporta?" e renderizam a partir daqui.
/// </summary>
public enum EquipamentoCapacidade
{
    Face = 1,
    Tag = 2,
    QrCode = 3,
    Senha = 4,
    Armar = 5,
    Desarmar = 6,
    Pgm = 7,
    Streaming = 8,
    Ptz = 9,
}
