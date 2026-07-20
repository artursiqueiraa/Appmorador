namespace AppMorador.Domain.ContactId;

/// <summary>
/// Definicao de um codigo Contact ID conhecido. Adicionar suporte a um novo codigo e
/// so acrescentar uma entrada em <see cref="ContactIdCatalog"/> — nenhuma logica do
/// sistema (parser, processor) precisa mudar.
/// </summary>
/// <param name="FriendlyMessage">
/// Texto para o usuario final do app (nunca "Contact ID"/"Zona 04"/"CID") — ex.:
/// "Movimento detectado". O Dashboard compoe isso com o nome amigavel da zona.
/// </param>
public sealed record ContactIdDefinition(string Code, string Description, bool GeneratesOccurrence, string FriendlyMessage);
