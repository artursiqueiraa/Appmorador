using AppMorador.Application.ControlId;

namespace AppMorador.Application.Hikvision;

/// <summary>
/// Sprint 26 (ADR 0042) — terminais faciais Hikvision via ISAPI (padrão de fabricante da ADR 0014;
/// nenhum código fora da implementação conhece ISAPI). Validado num simulador local; pendente de
/// homologação no aparelho físico. A pessoa é identificada no terminal pelo id do morador
/// (employeeNo, 32 caracteres).
/// </summary>
public interface IHikvisionAcessoProvider
{
    /// <summary>Cria/atualiza a pessoa e envia (ou troca) o rosto.</summary>
    Task<ResultadoTesteConexao> EnviarRostoAsync(ConexaoEquipamento conexao, Guid moradorId, string nome, byte[] foto, CancellationToken cancellationToken);

    /// <summary>Apaga o rosto e a pessoa do terminal.</summary>
    Task<ResultadoTesteConexao> RemoverPessoaAsync(ConexaoEquipamento conexao, Guid moradorId, CancellationToken cancellationToken);

    Task<ResultadoTesteConexao> AbrirPortaAsync(ConexaoEquipamento conexao, int porta, CancellationToken cancellationToken);
}
