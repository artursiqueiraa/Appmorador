using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using AppMorador.Application.Common;
using AppMorador.Application.Email;
using AppMorador.Application.Rbac;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Pessoas;

public sealed class PessoaComAcessoResponse
{
    public Guid? VinculoId { get; init; }

    public required Guid UsuarioId { get; init; }

    public required string Nome { get; init; }

    public required string Email { get; init; }

    public required PerfilPropriedade Papel { get; init; }

    public required bool EhDono { get; init; }
}

public sealed class ConvitePendenteResponse
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required PerfilPropriedade Papel { get; init; }

    public required DateTime ExpiraEmUtc { get; init; }
}

public sealed class PessoasPropriedadeResponse
{
    public required IReadOnlyList<PessoaComAcessoResponse> Pessoas { get; init; }

    public required IReadOnlyList<ConvitePendenteResponse> ConvitesPendentes { get; init; }
}

public sealed class ConvidarPessoaRequest
{
    public string Email { get; init; } = string.Empty;

    public PerfilPropriedade Papel { get; init; } = PerfilPropriedade.Morador;
}

public sealed class ConviteCriadoResponse
{
    public required Guid Id { get; init; }

    /// <summary>Link para o dono compartilhar no WhatsApp/SMS (o mesmo que vai no e-mail).</summary>
    public required string Link { get; init; }

    public required DateTime ExpiraEmUtc { get; init; }

    public required bool EmailEnviado { get; init; }
}

public sealed class AlterarPapelRequest
{
    public PerfilPropriedade Papel { get; init; }
}

/// <summary>O que a tela de aceite mostra antes de a pessoa entrar/criar conta.</summary>
public sealed class ConvitePublicoResponse
{
    public required bool Valido { get; init; }

    public string? Motivo { get; init; }

    public string? NomePropriedade { get; init; }

    public PerfilPropriedade? Papel { get; init; }

    public string? Email { get; init; }

    public string? ConvidadoPor { get; init; }
}

public interface IPessoasPropriedadeServico
{
    Task<Result<PessoasPropriedadeResponse>> ListarAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken);

    Task<Result<ConviteCriadoResponse>> ConvidarAsync(Guid usuarioId, Guid propriedadeId, ConvidarPessoaRequest request, CancellationToken cancellationToken);

    Task<Result> RevogarConviteAsync(Guid usuarioId, Guid conviteId, CancellationToken cancellationToken);

    Task<Result> AlterarPapelAsync(Guid usuarioId, Guid vinculoId, AlterarPapelRequest request, CancellationToken cancellationToken);

    Task<Result> RemoverAsync(Guid usuarioId, Guid vinculoId, CancellationToken cancellationToken);

    Task<ConvitePublicoResponse> ObterConviteAsync(string codigo, CancellationToken cancellationToken);

    Task<Result<Guid>> AceitarConviteAsync(Guid usuarioId, string codigo, CancellationToken cancellationToken);
}

/// <summary>
/// Sprint 25 (ADR 0041) — quem tem acesso a uma propriedade. Só o Proprietário gerencia pessoas
/// (<see cref="NivelAcesso.Proprietario"/>). O papel define as permissões funcionais do vínculo
/// (tabela <see cref="PermissoesDoPapel"/>), as mesmas que o app lê para mostrar/esconder botões.
/// </summary>
public sealed class PessoasPropriedadeServico : IPessoasPropriedadeServico
{
    public static readonly TimeSpan ValidadeConvite = TimeSpan.FromHours(48);

    private static readonly PerfilPropriedade[] PapeisConvidaveis =
        [PerfilPropriedade.Gerente, PerfilPropriedade.Morador, PerfilPropriedade.Visualizador];

    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IUsuarioPropriedadeRepositorio _vinculos;
    private readonly IUsuarioPropriedadePermissaoRepositorio _permissoes;
    private readonly IConvitePropriedadeRepositorio _convites;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IEnvioEmail _email;
    private readonly EmailOptions _opcoesEmail;
    private readonly IAcessoPropriedadeServico _acesso;

    public PessoasPropriedadeServico(
        IPropriedadeRepositorio propriedades,
        IUsuarioPropriedadeRepositorio vinculos,
        IUsuarioPropriedadePermissaoRepositorio permissoes,
        IConvitePropriedadeRepositorio convites,
        IUsuarioRepositorio usuarios,
        IEnvioEmail email,
        EmailOptions opcoesEmail,
        IAcessoPropriedadeServico acesso)
    {
        _propriedades = propriedades;
        _vinculos = vinculos;
        _permissoes = permissoes;
        _convites = convites;
        _usuarios = usuarios;
        _email = email;
        _opcoesEmail = opcoesEmail;
        _acesso = acesso;
    }

    /// <summary>Permissões funcionais de cada papel (Sprint 25, aprovada pelo usuário).</summary>
    public static IReadOnlyCollection<PermissaoFuncionalidade> PermissoesDoPapel(PerfilPropriedade papel) => papel switch
    {
        PerfilPropriedade.Administrador or PerfilPropriedade.Gerente =>
        [
            PermissaoFuncionalidade.CadastrarMorador, PermissaoFuncionalidade.CadastrarFacial, PermissaoFuncionalidade.CadastrarTag,
            PermissaoFuncionalidade.AbrirPortao, PermissaoFuncionalidade.VerCameras, PermissaoFuncionalidade.CriarVisitante,
        ],
        PerfilPropriedade.Morador => [PermissaoFuncionalidade.AbrirPortao, PermissaoFuncionalidade.VerCameras, PermissaoFuncionalidade.CriarVisitante],
        PerfilPropriedade.Visualizador => [PermissaoFuncionalidade.VerCameras],
        _ => [],
    };

    public async Task<Result<PessoasPropriedadeResponse>> ListarAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await ObterComoProprietarioAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<PessoasPropriedadeResponse>.Fail("Propriedade não encontrada.");
        }

        var vinculos = await _vinculos.ListByPropriedadeAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        var pessoas = vinculos
            .Where(v => v.Ativo && v.Usuario is not null)
            .Select(v => new PessoaComAcessoResponse
            {
                VinculoId = v.Id,
                UsuarioId = v.UsuarioId,
                Nome = v.Usuario!.Nome,
                Email = v.Usuario.Email,
                Papel = v.UsuarioId == propriedade.ProprietarioId ? PerfilPropriedade.Administrador : v.Perfil,
                EhDono = v.UsuarioId == propriedade.ProprietarioId,
            })
            .OrderByDescending(p => p.EhDono)
            .ThenBy(p => p.Nome)
            .ToList();

        var convites = await _convites.ListPendentesAsync(propriedadeId, DateTime.UtcNow, cancellationToken).ConfigureAwait(false);
        return Result<PessoasPropriedadeResponse>.Ok(new PessoasPropriedadeResponse
        {
            Pessoas = pessoas,
            ConvitesPendentes = convites
                .Select(c => new ConvitePendenteResponse { Id = c.Id, Email = c.Email, Papel = c.Papel, ExpiraEmUtc = c.ExpiraEmUtc })
                .ToList(),
        });
    }

    public async Task<Result<ConviteCriadoResponse>> ConvidarAsync(
        Guid usuarioId, Guid propriedadeId, ConvidarPessoaRequest request, CancellationToken cancellationToken)
    {
        var propriedade = await ObterComoProprietarioAsync(usuarioId, propriedadeId, cancellationToken).ConfigureAwait(false);
        if (propriedade is null)
        {
            return Result<ConviteCriadoResponse>.Fail("Propriedade não encontrada.");
        }

        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!EmailValido(email))
        {
            return Result<ConviteCriadoResponse>.Fail("Informe um e-mail válido.");
        }

        if (!PapeisConvidaveis.Contains(request.Papel))
        {
            return Result<ConviteCriadoResponse>.Fail("Escolha Gerente, Membro ou Visualizador.");
        }

        var existente = await _usuarios.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (existente is not null)
        {
            var vinculo = await _vinculos.GetAsync(existente.Id, propriedadeId, cancellationToken).ConfigureAwait(false);
            if (existente.Id == propriedade.ProprietarioId || vinculo is { Ativo: true })
            {
                return Result<ConviteCriadoResponse>.Fail("Essa pessoa já tem acesso a esta propriedade.");
            }
        }

        var codigo = GerarCodigo();
        var agora = DateTime.UtcNow;
        var convite = new ConvitePropriedade
        {
            Id = Guid.NewGuid(),
            PropriedadeId = propriedadeId,
            Papel = request.Papel,
            Email = email,
            CodigoHash = Hash(codigo),
            CriadoPorUsuarioId = usuarioId,
            CriadoEmUtc = agora,
            ExpiraEmUtc = agora + ValidadeConvite,
        };
        await _convites.AddAsync(convite, cancellationToken).ConfigureAwait(false);
        await _convites.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var link = $"{_opcoesEmail.UrlApp.TrimEnd('/')}/convite/{codigo}";
        var dono = await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        var enviado = await _email.EnviarAsync(
            email,
            $"{dono?.Nome ?? "Alguém"} convidou você para {propriedade.Nome}",
            MontarEmail(dono?.Nome ?? "Alguém", propriedade.Nome, request.Papel, link),
            cancellationToken).ConfigureAwait(false);

        return Result<ConviteCriadoResponse>.Ok(new ConviteCriadoResponse
        {
            Id = convite.Id,
            Link = link,
            ExpiraEmUtc = convite.ExpiraEmUtc,
            EmailEnviado = enviado,
        });
    }

    public async Task<Result> RevogarConviteAsync(Guid usuarioId, Guid conviteId, CancellationToken cancellationToken)
    {
        var convite = await _convites.GetByIdAsync(conviteId, cancellationToken).ConfigureAwait(false);
        if (convite is null || await ObterComoProprietarioAsync(usuarioId, convite.PropriedadeId, cancellationToken).ConfigureAwait(false) is null)
        {
            return Result.Fail("Convite não encontrado.");
        }

        convite.RevogadoEmUtc ??= DateTime.UtcNow;
        await _convites.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> AlterarPapelAsync(Guid usuarioId, Guid vinculoId, AlterarPapelRequest request, CancellationToken cancellationToken)
    {
        var (vinculo, erro) = await ObterVinculoGerenciavelAsync(usuarioId, vinculoId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null)
        {
            return Result.Fail(erro!);
        }

        if (!PapeisConvidaveis.Contains(request.Papel))
        {
            return Result.Fail("Escolha Gerente, Membro ou Visualizador.");
        }

        vinculo.Perfil = request.Papel;
        await _permissoes.SubstituirAsync(vinculo.Id, PermissoesDoPapel(request.Papel), cancellationToken).ConfigureAwait(false);
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<Result> RemoverAsync(Guid usuarioId, Guid vinculoId, CancellationToken cancellationToken)
    {
        var (vinculo, erro) = await ObterVinculoGerenciavelAsync(usuarioId, vinculoId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null)
        {
            return Result.Fail(erro!);
        }

        // Vale na próxima requisição da pessoa: os vínculos são lidos a cada chamada (ADR 0041).
        vinculo.Ativo = false;
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }

    public async Task<ConvitePublicoResponse> ObterConviteAsync(string codigo, CancellationToken cancellationToken)
    {
        var convite = await _convites.GetByCodigoHashAsync(Hash(codigo ?? string.Empty), cancellationToken).ConfigureAwait(false);
        if (convite?.Propriedade is null)
        {
            return new ConvitePublicoResponse { Valido = false, Motivo = "Convite não encontrado. Peça um novo link a quem convidou você." };
        }

        var dono = await _usuarios.GetByIdAsync(convite.CriadoPorUsuarioId, cancellationToken).ConfigureAwait(false);
        return new ConvitePublicoResponse
        {
            Valido = convite.Pendente(DateTime.UtcNow),
            Motivo = MotivoInvalido(convite),
            NomePropriedade = convite.Propriedade.Nome,
            Papel = convite.Papel,
            Email = convite.Email,
            ConvidadoPor = dono?.Nome,
        };
    }

    public async Task<Result<Guid>> AceitarConviteAsync(Guid usuarioId, string codigo, CancellationToken cancellationToken)
    {
        var convite = await _convites.GetByCodigoHashAsync(Hash(codigo ?? string.Empty), cancellationToken).ConfigureAwait(false);
        if (convite?.Propriedade is null)
        {
            return Result<Guid>.Fail("Convite não encontrado. Peça um novo link a quem convidou você.");
        }

        if (!convite.Pendente(DateTime.UtcNow))
        {
            return Result<Guid>.Fail(MotivoInvalido(convite)!);
        }

        if (convite.Propriedade.ProprietarioId == usuarioId)
        {
            return Result<Guid>.Fail("Você já é o dono desta propriedade.");
        }

        var vinculo = await _vinculos.GetAsync(usuarioId, convite.PropriedadeId, cancellationToken).ConfigureAwait(false);
        if (vinculo is null)
        {
            vinculo = new UsuarioPropriedade
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                PropriedadeId = convite.PropriedadeId,
                Perfil = convite.Papel,
                CreatedAtUtc = DateTime.UtcNow,
                Ativo = true,
            };
            await _vinculos.AddAsync(vinculo, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            vinculo.Perfil = convite.Papel;
            vinculo.Ativo = true;
        }

        convite.AceitoPorUsuarioId = usuarioId;
        convite.AceitoEmUtc = DateTime.UtcNow;
        await _vinculos.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await _permissoes.SubstituirAsync(vinculo.Id, PermissoesDoPapel(convite.Papel), cancellationToken).ConfigureAwait(false);
        await _permissoes.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result<Guid>.Ok(convite.PropriedadeId);
    }

    private async Task<Propriedade?> ObterComoProprietarioAsync(Guid usuarioId, Guid propriedadeId, CancellationToken cancellationToken)
    {
        var propriedade = await _propriedades.GetByIdAsync(propriedadeId, cancellationToken).ConfigureAwait(false);
        return propriedade is not null && _acesso.Pode(usuarioId, propriedade, NivelAcesso.Proprietario) ? propriedade : null;
    }

    private async Task<(UsuarioPropriedade? Vinculo, string? Erro)> ObterVinculoGerenciavelAsync(Guid usuarioId, Guid vinculoId, CancellationToken cancellationToken)
    {
        var vinculo = await _vinculos.GetByIdAsync(vinculoId, cancellationToken).ConfigureAwait(false);
        if (vinculo?.Propriedade is null || !_acesso.Pode(usuarioId, vinculo.Propriedade, NivelAcesso.Proprietario))
        {
            return (null, "Pessoa não encontrada.");
        }

        return vinculo.UsuarioId == vinculo.Propriedade.ProprietarioId
            ? (null, "O dono da propriedade não pode ser alterado nem removido.")
            : (vinculo, null);
    }

    private static string? MotivoInvalido(ConvitePropriedade convite) =>
        convite.AceitoEmUtc is not null ? "Este convite já foi usado."
        : convite.RevogadoEmUtc is not null ? "Este convite foi cancelado por quem convidou."
        : convite.ExpiraEmUtc <= DateTime.UtcNow ? "Este convite expirou. Peça um novo link."
        : null;

    private static bool EmailValido(string email) =>
        email.Length is > 3 and <= 200 && MailAddress.TryCreate(email, out var endereco) && endereco.Address == email;

    private static string GerarCodigo() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string codigo) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(codigo)));

    private static string NomePapel(PerfilPropriedade papel) => papel switch
    {
        PerfilPropriedade.Gerente => "Gerente",
        PerfilPropriedade.Visualizador => "Visualizador",
        _ => "Membro",
    };

    private static string MontarEmail(string dono, string propriedade, PerfilPropriedade papel, string link) => $"""
        <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#111">
          <h2 style="margin:0 0 12px">Você foi convidado para o Akteck Seg</h2>
          <p><b>{System.Net.WebUtility.HtmlEncode(dono)}</b> convidou você para acessar <b>{System.Net.WebUtility.HtmlEncode(propriedade)}</b>
             como <b>{NomePapel(papel)}</b>.</p>
          <p><a href="{link}" style="display:inline-block;background:#25C98D;color:#062015;padding:12px 20px;border-radius:10px;text-decoration:none;font-weight:bold">Aceitar convite</a></p>
          <p style="color:#555;font-size:13px">O convite vale por 48 horas e só pode ser usado uma vez. Se você não esperava este e-mail, pode ignorá-lo.</p>
        </div>
        """;
}
