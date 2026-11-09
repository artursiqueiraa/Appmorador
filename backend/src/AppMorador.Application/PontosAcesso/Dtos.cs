using System.ComponentModel.DataAnnotations;
using AppMorador.Domain.Entities;

namespace AppMorador.Application.PontosAcesso;

public sealed class CriarPontoAcessoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>Nulo = Geral (compatibilidade com o comportamento anterior à Sprint 9).</summary>
    public TipoPontoAcesso? Tipo { get; set; }

    /// <summary>Sprint 24.5 — equipamento que abre esta porta (central JFL ou leitor Control iD). Null = nenhum.</summary>
    public Guid? EquipamentoAcionadorId { get; set; }

    /// <summary>PGM (JFL) ou porta (Control iD), a partir de 1. Obrigatório quando há acionador.</summary>
    [Range(1, 32)]
    public int? NumeroSaida { get; set; }

    /// <summary>Sprint 24.5 — câmera que mostra esta porta. Null = nenhuma.</summary>
    public Guid? CameraId { get; set; }
}

public sealed class AtualizarPontoAcessoRequest
{
    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    public TipoPontoAcesso? Tipo { get; set; }

    /// <summary>Sprint 24.5 — equipamento que abre esta porta (central JFL ou leitor Control iD). Null = nenhum.</summary>
    public Guid? EquipamentoAcionadorId { get; set; }

    /// <summary>PGM (JFL) ou porta (Control iD), a partir de 1. Obrigatório quando há acionador.</summary>
    [Range(1, 32)]
    public int? NumeroSaida { get; set; }

    /// <summary>Sprint 24.5 — câmera que mostra esta porta. Null = nenhuma.</summary>
    public Guid? CameraId { get; set; }
}

public sealed class PontoAcessoResponse
{
    public required Guid Id { get; init; }

    public required Guid PropriedadeId { get; init; }

    public required string Nome { get; init; }

    public required TipoPontoAcesso Tipo { get; init; }

    public Guid? EquipamentoAcionadorId { get; init; }

    public string? NomeEquipamentoAcionador { get; init; }

    public FabricanteEquipamento? FabricanteAcionador { get; init; }

    public int? NumeroSaida { get; init; }

    /// <summary>true quando há acionador — o app mostra o botão "Abrir".</summary>
    public bool PodeAbrir { get; init; }

    /// <summary>Equipamento acionador comunicando agora (Status = Online).</summary>
    public bool AcionadorOnline { get; init; }

    public Guid? CameraId { get; init; }

    public string? NomeCamera { get; init; }
}

/// <summary>Sprint 24.5 — equipamento da propriedade que pode abrir portas.</summary>
public sealed class AcionadorDisponivelResponse
{
    public required Guid EquipamentoId { get; init; }

    public required string Nome { get; init; }

    public required FabricanteEquipamento Fabricante { get; init; }

    public required bool Online { get; init; }

    /// <summary>"Saída (PGM)" para central JFL, "Porta" para leitor Control iD.</summary>
    public required string RotuloSaida { get; init; }
}

public sealed class ResultadoAberturaResponse
{
    public required bool Sucesso { get; init; }

    public required string Mensagem { get; init; }
}
