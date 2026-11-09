using System.Collections.Concurrent;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Saude;

public enum SituacaoComponente
{
    Ok,
    Atencao,
    Fora,
}

public sealed class ComponenteSaudeResponse
{
    public required string Chave { get; init; }

    public required string Nome { get; init; }

    public required SituacaoComponente Situacao { get; init; }

    public string? Detalhe { get; init; }

    /// <summary>Desde quando o componente está fora/atenção (visto pelo vigia), se estiver.</summary>
    public DateTime? ForaDesdeUtc { get; init; }
}

public sealed class RegistroBackupResponse
{
    public required bool Sucesso { get; init; }

    public string? Arquivo { get; init; }

    public long? TamanhoBytes { get; init; }

    public string? Mensagem { get; init; }

    public required DateTime OcorridoEmUtc { get; init; }
}

public sealed class SaudeSistemaResponse
{
    public required SituacaoComponente Geral { get; init; }

    public required IReadOnlyList<ComponenteSaudeResponse> Componentes { get; init; }

    public RegistroBackupResponse? UltimoBackup { get; init; }

    public RegistroBackupResponse? UltimoTesteRestauracao { get; init; }

    public required DateTime VerificadoEmUtc { get; init; }
}

/// <summary>
/// Sprint 27 (ADR 0043) — batimento das rotinas em segundo plano: cada rodada registra que está viva.
/// Singleton em memória (se o backend cai, o próprio /health deixa de responder).
/// </summary>
public interface IBatimentosServicos
{
    void Registrar(string chave, string nome, TimeSpan intervalo);

    IReadOnlyList<(string Chave, string Nome, TimeSpan Intervalo, DateTime UltimoUtc)> Listar();
}

public sealed class BatimentosServicos : IBatimentosServicos
{
    private readonly ConcurrentDictionary<string, (string Nome, TimeSpan Intervalo, DateTime UltimoUtc)> _batimentos = new();

    public void Registrar(string chave, string nome, TimeSpan intervalo) =>
        _batimentos[chave] = (nome, intervalo, DateTime.UtcNow);

    public IReadOnlyList<(string Chave, string Nome, TimeSpan Intervalo, DateTime UltimoUtc)> Listar() =>
        _batimentos.Select(b => (b.Key, b.Value.Nome, b.Value.Intervalo, b.Value.UltimoUtc)).OrderBy(b => b.Nome).ToList();
}

/// <summary>Porta: o banco responde?</summary>
public interface IVerificadorBanco
{
    Task<bool> ConectaAsync(CancellationToken cancellationToken);
}

/// <summary>Porta: o servidor TCP que recebe as centrais JFL está escutando?</summary>
public interface IEstadoRecepcaoJfl
{
    bool Escutando { get; }

    int CentraisConectadas { get; }
}

/// <summary>
/// Sprint 27 (ADR 0043) — guarda desde quando cada componente está com problema e decide quando
/// avisar (fora há mais de <see cref="TempoParaAvisar"/>) e quando avisar que voltou.
/// </summary>
public sealed class MonitorQuedas
{
    public static readonly TimeSpan TempoParaAvisar = TimeSpan.FromMinutes(2);

    private readonly ConcurrentDictionary<string, (DateTime DesdeUtc, bool Avisado)> _problemas = new();

    public DateTime? ForaDesde(string chave) => _problemas.TryGetValue(chave, out var p) ? p.DesdeUtc : null;

    /// <summary>Atualiza com a verificação atual e devolve o que precisa virar aviso agora.</summary>
    public (IReadOnlyList<ComponenteSaudeResponse> Cairam, IReadOnlyList<ComponenteSaudeResponse> Voltaram) Atualizar(
        IReadOnlyList<ComponenteSaudeResponse> componentes, DateTime agoraUtc)
    {
        var cairam = new List<ComponenteSaudeResponse>();
        var voltaram = new List<ComponenteSaudeResponse>();
        foreach (var c in componentes)
        {
            if (c.Situacao == SituacaoComponente.Ok)
            {
                if (_problemas.TryRemove(c.Chave, out var anterior) && anterior.Avisado)
                {
                    voltaram.Add(c);
                }

                continue;
            }

            var atual = _problemas.GetOrAdd(c.Chave, _ => (agoraUtc, false));
            if (!atual.Avisado && agoraUtc - atual.DesdeUtc >= TempoParaAvisar)
            {
                _problemas[c.Chave] = (atual.DesdeUtc, true);
                cairam.Add(c);
            }
        }

        return (cairam, voltaram);
    }
}

public interface ISaudeSistemaServico
{
    Task<SaudeSistemaResponse> VerificarAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 27 (ADR 0043) — saúde do sistema: banco, recepção das centrais JFL, rotinas em segundo
/// plano (batimento) e backup (último backup com sucesso há menos de 26 h).
/// </summary>
public sealed class SaudeSistemaServico : ISaudeSistemaServico
{
    public static readonly TimeSpan LimiteBackup = TimeSpan.FromHours(26);

    private readonly IVerificadorBanco _banco;
    private readonly IEstadoRecepcaoJfl _jfl;
    private readonly IBatimentosServicos _batimentos;
    private readonly IRegistroBackupRepositorio _backups;
    private readonly MonitorQuedas _monitor;

    public SaudeSistemaServico(
        IVerificadorBanco banco, IEstadoRecepcaoJfl jfl, IBatimentosServicos batimentos, IRegistroBackupRepositorio backups, MonitorQuedas monitor)
    {
        _banco = banco;
        _jfl = jfl;
        _batimentos = batimentos;
        _backups = backups;
        _monitor = monitor;
    }

    public async Task<SaudeSistemaResponse> VerificarAsync(CancellationToken cancellationToken)
    {
        var agora = DateTime.UtcNow;
        var componentes = new List<ComponenteSaudeResponse>();

        var bancoOk = await _banco.ConectaAsync(cancellationToken).ConfigureAwait(false);
        componentes.Add(Componente("banco", "Banco de dados", bancoOk ? SituacaoComponente.Ok : SituacaoComponente.Fora,
            bancoOk ? null : "O servidor não consegue falar com o banco."));

        componentes.Add(Componente("jfl", "Recepção das centrais de alarme",
            _jfl.Escutando ? SituacaoComponente.Ok : SituacaoComponente.Fora,
            _jfl.Escutando
                ? (_jfl.CentraisConectadas == 1 ? "1 central conectada" : $"{_jfl.CentraisConectadas} centrais conectadas")
                : "A porta que recebe as centrais não está aberta: disparos não chegam."));

        foreach (var (chave, nome, intervalo, ultimo) in _batimentos.Listar())
        {
            var limite = intervalo * 3 < TimeSpan.FromMinutes(2) ? TimeSpan.FromMinutes(2) : intervalo * 3;
            var parada = agora - ultimo > limite;
            componentes.Add(Componente(chave, nome, parada ? SituacaoComponente.Fora : SituacaoComponente.Ok,
                parada ? $"Sem rodar desde {ultimo:HH:mm} (UTC)." : null));
        }

        RegistroBackup? backup = null;
        RegistroBackup? teste = null;
        if (bancoOk)
        {
            backup = await _backups.UltimoAsync(TipoRegistroBackup.Backup, cancellationToken).ConfigureAwait(false);
            teste = await _backups.UltimoAsync(TipoRegistroBackup.TesteRestauracao, cancellationToken).ConfigureAwait(false);
            var (situacaoBackup, detalheBackup) = backup switch
            {
                null => (SituacaoComponente.Atencao, "Nenhum backup registrado ainda."),
                { Sucesso: false } => (SituacaoComponente.Atencao, $"O último backup falhou: {backup.Mensagem}"),
                _ when agora - backup.OcorridoEmUtc > LimiteBackup => (SituacaoComponente.Atencao, "O backup diário está atrasado."),
                _ when teste is { Sucesso: false } => (SituacaoComponente.Atencao, $"O teste de restauração falhou: {teste.Mensagem}"),
                _ => (SituacaoComponente.Ok, (string?)null),
            };
            componentes.Add(Componente("backup", "Backup do banco", situacaoBackup, detalheBackup));
        }

        return new SaudeSistemaResponse
        {
            Geral = componentes.Any(c => c.Situacao == SituacaoComponente.Fora) ? SituacaoComponente.Fora
                : componentes.Any(c => c.Situacao == SituacaoComponente.Atencao) ? SituacaoComponente.Atencao
                : SituacaoComponente.Ok,
            Componentes = componentes,
            UltimoBackup = Mapear(backup),
            UltimoTesteRestauracao = Mapear(teste),
            VerificadoEmUtc = agora,
        };
    }

    private ComponenteSaudeResponse Componente(string chave, string nome, SituacaoComponente situacao, string? detalhe) => new()
    {
        Chave = chave,
        Nome = nome,
        Situacao = situacao,
        Detalhe = detalhe,
        ForaDesdeUtc = situacao == SituacaoComponente.Ok ? null : _monitor.ForaDesde(chave),
    };

    private static RegistroBackupResponse? Mapear(RegistroBackup? r) => r is null ? null : new RegistroBackupResponse
    {
        Sucesso = r.Sucesso,
        Arquivo = r.Arquivo,
        TamanhoBytes = r.TamanhoBytes,
        Mensagem = r.Mensagem,
        OcorridoEmUtc = r.OcorridoEmUtc,
    };
}
