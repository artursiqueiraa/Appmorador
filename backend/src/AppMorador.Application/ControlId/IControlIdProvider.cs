namespace AppMorador.Application.ControlId;

/// <summary>
/// Porta do Provider Control iD — ÚNICO ponto do sistema que pode saber que o
/// protocolo é Control iD. Controllers/Entidades/Casos de uso nunca importam este
/// namespace diretamente com conhecimento do fabricante: só
/// <see cref="AppMorador.Application.Equipamentos.EquipamentoIntegracaoServico"/> injeta
/// esta porta, resolvida por <see cref="AppMorador.Domain.Entities.FabricanteEquipamento"/>.
/// Esta é a arquitetura OFICIAL de integração do AppMorador (ADR 0014): futuros
/// fabricantes (Intelbras, Hikvision, Dahua, JFL) implementam a mesma forma de porta,
/// cada um com seu próprio Provider — nenhum altera o domínio.
/// </summary>
public interface IControlIdProvider
{
    Task<ResultadoTesteConexao> TestarConexaoAsync(ConexaoEquipamento conexao, CancellationToken cancellationToken);

    Task<InformacoesEquipamento> ConsultarInformacoesAsync(ConexaoEquipamento conexao, CancellationToken cancellationToken);

    Task<ResultadoSincronizacao> SincronizarMoradoresAsync(
        ConexaoEquipamento conexao, IReadOnlyList<MoradorParaSincronizar> moradores, CancellationToken cancellationToken);

    Task<ResultadoSincronizacao> SincronizarCredenciaisAsync(
        ConexaoEquipamento conexao, IReadOnlyList<CredencialParaSincronizar> credenciais, CancellationToken cancellationToken);

    Task<ResultadoSincronizacao> SincronizarPermissoesAsync(
        ConexaoEquipamento conexao, IReadOnlyList<PermissaoParaSincronizar> permissoes, CancellationToken cancellationToken);

    /// <summary>Importação sob demanda (poller mínimo, decisão da Fase 1) — nunca um serviço de background contínuo (fora de escopo).</summary>
    Task<IReadOnlyList<EventoImportado>> ImportarEventosAsync(ConexaoEquipamento conexao, CancellationToken cancellationToken);
}
