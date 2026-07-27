namespace AppMorador.Domain.Entities;

/// <summary>Um canal de câmera de um <see cref="Gravador"/>.</summary>
public class Camera
{
    public Guid Id { get; set; }

    public Guid PropriedadeId { get; set; }

    public Propriedade? Propriedade { get; set; }

    public Guid GravadorId { get; set; }

    public Gravador? Gravador { get; set; }

    /// <summary>Numero do canal no gravador (ex.: 1, 2, 3...).</summary>
    public required int Canal { get; set; }

    public required string Nome { get; set; }

    /// <summary>Sprint 20 — atualizado a cada tentativa de captura (sucesso ou falha), nunca por um poller em segundo plano (não existe um hoje).</summary>
    public StatusCamera Status { get; set; } = StatusCamera.Desconhecido;

    /// <summary>Caminho RELATIVO (nunca absoluto) do último snapshot salvo com sucesso — mesma convenção de <see cref="Ocorrencia.ImagePath"/>, resolvido por <see cref="AppMorador.Domain.Snapshots.ISnapshotStorage"/>.</summary>
    public string? UltimoSnapshotPath { get; set; }

    /// <summary>Quando o sistema tentou capturar pela última vez, com sucesso ou não — sem isso não dá para saber se a câmera falha há dias ou se nunca foi tentada.</summary>
    public DateTime? UltimaTentativaCapturaUtc { get; set; }

    /// <summary>Quando a última captura BEM-SUCEDIDA aconteceu — é o timestamp que a Api expõe como "última imagem"/"visto há X".</summary>
    public DateTime? UltimoSucessoCapturaUtc { get; set; }
}
