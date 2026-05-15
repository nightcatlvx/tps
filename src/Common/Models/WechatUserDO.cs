using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Models;

[Table("tps_wechat_user_data")]
public class WechatUserDO
{
    public uint id { get; set; }
    public string mobile { get; set; } = string.Empty;
    public string wxid { get; set; } = string.Empty;
    public DateTime create_time { get; set; }
}
