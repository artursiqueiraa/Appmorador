using System.Text.Json.Serialization;

namespace AppMorador.Infrastructure.ControlId;

/// <summary>
/// Formato de payload REST do Control iD (login.fcgi/load_objects.fcgi/create_objects.fcgi) —
/// usado só dentro deste namespace. Baseado no protocolo de referência investigado em
/// Teste-portaria-main1 (nunca validado contra hardware real, ver Fase 1/ADR 0014):
/// tratado como referência de wire-format, não como comportamento homologado.
/// </summary>
internal sealed class ControlIdLoginRequest
{
    [JsonPropertyName("login")]
    public required string Login { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }
}

internal sealed class ControlIdLoginResponse
{
    [JsonPropertyName("session")]
    public string? Session { get; init; }
}

/// <summary>Endpoint novo (system_information.fcgi) exigido pelo escopo desta Sprint — o legado nunca implementou consulta de versão/informações.</summary>
internal sealed class ControlIdInformationResponse
{
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("device_id")]
    public string? DeviceId { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

internal sealed class ControlIdCreateObjectsRequest
{
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    [JsonPropertyName("values")]
    public required Dictionary<string, object?> Values { get; init; }
}

internal sealed class ControlIdCreateObjectsResponse
{
    [JsonPropertyName("id")]
    public long Id { get; init; }
}

internal sealed class ControlIdLoadObjectsRequest
{
    [JsonPropertyName("object")]
    public required string Object { get; init; }

    [JsonPropertyName("fields")]
    public required string[] Fields { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }
}

/// <summary>Log bruto de acesso do equipamento — <c>Event</c> é o código cru do fabricante, nunca mostrado ao usuário final sem tradução (mesmo espírito de Contact ID/JFL).</summary>
internal sealed class ControlIdAccessLogEntry
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("event")]
    public int Event { get; init; }

    [JsonPropertyName("time")]
    public long Time { get; init; }

    [JsonPropertyName("user_id")]
    public long? UserId { get; init; }
}

internal sealed class ControlIdLoadAccessLogsResponse
{
    [JsonPropertyName("access_logs")]
    public List<ControlIdAccessLogEntry>? AccessLogs { get; init; }
}
