using System.ComponentModel.DataAnnotations;

namespace TpsApi.Request;

public class QywxBatchSendRequest
{
    [Required, MinLength(1)]
    public List<string> Mobiles { get; set; } = [];

    [Required, MaxLength(2048)]
    public string Message { get; set; } = string.Empty;
}
