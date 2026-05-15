namespace TpsApi.Param.Qywx;

public class QywxSendMessageParam
{
    public string touser { get; set; } = string.Empty;
    public string msgtype { get; set; } = "text";
    public string agentid { get; set; } = string.Empty;
    public QywxTextContent text { get; set; } = new();
}

public class QywxTextContent
{
    public string content { get; set; } = string.Empty;
}

public class QywxSendMessageResult
{
    public int errcode { get; set; }
    public string errmsg { get; set; } = string.Empty;
    public string invaliduser { get; set; } = string.Empty;

    public bool IsSuccess => errcode == 0;
}
