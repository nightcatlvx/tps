namespace TpsApi.Response;

public class QywxSendResultResponse
{
    public string Mobile { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}
