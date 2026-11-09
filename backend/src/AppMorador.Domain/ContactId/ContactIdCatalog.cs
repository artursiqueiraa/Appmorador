namespace AppMorador.Domain.ContactId;

/// <summary>
/// Catalogo de codigos Contact ID reconhecidos. Um codigo que chegar fora deste catalogo e
/// tratado como desconhecido por quem chamar <see cref="TryGet"/> — nunca inventar significado
/// sem fonte.
///
/// Formato da chave: qualificador (1 = novo evento, 3 = restauracao) + codigo de 3 digitos.
///
/// Fontes:
/// - "1130" (disparo de zona): confirmacao documental propria no protocolo JFL
///   (Integra-o-FL-main, Documentation/Protocol/09_EVENTS.md).
/// - Demais codigos (Sprint 24, ADR 0040): padrao publico Contact ID (SIA DC-05-1999.09), que as
///   centrais JFL seguem, mas AINDA NAO homologados numa central fisica — por isso
///   <see cref="ContactIdDefinition.PendenteHomologacao"/> = true. Um codigo que a central JFL
///   envie com outro significado so deixa de notificar certo; nao quebra o processamento.
///
/// Restauracao de zona de intrusao (3130–3134) e catalogada sem gerar Ocorrencia: acontece cada
/// vez que um sensor volta ao repouso e so poluiria o historico.
/// </summary>
public static class ContactIdCatalog
{
    private static readonly IReadOnlyDictionary<string, ContactIdDefinition> Definitions =
        new Dictionary<string, ContactIdDefinition>
        {
            // Intrusao
            ["1130"] = new("1130", "Disparo de zona", true, "Movimento detectado", CategoriaContactId.Disparo, UsaZona: true, PendenteHomologacao: false),
            ["1131"] = new("1131", "Disparo de zona perimetral", true, "Sensor do perímetro acionado", CategoriaContactId.Disparo),
            ["1132"] = new("1132", "Disparo de zona interna", true, "Movimento detectado", CategoriaContactId.Disparo),
            ["1133"] = new("1133", "Disparo de zona 24 horas", true, "Sensor 24 horas acionado", CategoriaContactId.Disparo),
            ["1134"] = new("1134", "Disparo de zona de entrada/saida", true, "Porta de entrada aberta com o alarme ligado", CategoriaContactId.Disparo),
            ["3130"] = new("3130", "Restauracao de zona", false, "Sensor voltou ao normal", CategoriaContactId.Restauracao),
            ["3131"] = new("3131", "Restauracao de zona perimetral", false, "Sensor voltou ao normal", CategoriaContactId.Restauracao),
            ["3132"] = new("3132", "Restauracao de zona interna", false, "Sensor voltou ao normal", CategoriaContactId.Restauracao),
            ["3133"] = new("3133", "Restauracao de zona 24 horas", false, "Sensor voltou ao normal", CategoriaContactId.Restauracao),
            ["3134"] = new("3134", "Restauracao de zona de entrada/saida", false, "Sensor voltou ao normal", CategoriaContactId.Restauracao),

            // Panico e coacao (o campo zona/usuario traz o usuario do teclado, nao uma zona)
            ["1120"] = new("1120", "Panico", true, "Pânico acionado na central", CategoriaContactId.Panico, UsaZona: false),
            ["1121"] = new("1121", "Coacao", true, "Senha de coação usada no teclado", CategoriaContactId.Panico, UsaZona: false),
            ["1122"] = new("1122", "Panico silencioso", true, "Pânico silencioso acionado", CategoriaContactId.Panico, UsaZona: false),
            ["1123"] = new("1123", "Panico audivel", true, "Pânico acionado na central", CategoriaContactId.Panico, UsaZona: false),

            // Violacao (tamper)
            ["1137"] = new("1137", "Violacao da central", true, "Tentativa de abrir a central de alarme", CategoriaContactId.Violacao, UsaZona: false),
            ["1144"] = new("1144", "Violacao de sensor", true, "Tentativa de violar um sensor", CategoriaContactId.Violacao),
            ["1145"] = new("1145", "Violacao de modulo de expansao", true, "Tentativa de violar um módulo da central", CategoriaContactId.Violacao, UsaZona: false),
            ["1383"] = new("1383", "Violacao de sensor (supervisao)", true, "Tentativa de violar um sensor", CategoriaContactId.Violacao),
            ["3137"] = new("3137", "Restauracao de violacao da central", true, "Central de alarme fechada novamente", CategoriaContactId.Restauracao, UsaZona: false),
            ["3144"] = new("3144", "Restauracao de violacao de sensor", true, "Sensor voltou ao normal após violação", CategoriaContactId.Restauracao),
            ["3145"] = new("3145", "Restauracao de violacao de modulo", true, "Módulo da central voltou ao normal", CategoriaContactId.Restauracao, UsaZona: false),
            ["3383"] = new("3383", "Restauracao de violacao de sensor (supervisao)", true, "Sensor voltou ao normal após violação", CategoriaContactId.Restauracao),

            // Energia e bateria
            ["1301"] = new("1301", "Falta de energia AC", true, "Falta de energia elétrica", CategoriaContactId.FalhaEnergia, UsaZona: false),
            ["3301"] = new("3301", "Retorno de energia AC", true, "Energia elétrica restabelecida", CategoriaContactId.Restauracao, UsaZona: false),
            ["1302"] = new("1302", "Bateria baixa da central", true, "Bateria da central fraca", CategoriaContactId.BateriaBaixa, UsaZona: false),
            ["3302"] = new("3302", "Bateria da central restaurada", true, "Bateria da central normalizada", CategoriaContactId.Restauracao, UsaZona: false),
            ["1384"] = new("1384", "Bateria baixa de sensor sem fio", true, "Bateria de sensor fraca", CategoriaContactId.BateriaBaixa),
            ["3384"] = new("3384", "Bateria de sensor sem fio restaurada", true, "Bateria do sensor normalizada", CategoriaContactId.Restauracao),
        };

    public static bool TryGet(string code, out ContactIdDefinition? definition) =>
        Definitions.TryGetValue(code, out definition);

    /// <summary>Sprint 24 — usado pela Central de Eventos para filtrar por gravidade direto no banco.</summary>
    public static IReadOnlyList<string> CodigosDaCategoria(params CategoriaContactId[] categorias) =>
        Definitions.Values.Where(d => categorias.Contains(d.Categoria)).Select(d => d.Code).ToList();

    /// <summary>Sprint 24 — categorias que viram alerta critico (com confirmacao e escalonamento).</summary>
    public static bool EhCritica(CategoriaContactId categoria) =>
        categoria is CategoriaContactId.Disparo or CategoriaContactId.Panico or CategoriaContactId.Violacao;
}
