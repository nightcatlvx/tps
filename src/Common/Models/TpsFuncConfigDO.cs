using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models;

/// <summary>
/// 功能配置实体 (tps_func_config)
/// </summary>
[Table("tps_func_config")]
public class TpsFuncConfigDO
{
    public uint id { get; set; }
    public string func_code { get; set; } = string.Empty;
    public uint service_id { get; set; }
    public string path { get; set; } = string.Empty;
    public bool enabled { get; set; }
    public bool is_deleted { get; set; }
    public int version_no { get; set; } = 1;
    public DateTime create_time { get; set; }
    public string created_by { get; set; } = string.Empty;
    public DateTime update_time { get; set; }
    public string updated_by { get; set; } = string.Empty;
    public string remark { get; set; } = string.Empty;
}
