using AppMorador.Application.Rbac;
using AppMorador.Application.Alertas;
using AppMorador.Application.Notificacoes;
using AppMorador.Application.Operacional;
using AppMorador.Application.Sms;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AppMorador.Tests.Alertas;

public class AlertaCriticoServicoTests
{
    private sealed class Cenario
    {
        public readonly Guid DonoId = Guid.NewGuid();
        public readonly Propriedade Propriedade;
        public readonly Mock<IAlertaCriticoRepositorio> Alertas = new();
        public readonly Mock<IPropriedadeRepositorio> Propriedades = new();
        public readonly Mock<IUsuarioPropriedadeRepositorio> Vinculos = new();
        public readonly Mock<IContatoEmergenciaRepositorio> Contatos = new();
        public readonly Mock<INotificationDispatcher> Notificacoes = new();
        public readonly Mock<IEnvioSms> Sms = new();
        public readonly Mock<IOperacionalEventoPublicador> Publicador = new();
        public readonly AlertaCriticoServico Servico;

        public Cenario(List<UsuarioPropriedade>? vinculos = null, List<ContatoEmergencia>? contatos = null)
        {
            Propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa Serra", Tipo = TipoPropriedade.Residencial, ProprietarioId = DonoId, MinutosEscalonamento = 2 };
            Propriedades.Setup(p => p.GetByIdAsync(Propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Propriedade);
            Vinculos.Setup(v => v.ListByPropriedadeAsync(Propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vinculos ?? []);
            Contatos.Setup(c => c.ListByPropriedadeAsync(Propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(contatos ?? []);
            Sms.Setup(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            Servico = new AlertaCriticoServico(
                Alertas.Object, Propriedades.Object, Vinculos.Object, Contatos.Object,
                Notificacoes.Object, Sms.Object, Publicador.Object, NullLogger<AlertaCriticoServico>.Instance, new AcessoPropriedadeServico());
        }

        public AlertaCritico AlertaAberto(NivelEscalonamentoAlerta nivel = NivelEscalonamentoAlerta.Dono)
        {
            var alerta = new AlertaCritico
            {
                Id = Guid.NewGuid(),
                PropriedadeId = Propriedade.Id,
                Tipo = TipoAlertaCritico.Disparo,
                Descricao = "Movimento detectado — Sala",
                CriadoEmUtc = DateTime.UtcNow.AddMinutes(-3),
                NivelEscalonamento = nivel,
                ProximoEscalonamentoEmUtc = DateTime.UtcNow.AddMinutes(-1),
            };
            Alertas.Setup(a => a.GetByIdAsync(alerta.Id, It.IsAny<CancellationToken>())).ReturnsAsync(alerta);
            Alertas.Setup(a => a.ListEscalonamentoVencidoAsync(It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([alerta]);
            return alerta;
        }
    }

    private static NovoAlertaCritico Novo(Guid propriedadeId, TipoAlertaCritico tipo = TipoAlertaCritico.Disparo) => new()
    {
        PropriedadeId = propriedadeId,
        Tipo = tipo,
        Descricao = "Movimento detectado — Sala",
    };

    [Fact]
    public async Task CriarAsync_AbreAlerta_AgendaEscalonamento_NotificaDonoEPublicaTempoReal()
    {
        var c = new Cenario();
        AlertaCritico? gravado = null;
        c.Alertas.Setup(a => a.AddAsync(It.IsAny<AlertaCritico>(), It.IsAny<CancellationToken>())).Callback<AlertaCritico, CancellationToken>((a, _) => gravado = a);

        var resposta = await c.Servico.CriarAsync(Novo(c.Propriedade.Id), CancellationToken.None);

        Assert.NotNull(resposta);
        Assert.NotNull(gravado);
        Assert.Equal(NivelEscalonamentoAlerta.Dono, gravado!.NivelEscalonamento);
        Assert.InRange(gravado.ProximoEscalonamentoEmUtc!.Value, DateTime.UtcNow.AddMinutes(1.9), DateTime.UtcNow.AddMinutes(2.1));
        c.Notificacoes.Verify(n => n.NotificarAsync(
            EventoNotificacaoTipo.AlarmeDisparado,
            It.Is<ContextoNotificacao>(x => x.AlertaId == gravado.Id && x.Descricao == "Movimento detectado — Sala"),
            It.IsAny<CancellationToken>()), Times.Once);
        c.Publicador.Verify(p => p.PublicarAlertaCriticoAsync(c.Propriedade.Id, It.IsAny<AlertaCriticoResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CriarAsync_ComAlertaAbertoDoMesmoTipo_ReaproveitaSemNotificarDeNovo()
    {
        var c = new Cenario();
        var aberto = c.AlertaAberto();
        c.Alertas.Setup(a => a.GetAbertoAsync(c.Propriedade.Id, TipoAlertaCritico.Disparo, It.IsAny<CancellationToken>())).ReturnsAsync(aberto);

        var resposta = await c.Servico.CriarAsync(Novo(c.Propriedade.Id), CancellationToken.None);

        Assert.Equal(aberto.Id, resposta!.Id);
        c.Alertas.Verify(a => a.AddAsync(It.IsAny<AlertaCritico>(), It.IsAny<CancellationToken>()), Times.Never);
        c.Notificacoes.Verify(n => n.NotificarAsync(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<ContextoNotificacao>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CriarAsync_FalhaNoPush_NaoDesfazOAlerta()
    {
        var c = new Cenario();
        c.Notificacoes
            .Setup(n => n.NotificarAsync(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<ContextoNotificacao>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("firebase fora"));

        var resposta = await c.Servico.CriarAsync(Novo(c.Propriedade.Id), CancellationToken.None);

        Assert.NotNull(resposta);
        c.Alertas.Verify(a => a.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcionarPanicoAsync_RegistraAutorENotificaComoPanico()
    {
        var c = new Cenario();
        AlertaCritico? gravado = null;
        c.Alertas.Setup(a => a.AddAsync(It.IsAny<AlertaCritico>(), It.IsAny<CancellationToken>())).Callback<AlertaCritico, CancellationToken>((a, _) => gravado = a);

        var result = await c.Servico.AcionarPanicoAsync(c.DonoId, c.Propriedade.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(TipoAlertaCritico.Panico, gravado!.Tipo);
        Assert.Equal(c.DonoId, gravado.AcionadoPorUsuarioId);
        Assert.True(result.Data!.AcionadoPeloApp);
        c.Notificacoes.Verify(n => n.NotificarAsync(EventoNotificacaoTipo.PanicoAcionado, It.IsAny<ContextoNotificacao>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcionarPanicoAsync_UsuarioSemVinculo_Falha()
    {
        var c = new Cenario();

        var result = await c.Servico.AcionarPanicoAsync(Guid.NewGuid(), c.Propriedade.Id, CancellationToken.None);

        Assert.False(result.Success);
        c.Alertas.Verify(a => a.AddAsync(It.IsAny<AlertaCritico>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConfirmarAsync_RegistraQuemEQuando_EEncerraEscalonamento()
    {
        var c = new Cenario();
        var alerta = c.AlertaAberto();

        var result = await c.Servico.ConfirmarAsync(c.DonoId, alerta.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(alerta.ConfirmadoEmUtc);
        Assert.Equal(c.DonoId, alerta.ConfirmadoPorUsuarioId);
        Assert.Null(alerta.ProximoEscalonamentoEmUtc);
        c.Alertas.Verify(a => a.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_MembroAtivoPodeConfirmar()
    {
        var membroId = Guid.NewGuid();
        var c = new Cenario();
        var alerta = c.AlertaAberto();
        c.Vinculos.Setup(v => v.GetAsync(membroId, c.Propriedade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioPropriedade { UsuarioId = membroId, PropriedadeId = c.Propriedade.Id, CreatedAtUtc = DateTime.UtcNow, Ativo = true });

        var result = await c.Servico.ConfirmarAsync(membroId, alerta.Id, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(membroId, alerta.ConfirmadoPorUsuarioId);
    }

    [Fact]
    public async Task ConfirmarAsync_UsuarioDeOutraPropriedade_Falha()
    {
        var c = new Cenario();
        var alerta = c.AlertaAberto();

        var result = await c.Servico.ConfirmarAsync(Guid.NewGuid(), alerta.Id, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Null(alerta.ConfirmadoEmUtc);
    }

    [Fact]
    public async Task Escalonar_ComMembros_AvisaMembrosEDono_EAgendaContatos()
    {
        var membroId = Guid.NewGuid();
        var contato = new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Ana", Telefone = "11987654321", Ordem = 1 };
        var c = new Cenario(
            vinculos: [new UsuarioPropriedade { UsuarioId = membroId, CreatedAtUtc = DateTime.UtcNow, Ativo = true }],
            contatos: [contato]);
        var alerta = c.AlertaAberto();
        var agora = DateTime.UtcNow;
        c.Alertas.Setup(a => a.TentarAvancarNivelAsync(alerta.Id, NivelEscalonamentoAlerta.Dono, NivelEscalonamentoAlerta.Membros,
            It.IsAny<DateTime?>(), agora, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var avancados = await c.Servico.EscalonarVencidosAsync(agora, CancellationToken.None);

        Assert.Equal(1, avancados);
        c.Alertas.Verify(a => a.TentarAvancarNivelAsync(alerta.Id, NivelEscalonamentoAlerta.Dono, NivelEscalonamentoAlerta.Membros,
            agora.AddMinutes(2), agora, It.IsAny<CancellationToken>()), Times.Once);
        c.Notificacoes.Verify(n => n.NotificarUsuariosAsync(
            EventoNotificacaoTipo.AlertaEscalonado, It.IsAny<ContextoNotificacao>(),
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(membroId) && ids.Contains(c.DonoId)),
            It.IsAny<CancellationToken>()), Times.Once);
        c.Sms.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Escalonar_SemMembros_PulaDiretoParaContatos_EEnviaSms()
    {
        var c = new Cenario(contatos:
        [
            new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Ana", Telefone = "11987654321", Ordem = 1 },
            new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Beto", Telefone = "11912345678", Ordem = 2 },
        ]);
        var alerta = c.AlertaAberto();
        var agora = DateTime.UtcNow;
        c.Alertas.Setup(a => a.TentarAvancarNivelAsync(alerta.Id, NivelEscalonamentoAlerta.Dono, NivelEscalonamentoAlerta.ContatosEmergencia,
            null, agora, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await c.Servico.EscalonarVencidosAsync(agora, CancellationToken.None);

        c.Sms.Verify(s => s.EnviarAsync("11987654321", It.Is<string>(m => m.Contains("Casa Serra") && m.Contains("190")), It.IsAny<CancellationToken>()), Times.Once);
        c.Sms.Verify(s => s.EnviarAsync("11912345678", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Escalonar_SemNinguemParaAvisar_EncerraSemEnviar()
    {
        var c = new Cenario();
        var alerta = c.AlertaAberto();
        var agora = DateTime.UtcNow;

        var avancados = await c.Servico.EscalonarVencidosAsync(agora, CancellationToken.None);

        Assert.Equal(0, avancados);
        c.Alertas.Verify(a => a.TentarAvancarNivelAsync(alerta.Id, NivelEscalonamentoAlerta.Dono, NivelEscalonamentoAlerta.Dono,
            null, agora, It.IsAny<CancellationToken>()), Times.Once);
        c.Notificacoes.Verify(n => n.NotificarUsuariosAsync(It.IsAny<EventoNotificacaoTipo>(), It.IsAny<ContextoNotificacao>(),
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Escalonar_OutraExecucaoJaAvancou_NaoEnviaNada()
    {
        var c = new Cenario(contatos: [new ContatoEmergencia { Id = Guid.NewGuid(), Nome = "Ana", Telefone = "11987654321", Ordem = 1 }]);
        c.AlertaAberto();
        c.Alertas.Setup(a => a.TentarAvancarNivelAsync(It.IsAny<Guid>(), It.IsAny<NivelEscalonamentoAlerta>(), It.IsAny<NivelEscalonamentoAlerta>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var avancados = await c.Servico.EscalonarVencidosAsync(DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(0, avancados);
        c.Sms.Verify(s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public async Task AtualizarConfiguracao_AceitaSoDe1a30Minutos(int minutos, bool aceito)
    {
        var c = new Cenario();

        var result = await c.Servico.AtualizarConfiguracaoAsync(
            c.DonoId, c.Propriedade.Id, new AtualizarConfiguracaoAlertasRequest { MinutosEscalonamento = minutos }, CancellationToken.None);

        Assert.Equal(aceito, result.Success);
        Assert.Equal(aceito ? minutos : 2, c.Propriedade.MinutosEscalonamento);
    }

    [Fact]
    public async Task EncerrarEscalonamento_ParaDeAvisarMasMantemAberto()
    {
        var c = new Cenario();
        var alerta = c.AlertaAberto();
        alerta.Tipo = TipoAlertaCritico.SemComunicacao;
        c.Alertas.Setup(a => a.GetAbertoAsync(c.Propriedade.Id, TipoAlertaCritico.SemComunicacao, It.IsAny<CancellationToken>())).ReturnsAsync(alerta);

        await c.Servico.EncerrarEscalonamentoAsync(c.Propriedade.Id, TipoAlertaCritico.SemComunicacao, CancellationToken.None);

        Assert.Null(alerta.ProximoEscalonamentoEmUtc);
        Assert.False(alerta.Confirmado);
    }
}
