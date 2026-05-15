using System.ComponentModel.DataAnnotations;

namespace TpsApi.Request;

public class TranslateRequest
{
    [Required(ErrorMessage = "原文不能为空")]
    [MaxLength(5000)]
    public string Text { get; set; } = string.Empty;

    public string TargetLang { get; set; } = "zh-CHS";
}
