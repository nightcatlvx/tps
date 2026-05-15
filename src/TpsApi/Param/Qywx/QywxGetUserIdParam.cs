namespace TpsApi.Param.Qywx;

public class QywxGetUserIdParam
{
    public string mobile { get; set; } = string.Empty;
}

public class QywxGetUserIdResult
{
    public int errcode { get; set; }
    public string errmsg { get; set; } = string.Empty;
    public string userid { get; set; } = string.Empty;

    public bool IsSuccess => errcode == 0;
}
