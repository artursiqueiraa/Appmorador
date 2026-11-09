using AppMorador.Application.Alertas;
using AppMorador.Application.Auditoria;
using AppMorador.Application.Common;
using AppMorador.Application.Eventos;
using AppMorador.Application.Notificacoes;
using AppMorador.Application.Operacional;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel.Simulador;

public enum TipoEventoSimulado
{
    Disparo,
    Panico,
    Violacao,
    SemComunicacao,
    FalhaEnergia,
    BateriaBaixa,
}

public sealed class SimularEventoRequest
{
    public TipoEventoSimulado Tipo { get; init; }
}

public sealed class SimularEventoResponse
{
    public required string Mensagem { get; init; }

    /// <summary>Alerta crítico aberto (ou o que já estava aberto), quando o tipo gera alerta.</summary>
    public Guid? AlertaId { get; init; }
}

public interface ISimuladorEventosServico
{
    Task<Result<SimularEventoResponse>> SimularAsync(
        Guid usuarioId, Guid propriedadeId, SimularEventoRequest request, string? ipAddress, CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 24 (ADR 0040) — simulador de eventos do painel (só equipe interna), para homologar
/// alertas sem central física. Passa pelos mesmos serviços de uma central real (alerta crítico,
/// escalonamento, push, tempo real), mas NUNCA cria uma Ocorrência falsa da central: todo texto
/// sai marcado "Teste:", e cada simulação fica na auditoria com quem fez.
/// </summary>
public sealed class SimuladorEventosServico : ISimuladorEventosServico
{
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IAlertaCriticoServico _alertas;
    private readonly INotificationDispatcher _notificacoes;
    private readonly IOperacionalEventoPublicador _publicador;
    private readonly IAuditoriaService _auditoria;

    public SimuladorEventosServico(
        IPropriedadeRepositorio propriedades,
        IUsuarioRepositorio usuarios,
        IAlertaCriticoServico alertas,
        INotificationDispatcher notificacoes,
        IOperacionalEventoPublicador publicador,
        IAuditoriaService auditoria)
    {
        _propriedades = propriedades;
        _usuarios = usuarios;
        _alertas = alertas;
        _notificacoes = notificacoes;
        _publicador = publicador;
        _auditoria = auditoria;
    }

    public async Task<Result<SimularEventoResponse>> SimularAsync(
        Guid usuarioId, Guid propriedadeId, SimularEventoRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<SimularEventoResponse>.Fail("Propriedade não encontrada.");
        }

        if (!Enum.IsDefined(request.Tipo))
        {
            return Result<SimularEventoResponse>.Fail("Tipo de evento inválido.");
        }

        var resposta = request.Tipo switch
        {
            TipoEventoSimulado.FalhaEnergia => await NotificarAsync(propriedade, EventoNotificacaoTipo.FalhaEnergia, "Teste: falta de energia", cancellationToken).ConfigureAwait(false),
            TipoEventoSimulado.BateriaBaixa => await NotificarAsync(propriedade, EventoNotificacaoTipo.BateriaBaixa, "Teste: bateria da central fraca", cancellationToken).ConfigureAwait(false),
            _ => await AbrirAlertaAsync(propriedade, request.Tipo, cancellationToken).ConfigureAwait(false),
        };

        var usuarioNome = (await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false))?.Nome ?? "(desconhecido)";
        await _auditoria.RegistrarAsync(
            usuarioId, usuarioNome, TipoAcaoAuditoria.SimularEvento, "Propriedade", propriedade.Id.ToString(),
            $"Simulação de evento {request.Tipo} em {propriedade.Nome}", ipAddress, cancellationToken).ConfigureAwait(false);

        return Result<SimularEventoResponse>.Ok(resposta);
    }

    private async Task<SimularEventoResponse> AbrirAlertaAsync(Propriedade propriedade, TipoEventoSimulado tipo, CancellationToken cancellationToken)
    {
        var (tipoAlerta, descricao) = tipo switch
        {
            TipoEventoSimulado.Panico => (TipoAlertaCritico.Panico, "Teste: pânico acionado pelo painel"),
            TipoEventoSimulado.Violacao => (TipoAlertaCritico.Violacao, "Teste: tentativa de abrir a central de alarme"),
            TipoEventoSimulado.SemComunicacao => (TipoAlertaCritico.SemComunicacao, "Teste: a central parou de se comunicar"),
            _ => (TipoAlertaCritico.Disparo, "Teste: movimento detectado"),
        };

        var antes = DateTime.UtcNow;
        var alerta = await _alertas.CriarAsync(
            new NovoAlertaCritico { PropriedadeId = propriedade.Id, Tipo = tipoAlerta, Descricao = descricao },
            cancellationToken).ConfigureAwait(false);

        if (alerta is null)
        {
            return new SimularEventoResponse { Mensagem = "Não foi possível abrir o alerta de teste." };
        }

        var jaExistia = alerta.CriadoEmUtc < antes.AddSeconds(-1);
        return new SimularEventoResponse
        {
            AlertaId = alerta.Id,
            Mensagem = jaExistia
                ? "Já existe um alerta deste tipo aberto nesta propriedade. Confirme-o no app (\"Estou ciente\") para simular de novo."
                : "Alerta de teste enviado. Com o app aberto na propriedade, a tela de alerta aparece na hora.",
        };
    }

    private async Task<SimularEventoResponse> NotificarAsync(
        Propriedade propriedade, EventoNotificacaoTipo tipo, string descricao, CancellationToken cancellationToken)
    {
        await _notificacoes.NotificarAsync(
            tipo,
            new ContextoNotificacao { PropriedadeId = propriedade.Id, NomePropriedade = propriedade.Nome, Descricao = descricao },
            cancellationToken).ConfigureAwait(false);

        // Aparece ao vivo na "Atividade recente" do app; não é gravado (não houve evento de central).
        await _publicador.PublicarNovoEventoAsync(
            propriedade.Id,
            new EventoResponse { Id = Guid.NewGuid(), Titulo = descricao, Descricao = null, OcorridoEmUtc = DateTime.UtcNow, Destaque = false },
            cancellationToken).ConfigureAwait(false);

        return new SimularEventoResponse
        {
            Mensagem = "Notificação de teste enviada. Repetições do mesmo tipo em menos de 1 minuto são agrupadas.",
        };
    }
}
