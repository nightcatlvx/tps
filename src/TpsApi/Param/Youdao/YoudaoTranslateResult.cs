namespace TpsApi.Param.Youdao;

public class YoudaoTranslateResult
{
    public string errorCode { get; set; } = string.Empty;
    public string from { get; set; } = string.Empty;
    public string to { get; set; } = string.Empty;
    public List<string> translation { get; set; } = [];
    public YoudaoBasic? basic { get; set; }

    public bool IsSuccess => errorCode == "0";

    public string GetFirstTranslation()
        => translation.FirstOrDefault() ?? string.Empty;
}

public class YoudaoBasic
{
    public string? phonetic { get; set; }
    public string? ukPhonetic { get; set; }
    public string? usPhonetic { get; set; }
    public List<string> explains { get; set; } = [];
}
