using AppMorador.Application.Notificacoes;
using AppMorador.Domain.Entities;
using AppMorador.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppMorador.Infrastructure.Notifications;

/// <summary>
/// Sprint 24 (ADR 0040) — substitui o debounce em memória da Sprint 19 (dívida 41): o último
/// envio de cada (tipo, chave) fica na tabela ControlesEnvioNotificacao, então reiniciar o
/// servidor não reenvia a mesma notificação. Scoped (usa o AppDbContext da requisição).
/// </summary>
public sealed class DebounceNotificacaoPersistido : IDebounceNotificacao
{
    public static readonly TimeSpan Janela = TimeSpan.FromSeconds(60);

    private readonly AppDbContext _db;

    public DebounceNotificacaoPersistido(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> PodeNotificarAsync(EventoNotificacaoTipo tipo, Guid chave, CancellationToken cancellationToken)
    {
        var id = Chave(tipo, chave);
        var ultimoEnvio = await _db.ControlesEnvioNotificacao
            .AsNoTracking()
            .Where(c => c.Chave == id)
            .Select(c => (DateTime?)c.UltimoEnvioEmUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return ultimoEnvio is null || DateTime.UtcNow - ultimoEnvio.Value >= Janela;
    }

    public async Task RegistrarEnvioAsync(EventoNotificacaoTipo tipo, Guid chave, CancellationToken cancellationToken)
    {
        var id = Chave(tipo, chave);
        var controle = await _db.ControlesEnvioNotificacao.FirstOrDefaultAsync(c => c.Chave == id, cancellationToken).ConfigureAwait(false);
        if (controle is null)
        {
            _db.ControlesEnvioNotificacao.Add(new ControleEnvioNotificacao { Chave = id, UltimoEnvioEmUtc = DateTime.UtcNow });
        }
        else
        {
            controle.UltimoEnvioEmUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static string Chave(EventoNotificacaoTipo tipo, Guid chave) => $"{tipo}:{chave:N}";
}
