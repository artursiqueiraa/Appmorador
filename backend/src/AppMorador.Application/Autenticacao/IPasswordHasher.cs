namespace AppMorador.Application.Autenticacao;

/// <summary>Porta de hashing de senha — implementacao (BCrypt) fica em Infrastructure.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
