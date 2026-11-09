using AppMorador.Application.Autenticacao;
using AppMorador.Application.Comandos;
using AppMorador.Application.Common;
using AppMorador.Application.Rbac;
using AppMorador.Application.Saude;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Comandos;

/// <summary>Sprint 27 (ADR 0043) — PIN de confirmação, auditoria/idempotência de comandos e saúde.</summary>
public class SegurancaContaTests
{
    private sealed class HasherSimples : IPasswordHasher
    {
        public string Hash(string password) => "h:" + password;

        public bool Verify(string password, string hash) => hash == "h:" + password;
    }

    private readonly Usuario _dono = new() { Id = Guid.NewGuid(), Nome = "Ana", Email = "ana@x.com", SenhaHash = "h:Senha123", CreatedAtUtc = DateTime.UtcNow };
    private readonly Propriedade _propriedade;
    private readonly Equipamento _central;
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Mock<IComandoExecutadoRepositorio> _comandos = new();
    private readonly Mock<IEquipamentoRepositorio> _equipamentos = new();
    private readonly List<ComandoExecutado> _gravados = [];

    public SegurancaContaTests()
    {
        _propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = _dono.Id };
        _central = new Equipamento
        {
            Id = Guid.NewGuid(), PropriedadeId = _propriedade.Id, Propriedade = _propriedade, Nome = "Central", Fabricante = FabricanteEquipamento.Jfl,
            Status = StatusEquipamento.Online, CreatedAtUtc = DateTime.UtcNow,
        };
        _usuarios.Setup(u => u.GetByIdAsync(_dono.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_dono);
        _equipamentos.Setup(e => e.GetByIdAsync(_central.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_central);
        _comandos.Setup(c => c.TentarReservarAsync(It.IsAny<ComandoExecutado>(), It.IsAny<CancellationToken>()))
            .Callback<ComandoExecutado, CancellationToken>((c, _) => _gravados.Add(c))
            .ReturnsAsync(true);
    }

    private PinConfirmacaoServico Pin() => new(_usuarios.Object, new HasherSimples());

    private ExecucaoComandoServico Execucao() =>
        new(_comandos.Object, _equipamentos.Object, new Mock<IPontoAcessoRepositorio>().Object, new AcessoPropriedadeServico(), Pin());

    private SolicitacaoComando Desarmar(string? pin = null, string? chave = null, bool biometria = false) => new()
    {
        UsuarioId = _dono.Id, Tipo = TipoComando.Desarmar, EquipamentoId = _central.Id, Parametros = "partição 1", Pin = pin, ChaveIdempotencia = chave, ViaBiometria = biometria,
    };

    private static Task<Result<string>> Ok(CancellationToken _) => Task.FromResult(Result<string>.Ok("feito"));

    private static (bool, string?) Avaliar(string _) => (true, null);

    // --- PIN ---------------------------------------------------------------------------------

    [Theory]
    [InlineData("123", "O PIN precisa ter de 4 a 6 números.")]
    [InlineData("12a4", "O PIN precisa ter de 4 a 6 números.")]
    [InlineData("0000", "Escolha um PIN menos óbvio (sem números repetidos ou em sequência).")]
    [InlineData("123456", "Escolha um PIN menos óbvio (sem números repetidos ou em sequência).")]
    [InlineData("4321", "Escolha um PIN menos óbvio (sem números repetidos ou em sequência).")]
    [InlineData("2580", null)]
    public void FormatoDoPin(string pin, string? erro) => Assert.Equal(erro, PinConfirmacaoServico.ValidarFormato(pin));

    [Fact]
    public async Task DefinirPin_ExigeSenhaDaConta()
    {
        var r = await Pin().DefinirAsync(_dono.Id, new DefinirPinRequest { Senha = "errada", Pin = "2580" }, CancellationToken.None);

        Assert.Equal("Senha da conta incorreta.", r.Error);
        Assert.Null(_dono.PinConfirmacaoHash);
    }

    [Fact]
    public async Task CincoErros_BloqueiamQuinzeMinutos_EAcertoNaoPassaDuranteBloqueio()
    {
        _dono.PinConfirmacaoHash = "h:2580";
        var pin = Pin();
        for (var i = 1; i <= 4; i++)
        {
            var r = await pin.ValidarAsync(_dono.Id, "1111", CancellationToken.None);
            Assert.Equal(SituacaoValidacaoPin.Incorreto, r.Situacao);
            Assert.Equal(5 - i, r.TentativasRestantes);
        }

        Assert.Equal(SituacaoValidacaoPin.Bloqueado, (await pin.ValidarAsync(_dono.Id, "1111", CancellationToken.None)).Situacao);
        Assert.True(_dono.PinBloqueadoAteUtc > DateTime.UtcNow.AddMinutes(14));
        Assert.Equal(SituacaoValidacaoPin.Bloqueado, (await pin.ValidarAsync(_dono.Id, "2580", CancellationToken.None)).Situacao);
    }

    [Fact]
    public async Task RedefinirPin_TiraOBloqueio()
    {
        _dono.PinConfirmacaoHash = "h:2580";
        _dono.PinBloqueadoAteUtc = DateTime.UtcNow.AddMinutes(10);

        await Pin().DefinirAsync(_dono.Id, new DefinirPinRequest { Senha = "Senha123", Pin = "7391" }, CancellationToken.None);

        Assert.Equal(SituacaoValidacaoPin.Valido, (await Pin().ValidarAsync(_dono.Id, "7391", CancellationToken.None)).Situacao);
    }

    // --- Execução de comandos ----------------------------------------------------------------

    [Fact]
    public async Task SemPinCriado_DesarmaSemConfirmacao_ERegistraAutor()
    {
        var r = await Execucao().ExecutarAsync(Desarmar(), Ok, Avaliar, CancellationToken.None);

        Assert.Equal(DesfechoComando.Executado, r.Desfecho);
        var registro = Assert.Single(_gravados);
        Assert.Equal(_dono.Id, registro.UsuarioId);
        Assert.Equal(_propriedade.Id, registro.PropriedadeId);
        Assert.Equal(FormaConfirmacao.Nenhuma, registro.Confirmacao);
        Assert.True(registro.Sucesso);
        Assert.True(registro.Concluido);
    }

    [Fact]
    public async Task ComPin_DesarmarSemPin_PedeConfirmacao_ENaoExecuta()
    {
        _dono.PinConfirmacaoHash = "h:2580";
        var executou = false;

        var r = await Execucao().ExecutarAsync(Desarmar(), _ => { executou = true; return Ok(default); }, Avaliar, CancellationToken.None);

        Assert.Equal(DesfechoComando.ConfirmacaoNecessaria, r.Desfecho);
        Assert.Equal("PIN_OBRIGATORIO", r.CodigoConfirmacao);
        Assert.False(executou);
    }

    [Fact]
    public async Task PinErrado_NaoExecuta_ERegistraATentativa()
    {
        _dono.PinConfirmacaoHash = "h:2580";

        var r = await Execucao().ExecutarAsync(Desarmar("1111"), Ok, Avaliar, CancellationToken.None);

        Assert.Equal("PIN_INCORRETO", r.CodigoConfirmacao);
        Assert.Equal(4, r.TentativasRestantes);
        var registro = Assert.Single(_gravados);
        Assert.False(registro.Sucesso);
        Assert.Equal("PIN incorreto.", registro.Mensagem);
    }

    [Fact]
    public async Task PinCertoViaBiometria_RegistraComoBiometria()
    {
        _dono.PinConfirmacaoHash = "h:2580";

        var r = await Execucao().ExecutarAsync(Desarmar("2580", biometria: true), Ok, Avaliar, CancellationToken.None);

        Assert.Equal(DesfechoComando.Executado, r.Desfecho);
        Assert.Equal(FormaConfirmacao.Biometria, Assert.Single(_gravados).Confirmacao);
    }

    [Fact]
    public async Task Armar_NaoPedePin()
    {
        _dono.PinConfirmacaoHash = "h:2580";
        var armar = new SolicitacaoComando { UsuarioId = _dono.Id, Tipo = TipoComando.Armar, EquipamentoId = _central.Id };

        var r = await Execucao().ExecutarAsync(armar, Ok, Avaliar, CancellationToken.None);

        Assert.Equal(DesfechoComando.Executado, r.Desfecho);
    }

    [Fact]
    public async Task MesmaChaveRepetida_DevolveARespostaGuardada_SemReenviar()
    {
        _comandos.Setup(c => c.GetPorChaveAsync(_dono.Id, "k1", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ComandoExecutado { Concluido = true, Sucesso = true, RespostaJson = "\"feito antes\"" });
        var execucoes = 0;

        var r = await Execucao().ExecutarAsync(Desarmar(chave: "k1"), _ => { execucoes++; return Ok(default); }, Avaliar, CancellationToken.None);

        Assert.Equal("feito antes", r.Dados);
        Assert.Equal(0, execucoes);
        Assert.Empty(_gravados);
    }

    [Fact]
    public async Task ToqueDuploSimultaneo_SegundoNaoExecuta()
    {
        _comandos.Setup(c => c.TentarReservarAsync(It.IsAny<ComandoExecutado>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var execucoes = 0;

        var r = await Execucao().ExecutarAsync(Desarmar(chave: "k2"), _ => { execucoes++; return Ok(default); }, Avaliar, CancellationToken.None);

        Assert.Equal(DesfechoComando.EmAndamento, r.Desfecho);
        Assert.Equal(0, execucoes);
    }

    [Fact]
    public async Task EquipamentoQueNaoObedeceu_FicaComoFalhaNaAuditoria()
    {
        var r = await Execucao().ExecutarAsync(Desarmar(chave: "k3"), Ok, _ => (false, "A central não respondeu."), CancellationToken.None);

        Assert.Equal(DesfechoComando.Executado, r.Desfecho);
        var registro = Assert.Single(_gravados);
        Assert.False(registro.Sucesso);
        Assert.Equal("A central não respondeu.", registro.Mensagem);
        Assert.Equal("\"feito\"", registro.RespostaJson);
    }

    [Fact]
    public async Task UsuarioSemAcesso_NaoEncontrado_SemConsultarPin()
    {
        var estranho = new SolicitacaoComando { UsuarioId = Guid.NewGuid(), Tipo = TipoComando.Desarmar, EquipamentoId = _central.Id };

        var r = await Execucao().ExecutarAsync(estranho, Ok, Avaliar, CancellationToken.None);

        Assert.Equal(DesfechoComando.NaoEncontrado, r.Desfecho);
        Assert.Empty(_gravados);
    }

    // --- Saúde ---------------------------------------------------------------------------------

    [Fact]
    public void MonitorQuedas_AvisaDepoisDeDoisMinutos_UmaVez_EAvisaQuandoVolta()
    {
        var monitor = new MonitorQuedas();
        var fora = new ComponenteSaudeResponse { Chave = "jfl", Nome = "Recepção", Situacao = SituacaoComponente.Fora };
        var ok = new ComponenteSaudeResponse { Chave = "jfl", Nome = "Recepção", Situacao = SituacaoComponente.Ok };
        var t0 = new DateTime(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

        Assert.Empty(monitor.Atualizar([fora], t0).Cairam);
        Assert.Empty(monitor.Atualizar([fora], t0.AddMinutes(1)).Cairam);
        Assert.Single(monitor.Atualizar([fora], t0.AddMinutes(2)).Cairam);
        Assert.Empty(monitor.Atualizar([fora], t0.AddMinutes(3)).Cairam);
        Assert.Single(monitor.Atualizar([ok], t0.AddMinutes(4)).Voltaram);
    }

    [Fact]
    public void MonitorQuedas_QuedaCurta_NaoAvisaNemNaVolta()
    {
        var monitor = new MonitorQuedas();
        var t0 = DateTime.UtcNow;
        monitor.Atualizar([new ComponenteSaudeResponse { Chave = "banco", Nome = "Banco", Situacao = SituacaoComponente.Fora }], t0);

        var (cairam, voltaram) = monitor.Atualizar([new ComponenteSaudeResponse { Chave = "banco", Nome = "Banco", Situacao = SituacaoComponente.Ok }], t0.AddSeconds(30));

        Assert.Empty(cairam);
        Assert.Empty(voltaram);
    }

    [Fact]
    public async Task Saude_BackupAtrasado_EListenerParado()
    {
        var banco = new Mock<IVerificadorBanco>();
        banco.Setup(b => b.ConectaAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var jfl = new Mock<IEstadoRecepcaoJfl>();
        jfl.Setup(j => j.Escutando).Returns(false);
        var backups = new Mock<IRegistroBackupRepositorio>();
        backups.Setup(b => b.UltimoAsync(TipoRegistroBackup.Backup, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegistroBackup { Sucesso = true, OcorridoEmUtc = DateTime.UtcNow.AddHours(-30) });
        var batimentos = new BatimentosServicos();
        batimentos.Registrar("escalonamento", "Escalonamento", TimeSpan.FromSeconds(30));

        var saude = await new SaudeSistemaServico(banco.Object, jfl.Object, batimentos, backups.Object, new MonitorQuedas()).VerificarAsync(CancellationToken.None);

        Assert.Equal(SituacaoComponente.Fora, saude.Geral);
        Assert.Equal(SituacaoComponente.Fora, saude.Componentes.Single(c => c.Chave == "jfl").Situacao);
        Assert.Equal(SituacaoComponente.Ok, saude.Componentes.Single(c => c.Chave == "escalonamento").Situacao);
        var backup = saude.Componentes.Single(c => c.Chave == "backup");
        Assert.Equal(SituacaoComponente.Atencao, backup.Situacao);
        Assert.Equal("O backup diário está atrasado.", backup.Detalhe);
    }
}
