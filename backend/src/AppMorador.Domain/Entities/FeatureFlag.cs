namespace AppMorador.Domain.Entities;

/// <summary>
/// Sprint 21 (ADR 0026) — responde "o que esta propriedade CONTRATOU", nunca "quem
/// pode fazer o quê" (isso é <see cref="PermissaoFuncionalidade"/>). Um recurso sem
/// FeatureFlag ativo fica indisponível mesmo para quem teria permissão para usá-lo.
/// </summary>
public enum FeatureFlag
{
    Facial = 1,
    Cameras = 2,
    Pgm = 3,
    Push = 4,
    Snapshot = 5,
    InterfoneSip = 6,
    StreamingAoVivo = 7,
    Ia = 8,
}
