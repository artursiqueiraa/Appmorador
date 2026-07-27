using AppMorador.Application.Common;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Notificacoes;

public sealed class DispositivoPushServico : IDispositivoPushServico
{
    private readonly IDispositivoPushRepositorio _dispositivos;

    public DispositivoPushServico(IDispositivoPushRepositorio dispositivos)
    {
        _dispositivos = dispositivos;
    }

    public async Task<DispositivoPushResponse> RegistrarAsync(Guid usuarioId, RegistrarDispositivoPushRequest request, CancellationToken cancellationToken)
    {
        // Sprint 19 — o mesmo token físico pode reaparecer (reinstalação do app, ou
        // login com outra conta no mesmo aparelho): nunca duplica, sempre reassume o
        // registro existente para o usuário atual — evita um dispositivo "fantasma"
        // continuar registrado para a conta anterior depois de um logout/login.
        var existente = await _dispositivos.GetByTokenAsync(request.Token, cancellationToken).ConfigureAwait(false);
        var agora = DateTime.UtcNow;

        if (existente is not null)
        {
            existente.UsuarioId = usuarioId;
            existente.PropriedadeId = request.PropriedadeId;
            existente.Plataforma = request.Plataforma;
            existente.Modelo = request.Modelo;
            existente.VersaoApp = request.VersaoApp;
            existente.Ativo = true;
            existente.UltimoUsoUtc = agora;
            await _dispositivos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return DispositivoPushResponse.FromEntity(existente);
        }

        var novo = new DispositivoPush
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuarioId,
            PropriedadeId = request.PropriedadeId,
            Plataforma = request.Plataforma,
            Token = request.Token,
            Modelo = request.Modelo,
            VersaoApp = request.VersaoApp,
            Ativo = true,
            UltimoUsoUtc = agora,
            CreatedAtUtc = agora,
        };

        await _dispositivos.AddAsync(novo, cancellationToken).ConfigureAwait(false);
        await _dispositivos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return DispositivoPushResponse.FromEntity(novo);
    }

    public async Task<Result<DispositivoPushResponse>> AtualizarTokenAsync(Guid usuarioId, Guid id, AtualizarDispositivoPushRequest request, CancellationToken cancellationToken)
    {
        var dispositivo = await _dispositivos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dispositivo is null || dispositivo.UsuarioId != usuarioId)
        {
            return Result<DispositivoPushResponse>.Fail("Dispositivo não encontrado.");
        }

        // Sprint 19 (Fase 6.2) — o FCM pode rotacionar o token a qualquer momento; o
        // Mobile detecta via listener de refresh e manda o novo token para cá.
        dispositivo.Token = request.Token;
        dispositivo.VersaoApp = request.VersaoApp ?? dispositivo.VersaoApp;
        dispositivo.Ativo = true;
        dispositivo.UltimoUsoUtc = DateTime.UtcNow;
        await _dispositivos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<DispositivoPushResponse>.Ok(DispositivoPushResponse.FromEntity(dispositivo));
    }

    public async Task<Result<DispositivoPushResponse>> AtualizarPreferenciasAsync(Guid usuarioId, Guid id, AtualizarPreferenciasDispositivoPushRequest request, CancellationToken cancellationToken)
    {
        var dispositivo = await _dispositivos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dispositivo is null || dispositivo.UsuarioId != usuarioId)
        {
            return Result<DispositivoPushResponse>.Fail("Dispositivo não encontrado.");
        }

        dispositivo.NotificarAlertas = request.NotificarAlertas;
        dispositivo.NotificarAtividades = request.NotificarAtividades;
        dispositivo.NotificarGeral = request.NotificarGeral;
        await _dispositivos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<DispositivoPushResponse>.Ok(DispositivoPushResponse.FromEntity(dispositivo));
    }

    public async Task<Result> DesativarAsync(Guid usuarioId, Guid id, CancellationToken cancellationToken)
    {
        var dispositivo = await _dispositivos.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (dispositivo is null || dispositivo.UsuarioId != usuarioId)
        {
            // Sprint 19 — desativar é chamado no logout, num momento em que o app já
            // pode ter perdido a sessão; idempotente por design (nunca falha alto o
            // suficiente para atrapalhar o fluxo de logout do morador).
            return Result.Ok();
        }

        dispositivo.Ativo = false;
        await _dispositivos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
