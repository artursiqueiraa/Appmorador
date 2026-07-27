namespace AppMorador.Domain.Entities;

public enum PlataformaDispositivo
{
    Android,
    Ios,
}

/// <summary>
/// Sprint 19 (ADR 0023) — um dispositivo físico que pode receber notificação push,
/// vinculado a um <see cref="Usuario"/> (nunca reaproveitando <see cref="RefreshToken"/>
/// nem o próprio <see cref="Usuario"/> — ver ADR 0023 para o racional completo: um
/// usuário pode ter vários dispositivos ao mesmo tempo — celular pessoal, tablet,
/// celular do cônjuge — cada um com seu próprio token de push, ciclo de vida e
/// preferências de canal independentes). <see cref="PropriedadeId"/> é opcional
/// porque o token é obtido no login, antes (ou independente) de qualquer
/// Propriedade estar selecionada — granularidade por Propriedade fica para uma
/// Sprint futura se um usuário multi-propriedade precisar filtrar por ela.
/// </summary>
public class DispositivoPush
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    public Guid? PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public PlataformaDispositivo Plataforma { get; set; }

    /// <summary>Token de push bruto do FCM (nunca o token do Expo Push Service — ver ADR 0023, o backend fala direto com o FCM).</summary>
    public required string Token { get; set; }

    public string? Modelo { get; set; }

    public string? VersaoApp { get; set; }

    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Sprint 19 — preferências de canal por dispositivo (não por usuário): o mesmo
    /// usuário pode querer alertas no celular pessoal e nada no tablet da sala.
    /// Correspondem 1:1 aos 3 canais Android (Fase 9 da missão) — nunca filtrados
    /// no aparelho depois de entregues (o SO já mostraria a notificação do canal
    /// antes do app ter chance de reagir); filtrados aqui, antes do envio.
    /// </summary>
    public bool NotificarAlertas { get; set; } = true;

    public bool NotificarAtividades { get; set; } = true;

    public bool NotificarGeral { get; set; } = true;

    public DateTime UltimoUsoUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
