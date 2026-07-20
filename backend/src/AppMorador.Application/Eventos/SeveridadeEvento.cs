namespace AppMorador.Application.Eventos;

/// <summary>Nível de importância interno do evento — usado para derivar sinais de produto (ex.: <see cref="EventoResponse.Destaque"/>), nunca exposto diretamente na Api.</summary>
public enum SeveridadeEvento
{
    Informativo,
    Atencao,
    Critico,
}