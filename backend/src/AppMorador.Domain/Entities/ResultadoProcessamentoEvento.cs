namespace AppMorador.Domain.Entities;

/// <summary>Resultado do processamento de um evento JFL, gravado em <see cref="RegistroEventoAlarme"/>.</summary>
public enum ResultadoProcessamentoEvento
{
    /// <summary>O codigo esta no catalogo e gera Ocorrencia; uma Ocorrencia foi criada.</summary>
    OcorrenciaCriada,

    /// <summary>O codigo esta no catalogo mas marcado como GeneratesOccurrence=false — nenhuma Ocorrencia criada.</summary>
    IgnoradoPorFiltro,

    /// <summary>O codigo nao esta no ContactIdCatalog (painel/firmware ainda nao homologado para este codigo) — nenhuma Ocorrencia criada.</summary>
    CodigoDesconhecido,

    /// <summary>Uma excecao impediu o processamento (ex.: banco indisponivel ao resolver central/zona).</summary>
    ErroAoProcessar,
}
