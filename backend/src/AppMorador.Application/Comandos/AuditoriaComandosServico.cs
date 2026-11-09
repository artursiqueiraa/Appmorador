using AppMorador.Application.Common;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Comandos;

public sealed class ComandoExecutadoResponse
{
    public required Guid Id { get; init; }

    public required TipoComando Tipo { get; init; }

    public required string Descricao { get; init; }

    public required string Autor { get; init; }

    public required FormaConfirmacao Confirmacao { get; init; }

    public required bool Sucesso { get; init; }

    public string? Mensagem { get; init; }

    public Guid? EquipamentoId { get; init; }

    public Guid? PontoAcessoId { get; init; }

    public required DateTime OcorridoEmUtc { get; init; }
}

public interface IAuditoriaComandosServico
{
    Task<Result<IReadOnlyList<ComandoExecutadoResponse>>> ListarAsync(Guid usuarioId, Guid propriedadeId, int limite, CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 27 (ADR 0043) — "quem armou, desarmou, abriu": leitura da auditoria de comandos para quem
/// administra a propriedade (dono e gerente). Base dos relatórios da Sprint 28.
/// </summary>
public sealed class AuditoriaComandosServico : IAuditoriaComandosServico
{
    private readonly IComandoExecutadoRepositorio _comandos;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAcessoPropriedadeServico _acesso;

    public AuditoriaComandosServico(
        IComandoExecutadoRepositorio comandos, IPropriedadeRepositorio propriedades, IUsuarioRepositorio usuarios, IAcessoPropriedadeServico acesso)
    {
        _comandos = comandos;
        _propriedades = propriedades;
        _usuarios = usuarios;
        _acesso = acesso;
    }

    public async Task<Result<IReadOnlyList<ComandoExecutadoResponse>>> ListarAsync(Guid usuarioId, Guid propriedadeId, int limite, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || !_acesso.Pode(usuarioId, propriedade, NivelAcesso.Alterar))
        {
            return Result<IReadOnlyList<ComandoExecutadoResponse>>.Fail("Propriedade não encontrada.");
        }

        var comandos = await _comandos.ListarPorPropriedadeAsync(propriedadeId, Math.Clamp(limite, 1, 200), cancellationToken).ConfigureAwait(false);

        var nomes = new Dictionary<Guid, string>();
        foreach (var autorId in comandos.Select(c => c.UsuarioId).Distinct())
        {
            var autor = await _usuarios.GetByIdAsync(autorId, cancellationToken).ConfigureAwait(false);
            nomes[autorId] = autor?.Nome ?? "Conta removida";
        }

        return Result<IReadOnlyList<ComandoExecutadoResponse>>.Ok(comandos.Select(c => new ComandoExecutadoResponse
        {
            Id = c.Id,
            Tipo = c.Tipo,
            Descricao = Descrever(c),
            Autor = nomes[c.UsuarioId],
            Confirmacao = c.Confirmacao,
            Sucesso = c.Sucesso,
            Mensagem = c.Mensagem,
            EquipamentoId = c.EquipamentoId,
            PontoAcessoId = c.PontoAcessoId,
            OcorridoEmUtc = c.OcorridoEmUtc,
        }).ToList());
    }

    public static string Descrever(ComandoExecutado c)
    {
        var acao = c.Tipo switch
        {
            TipoComando.Armar => "Armou o alarme",
            TipoComando.ArmarStay => "Armou em modo parcial",
            TipoComando.ArmarAway => "Armou em modo total",
            TipoComando.Desarmar => "Desarmou o alarme",
            TipoComando.AcionarPgm => "Acionou saída",
            TipoComando.DesligarPgm => "Desligou saída",
            TipoComando.InibirZona => "Ignorou sensor",
            TipoComando.DesinibirZona => "Voltou a monitorar sensor",
            TipoComando.AbrirAcesso => "Abriu acesso",
            _ => c.Tipo.ToString(),
        };
        return string.IsNullOrEmpty(c.Parametros) ? acao : $"{acao} ({c.Parametros})";
    }
}
