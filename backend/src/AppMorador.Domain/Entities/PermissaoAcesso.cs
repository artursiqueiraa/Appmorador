using AppMorador.Domain.Common;

namespace AppMorador.Domain.Entities;

/// <summary>
/// Vínculo entre uma <see cref="Credencial"/> e um <see cref="PontoAcesso"/> (mesmo
/// padrão de entidade de vínculo já usado em <see cref="VinculoZonaCamera"/>): uma
/// credencial pode ter várias permissões, uma por ponto que acessa, cada uma com
/// suas próprias regras de dia/horário/data. Regras nulas/`Todos` = sem restrição
/// naquele aspecto (ex.: <see cref="HorarioInicial"/> nulo = sem limite de início).
/// </summary>
public class PermissaoAcesso : EntidadeComSoftDelete
{
    public Guid Id { get; set; }

    public Guid CredencialId { get; set; }

    public Credencial? Credencial { get; set; }

    public Guid PontoAcessoId { get; set; }

    public PontoAcesso? PontoAcesso { get; set; }

    public required DiaSemana DiasPermitidos { get; set; }

    public TimeOnly? HorarioInicial { get; set; }

    public TimeOnly? HorarioFinal { get; set; }

    public DateTime? DataInicial { get; set; }

    public DateTime? DataFinal { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
