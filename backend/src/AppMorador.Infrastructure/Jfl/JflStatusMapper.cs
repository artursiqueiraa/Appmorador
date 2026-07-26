using AppMorador.Application.Jfl;
using AppMorador.Jfl.Messages.Status;

namespace AppMorador.Infrastructure.Jfl;

/// <summary>
/// Única fronteira de tradução entre o tipo de protocolo (<see cref="CentralStatusResponse"/>,
/// projeto AppMorador.Jfl) e o DTO interno (<see cref="StatusCentralJflInfo"/>, Application/Jfl)
/// — nenhum outro ponto do sistema traduz esse formato (ver ADR 0014/0015).
/// </summary>
internal static class JflStatusMapper
{
    public static StatusCentralJflInfo ParaStatusCentralJflInfo(CentralStatusResponse status) => new()
    {
        DataHoraCentral = status.DataHoraCentral,
        BateriaTipo = status.Bateria.Tipo.ToString(),
        BateriaPercentual = status.Bateria.PercentualLitio,
        BateriaTensaoAproximada = status.Bateria.TensaoChumboAproximada,
        EletrificadorArmado = status.Eletrificador.Estado == ElectrifierState.Armado,
        Particoes = status.Particoes.Select(ParaParticaoInfo).ToList(),
        Zonas = status.Zonas.Select(ParaZonaInfo).ToList(),
        Pgms = status.Pgms.Select(p => new PgmStatusInfo { Numero = p.Numero, Acionada = p.Acionada, Permitida = p.Permitida }).ToList(),
        ProblemasAtivos = ListarProblemasAtivos(status.Problemas),
    };

    private static ParticaoStatusInfo ParaParticaoInfo(PartitionStatus particao) => new()
    {
        Numero = particao.Numero,
        Desabilitada = particao.Desabilitada,
        Armada = particao.Estado is PartitionState.Armada or PartitionState.ArmadaEmDisparo,
        ArmadaStay = particao.Estado is PartitionState.ArmadaStay or PartitionState.ArmadaStayEmDisparo,
        EmDisparo = particao.Estado is PartitionState.DesarmadaEmDisparo or PartitionState.ArmadaEmDisparo or PartitionState.ArmadaStayEmDisparo,
    };

    private static ZonaStatusInfo ParaZonaInfo(ZoneStatus zona) => new()
    {
        Numero = zona.Numero,
        Estado = DescreverEstadoZona(zona.Estado),
        PermiteInibir = zona.PermiteInibir,
    };

    private static string DescreverEstadoZona(ZoneState? estado) => estado switch
    {
        ZoneState.Aberta => "Aberta",
        ZoneState.Fechada => "Fechada",
        ZoneState.Inibida => "Inibida",
        ZoneState.Disparo => "Disparo",
        ZoneState.SemComunicacao => "Sem comunicação",
        ZoneState.Curto => "Curto-circuito",
        ZoneState.TamperAberto => "Tamper aberto",
        ZoneState.BateriaBaixa => "Bateria baixa",
        _ => "Desabilitada",
    };

    private static IReadOnlyList<string> ListarProblemasAtivos(ProblemFlags problemas)
    {
        var ativos = new List<string>();

        void Adicionar(bool ativo, string descricao)
        {
            if (ativo)
            {
                ativos.Add(descricao);
            }
        }

        Adicionar(problemas.Bateria, "Problema na bateria");
        Adicionar(problemas.Ac, "Falha na energia elétrica");
        Adicionar(problemas.BateriaFracaControleOuSensorSemFio, "Bateria fraca em controle/sensor sem fio");
        Adicionar(problemas.BateriaInvertidaOuEmCurto, "Bateria invertida ou em curto");
        Adicionar(problemas.SupervisaoSensor, "Falha de supervisão de sensor");
        Adicionar(problemas.SaidaAuxiliar, "Problema na saída auxiliar");
        Adicionar(problemas.Tamper, "Tamper (violação) detectado");
        Adicionar(problemas.TamperTeclado, "Tamper no teclado");
        Adicionar(problemas.Curto, "Curto-circuito");
        Adicionar(problemas.CaboDeRede, "Cabo de rede desconectado");
        Adicionar(problemas.Ethernet, "Falha na conexão Ethernet");
        Adicionar(problemas.ModuloEthernet, "Falha no módulo Ethernet");
        Adicionar(problemas.Dhcp, "Falha ao obter endereço via DHCP");
        Adicionar(problemas.ConflitoIp, "Conflito de IP na rede");
        Adicionar(problemas.ServidorDns, "Falha no servidor DNS");
        Adicionar(problemas.IpDestino1, "Falha de comunicação com o servidor principal");
        Adicionar(problemas.IpDestino2, "Falha de comunicação com o servidor secundário");
        Adicionar(problemas.Ddns, "Falha no serviço de DNS dinâmico");
        Adicionar(problemas.Notificacao, "Falha ao enviar notificação");
        Adicionar(problemas.ModuloCelular, "Falha no módulo celular");
        Adicionar(problemas.ChipCelular, "Falha no chip celular");
        Adicionar(problemas.NivelSinalOperadora, "Sinal fraco da operadora celular");
        Adicionar(problemas.Gprs, "Falha na conexão GPRS");
        Adicionar(problemas.Sms, "Falha ao enviar SMS");
        Adicionar(problemas.LinhaTelefonica, "Falha na linha telefônica");
        Adicionar(problemas.Teclado, "Falha de comunicação com o teclado");
        Adicionar(problemas.Sirene, "Falha na sirene");
        Adicionar(problemas.SupervisaoSirene, "Falha de supervisão da sirene");
        Adicionar(problemas.SupervisaoPgm, "Falha de supervisão de PGM");
        Adicionar(problemas.Barramento, "Falha no barramento de comunicação");
        Adicionar(problemas.RedeTecladoAc, "Falha na alimentação da rede de teclados");
        Adicionar(problemas.SenhaRedeSemFio, "Senha da rede sem fio incorreta");
        Adicionar(problemas.AutenticacaoRedeSemFio, "Falha de autenticação na rede sem fio");
        Adicionar(problemas.SsidNaoEncontrado, "Rede sem fio (SSID) não encontrada");

        return ativos;
    }
}
