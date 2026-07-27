using AppMorador.Domain.Entities;

namespace AppMorador.Application.Notificacoes;

public sealed class RegistrarDispositivoPushRequest
{
    public Guid? PropriedadeId { get; set; }

    public required PlataformaDispositivo Plataforma { get; set; }

    public required string Token { get; set; }

    public string? Modelo { get; set; }

    public string? VersaoApp { get; set; }
}

public sealed class AtualizarDispositivoPushRequest
{
    public required string Token { get; set; }

    public string? VersaoApp { get; set; }
}

public sealed class AtualizarPreferenciasDispositivoPushRequest
{
    public bool NotificarAlertas { get; set; } = true;

    public bool NotificarAtividades { get; set; } = true;

    public bool NotificarGeral { get; set; } = true;
}

public sealed class DispositivoPushResponse
{
    public Guid Id { get; set; }

    public PlataformaDispositivo Plataforma { get; set; }

    public bool Ativo { get; set; }

    public bool NotificarAlertas { get; set; }

    public bool NotificarAtividades { get; set; }

    public bool NotificarGeral { get; set; }

    public DateTime UltimoUsoUtc { get; set; }

    public static DispositivoPushResponse FromEntity(DispositivoPush d) => new()
    {
        Id = d.Id,
        Plataforma = d.Plataforma,
        Ativo = d.Ativo,
        NotificarAlertas = d.NotificarAlertas,
        NotificarAtividades = d.NotificarAtividades,
        NotificarGeral = d.NotificarGeral,
        UltimoUsoUtc = d.UltimoUsoUtc,
    };
}
