namespace AppMorador.Application.Eventos;

/// <summary>
/// Qual integração produziu o evento. Detalhe interno de implementação — nunca exposto
/// na Api (protocolo/integração não é linguagem de usuário). Novas fontes reais
/// (ControlId, Intelbras) ganham um valor aqui só quando existirem de fato.
/// </summary>
public enum OrigemEvento
{
    /// <summary>Central de alarme (protocolo JFL).</summary>
    Jfl,

    /// <summary>Gerado pelo próprio AppMorador, sem integração externa por trás.</summary>
    Aplicacao,
}
