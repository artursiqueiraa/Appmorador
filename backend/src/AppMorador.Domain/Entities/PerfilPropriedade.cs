namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0021) — perfil do CLIENTE dentro de uma propriedade especifica.
/// Exclusivo de quem tem um vínculo em <see cref="UsuarioPropriedade"/> — nunca se
/// aplica a um usuário interno da plataforma (ver <see cref="RoleSistema"/>, o
/// conceito equivalente do lado interno).
/// </summary>
public enum PerfilPropriedade
{
    Administrador = 1,
    Morador = 2,
}
