namespace AppMorador.Domain.Entities;

/// <summary>
/// Um evento da central JFL registrado como ocorrencia. Criada imediatamente apos o
/// ACK ao painel (Fase 1 - confiabilidade do evento), sem esperar qualquer I/O alem
/// da propria gravacao.
///
/// Desenho deliberado: os campos brutos do evento (NumeroSeriePainel, CodigoEvento,
/// ZonaOuUsuario, Particao) sao sempre preenchidos, mesmo que a central ou a zona
/// ainda nao estejam provisionadas no banco — os FKs resolvidos (CentralId,
/// PropriedadeId, ZonaId) sao nullable e so preenchidos quando o vinculo existe. Isso
/// garante que nenhum disparo seja perdido por falta de cadastro previo do
/// equipamento, que e exatamente o objetivo desta fase.
/// </summary>
public class Ocorrencia
{
    public Guid Id { get; set; }

    public required string NumeroSeriePainel { get; set; }

    /// <summary>Codigo Contact ID de 4 digitos, sem nenhuma classificacao/filtro aplicado (fase futura).</summary>
    public required string CodigoEvento { get; set; }

    public required string ZonaOuUsuario { get; set; }

    public required string Particao { get; set; }

    public required DateTime CreatedAtUtc { get; set; }

    public Guid? CentralId { get; set; }

    public Central? Central { get; set; }

    public Guid? PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public Guid? ZonaId { get; set; }

    public Zona? Zona { get; set; }

    /// <summary>
    /// Resolvido quando CentralId e ZonaId foram encontrados; NaoResolvido quando a
    /// ocorrencia foi criada so com os dados brutos (central e/ou zona nao cadastrados
    /// ainda). Permite localizar rapidamente eventos que precisam de provisionamento.
    /// </summary>
    public required StatusResolucao StatusResolucao { get; set; }

    /// <summary>
    /// Caminho relativo do snapshot salvo em disco (ex.: "snapshots/{propriedadeId}/2026/07/18/{guid}.jpg"),
    /// ou null quando nao ha camera vinculada a zona, a zona nao foi resolvida, ou a
    /// captura falhou.
    /// </summary>
    public string? ImagePath { get; set; }
}
