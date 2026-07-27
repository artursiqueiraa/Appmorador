namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0021) — papel GLOBAL, exclusivo de usuários INTERNOS da plataforma
/// (equipe do AppMorador). Um cliente (dono de propriedade) nunca tem valor aqui —
/// <see cref="Usuario.RoleGlobal"/> é sempre null para clientes. "Administrador"/
/// "Morador" (o lado do cliente) NÃO são valores deste enum — ver
/// <see cref="PerfilPropriedade"/>, que é um conceito propositalmente separado.
/// </summary>
public enum RoleSistema
{
    Master = 1,
    Tecnico = 2,
    Suporte = 3,
}
