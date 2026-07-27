namespace AppMorador.Domain.Entities;

public enum TemplateProvisionamento
{
    Residencia = 1,
    Loja = 2,
    Escritorio = 3,
}

public enum StatusProvisionamento
{
    Rascunho = 1,
    Ativo = 2,
    Arquivado = 3,
}

/// <summary>
/// Sprint 21 (ADR 0028) — "pacote de instalação" de uma Propriedade: registro de QUAL
/// template foi usado e em que estado está, para reinstalação rápida/suporte saber o
/// que foi instalado. Nesta Sprint é um registro de metadados (Nome/Template/Status)
/// — a árvore de equipamentos vinculados (Centrais/Gravadores/Câmeras/Leitores/PGMs/
/// Zonas, descrita na missão) fica para uma Sprint futura dedicada (exigiria uma
/// coluna de vínculo em cada uma dessas entidades já existentes, um escopo bem maior
/// que "estabelecer a base de autorização", ver ADR 0028/dívida técnica).
/// </summary>
public class Provisionamento
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public required string Nome { get; set; }

    public TemplateProvisionamento Template { get; set; }

    public StatusProvisionamento Status { get; set; } = StatusProvisionamento.Rascunho;

    public required DateTime CreatedAtUtc { get; set; }

    public DateTime? AtualizadoEmUtc { get; set; }
}
