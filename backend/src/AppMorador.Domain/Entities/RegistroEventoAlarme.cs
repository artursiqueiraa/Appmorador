namespace AppMorador.Domain.Entities;

/// <summary>
/// Registro tecnico de auditoria/diagnostico: TODO evento 0x24 recebido de uma central
/// grava uma linha aqui, independentemente de ter passado no filtro Contact ID ou de
/// ter virado uma <see cref="Ocorrencia"/>. Existe apenas para suporte/diagnostico —
/// nenhuma regra de negocio le ou depende desta tabela.
/// </summary>
public class RegistroEventoAlarme
{
    public Guid Id { get; set; }

    /// <summary>Bytes do campo Dados do pacote 0x24, em hexadecimal.</summary>
    public required string Payload { get; set; }

    public required string NumeroSerie { get; set; }

    /// <summary>Codigo Contact ID de 4 digitos, como recebido (sem classificacao).</summary>
    public required string CodigoEvento { get; set; }

    /// <summary>Campo U/Z bruto do evento (usuario ou zona, conforme o codigo).</summary>
    public required string Zona { get; set; }

    public required DateTime Timestamp { get; set; }

    public required ResultadoProcessamentoEvento ResultadoProcessamento { get; set; }
}
