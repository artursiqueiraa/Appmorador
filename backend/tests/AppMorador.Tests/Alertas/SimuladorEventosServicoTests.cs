using AppMorador.Application.Alertas;
using AppMorador.Application.Auditoria;
using AppMorador.Application.Eventos;
using AppMorador.Application.Notificacoes;
using AppMorador.Application.Operacional;
using AppMorador.Application.Painel.Simulador;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Alertas;

public class SimuladorEventosServicoTests
{
    private readonly Propriedade _propriedade = new() { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = Guid.NewGuid() };
    private readonly Mock<IPropriedadeRepositorio> _propriedades = new();
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Mock<IAlertaCriticoServico> _alertas = new();
    private readonly Mock<INotificationDispatcher> _notificacoes = new();
    private readonly Mock<IOperacionalEventoPublicador> _publicador = new();
    private readonly Mock<IAuditoriaService> _auditoria = new();
    private readonly SimuladorEventosServico _servico;

    public SimuladorEventosServicoTests()
    {
        _propriedades.Setup(p => p.GetByIdAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_propriedade);
        _servico = new SimuladorEventosServico(_propriedades.Object, _usuarios.Object, _alertas.Object, _notificacoes.Object, _publicador.Object, _auditoria.Object);
    }

    private static AlertaCriticoResponse Resposta(DateTime criadoEm) => new()
    {
        Id = Guid.NewGuid(),
        PropriedadeId = Guid.NewGuid(),
        Tipo = TipoAlertaCritico.Panico,
        Titulo = "Pânico acionado",
        Descricao = "Teste",
        CriadoEmUtc = criadoEm,
        Confirmado = false,
        NivelEscalonamento = NivelEscalonamentoAlerta.Dono,
    };

    [Fact]
    public async Task Panico_AbreAlertaMarcadoComoTeste_ERegistraAuditoria()
    {
        _alertas.Setup(a => a.CriarAsync(It.IsAny<NovoAlertaCritico>(), It.IsAny<CancellationToken>())).ReturnsAsync(Resposta(DateTime.UtcNow));

        var result = await _servico.SimularAsync(Guid.NewGuid(), _propriedade.Id, new SimularEventoRequest { Tipo = TipoEventoSimulado.Panico }, "127.0.0.1", CancellationToken.None);

        Assert.True(result.Success);
        _alertas.Verify(a => a.CriarAsync(
            It.Is<NovoAlertaCritico>(n => n.Tipo == TipoAlertaCritico.Panico && n.Descricao.StartsWith("Teste:")), It.IsAny<CancellationToken>()), Times.Once);
        _auditoria.Verify(a => a.RegistrarAsync(It.IsAny<Guid>(), It.IsAny<string>(), TipoAcaoAuditoria.SimularEvento, "Propriedade",
            _propriedade.Id.ToString(), It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AlertaJaAberto_AvisaQuePrecisaConfirmarAntes()
    {
        _alertas.Setup(a => a.CriarAsync(It.IsAny<NovoAlertaCritico>(), It.IsAny<CancellationToken>())).ReturnsAsync(Resposta(DateTime.UtcNow.AddMinutes(-5)));

        var result = await _servico.SimularAsync(Guid.NewGuid(), _propriedade.Id, new SimularEventoRequest { Tipo = TipoEventoSimulado.Disparo }, null, CancellationToken.None);

        Assert.Contains("Já existe um alerta", result.Data!.Mensagem);
    }

    [Fact]
    public async Task FalhaEnergia_NotificaSemAbrirAlerta()
    {
        var result = await _servico.SimularAsync(Guid.NewGuid(), _propriedade.Id, new SimularEventoRequest { Tipo = TipoEventoSimulado.FalhaEnergia }, null, CancellationToken.None);

        Assert.True(result.Success);
        _notificacoes.Verify(n => n.NotificarAsync(EventoNotificacaoTipo.FalhaEnergia,
            It.Is<ContextoNotificacao>(c => c.Descricao!.StartsWith("Teste:")), It.IsAny<CancellationToken>()), Times.Once);
        _publicador.Verify(p => p.PublicarNovoEventoAsync(_propriedade.Id, It.IsAny<EventoResponse>(), It.IsAny<CancellationToken>()), Times.Once);
        _alertas.Verify(a => a.CriarAsync(It.IsAny<NovoAlertaCritico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PropriedadeInexistente_Falha()
    {
        var result = await _servico.SimularAsync(Guid.NewGuid(), Guid.NewGuid(), new SimularEventoRequest { Tipo = TipoEventoSimulado.Panico }, null, CancellationToken.None);

        Assert.False(result.Success);
    }
}
