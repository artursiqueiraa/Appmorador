namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0025) — funcionalidades que podem ser ativadas/desativadas por
/// vínculo (<see cref="UsuarioPropriedade"/>), independente do Perfil — permite
/// oferecer planos diferentes (Básico/Avançado/TI Própria) sem criar novos Perfis.
/// Extensível: um valor novo aqui nunca exige migração de dado existente.
/// </summary>
public enum PermissaoFuncionalidade
{
    CadastrarMorador = 1,
    CadastrarFacial = 2,
    CadastrarTag = 3,
    CadastrarCamera = 4,
    AlterarLeitor = 5,
    AlterarGravador = 6,
    ConfigurarPgm = 7,
    ConfigurarIntegracoes = 8,
    VerLogs = 9,
    AbrirPortao = 10,
    VerCameras = 11,
    CriarVisitante = 12,
}
