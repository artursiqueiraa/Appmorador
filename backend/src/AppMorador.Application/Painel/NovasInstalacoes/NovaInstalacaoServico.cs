using AppMorador.Application.Auditoria;
using AppMorador.Application.Autenticacao;
using AppMorador.Application.Common;
using AppMorador.Application.Painel.VinculosEquipamento;
using AppMorador.Application.Propriedades;
using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel.NovasInstalacoes;

/// <summary>
/// Akteck Seg (ADR 0035) — "Nova instalação" do Painel. Não duplica nenhuma regra:
/// orquestra os casos de uso que já existem (cadastro do cliente, criação da
/// propriedade com o vínculo Administrador e as permissões do plano, provisionamento
/// do equipamento). Tudo que pode falhar por regra de negócio (e-mail em uso,
/// equipamento inexistente ou já provisionado) é checado ANTES de criar qualquer
/// registro, para nunca deixar um cliente sem propriedade por um erro previsível.
/// </summary>
public sealed class NovaInstalacaoServico : INovaInstalacaoServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IEquipamentoRepositorio _equipamentos;
    private readonly IVinculoEquipamentoPropriedadeRepositorio _vinculos;
    private readonly IAutenticacaoServico _autenticacao;
    private readonly IPropriedadeServico _propriedades;
    private readonly IVinculoEquipamentoServico _provisionamento;
    private readonly IAuditoriaService _auditoria;

    public NovaInstalacaoServico(
        IUsuarioRepositorio usuarios,
        IEquipamentoRepositorio equipamentos,
        IVinculoEquipamentoPropriedadeRepositorio vinculos,
        IAutenticacaoServico autenticacao,
        IPropriedadeServico propriedades,
        IVinculoEquipamentoServico provisionamento,
        IAuditoriaService auditoria)
    {
        _usuarios = usuarios;
        _equipamentos = equipamentos;
        _vinculos = vinculos;
        _autenticacao = autenticacao;
        _propriedades = propriedades;
        _provisionamento = provisionamento;
        _auditoria = auditoria;
    }

    public async Task<Result<NovaInstalacaoResponse>> CriarAsync(
        Guid usuarioId, CriarNovaInstalacaoRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        // Rota interna, exclusiva de Master: pode dizer claramente que o e-mail já existe
        // (o autocadastro público é genérico de propósito, para não revelar contas).
        var email = request.EmailCliente.Trim().ToLowerInvariant();
        if (await _usuarios.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false) is not null)
        {
            return Result<NovaInstalacaoResponse>.Fail("Já existe uma conta com este e-mail.");
        }

        if (request.EquipamentoId is { } equipamentoId)
        {
            if (await _equipamentos.GetByIdAsync(equipamentoId, cancellationToken).ConfigureAwait(false) is null)
            {
                return Result<NovaInstalacaoResponse>.Fail("Equipamento não encontrado.");
            }
            if (await _vinculos.GetVinculoAtivoPorEquipamentoAsync(equipamentoId, cancellationToken).ConfigureAwait(false) is not null)
            {
                return Result<NovaInstalacaoResponse>.Fail("Este equipamento já está provisionado em outra propriedade.");
            }
        }

        var cadastro = await _autenticacao.RegisterAsync(
            new CadastrarUsuarioRequest { Nome = request.NomeCliente, Email = email, Senha = request.SenhaInicial },
            cancellationToken).ConfigureAwait(false);
        if (!cadastro.Success)
        {
            return Result<NovaInstalacaoResponse>.Fail("Não foi possível criar a conta do cliente.");
        }
        var clienteId = cadastro.Data;

        var propriedade = await _propriedades.CreateAsync(
            clienteId,
            new CriarPropriedadeRequest
            {
                Nome = request.NomePropriedade,
                Tipo = request.TipoPropriedade,
                Endereco = string.IsNullOrWhiteSpace(request.EnderecoPropriedade) ? null : request.EnderecoPropriedade,
            },
            cancellationToken).ConfigureAwait(false);

        Guid? vinculoId = null;
        if (request.EquipamentoId is { } idEquipamento)
        {
            var vinculo = await _provisionamento.ProvisionarAsync(
                usuarioId,
                new ProvisionarEquipamentoRequest { EquipamentoId = idEquipamento, PropriedadeId = propriedade.Id, Observacoes = "Nova instalação" },
                ipAddress,
                cancellationToken).ConfigureAwait(false);
            // Já validado acima; só falharia numa corrida com outro provisionamento simultâneo.
            // Nesse caso cliente e propriedade ficam criados e o vínculo pode ser refeito em Provisionamentos.
            vinculoId = vinculo.Success ? vinculo.Data!.Id : null;
        }

        var usuarioNome = (await _usuarios.GetByIdAsync(usuarioId, cancellationToken).ConfigureAwait(false))?.Nome ?? "(desconhecido)";
        await _auditoria.RegistrarAsync(
            usuarioId, usuarioNome, TipoAcaoAuditoria.Criar, "Usuario", clienteId.ToString(),
            $"Nova instalação: cliente {request.NomeCliente.Trim()} com a propriedade {propriedade.Nome}", ipAddress, cancellationToken)
            .ConfigureAwait(false);

        return Result<NovaInstalacaoResponse>.Ok(new NovaInstalacaoResponse
        {
            ClienteId = clienteId,
            PropriedadeId = propriedade.Id,
            NomePropriedade = propriedade.Nome,
            VinculoEquipamentoId = vinculoId,
        });
    }
}
