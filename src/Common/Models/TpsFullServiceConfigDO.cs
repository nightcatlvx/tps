using System.Text.Json;

namespace Common.Models;

/// <summary>
/// 聚合配置：service_config JOIN func_config JOIN func_rate_limit_rule
/// </summary>
public class TpsFullServiceConfigDO
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public uint service_id { get; init; }
    public string service_code { get; init; } = string.Empty;
    public string base_url { get; init; } = string.Empty;
    public string config_json { get; init; } = "{}";

    public uint func_id { get; init; }
    public string func_code { get; init; } = string.Empty;
    public string path { get; init; } = string.Empty;

    public int? window_seconds { get; init; }
    public int? max_requests { get; init; }
    public int? burst_per_second { get; init; }

    public bool HasRateLimit => burst_per_second.HasValue;

    public T GetAuthConfig<T>() where T : class, new()
        => JsonSerializer.Deserialize<T>(config_json, _jsonOptions) ?? new T();
}
