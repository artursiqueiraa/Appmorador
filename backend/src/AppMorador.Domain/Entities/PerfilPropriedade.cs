namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0021) — perfil do CLIENTE dentro de uma propriedade especifica.
/// Exclusivo de quem tem um vínculo em <see cref="UsuarioPropriedade"/> — nunca se
/// aplica a um usuário interno da plataforma (ver <see cref="RoleSistema"/>, o
/// conceito equivalente do lado interno).
/// </summary>
public enum PerfilPropriedade
{
    /// <summary>Sprint 25 — exibido como "Proprietário": pode tudo.</summary>
    Administrador = 1,

    /// <summary>Sprint 25 — exibido como "Membro": ver, armar/desarmar, abrir portões, pânico.</summary>
    Morador = 2,

    /// <summary>Sprint 25 — tudo, menos gerenciar pessoas e excluir a propriedade.</summary>
    Gerente = 3,

    /// <summary>Sprint 25 — só ver (câmeras, histórico, status) e confirmar alertas.</summary>
    Visualizador = 4,
}
