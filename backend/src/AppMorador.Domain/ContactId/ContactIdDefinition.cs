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
/// <param name="Categoria">Sprint 24 — decide alerta critico, notificacao comum ou so historico.</param>
/// <param name="UsaZona">
/// Sprint 24 — true quando o campo zona/usuario do evento e um numero de zona (sensor). Eventos
/// do sistema (energia, bateria da central, violacao da caixa, panico) nao tem zona: a Ocorrencia
/// nao procura zona e o texto nunca diz "em {zona}".
/// </param>
/// <param name="PendenteHomologacao">
/// Sprint 24 — true para codigos vindos do padrao SIA DC-05 ainda nao confirmados numa central JFL
/// fisica. So "1130" tem confirmacao documental propria.
/// </param>
public sealed record ContactIdDefinition(
    string Code,
    string Description,
    bool GeneratesOccurrence,
    string FriendlyMessage,
    CategoriaContactId Categoria = CategoriaContactId.Disparo,
    bool UsaZona = true,
    bool PendenteHomologacao = true);
