using AppMorador.Domain.Entities;

namespace AppMorador.Application.Operacional;

/// <summary>
/// Implementação única do Classificador Operacional (ver ADR 0016 para o racional
/// completo de cada regra). Puro — sem I/O, sem dependência de repositório —
/// deliberadamente fácil de testar e auditar.
/// </summary>
public sealed class ClassificadorOperacionalServico : IClassificadorOperacionalServico
{
    public EstadoOperacional ClassificarEquipamento(EstadoBrutoEquipamento estado) => estado.Status switch
    {
        StatusEquipamento.Offline => EstadoOperacional.Offline,
        StatusEquipamento.Desconhecido => EstadoOperacional.Atencao,
        StatusEquipamento.Online when estado.TemProblemaAtivo => EstadoOperacional.Critico,
        StatusEquipamento.Online => EstadoOperacional.Saudavel,
        _ => EstadoOperacional.Atencao,
    };

    public EstadoOperacional ClassificarPropriedade(IReadOnlyList<EstadoBrutoEquipamento> equipamentos, int quantidadeAlarmesAtivos)
    {
        // Sem equipamento cadastrado ainda: nada a reportar, nunca alarmar o usuario
        // por uma integracao que ele simplesmente nao configurou (mesmo espirito da
        // "instalacao vazia" do Dashboard desde a Sprint 6).
        if (equipamentos.Count == 0)
        {
            return EstadoOperacional.Saudavel;
        }

        // Qualquer alarme/problema ativo tem prioridade sobre tudo — mesmo com a
        // maioria dos equipamentos online, uma central com problema e uma condicao
        // critica que nao pode ficar escondida atras de uma media otimista.
        if (quantidadeAlarmesAtivos > 0)
        {
            return EstadoOperacional.Critico;
        }

        var quantidadeOnline = equipamentos.Count(e => e.Status == StatusEquipamento.Online);
        if (quantidadeOnline == 0)
        {
            return EstadoOperacional.Offline;
        }

        return quantidadeOnline < equipamentos.Count ? EstadoOperacional.Atencao : EstadoOperacional.Saudavel;
    }
}
