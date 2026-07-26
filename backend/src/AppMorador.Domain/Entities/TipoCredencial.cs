namespace AppMorador.Domain.Entities;

/// <summary>
/// Tipo de credencial de acesso. "ChaveVirtual" e "estrutura preparada" (Sprint 7) —
/// sem nenhum fluxo de emissao real ainda, so o valor do enum existe para nao exigir
/// migration quando a funcionalidade for implementada de verdade.
/// </summary>
public enum TipoCredencial
{
    Facial,
    TagRfid,
    QrCode,
    Pin,
    Biometria,
    ChaveVirtual,
}
