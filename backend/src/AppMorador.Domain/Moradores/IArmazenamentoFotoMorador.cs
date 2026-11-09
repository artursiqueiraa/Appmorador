namespace AppMorador.Domain.Moradores;

/// <summary>
/// Akteck Seg (ADR 0036) — guarda a foto de rosto de um morador em disco local (mesma
/// abordagem sem nuvem dos snapshots, ADR 0024), numa pasta própria: foto de pessoa é dado
/// pessoal sensível e não se mistura com imagens de câmera.
/// </summary>
public interface IArmazenamentoFotoMorador
{
    /// <summary>Grava os bytes e devolve o caminho relativo a guardar em <c>Morador.FotoPath</c>.</summary>
    Task<string> SalvarAsync(Guid moradorId, byte[] conteudo, string extensao, CancellationToken cancellationToken);

    /// <summary>Abre um caminho salvo por <see cref="SalvarAsync"/>. Nulo se não existir (nunca lança).</summary>
    Stream? Abrir(string caminhoRelativo);

    /// <summary>Apaga o arquivo, se existir (nunca lança).</summary>
    void Remover(string caminhoRelativo);
}
