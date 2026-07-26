using AppMorador.Application.Equipamentos;
using Microsoft.AspNetCore.DataProtection;

namespace AppMorador.Infrastructure.Identity;

/// <summary>
/// Data Protection API (built-in do ASP.NET Core) — escolhida por gerenciar chaves
/// automaticamente (sem AES/config de chave manual). "Purpose" fixo isola este uso de
/// qualquer outro consumidor futuro da mesma API no processo.
/// </summary>
internal sealed class DataProtectionCriptografiaSimetrica : ICriptografiaSimetrica
{
    private const string Purpose = "AppMorador.Equipamentos.Senha";

    private readonly IDataProtector _protector;

    public DataProtectionCriptografiaSimetrica(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Purpose);
    }

    public string Criptografar(string textoPuro) => _protector.Protect(textoPuro);

    public string Descriptografar(string textoCriptografado) => _protector.Unprotect(textoCriptografado);
}
