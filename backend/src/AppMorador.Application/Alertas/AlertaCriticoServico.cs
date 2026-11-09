using AppMorador.Application.Rbac;
using AppMorador.Application.Common;
using AppMorador.Application.Notificacoes;
using AppMorador.Application.Operacional;
using AppMorador.Application.Sms;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AppMorador.Application.Alertas;

/// <summary>
/// Sprint 24 (ADR 0040). Regras:
/// - Abrir: um alerta aberto por (propriedade, tipo). Disparos repetidos reaproveitam o aberto.
/// - Avisar: o dono recebe push crítico na hora; o app aberto recebe pelo tempo real.
/// - Escalonar (a cada <see cref="Propriedade.MinutosEscalonamento"/> sem confirmação):
///   Dono → Membros (push para membros e de novo para o dono) → Contatos de emergência (SMS).
///   Nível sem ninguém é pulado. Depois do último nível o escalonamento termina.
/// - Confirmar: dono ou membro ativo; encerra o escalonamento.
/// Avisos (push, SMS, tempo real) são sempre "melhor esforço": uma falha neles nunca desfaz o
/// alerta já gravado.
/// </summary>
public sealed class AlertaCriticoServico : IAlertaCriticoServico
{
    private readonly IAcessoPropriedadeServico _acesso;

    public const int MinutosEscalonamentoMinimo = 1;
    public const int MinutosEscalonamentoMaximo = 30;

    // Brasil sem horário de verão desde 2019: o horário de Brasília é sempre UTC-3.
    private static readonly TimeSpan FusoBrasilia = TimeSpan.FromHours(-3);

    private readonly IAlertaCriticoRepositorio _alertas;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioPropriedadeRepositorio _usuariosPropriedade;
    private readonly IContatoEmergenciaRepositorio _contatos;
    private readonly INotificationDispatcher _notificacoes;
    private readonly IEnvioSms _sms;
    private readonly IOperacionalEventoPublicador _publicador;
    private readonly ILogger<AlertaCriticoServico> _logger;

    public AlertaCriticoServico(
        IAlertaCriticoRepositorio alertas,
        IPropriedadeRepositorio propriedades,
        IUsuarioPropriedadeRepositorio usuariosPropriedade,
        IContatoEmergenciaRepositorio contatos,
        INotificationDispatcher notificacoes,
        IEnvioSms sms,
        IOperacionalEventoPublicador publicador,
        ILogger<AlertaCriticoServico> logger,
        IAcessoPropriedadeServico acesso)
    {
        _acesso = acesso;
        _alertas = alertas;
        _propriedades = propriedades;
        _usuariosPropriedade = usuariosPropriedade;
        _contatos = contatos;
        _notificacoes = notificacoes;
        _sms = sms;
        _publicador = publicador;
        _logger = logger;
    }

    public async Task<AlertaCriticoResponse?> CriarAsync(NovoAlertaCritico novo, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(novo.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            _logger.LogWarning("Alerta critico {Tipo} descartado: propriedade {PropriedadeId} nao encontrada", novo.Tipo, novo.PropriedadeId);
            return null;
        }

        var aberto = await _alertas.GetAbertoAsync(novo.PropriedadeId, novo.Tipo, cancellationToken).ConfigureAwait(false);
        if (aberto is not null)
        {
            _logger.LogInformation(
                "Alerta critico {Tipo} ja aberto na propriedade {PropriedadeId} ({AlertaId}) — evento novo agregado a ele",
                novo.Tipo, novo.PropriedadeId, aberto.Id);
            return AlertaCriticoResponse.FromEntity(aberto);
        }

        var agora = DateTime.UtcNow;
        var alerta = new AlertaCritico
        {
            Id = Guid.NewGuid(),
            PropriedadeId = novo.PropriedadeId,
            Tipo = novo.Tipo,
            Descricao = novo.Descricao,
            OcorrenciaId = novo.OcorrenciaId,
            EquipamentoId = novo.EquipamentoId,
            AcionadoPorUsuarioId = novo.AcionadoPorUsuarioId,
            CriadoEmUtc = agora,
            NivelEscalonamento = NivelEscalonamentoAlerta.Dono,
            ProximoEscalonamentoEmUtc = agora.AddMinutes(MinutosValidos(propriedade.MinutosEscalonamento)),
        };

        await _alertas.AddAsync(alerta, cancellationToken).ConfigureAwait(false);
        await _alertas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogWarning(
            "Alerta critico {AlertaId} aberto: {Tipo} na propriedade {PropriedadeId} ({Descricao})",
            alerta.Id, alerta.Tipo, alerta.PropriedadeId, alerta.Descricao);

        var response = AlertaCriticoResponse.FromEntity(alerta);
        await PublicarAsync(response, cancellationToken).ConfigureAwait(false);
        await AvisarAsync(TipoNotificacao(alerta.Tipo), alerta, propriedade, usuarioIds: null, cancellationToken).ConfigureAwait(false);
        return response;
    }

    public async Task<Result<IReadOnlyList<AlertaCriticoResponse>>> ListarAsync(
        Guid usuarioId, Guid propriedadeId, bool apenasAbertos, CancellationToken cancellationToken)
    {
        var propriedade = await ObterComAcessoAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<IReadOnlyList<AlertaCriticoResponse>>.Fail("Propriedade não encontrada.");
        }

        var alertas = await _alertas.ListByPropriedadeAsync(propriedadeId, apenasAbertos, 50, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<AlertaCriticoResponse>>.Ok(alertas.Select(AlertaCriticoResponse.FromEntity).ToList());
    }

    public async Task<Result<AlertaCriticoResponse>> ObterAsync(Guid usuarioId, Guid alertaId, CancellationToken cancellationToken)
    {
        var alerta = await _alertas.GetByIdAsync(alertaId, cancellationToken).ConfigureAwait(false);
        if (alerta is null || await ObterComAcessoAsync(usuarioId, alerta.PropriedadeId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<AlertaCriticoResponse>.Fail("Alerta não encontrado.");
        }

        return Result<AlertaCriticoResponse>.Ok(AlertaCriticoResponse.FromEntity(alerta));
    }

    public async Task<Result<AlertaCriticoResponse>> ConfirmarAsync(Guid usuarioId, Guid alertaId, CancellationToken cancellationToken)
    {
        var alerta = await _alertas.GetByIdAsync(alertaId, cancellationToken).ConfigureAwait(false);
        if (alerta is null || await ObterComAcessoAsync(usuarioId, alerta.PropriedadeId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result<AlertaCriticoResponse>.Fail("Alerta não encontrado.");
        }

        if (!alerta.Confirmado)
        {
            alerta.ConfirmadoEmUtc = DateTime.UtcNow;
            alerta.ConfirmadoPorUsuarioId = usuarioId;
            alerta.ProximoEscalonamentoEmUtc = null;
            await _alertas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Recarrega para trazer o nome de quem confirmou (navegação ConfirmadoPorUsuario).
            alerta = await _alertas.GetByIdAsync(alertaId, cancellationToken).ConfigureAwait(false) ?? alerta;
            _logger.LogInformation("Alerta critico {AlertaId} confirmado pelo usuario {UsuarioId}", alertaId, usuarioId);

            await PublicarAsync(AlertaCriticoResponse.FromEntity(alerta), cancellationToken).ConfigureAwait(false);
        }

        return Result<AlertaCriticoResponse>.Ok(AlertaCriticoResponse.FromEntity(alerta));
    }

    public async Task<Result<AlertaCriticoResponse>> AcionarPanicoAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await ObterComAcessoAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<AlertaCriticoResponse>.Fail("Propriedade não encontrada.");
        }

        var alerta = await CriarAsync(
            new NovoAlertaCritico
            {
                PropriedadeId = propriedadeId,
                Tipo = TipoAlertaCritico.Panico,
                Descricao = "Botão de pânico acionado pelo app",
                AcionadoPorUsuarioId = usuarioId,
            },
            cancellationToken).ConfigureAwait(false);

        return alerta is null
            ? Result<AlertaCriticoResponse>.Fail("Propriedade não encontrada.")
            : Result<AlertaCriticoResponse>.Ok(alerta);
    }

    public async Task EncerrarEscalonamentoAsync(Guid propriedadeId, TipoAlertaCritico tipo, CancellationToken cancellationToken)
    {
        var aberto = await _alertas.GetAbertoAsync(propriedadeId, tipo, cancellationToken).ConfigureAwait(false);
        if (aberto?.ProximoEscalonamentoEmUtc is null)
        {
            return;
        }

        aberto.ProximoEscalonamentoEmUtc = null;
        await _alertas.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Escalonamento do alerta critico {AlertaId} encerrado ({Tipo} normalizado)", aberto.Id, tipo);
        await PublicarAsync(AlertaCriticoResponse.FromEntity(aberto), cancellationToken).ConfigureAwait(false);
    }

    public async Task<int> EscalonarVencidosAsync(DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var vencidos = await _alertas.ListEscalonamentoVencidoAsync(agoraUtc, 50, cancellationToken).ConfigureAwait(false);
        var avancados = 0;

        foreach (var alerta in vencidos)
        {
            try
            {
                if (await EscalonarAsync(alerta, agoraUtc, cancellationToken).ConfigureAwait(false))
                {
                    avancados++;
                }
            }
            catch (Exception ex)
            {
                // Um alerta com problema não pode travar o escalonamento dos outros.
                _logger.LogError(ex, "Falha ao escalonar alerta critico {AlertaId}", alerta.Id);
            }
        }

        return avancados;
    }

    public async Task<Result<ConfiguracaoAlertasResponse>> ObterConfiguracaoAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await ObterComAcessoAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        return propriedade is null
            ? Result<ConfiguracaoAlertasResponse>.Fail("Propriedade não encontrada.")
            : Result<ConfiguracaoAlertasResponse>.Ok(new ConfiguracaoAlertasResponse { MinutosEscalonamento = propriedade.MinutosEscalonamento });
    }

    public async Task<Result<ConfiguracaoAlertasResponse>> AtualizarConfiguracaoAsync(
        Guid usuarioId, Guid propriedadeId, AtualizarConfiguracaoAlertasRequest request, CancellationToken cancellationToken)
    {
        // Só o dono muda a configuração (membros podem confirmar alertas, não mudar as regras).
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null || !_acesso.Pode(usuarioId, propriedade, NivelAcesso.Alterar))
        {
            return Result<ConfiguracaoAlertasResponse>.Fail("Propriedade não encontrada.");
        }

        if (request.MinutosEscalonamento is < MinutosEscalonamentoMinimo or > MinutosEscalonamentoMaximo)
        {
            return Result<ConfiguracaoAlertasResponse>.Fail(
                $"Escolha um tempo entre {MinutosEscalonamentoMinimo} e {MinutosEscalonamentoMaximo} minutos.");
        }

        propriedade.MinutosEscalonamento = request.MinutosEscalonamento;
        await _propriedades.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<ConfiguracaoAlertasResponse>.Ok(new ConfiguracaoAlertasResponse { MinutosEscalonamento = propriedade.MinutosEscalonamento });
    }

    private async Task<bool> EscalonarAsync(AlertaCritico alerta, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(alerta.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            await _alertas.TentarAvancarNivelAsync(alerta.Id, alerta.NivelEscalonamento, alerta.NivelEscalonamento, null, agoraUtc, cancellationToken)
                .ConfigureAwait(false);
            return false;
        }

        var membros = await ListarMembrosAsync(propriedade, cancellationToken).ConfigureAwait(false);
        var contatos = await _contatos.ListByPropriedadeAsync(propriedade.Id, cancellationToken).ConfigureAwait(false);
        var intervalo = TimeSpan.FromMinutes(MinutosValidos(propriedade.MinutosEscalonamento));

        // Próximo nível com alguém para avisar; nível vazio é pulado.
        NivelEscalonamentoAlerta? proximo = alerta.NivelEscalonamento switch
        {
            NivelEscalonamentoAlerta.Dono when membros.Count > 0 => NivelEscalonamentoAlerta.Membros,
            NivelEscalonamentoAlerta.Dono or NivelEscalonamentoAlerta.Membros when contatos.Count > 0 => NivelEscalonamentoAlerta.ContatosEmergencia,
            _ => null,
        };

        if (proximo is null)
        {
            // Ninguém mais para avisar: encerra o escalonamento (o alerta continua aberto até alguém confirmar).
            await _alertas.TentarAvancarNivelAsync(alerta.Id, alerta.NivelEscalonamento, alerta.NivelEscalonamento, null, agoraUtc, cancellationToken)
                .ConfigureAwait(false);
            _logger.LogInformation("Alerta critico {AlertaId} sem proximo nivel para escalonar (sem membros/contatos)", alerta.Id);
            return false;
        }

        // Depois de Membros ainda há Contatos (se existirem); depois de Contatos, fim.
        DateTime? proximoEm = proximo == NivelEscalonamentoAlerta.Membros && contatos.Count > 0 ? agoraUtc + intervalo : null;

        var avancou = await _alertas
            .TentarAvancarNivelAsync(alerta.Id, alerta.NivelEscalonamento, proximo.Value, proximoEm, agoraUtc, cancellationToken)
            .ConfigureAwait(false);
        if (!avancou)
        {
            return false; // outra execução já avançou ou alguém confirmou no meio
        }

        _logger.LogWarning("Alerta critico {AlertaId} sem confirmacao: escalonado para {Nivel}", alerta.Id, proximo);

        var usuariosApp = membros.Append(propriedade.ProprietarioId).ToList();
        await AvisarAsync(EventoNotificacaoTipo.AlertaEscalonado, alerta, propriedade, usuariosApp, cancellationToken).ConfigureAwait(false);

        if (proximo == NivelEscalonamentoAlerta.ContatosEmergencia)
        {
            await EnviarSmsAsync(alerta, propriedade, contatos, cancellationToken).ConfigureAwait(false);
        }

        var atualizado = await _alertas.GetByIdAsync(alerta.Id, cancellationToken).ConfigureAwait(false);
        if (atualizado is not null)
        {
            await PublicarAsync(AlertaCriticoResponse.FromEntity(atualizado), cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private async Task EnviarSmsAsync(
        AlertaCritico alerta, Propriedade propriedade, IReadOnlyList<ContatoEmergencia> contatos, CancellationToken cancellationToken)
    {
        var hora = (alerta.CriadoEmUtc + FusoBrasilia).ToString("HH:mm");
        var mensagem =
            $"Akteck Seg: {AlertaCriticoResponse.TituloDe(alerta.Tipo)} em {propriedade.Nome} às {hora} e ninguém confirmou ainda. " +
            "Você é contato de emergência deste local. Tente falar com o responsável; em perigo, ligue 190.";

        foreach (var contato in contatos)
        {
            try
            {
                var enviado = await _sms.EnviarAsync(contato.Telefone, mensagem, cancellationToken).ConfigureAwait(false);
                if (!enviado)
                {
                    _logger.LogWarning("SMS do alerta {AlertaId} nao aceito pelo provedor para o contato {ContatoId}", alerta.Id, contato.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar SMS do alerta {AlertaId} para o contato {ContatoId}", alerta.Id, contato.Id);
            }
        }
    }

    private async Task AvisarAsync(
        EventoNotificacaoTipo tipo, AlertaCritico alerta, Propriedade propriedade, IReadOnlyCollection<Guid>? usuarioIds, CancellationToken cancellationToken)
    {
        try
        {
            var contexto = new ContextoNotificacao
            {
                PropriedadeId = propriedade.Id,
                NomePropriedade = propriedade.Nome,
                EquipamentoId = alerta.EquipamentoId,
                AlertaId = alerta.Id,
                Descricao = alerta.Descricao,
            };

            if (usuarioIds is null)
            {
                await _notificacoes.NotificarAsync(tipo, contexto, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _notificacoes.NotificarUsuariosAsync(tipo, contexto, usuarioIds, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao notificar alerta critico {AlertaId} ({Tipo})", alerta.Id, tipo);
        }
    }

    private async Task PublicarAsync(AlertaCriticoResponse alerta, CancellationToken cancellationToken)
    {
        try
        {
            await _publicador.PublicarAlertaCriticoAsync(alerta.PropriedadeId, alerta, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao publicar alerta critico {AlertaId} em tempo real", alerta.Id);
        }
    }

    /// <summary>Dono ou membro ativo da propriedade (Sprint 21, UsuarioPropriedade).</summary>
    private async Task<Propriedade?> ObterComAcessoAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return null;
        }

        if (propriedade.ProprietarioId == usuarioId)
        {
            return propriedade;
        }

        var vinculo = await _usuariosPropriedade.GetAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        return vinculo is { Ativo: true } ? propriedade : null;
    }

    private async Task<IReadOnlyList<Guid>> ListarMembrosAsync(Propriedade propriedade, CancellationToken cancellationToken)
    {
        var vinculos = await _usuariosPropriedade.ListByPropriedadeAsync(propriedade.Id, cancellationToken).ConfigureAwait(false);
        return vinculos
            .Where(v => v.Ativo && v.UsuarioId != propriedade.ProprietarioId)
            .Select(v => v.UsuarioId)
            .Distinct()
            .ToList();
    }

    private static int MinutosValidos(int minutos) =>
        Math.Clamp(minutos, MinutosEscalonamentoMinimo, MinutosEscalonamentoMaximo);

    private static EventoNotificacaoTipo TipoNotificacao(TipoAlertaCritico tipo) => tipo switch
    {
        TipoAlertaCritico.Panico => EventoNotificacaoTipo.PanicoAcionado,
        TipoAlertaCritico.Violacao => EventoNotificacaoTipo.ViolacaoDetectada,
        TipoAlertaCritico.SemComunicacao => EventoNotificacaoTipo.CentralSemComunicacao,
        _ => EventoNotificacaoTipo.AlarmeDisparado,
    };
}
