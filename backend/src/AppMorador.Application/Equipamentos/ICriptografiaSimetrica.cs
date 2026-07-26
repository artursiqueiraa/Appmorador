namespace AppMorador.Application.Equipamentos;

/// <summary>
/// Porta de criptografia reversível — usada só para a senha de
/// <see cref="AppMorador.Domain.Entities.Equipamento"/>. Senha de dispositivo precisa
/// ser decifrável (o Provider real precisa do texto puro para autenticar no
/// equipamento), diferente de <see cref="AppMorador.Application.Autenticacao.IPasswordHasher"/>
/// (hash de senha de usuário, propositalmente irreversível). Implementação (Data
/// Protection API) fica em Infrastructure.
/// </summary>
public interface ICriptografiaSimetrica
{
    string Criptografar(string textoPuro);

    string Descriptografar(string textoCriptografado);
}
