using AppMorador.Application.Autenticacao;

namespace AppMorador.Infrastructure.Identity;

/// <summary>BCrypt (work factor 12) — mesma biblioteca ja usada como referencia no Teste-portaria-main1.</summary>
internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: WorkFactor);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
