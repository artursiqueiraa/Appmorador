using AppMorador.Domain.Entities;
using AppMorador.Domain.Repositories;

namespace AppMorador.Application.Painel;

public sealed class DashboardOperacionalServico : IDashboardOperacionalServico
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IPropriedadeRepositorio _propriedades;
    private readonly IEquipamentoRepositorio _equipamentos;

    public DashboardOperacionalServico(IUsuarioRepositorio usuarios, IPropriedadeRepositorio propriedades, IEquipamentoRepositorio equipamentos)
    {
        _usuarios = usuarios;
        _propriedades = propriedades;
        _equipamentos = equipamentos;
    }

    public async Task<DashboardOperacionalResponse> ObterAsync(CancellationToken cancellationToken)
    {
        var totalClientes = await _usuarios.ContarClientesAsync(cancellationToken).ConfigureAwait(false);
        var novosPorMes = await _usuarios.ContarClientesPorMesAsync(12, cancellationToken).ConfigureAwait(false);
        var propriedadesPorTipo = await _propriedades.ContarPorTipoAsync(cancellationToken).ConfigureAwait(false);
        var equipamentosPorStatus = await _equipamentos.ContarPorStatusGlobalAsync(cancellationToken).ConfigureAwait(false);

        return new DashboardOperacionalResponse
        {
            TotalClientes = totalClientes,
            TotalPropriedades = propriedadesPorTipo.Values.Sum(),
            TotalEquipamentos = equipamentosPorStatus.Values.Sum(),
            TotalEquipamentosOffline = equipamentosPorStatus.GetValueOrDefault(StatusEquipamento.Offline, 0),
            NovosClientesPorMes = novosPorMes
                .Select(kv => new NovosClientesPorMesItem { Mes = kv.Key, Quantidade = kv.Value })
                .OrderBy(i => i.Mes)
                .ToList(),
            PropriedadesPorTipo = propriedadesPorTipo.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            EquipamentosPorStatus = equipamentosPorStatus.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
        };
    }
}
