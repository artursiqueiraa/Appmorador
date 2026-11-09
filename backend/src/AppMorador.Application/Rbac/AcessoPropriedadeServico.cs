using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Rbac;

/// <summary>Sprint 25 (ADR 0041) — o que se quer fazer numa propriedade, do mais leve ao mais forte.</summary>
public enum NivelAcesso
{
    /// <summary>Ver telas, câmeras, histórico; confirmar alertas.</summary>
    Ver = 1,

    /// <summary>Armar/desarmar, abrir portões, comandos na central.</summary>
    Operar = 2,

    /// <summary>Cadastrar, editar e excluir (pessoas, equipamentos, portões…).</summary>
    Alterar = 3,

    /// <summary>Só o Proprietário: gerenciar quem tem acesso, excluir a propriedade.</summary>
    Proprietario = 4,
}

/// <summary>
/// Sprint 25 (ADR 0041) — ponto único da pergunta "este usuário pode fazer isto nesta
/// propriedade?". Substitui as checagens espalhadas `Propriedade.ProprietarioId == usuario`.
///
/// O dono (<see cref="Propriedade.ProprietarioId"/>) sempre pode tudo. Os demais dependem do
/// vínculo ativo em <see cref="UsuarioPropriedade"/> e do papel. Os vínculos do usuário logado são
/// carregados uma vez por requisição (<see cref="CarregarAsync"/>, chamado por um middleware da
/// Api); por isso a checagem é síncrona e remover alguém vale na próxima requisição dele.
/// Sem carga (testes, serviços de fundo), só o dono passa — exatamente o comportamento anterior.
/// </summary>
public interface IAcessoPropriedadeServico
{
    Task CarregarAsync(Guid usuarioId, CancellationToken cancellationToken);

    bool Pode(Guid usuarioId, Propriedade propriedade, NivelAcesso nivel);

    /// <summary>Papel do usuário logado na propriedade (Administrador para o dono); null sem acesso.</summary>
    PerfilPropriedade? PapelEm(Guid usuarioId, Propriedade propriedade);

    /// <summary>Propriedades em que o usuário logado é membro ativo (não inclui as que ele é dono).</summary>
    IReadOnlyCollection<Guid> PropriedadesComoMembro { get; }
}

public sealed class AcessoPropriedadeServico : IAcessoPropriedadeServico
{
    private readonly IUsuarioPropriedadeRepositorio? _vinculos;
    private Guid? _usuarioCarregado;
    private Dictionary<Guid, PerfilPropriedade> _papeis = new();

    public AcessoPropriedadeServico(IUsuarioPropriedadeRepositorio vinculos)
    {
        _vinculos = vinculos;
    }

    /// <summary>Sem repositório: só o dono passa (usado em testes de serviços).</summary>
    public AcessoPropriedadeServico()
    {
    }

    public IReadOnlyCollection<Guid> PropriedadesComoMembro => _papeis.Keys;

    public async Task CarregarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        if (_vinculos is null)
        {
            return;
        }

        var vinculos = await _vinculos.ListAtivosByUsuarioAsync(usuarioId, cancellationToken).ConfigureAwait(false);
        _papeis = vinculos.GroupBy(v => v.PropriedadeId).ToDictionary(g => g.Key, g => g.First().Perfil);
        _usuarioCarregado = usuarioId;
    }

    public bool Pode(Guid usuarioId, Propriedade propriedade, NivelAcesso nivel)
    {
        var papel = PapelEm(usuarioId, propriedade);
        return papel is not null && Permite(papel.Value, nivel);
    }

    public PerfilPropriedade? PapelEm(Guid usuarioId, Propriedade propriedade)
    {
        if (propriedade.ProprietarioId == usuarioId)
        {
            return PerfilPropriedade.Administrador;
        }

        if (_usuarioCarregado != usuarioId)
        {
            return null;
        }

        return _papeis.TryGetValue(propriedade.Id, out var papel) ? papel : null;
    }

    /// <summary>Tabela de papéis aprovada na Sprint 25.</summary>
    public static bool Permite(PerfilPropriedade papel, NivelAcesso nivel) => papel switch
    {
        PerfilPropriedade.Administrador => true,
        PerfilPropriedade.Gerente => nivel != NivelAcesso.Proprietario,
        PerfilPropriedade.Morador => nivel is NivelAcesso.Ver or NivelAcesso.Operar,
        PerfilPropriedade.Visualizador => nivel == NivelAcesso.Ver,
        _ => false,
    };
}
