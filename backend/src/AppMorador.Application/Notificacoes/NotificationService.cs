using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AppMorador.Application.Notificacoes;

/// <summary>
/// Sprint 19 (ADR 0023). "Canal" (Fase 9 da missão, canais Android) é resolvido
/// aqui a partir do <see cref="EventoNotificacaoTipo"/> — cada dispositivo pode
/// desativar um canal inteiro (<see cref="DispositivoPush.NotificarAlertas"/>/
/// <see cref="DispositivoPush.NotificarAtividades"/>/<see cref="DispositivoPush.NotificarGeral"/>),
/// filtrado ANTES do envio (nunca depois — uma notificação já entregue não pode
/// ser "desfeita").
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IDispositivoPushRepositorio _dispositivos;
    private readonly INotificationProvider _provider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IPropriedadeRepositorio propriedades,
        IDispositivoPushRepositorio dispositivos,
        INotificationProvider provider,
        ILogger<NotificationService> logger)
    {
        _propriedades = propriedades;
        _dispositivos = dispositivos;
        _provider = provider;
        _logger = logger;
    }

    public async Task EnviarParaPropriedadeAsync(NotificacaoPayload payload, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(payload.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            _logger.LogWarning("Notificacao {Tipo} nao enviada: propriedade {PropriedadeId} nao encontrada", payload.EventoTipo, payload.PropriedadeId);
            return;
        }

        var dispositivos = await _dispositivos.ListAtivosByUsuarioAsync(propriedade.ProprietarioId, cancellationToken).ConfigureAwait(false);
        var elegiveis = dispositivos.Where(d => CanalHabilitado(d, payload.EventoTipo)).ToList();

        if (elegiveis.Count == 0)
        {
            _logger.LogInformation(
                "Notificacao {Tipo} nao enviada: nenhum dispositivo ativo/elegivel para o usuario {UsuarioId} ({TotalDispositivos} dispositivo(s) ativo(s), canal filtrado)",
                payload.EventoTipo, propriedade.ProprietarioId, dispositivos.Count);
            return;
        }

        var tokens = elegiveis.Select(d => d.Token).ToList();
        var resultado = await _provider.EnviarAsync(payload, tokens, cancellationToken).ConfigureAwait(false);

        if (resultado.TokensInvalidos.Count > 0)
        {
            foreach (var tokenInvalido in resultado.TokensInvalidos)
            {
                var dispositivo = elegiveis.FirstOrDefault(d => d.Token == tokenInvalido);
                if (dispositivo is not null)
                {
                    dispositivo.Ativo = false;
                }
            }

            await _dispositivos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("{Quantidade} dispositivo(s) desativado(s) por token invalido", resultado.TokensInvalidos.Count);
        }

        if (resultado.Sucesso)
        {
            _logger.LogInformation(
                "Notificacao {Tipo} enviada: {ComSucesso}/{Total} dispositivo(s), propriedade {PropriedadeId}",
                payload.EventoTipo, resultado.TokensComSucesso.Count, tokens.Count, payload.PropriedadeId);
        }
        else
        {
            _logger.LogWarning(
                "Falha ao enviar notificacao {Tipo} para propriedade {PropriedadeId}: {Erro}",
                payload.EventoTipo, payload.PropriedadeId, resultado.Erro);
        }
    }

    private static bool CanalHabilitado(DispositivoPush dispositivo, EventoNotificacaoTipo tipo) => tipo switch
    {
        EventoNotificacaoTipo.AlarmeDisparado or EventoNotificacaoTipo.EquipamentoOffline => dispositivo.NotificarAlertas,
        EventoNotificacaoTipo.ComandoAcionado or EventoNotificacaoTipo.VisitanteAutorizado or EventoNotificacaoTipo.EntregaRecebida => dispositivo.NotificarAtividades,
        EventoNotificacaoTipo.SistemaArmado or EventoNotificacaoTipo.SistemaDesarmado => dispositivo.NotificarGeral,
        _ => true,
    };
}
