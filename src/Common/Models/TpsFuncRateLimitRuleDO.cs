using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models;

/// <summary>
/// 功能限流规则实体 (tps_func_rate_limit_rule)
/// </summary>
[Table("tps_func_rate_limit_rule")]
public class TpsFuncRateLimitRuleDO
{
    public uint id { get; set; }
    public uint func_id { get; set; }
    public int window_seconds { get; set; }
    public int max_requests { get; set; }
    public int burst_per_second { get; set; }
    public bool enabled { get; set; }
    public bool is_deleted { get; set; }
    public int version_no { get; set; } = 1;
    public DateTime create_time { get; set; }
    public string created_by { get; set; } = string.Empty;
    public DateTime update_time { get; set; }
    public string updated_by { get; set; } = string.Empty;
    public string remark { get; set; } = string.Empty;
}
