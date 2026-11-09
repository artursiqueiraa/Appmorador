using AppMorador.Domain.Moradores;

namespace AppMorador.Infrastructure.Moradores;

public sealed class ArmazenamentoFotoMoradorOptions
{
    public string BasePath { get; set; } = "fotos-moradores";
}

/// <summary>
/// Disco local: {BasePath}/{moradorId}/{guid}.{ext}. O nome do arquivo nunca vem do cliente
/// (evita path traversal) e o caminho relativo usa "/" independente do sistema operacional,
/// mesmo formato de <c>SnapshotStorage</c>.
/// </summary>
internal sealed class ArmazenamentoFotoMorador : IArmazenamentoFotoMorador
{
    private readonly ArmazenamentoFotoMoradorOptions _options;

    public ArmazenamentoFotoMorador(ArmazenamentoFotoMoradorOptions options)
    {
        _options = options;
    }

    public async Task<string> SalvarAsync(Guid moradorId, byte[] conteudo, string extensao, CancellationToken cancellationToken)
    {
        var diretorio = Path.Combine(_options.BasePath, moradorId.ToString("D"));
        Directory.CreateDirectory(diretorio);
        var nomeArquivo = $"{Guid.NewGuid():D}.{extensao}";
        await File.WriteAllBytesAsync(Path.Combine(diretorio, nomeArquivo), conteudo, cancellationToken).ConfigureAwait(false);
        return $"{_options.BasePath}/{moradorId:D}/{nomeArquivo}";
    }

    public Stream? Abrir(string caminhoRelativo)
    {
        var caminho = Resolver(caminhoRelativo);
        return caminho is not null && File.Exists(caminho) ? File.OpenRead(caminho) : null;
    }

    public void Remover(string caminhoRelativo)
    {
        var caminho = Resolver(caminhoRelativo);
        if (caminho is not null && File.Exists(caminho))
        {
            File.Delete(caminho);
        }
    }

    /// <summary>Só aceita caminhos dentro de BasePath — um FotoPath adulterado nunca abre arquivo fora da pasta.</summary>
    private string? Resolver(string caminhoRelativo)
    {
        var raiz = Path.GetFullPath(_options.BasePath);
        var completo = Path.GetFullPath(caminhoRelativo.Replace('/', Path.DirectorySeparatorChar));
        return completo.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? completo : null;
    }
}
