namespace Common.Configs;

/// <summary>
/// 第三方服务配置基类
/// </summary>
public class ThirdPartyConfig
{
    /// <summary>
    /// 从 TpsServiceConfigDO.config_json 反序列化而来
    /// </summary>
    public string AppKey { get; set; } = string.Empty;
    public string AppSecret { get; set; } = string.Empty;
}
