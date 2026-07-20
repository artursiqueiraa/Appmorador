namespace AppMorador.Application.Eventos;

/// <summary>
/// Classificação de negócio do evento — ortogonal a <see cref="OrigemEvento"/> (fontes
/// diferentes podem produzir a mesma categoria; ex.: um leitor de controle de acesso e
/// uma fechadura inteligente podem ambos gerar <see cref="Acesso"/> no futuro).
/// </summary>
public enum CategoriaEvento
{
    Alarme,
    Acesso,
    Sistema,
}