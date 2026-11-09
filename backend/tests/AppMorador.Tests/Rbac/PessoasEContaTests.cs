using AppMorador.Application.Autenticacao;
using AppMorador.Application.Conta;
using AppMorador.Application.Email;
using AppMorador.Application.Pessoas;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;
using Moq;
using Xunit;

namespace AppMorador.Tests.Rbac;

/// <summary>Sprint 25 (ADR 0041) — convites, pessoas com acesso e exclusão de conta.</summary>
public class PessoasEContaTests
{
    private readonly Guid _donoId = Guid.NewGuid();
    private readonly Propriedade _propriedade;
    private readonly Mock<IPropriedadeRepositorio> _propriedades = new();
    private readonly Mock<IUsuarioPropriedadeRepositorio> _vinculos = new();
    private readonly Mock<IUsuarioPropriedadePermissaoRepositorio> _permissoes = new();
    private readonly Mock<IConvitePropriedadeRepositorio> _convites = new();
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Mock<IEnvioEmail> _email = new();

    public PessoasEContaTests()
    {
        _propriedade = new Propriedade { Id = Guid.NewGuid(), Nome = "Casa", Tipo = TipoPropriedade.Residencial, ProprietarioId = _donoId };
        _propriedades.Setup(p => p.GetByIdAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_propriedade);
    }

    private PessoasPropriedadeServico Pessoas() => new(
        _propriedades.Object, _vinculos.Object, _permissoes.Object, _convites.Object, _usuarios.Object, _email.Object,
        new EmailOptions { UrlApp = "https://app.exemplo" }, new AcessoPropriedadeServico());

    [Fact]
    public async Task Convidar_GeraLinkUnicoGuardaSoOHashEEnviaEmail()
    {
        ConvitePropriedade? salvo = null;
        _convites.Setup(c => c.AddAsync(It.IsAny<ConvitePropriedade>(), It.IsAny<CancellationToken>())).Callback<ConvitePropriedade, CancellationToken>((c, _) => salvo = c);
        _email.Setup(e => e.EnviarAsync("ana@exemplo.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var r = await Pessoas().ConvidarAsync(_donoId, _propriedade.Id, new ConvidarPessoaRequest { Email = " Ana@Exemplo.com ", Papel = PerfilPropriedade.Morador }, CancellationToken.None);

        Assert.True(r.Success);
        Assert.StartsWith("https://app.exemplo/convite/", r.Data!.Link);
        var codigo = r.Data.Link.Split('/').Last();
        Assert.Equal(PessoasPropriedadeServico.Hash(codigo), salvo!.CodigoHash);
        Assert.DoesNotContain(codigo, salvo.CodigoHash);
        Assert.True(r.Data.EmailEnviado);
        Assert.Equal("ana@exemplo.com", salvo.Email);
    }

    [Fact]
    public async Task Convidar_QuemNaoEDono_NaoConsegue()
    {
        var r = await Pessoas().ConvidarAsync(Guid.NewGuid(), _propriedade.Id, new ConvidarPessoaRequest { Email = "ana@exemplo.com" }, CancellationToken.None);

        Assert.False(r.Success);
    }

    [Fact]
    public async Task Convidar_ComoProprietario_NaoPermitido()
    {
        var r = await Pessoas().ConvidarAsync(_donoId, _propriedade.Id, new ConvidarPessoaRequest { Email = "ana@exemplo.com", Papel = PerfilPropriedade.Administrador }, CancellationToken.None);

        Assert.Equal("Escolha Gerente, Membro ou Visualizador.", r.Error);
    }

    [Fact]
    public async Task Aceitar_CriaVinculoComOPapelEMarcaUsado()
    {
        var convidado = Guid.NewGuid();
        var convite = new ConvitePropriedade
        {
            Id = Guid.NewGuid(), PropriedadeId = _propriedade.Id, Propriedade = _propriedade, Papel = PerfilPropriedade.Visualizador,
            Email = "ana@exemplo.com", CodigoHash = PessoasPropriedadeServico.Hash("abc"), ExpiraEmUtc = DateTime.UtcNow.AddHours(1),
        };
        _convites.Setup(c => c.GetByCodigoHashAsync(convite.CodigoHash, It.IsAny<CancellationToken>())).ReturnsAsync(convite);
        UsuarioPropriedade? criado = null;
        _vinculos.Setup(v => v.AddAsync(It.IsAny<UsuarioPropriedade>(), It.IsAny<CancellationToken>())).Callback<UsuarioPropriedade, CancellationToken>((v, _) => criado = v);

        var r = await Pessoas().AceitarConviteAsync(convidado, "abc", CancellationToken.None);

        Assert.True(r.Success);
        Assert.Equal(PerfilPropriedade.Visualizador, criado!.Perfil);
        Assert.NotNull(convite.AceitoEmUtc);
        _permissoes.Verify(p => p.SubstituirAsync(criado.Id,
            It.Is<IReadOnlyCollection<PermissaoFuncionalidade>>(l => l.Count == 1 && l.Contains(PermissaoFuncionalidade.VerCameras)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Aceitar_ConviteExpirado_Recusa()
    {
        var convite = new ConvitePropriedade
        {
            Id = Guid.NewGuid(), PropriedadeId = _propriedade.Id, Propriedade = _propriedade, Papel = PerfilPropriedade.Morador,
            Email = "ana@exemplo.com", CodigoHash = PessoasPropriedadeServico.Hash("velho"), ExpiraEmUtc = DateTime.UtcNow.AddMinutes(-1),
        };
        _convites.Setup(c => c.GetByCodigoHashAsync(convite.CodigoHash, It.IsAny<CancellationToken>())).ReturnsAsync(convite);

        var r = await Pessoas().AceitarConviteAsync(Guid.NewGuid(), "velho", CancellationToken.None);

        Assert.Equal("Este convite expirou. Peça um novo link.", r.Error);
    }

    [Fact]
    public async Task Remover_ODono_NaoPermitido()
    {
        var vinculoDono = new UsuarioPropriedade { Id = Guid.NewGuid(), UsuarioId = _donoId, PropriedadeId = _propriedade.Id, Propriedade = _propriedade, CreatedAtUtc = DateTime.UtcNow };
        _vinculos.Setup(v => v.GetByIdAsync(vinculoDono.Id, It.IsAny<CancellationToken>())).ReturnsAsync(vinculoDono);

        var r = await Pessoas().RemoverAsync(_donoId, vinculoDono.Id, CancellationToken.None);

        Assert.False(r.Success);
        Assert.True(vinculoDono.Ativo);
    }

    [Fact]
    public async Task ExcluirConta_Anonimiza_EEncerraSessoes()
    {
        var usuario = new Usuario { Id = _donoId, Nome = "Ana", Email = "ana@exemplo.com", SenhaHash = "h", CreatedAtUtc = DateTime.UtcNow, Telefone = "11987654321" };
        _usuarios.Setup(u => u.GetByIdAsync(_donoId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _propriedades.Setup(p => p.ListByOwnerAsync(_donoId, It.IsAny<CancellationToken>())).ReturnsAsync([_propriedade]);
        _vinculos.Setup(v => v.ListByPropriedadeAsync(_propriedade.Id, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _vinculos.Setup(v => v.ListAtivosByUsuarioAsync(_donoId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify("senha", "h")).Returns(true);
        hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("novo");
        var sessoes = new Mock<ISessoesUsuario>();

        var r = await new MinhaContaServico(_usuarios.Object, _propriedades.Object, _vinculos.Object, hasher.Object, sessoes.Object)
            .ExcluirAsync(_donoId, new ExcluirContaRequest { Senha = "senha" }, CancellationToken.None);

        Assert.True(r.Success);
        Assert.Equal("Conta excluída", usuario.Nome);
        Assert.EndsWith("@anonimo.invalid", usuario.Email);
        Assert.Null(usuario.Telefone);
        Assert.False(usuario.Ativo);
        Assert.True(_propriedade.Excluido);
        sessoes.Verify(s => s.EncerrarTodasAsync(_donoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExcluirConta_ComPessoasNaPropriedade_PedeParaRemoverAntes()
    {
        var usuario = new Usuario { Id = _donoId, Nome = "Ana", Email = "ana@exemplo.com", SenhaHash = "h", CreatedAtUtc = DateTime.UtcNow };
        _usuarios.Setup(u => u.GetByIdAsync(_donoId, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _propriedades.Setup(p => p.ListByOwnerAsync(_donoId, It.IsAny<CancellationToken>())).ReturnsAsync([_propriedade]);
        _vinculos.Setup(v => v.ListByPropriedadeAsync(_propriedade.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UsuarioPropriedade { UsuarioId = Guid.NewGuid(), PropriedadeId = _propriedade.Id, Ativo = true, CreatedAtUtc = DateTime.UtcNow }]);
        var hasher = new Mock<IPasswordHasher>();
        hasher.Setup(h => h.Verify("senha", "h")).Returns(true);

        var r = await new MinhaContaServico(_usuarios.Object, _propriedades.Object, _vinculos.Object, hasher.Object, new Mock<ISessoesUsuario>().Object)
            .ExcluirAsync(_donoId, new ExcluirContaRequest { Senha = "senha" }, CancellationToken.None);

        Assert.False(r.Success);
        Assert.Equal("Ana", usuario.Nome);
    }
}
