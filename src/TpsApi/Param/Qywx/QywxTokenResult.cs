namespace TpsApi.Param.Qywx;

public class QywxTokenResult
{
    public int errcode { get; set; }
    public string errmsg { get; set; } = string.Empty;
    public string access_token { get; set; } = string.Empty;
    public int expires_in { get; set; }

    public bool IsSuccess => errcode == 0;
}
