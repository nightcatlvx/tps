namespace TpsApi.Param.Youdao;

public class YoudaoTranslateParam
{
    public string q { get; set; } = string.Empty;
    public string from { get; set; } = "auto";
    public string to { get; set; } = "zh-CHS";
    public string? vocabId { get; set; }
}
