using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Common.Models;

/// <summary>
/// 服务配置实体 (tps_service_config)
/// </summary>
[Table("tps_service_config")]
public class TpsServiceConfigDO
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public uint id { get; set; }
    public string service_code { get; set; } = string.Empty;
    public int service_type { get; set; } = 1;
    public string base_url { get; set; } = string.Empty;
    public string config_json { get; set; } = "{}";
    public bool enabled { get; set; }
    public bool is_deleted { get; set; }
    public int version_no { get; set; } = 1;
    public DateTime create_time { get; set; }
    public string created_by { get; set; } = string.Empty;
    public DateTime update_time { get; set; }
    public string updated_by { get; set; } = string.Empty;
    public string remark { get; set; } = string.Empty;

    public bool IsSdk => service_type == 2;

    public T GetConfig<T>() where T : class, new()
        => JsonSerializer.Deserialize<T>(config_json, _jsonOptions) ?? new T();

    public void SetConfig<T>(T config) where T : class
        => config_json = JsonSerializer.Serialize(config, _jsonOptions);
}
