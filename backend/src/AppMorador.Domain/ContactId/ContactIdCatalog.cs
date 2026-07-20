namespace AppMorador.Domain.ContactId;

/// <summary>
/// Catalogo de codigos Contact ID reconhecidos. Hoje contem apenas "1130" (disparo
/// de zona), o unico codigo com confirmacao documental propria no protocolo
/// (Integra-o-FL-main, Documentation/Protocol/09_EVENTS.md). Um codigo que chegar
/// fora deste catalogo e tratado como desconhecido por quem chamar
/// <see cref="TryGet"/> — nunca inventar significado sem fonte.
/// </summary>
public static class ContactIdCatalog
{
    private static readonly IReadOnlyDictionary<string, ContactIdDefinition> Definitions =
        new Dictionary<string, ContactIdDefinition>
        {
            ["1130"] = new ContactIdDefinition(
                "1130", "Disparo de zona", GeneratesOccurrence: true, FriendlyMessage: "Movimento detectado"),
        };

    public static bool TryGet(string code, out ContactIdDefinition? definition) =>
        Definitions.TryGetValue(code, out definition);
}
